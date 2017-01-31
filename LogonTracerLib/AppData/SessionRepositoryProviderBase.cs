using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cassia;
using System.Reflection;
using Utils;

namespace LogonTracerLib.AppData
{
    public class SessionRepositoryProviderBase
    {
        public string ActiveHours;
        public string AgentMachineName;
        public string AgentVersion;
        public string UpdateDetailsMessage;
        public DateTime? UpdateMoment;
        public DateTime? ChunkBegin;
        public DateTime? ChunkEnd;
        private LogonTracerAdministrationLib.LogonTracerAdministrationWorker logonAdministrationWorker = new LogonTracerAdministrationLib.LogonTracerAdministrationWorker();

        public static SessionRepositoryProviderBase GetProvider ()
        {
            SessionRepositoryProviderBase baseProvider = null;

            Utils.DatabaseUtils dbu = new Utils.DatabaseUtils(LogonTracerConfig.Instance.ConnectionString);

            if (dbu.CheckDbAvailability())
            {                
                baseProvider = new SessionDbProvider();
                baseProvider.MergeRepositories();
            }
            else
                baseProvider = SessionLocalRepositoryProvider.Instance;

            return baseProvider;
        }

        public ActiveSession BuildSession(ITerminalServicesSession session)
        {
            ActiveSession activeSession = new ActiveSession
            {
                UserName = session.UserName,
                MachineName = session.Server.ServerName,
                SessionBegin = session.ConnectTime.Value,
                ClientUtcOffset = Convert.ToDouble(TimeZone.CurrentTimeZone.GetUtcOffset(session.ConnectTime.Value).Hours),
                ClientName = session.ClientName,
                ClientDisplayDetails = string.Format("Horizontal rezolution: {0}, Vertical rezolution: {1}, Bits per px: {2}", session.ClientDisplay.HorizontalResolution, session.ClientDisplay.VerticalResolution, session.ClientDisplay.BitsPerPixel),
                ClientReportedIPAddress = session.ClientIPAddress == null ? null : session.ClientIPAddress.ToString(),
                ClientBuildNumber = session.ClientBuildNumber,
                SessionState = LogonTracerWorker.SessionState.Active
            };
            logonAdministrationWorker.RegisterNewUser(session.UserName);
            return CheckSessionRepositoryExisting(activeSession);
        }

        protected virtual ActiveSession CheckSessionRepositoryExisting(ActiveSession activeSession) { return activeSession; }


        /// <summary>
        /// Save session to Database
        /// </summary>
        /// <param name="sessionToSave">Session needed to save</param>
        /// <param name="sessionSaveType">Session save type</param>
        /// <param name="sessionState">Session state</param>
        public void SaveSession(ActiveSession sessionToSave)
        {
            ActiveHours = sessionToSave.ActivityHours.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture);
            if (sessionToSave.SessionState == LogonTracerLib.LogonTracerWorker.SessionState.Disconnected || sessionToSave.SessionState == LogonTracerLib.LogonTracerWorker.SessionState.ServiceDown)
                sessionToSave.SessionEnd = sessionToSave.SessionEnd ?? DateTime.Now;

