using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ComponentModel;
using System.Xml.Serialization;

using Utils;
using Utils.ConfigurationUtils;

using DisplayName = Utils.ConfigurationUtils.DisplayNameAttribute;

namespace LogonTracerLib
{
    [Description("Configuration for Logon Tracer")]
    public class LogonTracerConfig : ConfigBase
    {
        private static LogonTracerConfig instance = null;

        public override string GetPath()
        {
            return FileName;
        }

        public override ConfigBase GetBaseInstance()
        {
            return Instance;
        }

        public override ConfigBase CreateDeepCopy()
        {
            LogonTracerConfig config = (LogonTracerConfig)MemberwiseClone();

            return config;
        }

        [XmlIgnore]
        public static LogonTracerConfig Instance
        {
            get
            {
                lock (lockFlag)
                {
                    if (instance == null)
                    {
                        //if (System.IO.File.Exists(FileName))
                        //{
                        instance = ReadFromFile<LogonTracerConfig>(FileName);
                        //}
                        //else
                        //{
                        //    //Utils.LoggingUtils.ErrorForm.AddLogMessageEx("ConfigUtils: file {0} not found, defaults applied.", FileName);
                        //    instance = new ConfigUtils();
                        //}

                    }

                    return instance;
                }
            }
        }

        public override void UpdateConfig(ConfigBase cb)
        {
            LogonTracerConfig c = cb as LogonTracerConfig;
            if (c != null)
            {
                instance = c;
                return;
            }
            else
                throw new ApplicationException("Argument for Config Update (cb) cannot be casted to LogonTracerConfig");
        }

        private static readonly string FileName = Utils.FileUtils.GetFullPathForFile("config_logontracer.xml");

        #region Logon Tracer
        #region Database connection string
        //private string _DbConnectionString = "Data Source=(local);Initial Catalog=DevTest; Integrated Security = SSPI;";
        private string _DbConnectionString = "Data Source=SRV-SOHO2;Initial Catalog=DevelopmentDashboard; User ID = DDUser; Password = gffgh3SDF5754swer";

        [Category("Data Storage")]
        [Description("Database Connection String")]
        [DisplayName("Database Connection String")]
        [ChangeProperty(ChangeAction.NoAction)]
        public string ConnectionString
        {
            get { return _DbConnectionString; }
            set { _DbConnectionString = value; }
        }
        #endregion

        #region Input Activity Time Period
        private int _InputActivityTimePeriod = 1;

        [Category("App Parameters")]
        [Description("Input Activity Time Period (m)")]
        [DisplayName("Input Activity Time Period (m)")]
        [ChangeProperty(ChangeAction.NoAction)]
        public int InputActivityTimePeriod
        {
            get { return _InputActivityTimePeriod; }
            set { _InputActivityTimePeriod = value; }
        }
        #endregion      

        #region Servers To Trace
        //private string[] _serversToTrace = new string[] {"WIN2012DEV0103", "WIN2012PUB0103"};
        private string[] _serversToTrace = new string[] { Environment.MachineName };

        [Category("App Parameters")]
        [Description("Servers To Trace")]
        [DisplayName("Servers To Trace")]
        [ChangeProperty(ChangeAction.NoAction)]
        public string[] ServersToTrace
        {
            get { return _serversToTrace; }
            set { _serversToTrace = value; }
        }
        #endregion

        #region Session Save Period
        private int _sessionSavePeriod = 5;

        [Category("App Parameters")]
        [Description("Session Save Period (s)")]
        [DisplayName("Session Save Period (s)")]
        [ChangeProperty(ChangeAction.NoAction)]
        public int SessionSavePeriod
        {
            get { return _sessionSavePeriod; }
            set { _sessionSavePeriod = value; }
        }
        #endregion

        #region Monitor Mode
        private LogonTracerWorker.MonitorMode _monitorMode = LogonTracerWorker.MonitorMode.Local;

        [Category("App Parameters")]
        [Description("Monitor Mode")]
        [DisplayName("Monitor Mode")]
        [ChangeProperty(ChangeAction.NoAction)]
        public LogonTracerWorker.MonitorMode MonitorMode
        {
            get { return _monitorMode; }
            set { _monitorMode = value; }
        }
        #endregion

        #region Log SaveSession Activity
        private bool _LogSaveSessionActivity = true;

        [Category("App Parameters")]
        [Description("Log SaveSession Activity")]
        [DisplayName("Log SaveSession Activity")]
        [ChangeProperty(ChangeAction.NoAction)]
        public bool LogSaveSessionActivity
        {
            get { return _LogSaveSessionActivity; }
            set { _LogSaveSessionActivity = value; }
        }
        #endregion

        #region Use Local Time Zone
        private bool _UseLocalTimeZone = false;

        [Category("App Parameters")]
        [Description("Use Local Time Zone")]
        [DisplayName("Use Local Time Zone")]
        [ChangeProperty(ChangeAction.NoAction)]
        public bool UseLocalTimeZone
        {
            get { return _UseLocalTimeZone; }
            set { _UseLocalTimeZone = value; }
        }
        #endregion

        #region UTC Offset
        private double _UtcOffset = +3;

        [Category("App Parameters")]
        [Description("UTC Offset")]
        [DisplayName("UTC Offset")]
        [ChangeProperty(ChangeAction.NoAction)]
        public double UtcOffset
        {
            get { return _UtcOffset; }
            set { _UtcOffset = value; }
        }
        #endregion      

        #region Log Session History
        private bool _LogSessionHistory = true;

        [Category("App Parameters")]
        [Description("Log Session History")]
        [DisplayName("Log Session History")]
        [ChangeProperty(ChangeAction.NoAction)]
        public bool LogSessionHistory
        {
            get { return _LogSessionHistory; }
            set { _LogSessionHistory = value; }
        }
        #endregion

        #region Listeners addresses
        //private string[] _serversToTrace = new string[] {"WIN2012DEV0103", "WIN2012PUB0103"};
        private string[] _listenersAddresses = new string[] { "http://localhost:9483/signalr" };

        [Category("App Parameters")]
        [Description("Listneres addresses")]
        [DisplayName("Listneres addresses")]
        [ChangeProperty(ChangeAction.NoAction)]
        public string[] ListenersAddresses
        {
            get { return _listenersAddresses; }
            set { _listenersAddresses = value; }
        }
        #endregion

        #region Memory Repository Limit
        private int _MemoryRepositoryLimit = 100;

        [Category("App Parameters")]
        [Description("Memory Repository Limit")]
        [DisplayName("Memory Repository Limit")]
        [ChangeProperty(ChangeAction.NoAction)]
        public int MemoryRepositoryLimit
        {
            get { return _MemoryRepositoryLimit; }
            set { _MemoryRepositoryLimit = value; }
        }
        #endregion      

        #region Check Repository Time Period
        private int _CheckRepositoryTimePeriod = 1;

        [Category("App Parameters")]
        [Description("Check Repository Time Period (m)")]
        [DisplayName("Check Repository Time Period (m)")]
        [ChangeProperty(ChangeAction.NoAction)]
        public int CheckRepositoryTimePeriod
        {
            get { return _CheckRepositoryTimePeriod; }
            set { _CheckRepositoryTimePeriod = value; }
        }
        #endregion      

        #endregion
    }
}
