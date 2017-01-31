using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;
using System.Reflection;
using System.Data.SqlClient;

namespace LogonTracerLib.AppData
{
    public class SessionDbProvider:SessionRepositoryProviderBase
    {
        DatabaseUtils dbu = new DatabaseUtils(LogonTracerConfig.Instance.ConnectionString);

        protected override ActiveSession CheckSessionRepositoryExisting(ActiveSession activeSession)
        {
            try
            {
                dbu.ExecuteRead(string.Format(@"
                                SELECT id, ActiveHours, LastInputTime, ClientUtcOffset
                                FROM Logins 
                                WHERE UserName ='{0}' AND MachineName = '{1}' AND FORMAT(SessionBegin, 'yyyy-MM-dd HH:mm:ss') = '{2}'",
                                activeSession.UserName, activeSession.MachineName, activeSession.SessionBegin.Value.ToString("yyyy-MM-dd HH:mm:ss")), null, delegate(SqlDataReader sdr)
                                {
                                    activeSession.DbId = sdr.GetInt32(0);
                                    activeSession.ActivityHours += sdr.GetDouble(1);
                                    if (sdr.IsDBNull(2))
                                        activeSession.LastInputTime = null;
                                    else
                                        activeSession.LastInputTime = sdr.GetDateTime(2);
                                }, -1);
            }
            catch (Exception ex)
            {
                Utils.LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Error, "Ошибка при попытке получить сессиию из БД. Message: {0}. Trace: {1}", ex.Message, ex.StackTrace);
            }
            return activeSession;
        }

        protected override void OnSessionSave(ActiveSession sessionToSave)
        {                        
            try
            {
                if (!sessionToSave.DbId.HasValue)
                {
                    sessionToSave.DbId = dbu.InsertNewRowAndGetItsId("Logins", new Dictionary<string, object>() 
                    { 
                        { "UserName", sessionToSave.UserName }, 
                        { "MachineName", sessionToSave.MachineName }, 
                        { "SessionBegin", (object)AppData.DateTimeConvertUtils.ConvertTimeByUtcOffset(sessionToSave.SessionBegin, LogonTracerConfig.Instance.UtcOffset) ?? DBNull.Value },
                        { "SessionEnd", (object)AppData.DateTimeConvertUtils.ConvertTimeByUtcOffset(sessionToSave.SessionEnd, LogonTracerConfig.Instance.UtcOffset) ?? DBNull.Value },
                        { "ActiveHours", ActiveHours },
                        { "SessionState", sessionToSave.SessionState.ToString() },
                        { "ClientUtcOffset", sessionToSave.ClientUtcOffset },
                        { "LastInputTime", (object)AppData.DateTimeConvertUtils.ConvertTimeByUtcOffset(sessionToSave.LastInputTime, LogonTracerConfig.Instance.UtcOffset) ?? DBNull.Value},
                        { "ClientName", sessionToSave.ClientName},
                        { "ClientDisplayDetails", sessionToSave.ClientDisplayDetails},
                        { "ClientReportedIPAddress", (object)sessionToSave.ClientReportedIPAddress??DBNull.Value},
                        { "ClientBuildNumber", sessionToSave.ClientBuildNumber}
                    }, true);


                }
                else
                {
                    dbu.ExecuteScalarQuery(@"UPDATE Logins set SessionEnd = @SessionEnd, ActiveHours = @ActiveHours, SessionState = @SessionState, LastInputTime = @LastInputTime WHERE id = @dbID",
                        new Dictionary<string, object>()
                        {
                            {"@SessionEnd", (object)AppData.DateTimeConvertUtils.ConvertTimeByUtcOffset(sessionToSave.SessionEnd, LogonTracerConfig.Instance.UtcOffset) ?? DBNull.Value},
                            {"@ActiveHours", ActiveHours},
                            {"@SessionState", sessionToSave.SessionState.ToString()},
                            {"@LastInputTime", (object)AppData.DateTimeConvertUtils.ConvertTimeByUtcOffset(sessionToSave.LastInputTime, LogonTracerConfig.Instance.UtcOffset) ?? DBNull.Value},
                            {"@dbID", sessionToSave.DbId}
                        });                   
                }
            }
            catch (Exception ex)
            {
                Utils.LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Error, "Ошибка при попытке сохранить результат сессии. Type:{0}. Message: {1}", ex.GetType(), ex.Message);
            }
        }

        protected override void OnUpdateSessionHistory(decimal sessionId)
        {
            try
            {
                dbu.InsertNewRowAndGetItsId("SessionsUpdateHistrory", new Dictionary<string, object>() 
                    { 
                        { "SessionId", sessionId }, 
                        { "AgentMachineName", AgentMachineName }, 
                        { "AgentVersion", AgentVersion },
                        { "UpdateMoment", UpdateMoment },
                        { "UpdateDetails", UpdateDetailsMessage }                   
                    }, false);
            }
            catch (Exception ex)
            {
                Utils.LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Error, "Ошибка при попытке сохранить историю сессии. Type:{0}. Message: {1}", ex.GetType(), ex.Message);
            }
        }

        protected override void OnSaveSessionActivityProfile(decimal? activeSessionId, bool activityRegistered)
        {
            try
            {
                dbu.InsertNewRowAndGetItsId("SessionActivityProfiles", new Dictionary<string, object>() 
                    { 
                        { "SessionId", activeSessionId }, 
                        { "ChunkBegin", ChunkBegin }, 
                        { "ChunkEnd", ChunkEnd },
                        { "IsUserActive", activityRegistered }
                    
                    }, false);
                
            }
            catch (Exception ex)
            {
                Utils.LoggingUtils.DefaultLogger.AddLogMessage(this, MessageType.Error, "Ошибка при попытке сохранить результат активности сессии. Type:{0}. Message: {1}", ex.GetType(), ex.Message);
            }
        }
    }
}
