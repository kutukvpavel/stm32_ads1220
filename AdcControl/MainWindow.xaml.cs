using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using AdcControl.Resources;
using AdcControl.Properties;
using System.Data;
using RJCP.IO.Ports;
using System.ComponentModel;

namespace AdcControl
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            App.InitGlobals();
            InitializeComponent();
        }

        private int TerminalLines = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool ReadyForConnection 
        { 
            get 
            {
                try
                {
                    return App.Stm32Ads1220.IsNotConnected &&
                        SerialPortStream.GetPortNames().Contains(App.Stm32Ads1220.Port.PortName);
                }
                catch (Exception)
                {
                    return false;
                }
            } 
        }

        private Dictionary<int, ScottPlot.Plottable> Plotted = new Dictionary<int, ScottPlot.Plottable>();

        private void OnPropertyChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Top = Settings.Default.MainWindowLocation.Y;
            Left = Settings.Default.MainWindowLocation.X;
            Height = Settings.Default.MainWindowSize.Height;
            Width = Settings.Default.MainWindowSize.Width;
            if (Settings.Default.Maximized) WindowState = WindowState.Maximized;
            App.Stm32Ads1220.TerminalEvent += Stm32Ads1220_TerminalEvent;
            App.Stm32Ads1220.AcquisitionDataReceived += Stm32Ads1220_AcquisitionDataReceived;
            App.NewChannelDetected += App_NewChannelDetected;
            pltMainPlot.plt.Ticks(dateTimeX: true, dateTimeFormatStringX: "HH:mm:ss");
            App.Logger.Info("Main window loaded.");
        }

        private void Stm32Ads1220_AcquisitionDataReceived(object sender, AcquisitionEventArgs e)
        {
            pltMainPlot.Dispatcher.Invoke(() =>
            {
                try
                {
                    pltMainPlot.plt.AxisAuto();
                }
                catch (InvalidOperationException)
                {

                }
                pltMainPlot.Render();
            });
        }

        private void App_NewChannelDetected(object sender, NewChannelDetectedEventArgs e)
        {
            Plotted.Add(e.Code,
                pltMainPlot.plt.PlotScatter(App.AdcChannels[e.Code].CalculatedX, App.AdcChannels[e.Code].CalculatedY));
            App.AdcChannels[e.Code].ArrayChanged += UpdateArray;
        }

        private void UpdateArray(object sender, EventArgs e)
        {
            int code = ((AdcChannel)sender).Code;
            pltMainPlot.plt.Remove(Plotted[code]);
            pltMainPlot.plt.PlotScatter(App.AdcChannels[code].CalculatedX, App.AdcChannels[code].CalculatedY);
        }

        private void Stm32Ads1220_TerminalEvent(object sender, TerminalEventArgs e)
        {
            txtTerminal.Dispatcher.Invoke(() =>
            {
                txtTerminal.AppendText(e.Line + Environment.NewLine);
                if (++TerminalLines > Settings.Default.TerminalLimit)
                {
                    txtTerminal.Text = txtTerminal.Text.Remove(0, Settings.Default.TerminalRemoveStep);
                    TerminalLines -= Settings.Default.TerminalRemoveStep;
                }
            });
        }

        private async void btnStartAcquisition_Click(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = Default.stsStartingAcq;
            if (await App.Stm32Ads1220.StartAcquisition(Settings.Default.AcquisitionDuration))
            {
                txtStatus.Text = Default.stsAcqInProgress;
            }
            else
            {
                txtStatus.Text = Default.stsFailure;
            }
        }

        private async void btnStopAcquisition_Click(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = Default.stsStoppingAcq;
            if (await App.Stm32Ads1220.StopAcquisition())
            {
                txtStatus.Text = Default.stsReady;
            }
            else
            {
                txtStatus.Text = Default.stsFailure;
            }
        }

        private void btnClearScreen_Click(object sender, RoutedEventArgs e)
        {
            pltMainPlot.plt.Clear();
            Plotted.Clear();
            App.AdcChannels.Clear();
            pltMainPlot.Render();
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = Default.stsConnecting;
            if (await App.Stm32Ads1220.Connect())
            {
                txtStatus.Text = Default.stsConnected;
            }
            else
            {
                txtStatus.Text = Default.stsFailure;
            }
        }

        private async void btnConfig_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow dialog = new SettingsWindow();
            if (dialog.ShowDialog() ?? false)
            {
                if (!App.Stm32Ads1220.IsConnected)
                {
                    App.Stm32Ads1220.Port.PortName = Settings.Default.PortName;
                }
                foreach (var item in App.AdcChannels)
                {
                    item.Value.Averaging = Settings.Default.Average;
                }
                if (!App.Stm32Ads1220.AcquisitionInProgress)
                {
                    foreach (var item in App.AdcChannels)
                    {
                        await AdcChannel.Recalculate(item.Value);
                    }
                }
            }
            OnPropertyChanged();
            pltMainPlot.Render();
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            App.Stm32Ads1220.Disconnect();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (WindowState != WindowState.Maximized)
            {
                Settings.Default.MainWindowLocation = new System.Drawing.Point((int)Left, (int)Top);
                Settings.Default.MainWindowSize = new System.Drawing.Size((int)Width, (int)Height);
            }
            Settings.Default.Maximized = WindowState == WindowState.Maximized;
            Settings.Default.Save();
        }

        private async void btnSendCustom_Click(object sender, RoutedEventArgs e)
        {
            await App.Stm32Ads1220.SendCustom(txtSendCustom.Text);
        }
    }
}
