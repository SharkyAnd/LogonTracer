using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogonTracerLib;
using System.IO;
using Cassia;
using System.ServiceProcess;
using LogonTracer;

namespace LogonTracerConsole
{
    class Program
    {
        private static LogonTracerWorker _logonTracerWorker;

        static void Main(string[] args)
        {
            Utils.LoggingUtils.DefaultLogger = new Utils.DatabaseLogger(LogonTracerLib.LogonTracerConfig.Instance.ConnectionString);


            _logonTracerWorker = new LogonTracerWorker();
            _logonTracerWorker.Initialize();
            _logonTracerWorker.RegisterSessions();
            Microsoft.Win32.SystemEvents.SessionSwitch += new Microsoft.Win32.SessionSwitchEventHandler(SystemEvents_SessionSwitch);
            //var service = new LogonTracer.LogonTracer();
            //service.CanHandleSessionChangeEvent = true;

            //ServiceBase[] servicesToRun = new ServiceBase[] { service };
            //if (Environment.UserInteractive)
            //{
            //    Console.CancelKeyPress += (x, y) => service.Stop();

            //    service.Start();
            //    Console.WriteLine("Sevice Start");

            //    Console.ReadKey();
            //    service.Stop();
            //    Console.WriteLine("Service Stop");
            //}
            string command = Console.ReadLine();
            if (command == "r")
                _logonTracerWorker.RegisterSessions();
            else
                _logonTracerWorker.StopWatching();
        }

        static void SystemEvents_SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                /*case Microsoft.Win32.SessionSwitchReason.RemoteConnect:
                    _logonTracerWorker.RegisterSession();
                    break;
                case Microsoft.Win32.SessionSwitchReason.SessionLogon:
                    _logonTracerWorker.RegisterSession();
                    break;*/
            }
        }
    }
}
