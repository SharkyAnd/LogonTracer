using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using Utils;
using System.IO;
using Cassia;
using System.Data.SqlClient;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace LogonTracerLib
{
    public class LogonTracerWorker
    {
        public enum SessionState
        {
            Active,
            Disconnected,
            ServiceDown,
            MonitorAborted
        }

        public enum MonitorMode
        {
            Local,
            Remote
        }

        public enum SessionUpdateDetails
        {
            NewSession,
            StateUpdated,
            ActivityHoursIncreased,
            LastInputTimeUpdate
        }

        public bool debugDBDown { get; set; }
        private const string SERVICE_NAME = "LogonTracerService";

        private string _defaultJournalName = "LogonTracerServiceJournal";
        private string _defaultSourceName = "LogonTracerService";

        private string _filePath = "logFile.txt";
        private static object LockObj = new object();

        private System.Timers.Timer inputActivityTimer;
        private System.Timers.Timer sessionStateUpdateTimer;
        private System.Timers.Timer checkRepositoryTimer;
        private System.Timers.Timer checkProgramActivityTimer;
        private System.Timers.Timer troubleShootingTimer;
        private List<ActiveSession> activeSessions;
        private ITerminalServicesManager manager;
        private static bool sessionLocked;
        private AppData.SessionRepositoryProviderBase repoProvider;
        private LogonTracerAdministrationLib.LogonTracerAdministrationWorker logonTracerAdministration = new LogonTracerAdministrationLib.LogonTracerAdministrationWorker();
        DatabaseUtils dbu = new DatabaseUtils(LogonTracerConfig.Instance.ConnectionString);

        public void Initialize()
        {
            checkProgramActivityTimer = new System.Timers.Timer();
            checkProgramActivityTimer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
            checkProgramActivityTimer.AutoReset = true;
            checkProgramActivityTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnProgramActivityTimer);

            troubleShootingTimer = new System.Timers.Timer();
            troubleShootingTimer.Interval = TimeSpan.FromHours(12).TotalMilliseconds;
            troubleShootingTimer.AutoReset = true;
            troubleShootingTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTroubleShootingTimer);

            manager = new TerminalServicesManager();

            if (LogonTracerConfig.Instance.MonitorMode == MonitorMode.Local)
                LogonTracerConfig.Instance.ServersToTrace = new string[] { Environment.MachineName };

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            inputActivityTimer = new System.Timers.Timer();
            inputActivityTimer.Interval = LogonTracerConfig.Instance.InputActivityTimePeriod * TimeSpan.FromMinutes(1).TotalMilliseconds;
            inputActivityTimer.AutoReset = true;
            inputActivityTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnInputActivityTimer);

            sessionStateUpdateTimer = new System.Timers.Timer();
            sessionStateUpdateTimer.Interval = LogonTracerConfig.Instance.SessionSavePeriod * TimeSpan.FromSeconds(1).TotalMilliseconds;
            sessionStateUpdateTimer.AutoReset = true;
            sessionStateUpdateTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnSessionStateUpdateTimer);

            checkRepositoryTimer = new System.Timers.Timer();
            checkRepositoryTimer.Interval = LogonTracerConfig.Instance.CheckRepositoryTimePeriod * TimeSpan.FromMinutes(1).TotalMilliseconds;
            checkRepositoryTimer.AutoReset = true;
            checkRepositoryTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnCheckRepositoryTimer);

            repoProvider = AppData.SessionRepositoryProviderBase.GetProvider();
            debugDBDown = false;
        }

        void OnTroubleShootingTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            logonTracerAdministration.FetchUnregisteredUsers();
            logonTracerAdministration.ClearOrphanedSessions();
        }

        void OnProgramActivityTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!inputActivityTimer.Enabled && !sessionStateUpdateTimer.Enabled && !checkRepositoryTimer.Enabled)
                RegisterSessions();
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            StopWatching();
            Exception ex = (Exception)e.ExceptionObject;
            LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Error, "Unhandled Exception. Type:{0}. Message: {1}", ex.GetType(), ex.Message);
        }

        private void OnSessionStateUpdateTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (sessionLocked)
                return;
            sessionLocked = true;

            for (int i = activeSessions.Count - 1; i > -1; i--)
            {
                bool sessionExist = false;
                ActiveSession activeSession = activeSessions[i];

                ITerminalServicesSession session = FindSession(activeSession);

                if (session != null)
                {
                    if (/*AppData.DateTimeConvertUtils.Compare(activeSession.SessionBegin.Value, session.ConnectTime.Value) != 1*/
                        activeSession.SessionBegin.Value != session.ConnectTime.Value)
                        activeSession.SessionState = SessionState.Disconnected;

                    if (session.ConnectionState == Cassia.ConnectionState.Disconnected)
                    {
                        activeSession.SessionEnd = session.DisconnectTime;
                        activeSession.SessionState = SessionState.Disconnected;
                    }
                    sessionExist = true;
                }
                if (!sessionExist)
                    activeSession.SessionState = SessionState.Disconnected;
                if (activeSession.SessionState != SessionState.Active)
                {
                    repoProvider.SaveSession(activeSession);
                    if (LogonTracerConfig.Instance.LogSessionHistory)
                        repoProvider.UpdateSessionHistory(activeSession.DbId.Value, SessionUpdateDetails.StateUpdated, new Dictionary<string, string>{
                            {"Old value", SessionState.Active.ToString()},
                            {"New value", SessionState.Disconnected.ToString()}
                        });
                    activeSessions.Remove(activeSession);
                }
            }

            sessionLocked = false;
        }

        private void OnInputActivityTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
