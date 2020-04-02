using System;
using System.Collections.Generic;
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

        public UserSettings()
        {
            InitializeComponent();
            outputFolder = Properties.Settings.Default.outputFolder;
            ftpIpAddress = Properties.Settings.Default.ftpIpAddress;
            ftpUsername = Properties.Settings.Default.ftpUsername;
            ftpPassword = Properties.Settings.Default.ftpPassword;
            txtOutputDirectory.Text = outputFolder;
            txtIpAddress.Text = ftpIpAddress;
            txtUsername.Text = ftpUsername;
            txtPassword.Text = ftpPassword;
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
