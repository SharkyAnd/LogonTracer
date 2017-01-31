using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Utils;

using LogonTracerLib;

namespace LogonTracerDesktopApp
{
    public partial class MainForm : Form
    {
        private static LogonTracerWorker _logonTracerWorker = new LogonTracerWorker();
        private static LogonTracerAdministrationLib.LogonTracerAdministrationWorker _logonTracerAdministrationWorker = new LogonTracerAdministrationLib.LogonTracerAdministrationWorker();
        public MainForm()
        {
            InitializeComponent();
            _logonTracerWorker.Initialize();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabelTime.Text = string.Format("{0:HH:mm:ss}", DateTime.Now);
        }

        private void configurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.ConfigurationUtils.MultiConfigurationForm mcf = new Utils.ConfigurationUtils.MultiConfigurationForm();
            mcf.Show();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Utils.LoggingUtils.CreateBasicLoggingForms(this);            
        }

        

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //_logonTracerWorker = new LogonTracerWorker();
            
            _logonTracerWorker.RegisterSessions();
           
            Microsoft.Win32.SystemEvents.SessionSwitch += new Microsoft.Win32.SessionSwitchEventHandler(SystemEvents_SessionSwitch);
            
        }

        static void SystemEvents_SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
        {
            if (Utils.LoggingUtils.DefaultLogger != null)
            {
                Utils.LoggingUtils.DefaultLogger.AddLogMessage("LogonTracer", MessageType.Info, "Session switch, reason = {0}", e.Reason);
            }
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _logonTracerWorker.StopWatching();

        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _logonTracerWorker.StopWatching();
        }

        private void throwExToolStripMenuItem_Click(object sender, EventArgs e)
        {
            throw new Exception("Unhandled Exception");
        }

        private void menuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            
        }

        private void imitateDBDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((sender as ToolStripMenuItem).Text == "DisableDb")
            {
                _logonTracerWorker.debugDBDown = true;
                (sender as ToolStripMenuItem).Text = "EnableDb";
            }
            else
            {
                _logonTracerWorker.debugDBDown = false;
                (sender as ToolStripMenuItem).Text = "DisableDb";
            }
        }

        private void processUnregisteredUsersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _logonTracerAdministrationWorker.FetchUnregisteredUsers();
        }

        private void clearOrphanedSessionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _logonTracerAdministrationWorker.ClearOrphanedSessions();
        }
    }
}