#if DEBUG
            if (sessionLocked)
                return;
#endif
            sessionLocked = true;
            if (activeSessions.Count == 0)
            {
                RegisterSessions();
                sessionLocked = false;
                return;
            }
            for (int i = 0; i < activeSessions.Count; i++)
            {
                bool activityRegistered = false;
                ActiveSession activeSession = activeSessions[i];

                ITerminalServicesSession session = FindSession(activeSession);

                if (session != null)
                {
                    if (AppData.DateTimeConvertUtils.Compare(activeSession.LastInputTime, session.LastInputTime) != 1 &&
                        (AppData.DateTimeConvertUtils.ConvertTimeByUtcOffset(DateTime.Now, LogonTracerConfig.Instance.UtcOffset).Value -
                        AppData.DateTimeConvertUtils.ConvertTimeByUtcOffset(session.ConnectTime, LogonTracerConfig.Instance.UtcOffset).Value).TotalMinutes > LogonTracerConfig.Instance.InputActivityTimePeriod)
                    {
                        Dictionary<string, string> activeHoursDict = new Dictionary<string, string> { { "Old value", activeSession.ActivityHours.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) } };
                        Dictionary<string, string> lastInputTimeDict = new Dictionary<string, string> { { "Old value", activeSession.LastInputTime.ToString() } };
                        activeSession.ActivityHours += Convert.ToDouble(LogonTracerConfig.Instance.InputActivityTimePeriod) / 60;
                        activeSession.LastInputTime = session.LastInputTime;
                        activeHoursDict.Add("New value", activeSession.ActivityHours.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                        lastInputTimeDict.Add("New value", activeSession.LastInputTime.ToString());
                        activityRegistered = true;
                        if (LogonTracerConfig.Instance.LogSessionHistory)
                        {
                            repoProvider.UpdateSessionHistory(activeSession.DbId.Value, SessionUpdateDetails.ActivityHoursIncreased, activeHoursDict);
                            repoProvider.UpdateSessionHistory(activeSession.DbId.Value, SessionUpdateDetails.LastInputTimeUpdate, lastInputTimeDict);
                        }
                        repoProvider.SaveSession(activeSession);
                    }
                    repoProvider.SaveSessionActivityProfile(activeSession.DbId, activityRegistered);
                }
            }
            UpdateListenersStatus();
            sessionLocked = false;
        }

        void OnCheckRepositoryTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            repoProvider = AppData.SessionRepositoryProviderBase.GetProvider();

#if DEBUG
            if (debugDBDown)
                repoProvider = AppData.SessionLocalRepositoryProvider.Instance;
            else
            {
                repoProvider = new AppData.SessionDbProvider();
                repoProvider.MergeRepositories();
            }
