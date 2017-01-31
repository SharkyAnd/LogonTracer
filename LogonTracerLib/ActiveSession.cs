using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogonTracerLib
{
    public class ActiveSession
    {
        #region Fields
        private decimal? dbId;
        private string userName;
        private string machineName;
        private DateTime? lastInputTime;
        private DateTime? sessionBegin;
        private DateTime? sessionEnd;
        private double clientUtcOffset;
        private double activityHours;
        public string ClientName { get; set; }
        public string ClientDisplayDetails { get; set; }
        public string ClientReportedIPAddress { get; set; }
        public int ClientBuildNumber { get; set; }
        public LogonTracerWorker.SessionState SessionState { get; set; }

        public decimal? DbId 
        {
            get { return dbId; }
            set
            {
                if (value != null)
                    dbId = value;
                else
                    dbId = null;
            }
        }

        public string UserName 
        {
            get { return userName; }
            set
            {
                if (value != null)
                    userName = value;
                else
                    userName = "unknown";
            }
        }
        public string MachineName 
        {
            get { return machineName; }
            set
            {
                if (value != null)
                    machineName = value;
                else
                    machineName = "unknown";
            }
        }
        public DateTime? LastInputTime 
        {
            get { return lastInputTime; }
            set
            {
                if (value.HasValue)
                    lastInputTime =  value;
                else
                    lastInputTime = null;
            }
        }
        public DateTime? SessionBegin 
        {
            get { return sessionBegin; }
            set
            {
                if (value.HasValue)
                    sessionBegin = value;
                else
                    sessionBegin = null;
            }
        }
        public DateTime? SessionEnd 
        {
            get { return sessionEnd; }
            set
            {
                if (value.HasValue)
                    sessionEnd = value;
                else
                    sessionEnd = null;
            }
        }

        public double ClientUtcOffset
        {
            get { return clientUtcOffset; }
            set { clientUtcOffset = value; }
        }

        public double ActivityHours 
        {
            get { return activityHours; }
            set
            {
                activityHours = value;
            }
        }
        #endregion
    }
}
