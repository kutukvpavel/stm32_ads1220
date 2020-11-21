using AdcControl.Properties;
using AdcControl.Resources;
using RJCP.IO.Ports;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows;

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
                    App.Logger.Error(Default.msgCantCheckPortExistence);
                    return false;
                }
            } 
        }

        public static readonly Dictionary<int, Color?> Colorset = new Dictionary<int, Color?>()
        {
            { 0, Color.FromKnownColor(KnownColor.Blue) },
            { 0x50, Color.FromKnownColor(KnownColor.Red) }
        };

        private BlockingCollectionQueue UpdateArrayQueue = new BlockingCollectionQueue();
        private ConcurrentDictionary<int, string> ChannelNames;
        private ConcurrentDictionary<int, bool> ChannelEnable;

        private void OnPropertyChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        private void PlotScatter(AdcChannel channel)
        {
            var res = pltMainPlot.plt.PlotScatter(channel.CalculatedX, channel.CalculatedY);
            channel.Plot = res; //This will apply predefined label, visibility and color if they exist
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Top = Settings.Default.MainWindowLocation.Y;
            Left = Settings.Default.MainWindowLocation.X;
            Height = Settings.Default.MainWindowSize.Height;
            Width = Settings.Default.MainWindowSize.Width;
            if (Settings.Default.Maximized) WindowState = WindowState.Maximized;
            ChannelNames = DictionarySaver.Parse(Settings.Default.ChannelNameMapping, x => x);
            ChannelEnable = DictionarySaver.Parse(Settings.Default.ChannelEnableMapping, x => bool.Parse(x));
            App.Stm32Ads1220.TerminalEvent += Stm32Ads1220_TerminalEvent;
            App.Stm32Ads1220.AcquisitionDataReceived += Stm32Ads1220_AcquisitionDataReceived;
            App.Stm32Ads1220.AcquisitionFinished += Stm32Ads1220_AcquisitionFinished;
            App.Stm32Ads1220.CommandCompleted += Stm32Ads1220_CommandCompleted;
            App.NewChannelDetected += App_NewChannelDetected;
            pltMainPlot.plt.Ticks(dateTimeX: true, dateTimeFormatStringX: "HH:mm:ss", numericFormatStringY: "F5");
            App.Logger.Info("Main window loaded.");
        }

        private void Stm32Ads1220_CommandCompleted(object sender, EventArgs e)
        {
            if (!App.Stm32Ads1220.AcquisitionInProgress)
            {
                txtStatus.Dispatcher.Invoke(() =>
                {
                    txtStatus.Text = Default.stsCommandCompleted;
                });
            }
        }

        private void Stm32Ads1220_AcquisitionFinished(object sender, EventArgs e)
        {
            txtStatus.Dispatcher.Invoke(() => { txtStatus.Text = Default.stsAcqCompleted; });
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
            bool success = false;
            pltMainPlot.Dispatcher.Invoke(() =>
            {
                if (ChannelEnable.ContainsKey(e.Code))
                {
                    App.AdcChannels[e.Code].IsVisible = ChannelEnable[e.Code];
                }
                if (ChannelNames.ContainsKey(e.Code))
                {
                    App.AdcChannels[e.Code].Name = ChannelNames[e.Code];
                }
                if (Colorset.ContainsKey(e.Code))
                {
                    App.AdcChannels[e.Code].Color = Colorset[e.Code];
                }
                PlotScatter(App.AdcChannels[e.Code]);
                pltMainPlot.Render();
                pltMainPlot.ContextMenu.Items.Add(DictionarySaver.WriteMapping(e.Code, App.AdcChannels[e.Code].Name));
            });
            if (success)
            {
                App.AdcChannels[e.Code].ArrayChanged += UpdateArray;
#if DEBUG
                App.AdcChannels[e.Code].DebugLogEvent += MainWindow_DebugLogEvent;
#endif
            }
            else
            {
                App.Logger.Error(Default.msgChannelPlotConcurrency);
            }
        }

        private void MainWindow_DebugLogEvent(object sender, LogEventArgs e)
        {
            App.Logger.Debug(e.Message);
            App.Logger.Debug(e.Exception.Message);
        }

        private void UpdateArray(object sender, EventArgs e)
        {
            int code = ((AdcChannel)sender).Code;
            UpdateArrayQueue.Enqueue(() =>
            {
                bool success = false;
                pltMainPlot.Dispatcher.Invoke(() =>
                {
                    pltMainPlot.plt.Remove(App.AdcChannels[code].Plot);
                    PlotScatter(App.AdcChannels[code]);
                    pltMainPlot.Render();
                    App.Logger.Debug("UpdateArray executed: readded " + code.ToString());
                });
                if (!success)
                {
                    App.Logger.Error(Default.msgUpdateChannelPlotFailed);
                }
            });
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
                txtTerminal.ScrollToEnd();
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
            pltMainPlot.ContextMenu.Items.Clear();
            App.AdcChannels.Clear();
            pltMainPlot.Render();
        }

        private async void btnExport_Click(object sender, RoutedEventArgs e)
        {
            bool success = true;
            foreach (var item in App.AdcChannels)
            {
                if (!await CsvExporter.Export(item.Key, item.Value)) success = false;
            }
            txtStatus.Text = success ? Default.stsCsvSaveSuccess : Default.stsCsvSaveFailed;
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
            OnPropertyChanged();
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
            if (App.Stm32Ads1220.Disconnect())
            {
                txtStatus.Text = Default.stsDisconnected;
            }
            else
            {
                txtStatus.Text = Default.stsFailure;
            }
            OnPropertyChanged();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (App.Stm32Ads1220.IsConnected)
            {
                App.Stm32Ads1220.Disconnect();
            }
            if (WindowState != WindowState.Maximized)
            {
                Settings.Default.MainWindowLocation = new System.Drawing.Point((int)Left, (int)Top);
                Settings.Default.MainWindowSize = new System.Drawing.Size((int)Width, (int)Height);
            }
            Settings.Default.Maximized = WindowState == WindowState.Maximized;
            DictionarySaver.Save(Settings.Default.ChannelNameMapping, ChannelNames);
            DictionarySaver.Save(Settings.Default.ChannelEnableMapping, ChannelEnable);
            Settings.Default.Save();
        }

        private async void btnSendCustom_Click(object sender, RoutedEventArgs e)
        {
            await App.Stm32Ads1220.SendCustom(txtSendCustom.Text);
        }

        private void btnForceRender_Click(object sender, RoutedEventArgs e)
        {
            pltMainPlot.plt.AxisAuto();
            pltMainPlot.Render();
        }
    }
}
