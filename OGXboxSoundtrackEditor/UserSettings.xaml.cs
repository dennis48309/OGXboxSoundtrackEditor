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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OGXboxSoundtrackEditor
{
    public partial class UserSettings : Window
    {
        public string outputFolder;
        public string ftpIpAddress;
        public string ftpUsername;
        public string ftpPassword;
        public int bitrate;

        public UserSettings()
        {
            InitializeComponent();
            outputFolder = Properties.Settings.Default.outputFolder;
            ftpIpAddress = Properties.Settings.Default.ftpIpAddress;
            ftpUsername = Properties.Settings.Default.ftpUsername;
            ftpPassword = Properties.Settings.Default.ftpPassword;
            bitrate = Properties.Settings.Default.bitrate;
            txtOutputDirectory.Text = outputFolder;
            txtIpAddress.Text = ftpIpAddress;
            txtUsername.Text = ftpUsername;
            txtPassword.Text = ftpPassword;

            if (bitrate == 96000)
            {
                cboBitrate.SelectedIndex = 0;
            }
            else if (bitrate == 128000)
            {
                cboBitrate.SelectedIndex = 1;
            }
            else if (bitrate == 192000)
            {
                cboBitrate.SelectedIndex = 2;
            }
            else if (bitrate == 256000)
            {
                cboBitrate.SelectedIndex = 3;
            }
            else if (bitrate == 320000)
            {
                cboBitrate.SelectedIndex = 4;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.outputFolder = outputFolder;
            Properties.Settings.Default.ftpIpAddress = txtIpAddress.Text.Trim();
            Properties.Settings.Default.ftpUsername = txtUsername.Text.Trim();
            Properties.Settings.Default.ftpPassword = txtPassword.Text.Trim();

            if (cboBitrate.SelectedIndex == 0)
            {
                Properties.Settings.Default.bitrate = 96000;
            }
            if (cboBitrate.SelectedIndex == 1)
            {
                Properties.Settings.Default.bitrate = 128000;
            }
            if (cboBitrate.SelectedIndex == 2)
            {
                Properties.Settings.Default.bitrate = 192000;
            }
            if (cboBitrate.SelectedIndex == 3)
            {
                Properties.Settings.Default.bitrate = 256000;
            }
            if (cboBitrate.SelectedIndex == 4)
            {
                Properties.Settings.Default.bitrate = 320000;
            }

            ftpIpAddress = txtIpAddress.Text.Trim();
            ftpUsername = txtUsername.Text.Trim();
            ftpPassword = txtPassword.Text.Trim();
            Properties.Settings.Default.Save();
            DialogResult = true;
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fDialog = new FolderBrowserDialog();
            if (fDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                outputFolder = fDialog.SelectedPath;
                txtOutputDirectory.Text = outputFolder;
            }
        }
    }
}
