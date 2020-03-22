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

        public UserSettings()
        {
            InitializeComponent();
            outputFolder = Properties.Settings.Default.outputFolder;
            txtOutputDirectory.Text = outputFolder;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Properties.Settings.Default.outputFolder = outputFolder;
            Properties.Settings.Default.Save();
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
