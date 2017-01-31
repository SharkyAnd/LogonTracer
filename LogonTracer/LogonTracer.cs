using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using LogonTracerLib;
using System.IO;
using System.Threading;

namespace LogonTracer
{
    public partial class LogonTracer : ServiceBase
    {
        private readonly Thread workingThread;
        LogonTracerWorker _logonTracerWorker;
        public LogonTracer()
        {
            InitializeComponent();
            CanHandleSessionChangeEvent = true;
            workingThread = new Thread(DoWork);
        }

        protected override void OnStart(string[] args)
        {           
            _logonTracerWorker = new LogonTracerWorker();
            _logonTracerWorker.Initialize();
            _logonTracerWorker.RegisterSessions();
        }

        public void Start()
        {
            workingThread.Start();
        }

        public void Stop()
        {
            workingThread.Abort();
        }

        protected override void OnStop()
        {
            _logonTracerWorker.StopWatching();
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            EventLog.WriteEntry("SimpleService.OnSessionChange", DateTime.Now.ToLongTimeString() +
            " - Session change notice received: " +
            changeDescription.Reason.ToString() + "  Session ID: " +
            changeDescription.SessionId.ToString());
        }

        private void DoWork()
        {
            _logonTracerWorker = new LogonTracerWorker();
            _logonTracerWorker.Initialize();
            _logonTracerWorker.RegisterSessions();
        }
    }
}