            if (sessionToSave.DbId.HasValue)
            {
                if (LogonTracerConfig.Instance.LogSaveSessionActivity)
                    Utils.LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Info,
                        "Session UPDATE. USERNAME: {0}; MACHINE_NAME:{1} SESSION_BEGIN:{2}; ACTIVE_HOURS: {3}; " + (sessionToSave.SessionEnd.HasValue ? "SESSION_END: {4};" : "{4}") + "SESSION_STATE: {5}",
                        sessionToSave.UserName, sessionToSave.MachineName, sessionToSave.SessionBegin, ActiveHours, sessionToSave.SessionEnd, sessionToSave.SessionState);
            }
            else
            {
                if (LogonTracerConfig.Instance.LogSaveSessionActivity)
                    Utils.LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Info, "New Session. USERNAME: {0}; MACHINE_NAME:{1}; SESSION_BEGIN:{2}",
                        sessionToSave.UserName, sessionToSave.MachineName, sessionToSave.SessionBegin);
                if (sessionToSave.DbId.HasValue && sessionToSave.DbId != 0 && LogonTracerConfig.Instance.LogSessionHistory)
                    UpdateSessionHistory(sessionToSave.DbId.Value, LogonTracerLib.LogonTracerWorker.SessionUpdateDetails.NewSession);
            }
                

            OnSessionSave(sessionToSave);           
        }

        protected virtual void OnSessionSave(ActiveSession sessionToSave) { }

        /// <summary>
        /// Update session history information
        /// </summary>
        /// <param name="sessionId">Session id</param>
        /// <param name="sessionUpdateDetails">Session update details</param>
        /// <param name="additionalParameters">Additional parameteres of session update details</param>
        public void UpdateSessionHistory(decimal sessionId, LogonTracerLib.LogonTracerWorker.SessionUpdateDetails sessionUpdateDetails, Dictionary<string, string> additionalParameters = null)
        {
            UpdateDetailsMessage = null;
            switch (sessionUpdateDetails)
            {
                case LogonTracerLib.LogonTracerWorker.SessionUpdateDetails.NewSession:
                    UpdateDetailsMessage = "New session registered. ";
                    break;
                case LogonTracerLib.LogonTracerWorker.SessionUpdateDetails.StateUpdated:
                    UpdateDetailsMessage = "Session state updated. ";
                    break;
                case LogonTracerLib.LogonTracerWorker.SessionUpdateDetails.ActivityHoursIncreased:
                    UpdateDetailsMessage = "Active hours increased. ";
                    break;
                case LogonTracerLib.LogonTracerWorker.SessionUpdateDetails.LastInputTimeUpdate:
                    UpdateDetailsMessage = "Last input time updated ";
                    break;
                default:
                    break;
            }

            if (additionalParameters != null)
                foreach (KeyValuePair<string, string> additionalParameter in additionalParameters)
                    UpdateDetailsMessage += string.Format("{0}:{1}. ", additionalParameter.Key, additionalParameter.Value);

            AgentMachineName = Environment.MachineName;
            AgentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            UpdateMoment = AppData.DateTimeConvertUtils.ConvertTimeByUtcOffset(DateTime.Now, LogonTracerConfig.Instance.UtcOffset);

            OnUpdateSessionHistory(sessionId);
        }

        protected virtual void OnUpdateSessionHistory(decimal sessionId) { }

        /// <summary>
        /// Save Session Activity
        /// </summary>
        /// <param name="activeSessionId">Session DB Id</param>
        /// <param name="activityRegistered">Activity registered</param>
        public void SaveSessionActivityProfile(decimal? activeSessionId, bool activityRegistered)
        {
            if (!activeSessionId.HasValue)
                return;

            ChunkBegin = AppData.DateTimeConvertUtils.ConvertTimeByUtcOffset(
                                    DateTime.Now.AddMinutes(-LogonTracerConfig.Instance.InputActivityTimePeriod),
                                    LogonTracerConfig.Instance.UtcOffset);
            ChunkEnd = AppData.DateTimeConvertUtils.ConvertTimeByUtcOffset(DateTime.Now, LogonTracerConfig.Instance.UtcOffset);

            OnSaveSessionActivityProfile(activeSessionId, activityRegistered);

            if (LogonTracerConfig.Instance.LogSaveSessionActivity)
                Utils.LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Info, "Session activity save. SESSION_ID: {0}; CHUNK_BEGIN:{1}; CHUNK_END: {2}; IS_USER_ACTIVE:{3}",
                    activeSessionId, ChunkBegin, ChunkEnd, activityRegistered ? "Yes" : "No");
        }

        protected virtual void OnSaveSessionActivityProfile(decimal? activeSessionId, bool activityRegistered) { }

        public void MergeRepositories()
        {
            Utils.DatabaseUtils dbu = new Utils.DatabaseUtils(LogonTracerConfig.Instance.ConnectionString);
            SessionDbProvider dbProvider = new SessionDbProvider();
            List<ActiveSession> activeSessions = SessionLocalRepositoryProvider.Instance.GetSessionsFromRepository();
            List<Dictionary<string, object>> sessionsHistory = SessionLocalRepositoryProvider.Instance.GetSessionsHistoryFromRepository();
            List<Dictionary<string, object>> sessionsActivity = SessionLocalRepositoryProvider.Instance.GetSessionsActivityFromRepository();

            foreach (ActiveSession session in activeSessions)
            {
                double dbSessionActivityHours = 0;
                if (session.DbId != 0)
                    dbSessionActivityHours += Convert.ToDouble(dbu.GetFieldValue("Logins", "ActiveHours", "Id", session.DbId));
                else
                {
                    dbProvider.SaveSession(session);
                    continue;
                }
                if (session.ActivityHours > dbSessionActivityHours)
                    dbProvider.SaveSession(session);
            }

            foreach (Dictionary<string, object> history in sessionsHistory)
            {
                dbProvider.UpdateDetailsMessage = (string)history.Where(h => h.Key == "UpdateDetails").Select(h => h.Value).FirstOrDefault();
                dbProvider.AgentMachineName = (string)history.Where(h => h.Key == "AgentMachineName").Select(h => h.Value).FirstOrDefault();
                dbProvider.AgentVersion = (string)history.Where(h => h.Key == "AgentVersion").Select(h => h.Value).FirstOrDefault();
                dbProvider.UpdateMoment = Convert.ToDateTime(history.Where(h => h.Key == "UpdateMoment").Select(h => h.Value).FirstOrDefault());
                dbProvider.OnUpdateSessionHistory(Convert.ToDecimal(history.Where(h => h.Key == "SessionId").Select(h => h.Value).FirstOrDefault()));
            }

            foreach (Dictionary<string,object> activity in sessionsActivity)
            {
                dbProvider.ChunkBegin = Convert.ToDateTime(activity.Where(a => a.Key == "ChunkBegin").Select(a => a.Value).FirstOrDefault());
                dbProvider.ChunkEnd = Convert.ToDateTime(activity.Where(a => a.Key == "ChunkEnd").Select(a => a.Value).FirstOrDefault());
                dbProvider.SaveSessionActivityProfile(
                    Convert.ToDecimal(activity.Where(a => a.Key == "SessionId").Select(a => a.Value).FirstOrDefault()),
                    Convert.ToBoolean(activity.Where(a => a.Key == "ActivityRegistered").Select(a => a.Value).FirstOrDefault())
                );
            }
        }
    }
}
