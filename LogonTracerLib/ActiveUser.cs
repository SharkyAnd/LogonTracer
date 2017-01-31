using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogonTracerLib
{
    class ActiveUser
    {
        #region Fields
        private string userName;
        private string machineName;
        private DateTime previousInputTime;
        private DateTime sessionBegin;
        private DateTime sessionEnd;
        private double activityHours;
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
        public DateTime PreviousInputTime 
        {
            get { return previousInputTime; }
            set
            {
                if (value != null)
                    previousInputTime = value;
                else
                    previousInputTime = DateTime.MinValue;
            }
        }
        public DateTime SessionBegin 
        {
            get { return sessionBegin; }
            set
            {
                if (value != null)
                    sessionBegin = value;
                else
                    sessionBegin = DateTime.MinValue;
            }
        }
        public DateTime SessionEnd 
        {
            get { return sessionEnd; }
            set
            {
                if (value != null)
                    sessionEnd = value;
                else
                    sessionBegin = DateTime.MinValue;
            }
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

        #region Constructor
        public ActiveUser(string userName, string machineName, DateTime sessionBegin)
        {
            UserName = userName;
            MachineName = machineName;
            SessionBegin = sessionBegin;
        }
        #endregion
    }
}
