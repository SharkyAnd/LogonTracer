using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Utils;

using System.Data.SqlClient;

using WorkmonitorLib;

namespace LogonTracerWorkmonitor
{
    public class LogonTracerUtils : IWorkmonitorUtils
    {
        public DatabaseUtils dbU = new DatabaseUtils("Data Source=SRV-SOHO2;Initial Catalog=DevelopmentDashboard; User ID = DDUser; Password = gffgh3SDF5754swer");

        public double GetWorkingHours(string userName, DateTime periodBegin, DateTime periodEnd)
        {
            double res = 0;


            dbU.ExecuteRead(@"select SUM(DATEDIFF(minute, ChunkBegin, ChunkEnd) * IsUserActive) * 1.0 / 60 as WorkingMinutes from SessionActivityProfiles
left join Logins on Logins.Id = SessionActivityProfiles.SessionId
where UserName = @userName and ChunkBegin BETWEEN @periodBegin AND @periodEnd", new Dictionary<string, object>()
                                                                              {
                                                                                  { "@periodBegin", periodBegin },
                                                                                  { "@periodEnd", periodEnd },
                                                                                  { "@userName", userName }
                                                                              }
                                                                              , delegate(SqlDataReader sdr)
            {
                if(!sdr.IsDBNull(0))
                    res = (double)sdr.GetDecimal(0);
            }, -1);

            return res;
        }

        public List<string> GetOnlineUsers()
        {
            List<string> res = new List<string>();

            dbU.ExecuteRead(@"select distinct UserName from Logins where SessionState = 'Active'", null, delegate(SqlDataReader sdr)
            {
                string str = sdr.GetValue(0) as string;
                if (!string.IsNullOrEmpty(str))
                    res.Add(str);
            }, -1);

            return res;
        }
    }
}
