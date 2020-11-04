using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using RJCP.IO.Ports;
using AdcControl.Properties;
using AdcControl.Resources;

namespace AdcControl
{
    /// <summary>
    /// Логика взаимодействия для Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        public bool AllFieldsOK { get { return (txtAveraging.Text.Length > 0) && (txtDuration.Text.Length > 0); } }

        private bool ValidateNumericInput(string val)
        {
            if (int.TryParse(val, out int i))
            {
                return (i > 0);
            }
            else
            {
                return false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cmbPorts.Items.Clear();
            cmbPorts.Items.Add(SerialPortStream.GetPortNames());
            int i = cmbPorts.Items.IndexOf(Settings.Default.PortName);
            if (i > -1)
            {
                cmbPorts.SelectedIndex = i;
            }
            txtAveraging.Text = Settings.Default.Average.ToString();
            txtDuration.Text = Settings.Default.AcquisitionDuration.ToString();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPorts.IsEnabled)
            {
                if (cmbPorts.SelectedIndex < 0)
                {
                    MessageBox.Show(Default.msgInvalidPortSetting);
                    return;
                }
                Settings.Default.PortName = (string)cmbPorts.SelectedItem;
            }
            Settings.Default.Average = int.Parse(txtAveraging.Text);
            Settings.Default.AcquisitionDuration = int.Parse(txtDuration.Text);
            Settings.Default.Save();
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void txtAveraging_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !ValidateNumericInput(e.Text);
        }

        private void txtDuration_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !ValidateNumericInput(e.Text);
        }
    }
}
