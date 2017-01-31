using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace LogonTracer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            Utils.LoggingUtils.DefaultLogger = new Utils.DatabaseLogger(LogonTracerLib.LogonTracerConfig.Instance.ConnectionString);

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
			{ 
				new LogonTracer() 
			};
            ServiceBase.Run(ServicesToRun);
        }
    }
}
