using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OGXboxSoundtrackEditor
{
    public partial class FtpLog : Window
    {
        public FtpLog(List<FtpLogEntry> logLines)
        {
            InitializeComponent();

            string log = "";
            foreach (FtpLogEntry fle in logLines)
            {
                log += fle.EntryTime.ToString() + " " + fle.EntryData + Environment.NewLine;
            }
            txtLog.Text = log;
        }
    }
}
