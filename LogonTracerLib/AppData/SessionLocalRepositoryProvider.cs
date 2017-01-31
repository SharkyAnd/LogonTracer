using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using Utils;
using System.Reflection;

namespace LogonTracerLib.AppData
{
    public class SessionLocalRepositoryProvider : SessionRepositoryProviderBase
    {
        private static List<ActiveSession> MemoryRepository;
        private static List<Dictionary<string, object>> HistoryMemoryRepository;
        private static List<Dictionary<string, object>> ActivityMemoryRepository;
        private static string RepositoryFileName;
        private static string HistoryFileName;
        private static string ActivityFileName;
        private static XDocument Repository;
        private static XDocument HistoryRepository;
        private static XDocument ActivityRepository;
        private static SessionLocalRepositoryProvider _instance;

        public static SessionLocalRepositoryProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SessionLocalRepositoryProvider();
                    RepositoryFileName = @"SessionRepository\session_repository.xml";
                    HistoryFileName = @"SessionRepository\session_repository_history.xml";
                    ActivityFileName = @"SessionRepository\session_repository_activity.xml";

                    MemoryRepository = new List<ActiveSession>();
                    HistoryMemoryRepository = new List<Dictionary<string, object>>();
                    ActivityMemoryRepository = new List<Dictionary<string, object>>();

                    if (!Directory.Exists("SessionRepository"))
                        Directory.CreateDirectory("SessionRepository");

                    if (!File.Exists(RepositoryFileName))
                    {
                        using (StreamWriter sw = new StreamWriter(RepositoryFileName))
                        {
                            sw.Write("<Sessions></Sessions>");
                        }
                    }
                    if (!File.Exists(HistoryFileName))
                    {
                        using (StreamWriter sw = new StreamWriter(HistoryFileName))
                        {
                            sw.Write("<History></History>");
                        }
                    }
                    if (!File.Exists(ActivityFileName))
                    {
                        using (StreamWriter sw = new StreamWriter(ActivityFileName))
                        {
                            sw.Write("<Activity></Activity>");
                        }
                    }

