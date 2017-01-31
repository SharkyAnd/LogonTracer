using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ComponentModel;
using System.Xml.Serialization;

using Utils;
using Utils.ConfigurationUtils;

using DisplayName = Utils.ConfigurationUtils.DisplayNameAttribute;

namespace LogonTracerAdministrationLib
{
    [Description("Configuration for Logon Tracer Administartion")]
    public class LogonTracerAdministrationConfig : ConfigBase
    {
        private static LogonTracerAdministrationConfig instance = null;

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
            LogonTracerAdministrationConfig config = (LogonTracerAdministrationConfig)MemberwiseClone();

            return config;
        }

        [XmlIgnore]
        public static LogonTracerAdministrationConfig Instance
        {
            get
            {
                lock (lockFlag)
                {
                    if (instance == null)
                    {
                        //if (System.IO.File.Exists(FileName))
                        //{
                        instance = ReadFromFile<LogonTracerAdministrationConfig>(FileName);
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
            LogonTracerAdministrationConfig c = cb as LogonTracerAdministrationConfig;
            if (c != null)
            {
                instance = c;
                return;
            }
            else
                throw new ApplicationException("Argument for Config Update (cb) cannot be casted to LogonTracerConfig");
        }

        private static readonly string FileName = Utils.FileUtils.GetFullPathForFile("config_logontracer_admin.xml");

        #region Logon Tracer Administartion
        #region Orhpaned Session Timeout In Hours
        private int _OrhpanedSessionTimeoutInHours = 48;

        [Category("App Parameters")]
        [Description("Orhpaned Session Timeout In Hours")]
        [DisplayName("Orhpaned Session Timeout In Hours")]
        [ChangeProperty(ChangeAction.NoAction)]
        public int OrhpanedSessionTimeoutInHours
        {
            get { return _OrhpanedSessionTimeoutInHours; }
            set { _OrhpanedSessionTimeoutInHours = value; }
        }
        #endregion

        #region Database connection string
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

        #endregion
    }
}
