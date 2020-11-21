using AdcControl.Properties;
using AdcControl.Resources;
using RJCP.IO.Ports;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AdcControl
{
    /// <summary>
    /// Логика взаимодействия для Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        public bool AllFieldsOK 
        { 
            get 
            { 
                if ((txtAveraging.Text.Length > 0) && (txtDuration.Text.Length > 0))
                {
                    int.TryParse(txtAveraging.Text, out int i);
                    int.TryParse(txtDuration.Text, out int j);
                    return i > 0 && j > 0;
                }
                else
                {
                    return false;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private bool ValidateNumericInput(string val)
        {
            return int.TryParse(val, out _);
        }

        private void OnPropertyChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cmbPorts.Items.Clear();
            cmbPorts.ItemsSource = SerialPortStream.GetPortNames();
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

        private void txtAveraging_TextChanged(object sender, TextChangedEventArgs e)
        {
            OnPropertyChanged();
        }

        private void txtDuration_TextChanged(object sender, TextChangedEventArgs e)
        {
            OnPropertyChanged();
        }
    }
}