                    Repository = XDocument.Load(RepositoryFileName);
                    HistoryRepository = XDocument.Load(HistoryFileName);
                    ActivityRepository = XDocument.Load(ActivityFileName);
                }
                return _instance;
            }
        }

        protected override void OnSessionSave(ActiveSession sessionToSave)
        {
            XElement repositorySession = GetSessionFormRepository(sessionToSave);
            ActiveSession memorySession = MemoryRepository.Where(acs => acs.UserName == sessionToSave.UserName && acs.MachineName == sessionToSave.MachineName &&
                acs.SessionBegin == sessionToSave.SessionBegin).FirstOrDefault();
            try
            {
                if (repositorySession != null)
                {
                    repositorySession.Attribute("SessionEnd").Value = sessionToSave.SessionEnd.HasValue ? sessionToSave.SessionEnd.Value.ToString() : "-";
                    repositorySession.Attribute("ActiveHours").Value = ActiveHours;
                    repositorySession.Attribute("SessionState").Value = sessionToSave.SessionState.ToString();
                    repositorySession.Attribute("LastInputTime").Value = sessionToSave.LastInputTime.HasValue ? sessionToSave.LastInputTime.Value.ToString() : "-";
                }
                else
                {
                    XElement xmlSession = new XElement("session",
                                            new XAttribute("DbId", sessionToSave.DbId ?? 0),
                                            new XAttribute("UserName", sessionToSave.UserName),
                                            new XAttribute("MachineName", sessionToSave.MachineName),
                                            new XAttribute("SessionBegin", sessionToSave.SessionBegin),
                                            new XAttribute("SessionEnd", (object)sessionToSave.SessionEnd ?? "-"),
                                            new XAttribute("LastInputTime", (object)sessionToSave.LastInputTime ?? "-"),
                                            new XAttribute("ActiveHours", ActiveHours),
                                            new XAttribute("ClientUtcOffset", sessionToSave.ClientUtcOffset),
                                            new XAttribute("SessionState", sessionToSave.SessionState.ToString()),
                                            new XAttribute("ClientName", sessionToSave.ClientName),
                                            new XAttribute("ClientDisplayDetails", sessionToSave.ClientDisplayDetails),
                                            new XAttribute("ClientReportedIPAddress", sessionToSave.ClientReportedIPAddress ?? "-"),
                                            new XAttribute("ClientBuildNumber", sessionToSave.ClientBuildNumber));
                    Repository.Root.Add(xmlSession);
                }

                if (memorySession != null)
                {
                    memorySession.SessionEnd = sessionToSave.SessionEnd;
                    memorySession.ActivityHours = sessionToSave.ActivityHours;
                    memorySession.SessionState = sessionToSave.SessionState;
                    memorySession.LastInputTime = sessionToSave.LastInputTime;
                }
                else
                    MemoryRepository.Add(sessionToSave);

                Repository.Save(RepositoryFileName);
            }
            catch (Exception ex)
            {
                Utils.LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Error, "Ошибка при попытке сохранить результат сессии в репозиторий. Type:{0}. Message: {1}", ex.GetType(), ex.Message);
            }
        }

        private XElement GetSessionFormRepository(ActiveSession session)
        {
            return Repository.Root.Elements()
                .Where(e =>
                    (string)e.Attribute("UserName").Value == session.UserName &&
                    (string)e.Attribute("MachineName").Value == session.MachineName &&
                    Convert.ToDateTime(e.Attribute("SessionBegin").Value) == session.SessionBegin.Value).FirstOrDefault();
        }

        protected override ActiveSession CheckSessionRepositoryExisting(ActiveSession session)
        {
            XElement repoSession = GetSessionFormRepository(session);
            if (repoSession != null)
            {
                session.DbId = Convert.ToDecimal(repoSession.Attribute("DbId").Value);
                session.ActivityHours += Convert.ToDouble(repoSession.Attribute("ActiveHours").Value.Replace('.', ','));
                if (repoSession.Attribute("LastInputTime").Value == "-")
                    session.LastInputTime = null;
                else
                    session.LastInputTime = Convert.ToDateTime(repoSession.Attribute("LastInputTime").Value);
            }
            return session;
        }

        protected override void OnUpdateSessionHistory(decimal sessionId)
        {
            try
            {
                XElement sessionHistory = new XElement("sessionHistory",
                                            new XAttribute("SessionId", sessionId),
                                            new XAttribute("AgentMachineName", Environment.MachineName),
                                            new XAttribute("AgentVersion", Assembly.GetExecutingAssembly().GetName().Version.ToString()),
                                            new XAttribute("UpdateMoment", AppData.DateTimeConvertUtils.ConvertTimeByUtcOffset(DateTime.Now, LogonTracerConfig.Instance.UtcOffset)),
                                            new XAttribute("UpdateDetails", UpdateDetailsMessage)
                                          );
                HistoryRepository.Root.Add(sessionHistory);
                HistoryRepository.Save(HistoryFileName);
                HistoryMemoryRepository.Add(new Dictionary<string, object>
                {
                    {"SessionId", sessionId},
                    {"AgentMachineName", Environment.MachineName},
                    {"AgentVersion", Assembly.GetExecutingAssembly().GetName().Version.ToString()},
                    {"UpdateMoment", AppData.DateTimeConvertUtils.ConvertTimeByUtcOffset(DateTime.Now, LogonTracerConfig.Instance.UtcOffset)},
                    {"UpdateDetails", UpdateDetailsMessage}
                });
                if (HistoryMemoryRepository.Count > LogonTracerConfig.Instance.MemoryRepositoryLimit)
                    HistoryMemoryRepository.RemoveAt(0);
            }
            catch (Exception ex)
            {
                Utils.LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Error, "Ошибка при попытке сохранить историю сессии в репозиторий. Type:{0}. Message: {1}", ex.GetType(), ex.Message);
            }
        }

        protected override void OnSaveSessionActivityProfile(decimal? activeSessionId, bool activityRegistered)
        {
            try
            {
                XElement sessionActivity = new XElement("sessionHistory",
                                            new XAttribute("SessionId", activeSessionId),
                                            new XAttribute("ChunkBegin", ChunkBegin),
                                            new XAttribute("ChunkEnd", ChunkEnd),
                                            new XAttribute("ActivityRegistered", activityRegistered)
                                          );
                ActivityRepository.Root.Add(sessionActivity);
                ActivityRepository.Save(ActivityFileName);
                ActivityMemoryRepository.Add(new Dictionary<string, object>
                {
                    {"SessionId", activeSessionId},
                    {"ChunkBegin", ChunkBegin},
                    {"ChunkEnd", ChunkEnd},
                    {"ActivityRegistered", activityRegistered}
                });
                if (ActivityMemoryRepository.Count > LogonTracerConfig.Instance.MemoryRepositoryLimit)
                    ActivityMemoryRepository.RemoveAt(0);
            }
            catch (Exception ex)
            {
                Utils.LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Error, "Ошибка при попытке сохранить активность сессии в репозиторий. Type:{0}. Message: {1}", ex.GetType(), ex.Message);
            }
        }

        public List<ActiveSession> GetSessionsFromRepository()
        {
            List<ActiveSession> activeSessions;

            if (MemoryRepository != null && MemoryRepository.Count != 0)
            {
                activeSessions = new List<ActiveSession>(MemoryRepository);
                MemoryRepository.Clear();
                Repository.Root.Elements().Remove();
                Repository.Save(RepositoryFileName);
                return activeSessions;
            }
            activeSessions = new List<ActiveSession>();
            foreach (XElement element in Repository.Root.Elements())
            {
                ActiveSession activeSession = new ActiveSession();
                activeSession.DbId = Convert.ToDecimal(element.Attribute("DbId").Value);
                activeSession.UserName = element.Attribute("UserName").Value;
                activeSession.MachineName = element.Attribute("MachineName").Value;
                activeSession.SessionBegin = Convert.ToDateTime(element.Attribute("SessionBegin").Value);
                if (element.Attribute("SessionEnd").Value == "-")
                    activeSession.SessionEnd = null;
                else
                    activeSession.SessionEnd = Convert.ToDateTime(element.Attribute("SessionEnd").Value);
                if (element.Attribute("LastInputTime").Value == "-")
                    activeSession.LastInputTime = null;
                else
                    activeSession.LastInputTime = Convert.ToDateTime(element.Attribute("LastInputTime").Value);
                activeSession.ClientUtcOffset = Convert.ToDouble(element.Attribute("ClientUtcOffset").Value);
                activeSession.ActivityHours += Convert.ToDouble(element.Attribute("ActiveHours").Value.Replace('.', ','));
                activeSession.ClientName = element.Attribute("ClientName").Value;
                activeSession.ClientDisplayDetails = element.Attribute("ClientDisplayDetails").Value;
                activeSession.ClientReportedIPAddress = element.Attribute("ClientReportedIPAddress").Value == "-" ? null : element.Attribute("ClientReportedIPAddress").Value;
                activeSession.ClientBuildNumber = Convert.ToInt32(element.Attribute("ClientBuildNumber").Value);
                activeSession.SessionState = (LogonTracerLib.LogonTracerWorker.SessionState)Enum.Parse(typeof(LogonTracerLib.LogonTracerWorker.SessionState), element.Attribute("SessionState").Value, true);
                activeSessions.Add(activeSession);
            }
            Repository.Root.Elements().Remove();
            Repository.Save(RepositoryFileName);
            return activeSessions;
        }

        public List<Dictionary<string, object>> GetSessionsHistoryFromRepository()
        {
            List<Dictionary<string, object>> sessionsHistory;

            if (HistoryMemoryRepository != null && HistoryMemoryRepository.Count != 0)
            {
                sessionsHistory = new List<Dictionary<string,object>>(HistoryMemoryRepository);
                HistoryMemoryRepository.Clear();
                HistoryRepository.Root.Elements().Remove();
                HistoryRepository.Save(HistoryFileName);
                return sessionsHistory;
            }
            sessionsHistory = new List<Dictionary<string, object>>();
            foreach (XElement elem in HistoryRepository.Root.Elements())
            {
                Dictionary<string, object> historyRec = new Dictionary<string, object>();
                historyRec.Add("SessionId", Convert.ToDecimal(elem.Attribute("SessionId").Value));
                historyRec.Add("AgentMachineName", elem.Attribute("AgentMachineName").Value);
                historyRec.Add("AgentVersion", elem.Attribute("AgentVersion").Value);
                historyRec.Add("UpdateMoment", Convert.ToDateTime(elem.Attribute("UpdateMoment").Value));
                historyRec.Add("UpdateDetails", elem.Attribute("UpdateDetails").Value);

                sessionsHistory.Add(historyRec);
            }
            HistoryRepository.Root.Elements().Remove();
            HistoryRepository.Save(HistoryFileName);
            return sessionsHistory;
        }

        public List<Dictionary<string, object>> GetSessionsActivityFromRepository()
        {
            List<Dictionary<string, object>> sessionsActivity;

            if (ActivityMemoryRepository != null && ActivityMemoryRepository.Count != 0)
            {
                sessionsActivity = new List<Dictionary<string,object>>(ActivityMemoryRepository);
                ActivityMemoryRepository.Clear();
                ActivityRepository.Root.Elements().Remove();
                ActivityRepository.Save(ActivityFileName);
                return sessionsActivity;
            }
            sessionsActivity = new List<Dictionary<string, object>>();
            foreach (XElement elem in ActivityRepository.Root.Elements())
            {
                Dictionary<string, object> activityRec = new Dictionary<string, object>();
                activityRec.Add("SessionId", Convert.ToDecimal(elem.Attribute("SessionId").Value));
                activityRec.Add("ChunkBegin", Convert.ToDateTime(elem.Attribute("ChunkBegin").Value));
                activityRec.Add("ChunkEnd", Convert.ToDateTime(elem.Attribute("ChunkEnd").Value));
                activityRec.Add("ActivityRegistered", Convert.ToBoolean(elem.Attribute("ActivityRegistered").Value));

                sessionsActivity.Add(activityRec);
            }
            ActivityRepository.Root.Elements().Remove();
            ActivityRepository.Save(ActivityFileName);
            return sessionsActivity;
        }
    }
}