#endif
        }

        /// <summary>
        /// Register all active sessions on server(s)
        /// </summary>
        public void RegisterSessions()
        {
            if (!checkProgramActivityTimer.Enabled)
                checkProgramActivityTimer.Start();
            if (!troubleShootingTimer.Enabled)
                troubleShootingTimer.Start();
            activeSessions = new List<ActiveSession>();
            for (int i = LogonTracerConfig.Instance.ServersToTrace.Count() - 1; i > -1; i--)
            {
                string serverName = LogonTracerConfig.Instance.ServersToTrace[i];
                try
                {
                    using (ITerminalServer server = manager.GetRemoteServer(serverName))
                    {
                        server.Open();
                        foreach (ITerminalServicesSession session in server.GetSessions())
                        {
                            ActiveSession activeSession = null;
                            if (session.UserAccount == null || session.ConnectionState != Cassia.ConnectionState.Active)
                                continue;
                            activeSession = repoProvider.BuildSession(session);
                            activeSessions.Add(activeSession);
                            if (activeSession.DbId.HasValue && LogonTracerConfig.Instance.LogSessionHistory)
                                repoProvider.UpdateSessionHistory(activeSession.DbId.Value, SessionUpdateDetails.StateUpdated, new Dictionary<string, string> 
                                {
                                    {"Old value", SessionState.ServiceDown.ToString()},
                                    {"New value", SessionState.Active.ToString()}
                                });
                            repoProvider.SaveSession(activeSession);
                        }
                    }
                }
                catch (Win32Exception ex)
                {
                    LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Error, "Error while trying access the server. Check Remote Desktop Services Permissions for this server and try again. Server:{0}. Message: {1}", serverName, ex.Message);
                    LogonTracerConfig.Instance.ServersToTrace = LogonTracerConfig.Instance.ServersToTrace.Where(stt => stt != serverName).ToArray();
                }
                catch (Exception ex)
                {
                    LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Error, "Type: {0}. Server:{1}. Message: {2}", ex.GetType(), serverName, ex.Message);
                }
            }
            inputActivityTimer.Start();
            sessionStateUpdateTimer.Start();
            checkRepositoryTimer.Start();
        }

        /// <summary>
        /// Stop monitoring the server(s)
        /// </summary>
        public void StopWatching()
        {
            if (activeSessions == null || activeSessions.Count == 0)
                return;

            inputActivityTimer.Stop();
            sessionStateUpdateTimer.Stop();
            checkRepositoryTimer.Stop();
            for (int i = activeSessions.Count - 1; i > -1; i--)
            {
                ActiveSession activeSession = activeSessions[i];
                if (LogonTracerConfig.Instance.LogSessionHistory)
                    repoProvider.UpdateSessionHistory(activeSession.DbId.Value, SessionUpdateDetails.StateUpdated, new Dictionary<string, string>
                    {
                        {"Old value", SessionState.Active.ToString()},
                        {"New value", SessionState.ServiceDown.ToString()}
                    });
                activeSession.SessionState = SessionState.ServiceDown;
                repoProvider.SaveSession(activeSession);
            }
        }

        /// <summary>
        /// Find session among all servers and sessions
        /// </summary>
        /// <param name="activeSession">Registered session</param>
        /// <returns>Session</returns>
        private ITerminalServicesSession FindSession(ActiveSession activeSession)
        {
            ITerminalServicesSession foundedSession = null;
            foreach (string serverName in LogonTracerConfig.Instance.ServersToTrace)
            {
                try
                {
                    using (ITerminalServer server = manager.GetRemoteServer(serverName))
                    {
                        server.Open();
                        foreach (ITerminalServicesSession session in server.GetSessions())
                        {                           
                            if (session.UserAccount == null)
                                continue;
                            if (activeSessions.Where(acs => acs.MachineName == session.Server.ServerName &&
                                acs.UserName == session.UserName &&
                                /*AppData.DateTimeConvertUtils.Compare(acs.SessionBegin.Value, session.ConnectTime.Value) == 1*/ acs.SessionBegin.Value == session.ConnectTime.Value).Count() == 0 &&
                                session.ConnectionState != Cassia.ConnectionState.Disconnected)
                            {
                                ActiveSession newSession = repoProvider.BuildSession(session);
                                activeSessions.Add(newSession);
                                repoProvider.SaveSession(newSession);
                                continue;
                            }
                            if (session.UserName == activeSession.UserName && session.Server.ServerName == activeSession.MachineName)
                                foundedSession = session;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Error, "Type: {0}. Server{1}. Message: {2}", ex.GetType(), serverName, ex.Message);
                }
            }
            return foundedSession;
        }

        /// <summary>
        /// Invoke Update on all listeneres
        /// </summary>
        private void UpdateListenersStatus()
        {
            foreach (string listenerAddress in LogonTracerConfig.Instance.ListenersAddresses)
            {
                if (String.IsNullOrEmpty(listenerAddress))
                    continue;
                HubConnection hubConnection;
                IHubProxy hubProxy;
                try
                {
                    hubConnection = new HubConnection(listenerAddress);
                    hubProxy = hubConnection.CreateHubProxy("ActiveSessionsHub");
                    hubConnection.Start().Wait();

                    hubProxy.Invoke("Update");
                }
                catch (Exception ex)
                {
                    LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Error, "Error while trying to Invoke Signalr update method. Address wil be removed from list of listeners addresses. Type: {0}. ListenerAddress:{1}. Message: {2}", ex.GetType(), listenerAddress, ex.Message);
                    LogonTracerConfig.Instance.ListenersAddresses = LogonTracerConfig.Instance.ListenersAddresses.Where(la => la != listenerAddress).ToArray();
                }
            }
        }

        private void Log(MessageType type, string mask, params object[] args)
        {
            lock (LockObj)
            {
                string msg = string.Format(mask, args);
                Utils.LoggingUtils.DefaultLogger.AddLogMessage(this, type, msg, new object[0]);
                using (StreamWriter writer = new StreamWriter(@_filePath, true))
                {
                    writer.WriteLine(string.Format("[{0}] ({1}) {2}: {3}", type, SERVICE_NAME, DateTime.Now, msg));
                }
            }
        }
    }
}
