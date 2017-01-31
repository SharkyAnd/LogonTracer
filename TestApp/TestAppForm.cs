using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Utils;

namespace TestApp
{
    public partial class TestAppForm : Form
    {
        public TestAppForm()
        {
            InitializeComponent();
        }

        private void buttonWorkingHours_Click(object sender, EventArgs e)
        {
            DateTime periodBegin = new DateTime(2016, 08, 21);

            DateTime periodEnd = new DateTime(2016, 08, 27, 23, 59, 59);

            string userName = "AKolmakov";

            LogonTracerWorkmonitor.LogonTracerUtils ltu = new LogonTracerWorkmonitor.LogonTracerUtils();

            double workingHours1 = ltu.GetWorkingHours(userName, periodBegin, periodEnd);

            double workingHours2 = ltu.GetWorkingHours(userName, periodBegin, periodEnd.AddDays(0.99));

            Utils.LoggingUtils.InfoForm.AddLogMessageEx("AKolmakov: {0} {1}", workingHours1, workingHours2);
        }
    }
}
