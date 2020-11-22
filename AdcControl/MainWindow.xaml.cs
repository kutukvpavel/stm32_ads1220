using AdcControl.Properties;
using AdcControl.Resources;
using RJCP.IO.Ports;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
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
        private string _CurrentStatus = "";

        public event PropertyChangedEventHandler PropertyChanged;

        public string CurrentStatus
        {
            get { return _CurrentStatus; }
            private set
            {
                _CurrentStatus = value;
                txtStatus.Text = _CurrentStatus;
                App.Logger.Info(_CurrentStatus);
            }
        }

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

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Top = Settings.Default.MainWindowLocation.Y;
            Left = Settings.Default.MainWindowLocation.X;
            Height = Settings.Default.MainWindowSize.Height;
            Width = Settings.Default.MainWindowSize.Width;
            if (Settings.Default.Maximized) WindowState = WindowState.Maximized;
            ChannelNames = DictionarySaver.Parse(Settings.Default.ChannelNameMapping, x => x);
            ChannelEnable = DictionarySaver.Parse(Settings.Default.ChannelEnableMapping, x => bool.Parse(x));
            btnEnableAutoAxis.IsChecked = Settings.Default.EnableAutoscaling;
            btnLockVerticalAxis.IsChecked = Settings.Default.LockVerticalScale;
            App.Stm32Ads1220.TerminalEvent += Stm32Ads1220_TerminalEvent;
            App.Stm32Ads1220.AcquisitionDataReceived += Stm32Ads1220_AcquisitionDataReceived;
            App.Stm32Ads1220.AcquisitionFinished += Stm32Ads1220_AcquisitionFinished;
            App.Stm32Ads1220.CommandCompleted += Stm32Ads1220_CommandCompleted;
            App.NewChannelDetected += App_NewChannelDetected;
            pltMainPlot.plt.Ticks(dateTimeX: true, dateTimeFormatStringX: "HH:mm:ss", numericFormatStringY: "F5");
            pltMainPlot.plt.Axis(y1: Settings.Default.YMin, y2: Settings.Default.YMax);
            pltMainPlot.Render();
            App.ConfigureCsvExporter();
            App.Logger.Info(Default.msgLoadedMainWindow);
        }

        private void MainWindow_DebugLogEvent(object sender, LogEventArgs e)
        {
            App.Logger.Debug(e.Message);
            App.Logger.Debug(e.Exception.Message);
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
            var s = pltMainPlot.plt.GetSettings().axes.y;
            Settings.Default.YMax = s.max;
            Settings.Default.YMin = s.min;
            DictionarySaver.Save(Settings.Default.ChannelNameMapping, ChannelNames);
            DictionarySaver.Save(Settings.Default.ChannelEnableMapping, ChannelEnable);
            Settings.Default.Save();
        }

        #endregion

        #region UI-related Application Events

        private void Stm32Ads1220_CommandCompleted(object sender, EventArgs e)
        {
            if (!App.Stm32Ads1220.AcquisitionInProgress)
            {
                txtStatus.Dispatcher.Invoke(() =>
                {
                    CurrentStatus = Default.stsCommandCompleted;
                });
            }
        }

        private void Stm32Ads1220_AcquisitionFinished(object sender, EventArgs e)
        {
            txtStatus.Dispatcher.Invoke(() => { CurrentStatus = Default.stsAcqCompleted; });
        }

        private void Stm32Ads1220_AcquisitionDataReceived(object sender, AcquisitionEventArgs e)
        {
            pltMainPlot.Dispatcher.Invoke(() =>
            {
                if (Settings.Default.EnableAutoscaling)
                {
                    if (Settings.Default.LockVerticalScale)
                    {
                        pltMainPlot.plt.AxisAutoX();
                    }
                    else
                    {
                        pltMainPlot.plt.AxisAuto();
                    }
                }
                pltMainPlot.Render();
            });
        }

        private void App_NewChannelDetected(object sender, NewChannelDetectedEventArgs e)
        {
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
                pltMainPlot.plt.Legend();
                pltMainPlot.Render();
                pltMainPlot.ContextMenu.Items.Add(App.AdcChannels[e.Code].ContextMenuItem);
            });
            App.AdcChannels[e.Code].ContextMenuItem.Click += ContextMenuItem_Click;
        }

        private void ContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            pltMainPlot.Dispatcher.Invoke(() => { pltMainPlot.Render(); });
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

        #endregion

        #region UI Events

        private void btnLockVerticalAxis_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.LockVerticalScale = btnLockVerticalAxis.IsChecked ?? false;
        }

        private void pltMainPlot_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            (double mouseX, double mouseY) = pltMainPlot.GetMouseCoordinates();
            txtCoordinates.Text = string.Format(
                "{0:F6} V @ {1}",
                mouseY,
                DateTime.FromOADate(mouseX).ToString("HH:mm:ss.ff")
                );
        }

        private void btnEnableAutoAxis_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.EnableAutoscaling = btnEnableAutoAxis.IsChecked ?? false;
        }

        private void btnOpenExportFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var s = new ProcessStartInfo(Path.GetFullPath(Environment.CurrentDirectory + Settings.Default.CsvSavePath))
                {
                    UseShellExecute = true
                };
                Process.Start(s);
            }
            catch (Exception ex)
            {
                App.Logger.Error(Default.msgCantOpenExportFolder);
                App.Logger.Info(ex.ToString());
            }
        }

        private async void btnStartAcquisition_Click(object sender, RoutedEventArgs e)
        {
            CurrentStatus = Default.stsStartingAcq;
            if (await App.Stm32Ads1220.StartAcquisition(Settings.Default.AcquisitionDuration))
            {
                CurrentStatus = Default.stsAcqInProgress;
            }
            else
            {
                CurrentStatus = Default.stsFailure;
            }
        }

        private async void btnStopAcquisition_Click(object sender, RoutedEventArgs e)
        {
            CurrentStatus = Default.stsStoppingAcq;
            if (await App.Stm32Ads1220.StopAcquisition())
            {
                CurrentStatus = Default.stsReady;
            }
            else
            {
                CurrentStatus = Default.stsFailure;
            }
        }

        private void btnClearScreen_Click(object sender, RoutedEventArgs e)
        {
            pltMainPlot.plt.Clear();
            pltMainPlot.ContextMenu.Items.Clear();
            foreach (var item in App.AdcChannels.Values)
            {
                item.ContextMenuItem.Click -= ContextMenuItem_Click;
            }
            App.AdcChannels.Clear();
            pltMainPlot.Render();
        }

        private async void btnExport_Click(object sender, RoutedEventArgs e)
        {
            InputBox dialog = null;
            Func<bool> showDialog = () =>
            {
                dialog = new InputBox()
                {
                    PromptLabel = Default.strEnterExperimentName,
                    InvalidCharacters = Path.GetInvalidFileNameChars()
                };
                return dialog.ShowDialog() ?? false;
            };
            while (showDialog())
            {
                if (CsvExporter.CheckIfAlreadyExists(dialog.InputText))
                {
                    if (MessageBox.Show(
                        Default.msgCsvAlreadyExistsReplace,
                        Default.strMessageBoxCaption,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.No) continue;
                }
                try
                {
                    CurrentStatus = Default.stsCsvSaveInProgress;
                    CurrentStatus = (await CsvExporter.Export(dialog.InputText, App.AdcChannels.Values)) ?
                        Default.stsCsvSaveSuccess :
                        Default.stsCsvSaveFailed;
                }
                catch (Exception ex)
                {
                    App.Logger.Error(Default.msgUnknownExportError);
                    App.Logger.Info(ex.ToString());
                }
                break;
            }
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            CurrentStatus = Default.stsConnecting;
            if (await App.Stm32Ads1220.Connect())
            {
                CurrentStatus = Default.stsConnected;
            }
            else
            {
                CurrentStatus = Default.stsFailure;
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
                CurrentStatus = Default.stsDisconnected;
            }
            else
            {
                CurrentStatus = Default.stsFailure;
            }
            OnPropertyChanged();
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

        #endregion
    }
}
