using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Utils;
using System.Data.SqlClient;
using LogonTracerAdministrationLib.Models;

namespace LogonTracerAdministrationLib
{
    public class LogonTracerAdministrationWorker
    {
        DatabaseUtils dbu = new DatabaseUtils(LogonTracerAdministrationConfig.Instance.ConnectionString);

        /// <summary>
        /// Handle unexpectedly ended sessions
        /// </summary>
        public void ClearOrphanedSessions()
        {
            List<OrphanedSession> orphanedSessions = new List<OrphanedSession>();
            string query = @"SELECT Id, ClientUtcOffset 
                            FROM Logins WHERE 
                            (SessionEnd IS NULL OR SessionState = 'Active') AND Comment IS NULL ";
            try
            {
                DataTable dt = dbu.ExecuteDataTable(query, null);

                orphanedSessions = dt.AsEnumerable().Select(r => new OrphanedSession
                {
                    Id = Convert.ToInt32(r["Id"]),
                    UtcOffset = r["ClientUtcOffset"] == DBNull.Value ? 0 : Convert.ToDouble(r["ClientUtcOffset"])
                }).ToList();
            }
            catch (Exception ex)
            {
                LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Error, "Ошибка при попытке получить разорванные сессии. Type:{0}. Message: {1}", ex.GetType(), ex.Message);
            }

            foreach (OrphanedSession orphanedSession in orphanedSessions)
            {
                DateTime? lastSessionActivityProfile = null;

                try
                {
                    dbu.ExecuteRead(string.Format(@"
                                SELECT TOP 1 ChunkBegin
                                FROM SessionActivityProfiles 
                                WHERE SessionId = {0} ORDER BY ChunkBegin DESC",
                            orphanedSession.Id), null, delegate(SqlDataReader sdr)
                            {
                                lastSessionActivityProfile = sdr.GetDateTime(0);
                            }, -1);

                    string comment = null;
                    if (!lastSessionActivityProfile.HasValue)
                    {
                        comment = "Session was aborted before any activity was registered";
                        dbu.ExecuteScalarQuery(@"UPDATE Logins set SessionEnd = (SELECT SessionBegin FROM Logins WHERE Id = @dbId), SessionState = @SessionState, Comment = @Comment WHERE id = @dbID",
                        new Dictionary<string, object>()
                            {
                                {"@SessionState", "MonitorAborted"},
                                {"@Comment", comment},
                                {"@dbID", orphanedSession.Id}
                            });
                    }
                    else if ((AppData.DateTimeConvertUtils.ConvertTimeByUtcOffset(DateTime.Now, orphanedSession.UtcOffset).Value -
                        lastSessionActivityProfile.Value).TotalHours > LogonTracerAdministrationConfig.Instance.OrhpanedSessionTimeoutInHours)
                    {
                        comment = string.Format("Session was aborted or unavaliable longer than {0} hours", LogonTracerAdministrationConfig.Instance.OrhpanedSessionTimeoutInHours);
                        dbu.ExecuteScalarQuery(@"UPDATE Logins set SessionEnd = @SessionEnd, SessionState = @SessionState, Comment = @Comment WHERE id = @dbID",
                        new Dictionary<string, object>()
                            {
                                {"@SessionEnd", (object)lastSessionActivityProfile ?? DBNull.Value},
                                {"@SessionState", "MonitorAborted"},
                                {"@Comment", comment},
                                {"@dbID", orphanedSession.Id}
                            });
                    }
                }
                catch (Exception ex)
                {
                    Utils.LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Error, "Ошибка при попытке обработать разорванные сессии. Type:{0}. Message: {1}", ex.GetType(), ex.Message);
                }
            }
        }

        public void FetchUnregisteredUsers()
        {
            try
            {
                string query = @"SELECT DISTINCT UserName FROM Logins WHERE UserName NOT IN (SELECT UserName FROM Users)";

                DataTable dt = dbu.ExecuteDataTable(query, null);

                var userNames = dt.AsEnumerable().Select(r => r["UserName"].ToString()).ToArray();

                foreach (string userName in userNames)
                {
                    RegisterNewUser(userName);
                }
            }
            catch (Exception ex)
            {
                Utils.LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Error, "Ошибка при попытке получить незарегистрированных пользователей из БД. Message: {0}. Trace: {1}", ex.Message, ex.StackTrace);
            }
        }

        public void RegisterNewUser(string userName)
        {
            int userId = 0;

            try
            {
                dbu.ExecuteRead(string.Format(@"
                                SELECT id
                                FROM Users 
                                WHERE UserName ='{0}' ",
                                userName), null, delegate(SqlDataReader sdr)
                                {
                                    userId = sdr.GetInt32(0);
                                }, -1);

                if (userId == 0)
                {
                    dbu.InsertNewRowAndGetItsId("Users", new Dictionary<string, object>() 
                    { 
                        { "UserName", userName }
                    }, false);
                }
            }
            catch (Exception ex)
            {
                Utils.LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Error, "Ошибка при попытке зарегистрировать нового пользователя. Message: {0}. Trace: {1}", ex.Message, ex.StackTrace);
            }
        }
    }
}
