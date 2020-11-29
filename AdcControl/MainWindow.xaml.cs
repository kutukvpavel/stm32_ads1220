using AdcControl.Properties;
using AdcControl.Resources;
using RJCP.IO.Ports;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

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

        //Public

        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly Dictionary<int, Color?> Colorset = new Dictionary<int, Color?>()
        {
            { 0, Color.FromKnownColor(KnownColor.Blue) },
            { 0x50, Color.FromKnownColor(KnownColor.Red) }
        };

        #region Properties

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

        #endregion

        //Private

        private string _CurrentStatus = "";
        private ConcurrentDictionary<int, string> ChannelNames;
        private ConcurrentDictionary<int, bool> ChannelEnable;
        private DispatcherTimer MouseTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 20), IsEnabled = false };
        private DispatcherTimer RefreshTimer = new DispatcherTimer() { IsEnabled = false };
#if TRACE
        private static BlockingCollectionQueue TraceQueue = new BlockingCollectionQueue();
#endif

        #region Functions
        private static void Trace(string s)
        {
#if TRACE
            TraceQueue.Enqueue(() => { System.Diagnostics.Trace.WriteLine(string.Format("{0:mm.ss.ff} {1}", DateTime.UtcNow, s)); });
#endif
        }

        private void OnPropertyChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        private void PlotChannel(AdcChannel channel)
        {
            Trace("Replotting");
            pltMainPlot.plt.Remove(channel.Plot);
            channel.Plot = pltMainPlot.plt.PlotSignal( //This will automatically apply all properties defined in AdcChannel
                channel.CalculatedY,
                lineWidth: Settings.Default.LineWidth,
                color: channel.Color); //Somehow signalPlot doesn't support color change (only markers change color after the field was modified)
        }

        private void SaveAxisLimits()
        {
            var s = pltMainPlot.plt.GetSettings();
            Settings.Default.YMax = s.axes.y.max;
            Settings.Default.YMin = s.axes.y.min;
            double xMin = s.axes.x.min;
            double xMax = s.axes.x.max;
            if (Settings.Default.LockHorizontalAxis)
            {
                xMax -= xMin;
                xMin = 0;
            }
            Settings.Default.XMax = xMax;
            Settings.Default.XMin = xMin;
        }

        private void RestoreAxisLimits()
        {
            pltMainPlot.plt.Axis(y1: Settings.Default.YMin, y2: Settings.Default.YMax);
        }

        private void LoadChannelNames()
        {
            ChannelNames = DictionarySaver.Parse(Settings.Default.ChannelNameMapping, x => x);
        }

        private void SaveChannelNames()
        {
            DictionarySaver.Save(Settings.Default.ChannelNameMapping, ChannelNames);
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Top = Settings.Default.MainWindowLocation.Y;
            Left = Settings.Default.MainWindowLocation.X;
            Height = Settings.Default.MainWindowSize.Height;
            Width = Settings.Default.MainWindowSize.Width;
            if (Settings.Default.Maximized) WindowState = WindowState.Maximized;
            LoadChannelNames();
            ChannelEnable = DictionarySaver.Parse(Settings.Default.ChannelEnableMapping, x => bool.Parse(x));
            btnEnableAutoAxis.IsChecked = Settings.Default.EnableAutoscaling;
            btnLockVerticalAxis.IsChecked = Settings.Default.LockVerticalScale;
            btnLockHorizontalAxis.IsChecked = Settings.Default.LockHorizontalAxis;
            btnAutoscrollRealTimeTable.IsChecked = Settings.Default.AutoscrollTable;
            App.Stm32Ads1220.TerminalEvent += Stm32Ads1220_TerminalEvent;
            App.Stm32Ads1220.AcquisitionFinished += Stm32Ads1220_AcquisitionFinished;
            App.Stm32Ads1220.CommandCompleted += Stm32Ads1220_CommandCompleted;
            App.NewChannelDetected += App_NewChannelDetected;
            pltMainPlot.plt.Ticks(numericFormatStringY: "F5");
            pltMainPlot.plt.YLabel(Settings.Default.YAxisLabel);
            pltMainPlot.plt.XLabel(Settings.Default.XAxisLabel);
            RestoreAxisLimits();
            pltMainPlot.Render();
            App.ConfigureCsvExporter();
            RefreshTimer.Interval = new TimeSpan(0, 0, 0, 0, Settings.Default.RefreshPeriod);
            RefreshTimer.Tick += RefreshTimer_Tick;
            MouseTimer.Tick += MouseTimer_Elapsed;
            MouseTimer.Start();
            App.Logger.Info(Default.msgLoadedMainWindow);
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (Settings.Default.EnableAutoscaling)
            {
                pltMainPlot.plt.AxisAutoX();
                if (Settings.Default.LockHorizontalAxis)
                {
                    var d = pltMainPlot.plt.GetSettings().axes.x.max - Settings.Default.XMax;
                    pltMainPlot.plt.Axis(Settings.Default.XMin + (d > 0 ? d : 0));
                }
                if (!Settings.Default.LockVerticalScale)
                {
                    pltMainPlot.plt.AxisAutoY();
                }
            }
            pltMainPlot.Render(skipIfCurrentlyRendering: true, lowQuality: true);
            if (Settings.Default.AutoscrollTable) scwRealTimeData.ScrollToBottom();
        }

        private void MouseTimer_Elapsed(object sender, EventArgs e)
        {
            if (!pltMainPlot.IsMouseOver) return;
            (double mouseX, double mouseY) = pltMainPlot.GetMouseCoordinates();
            txtCoordinates.Text = string.Format("{0:F6} V @ {1:F2} s", mouseY, mouseX);
        }

        private void MainWindow_DebugLogEvent(object sender, LogEventArgs e)
        {
            App.Logger.Debug(e.Message);
            App.Logger.Debug(e.Exception.Message);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            MouseTimer.Stop();
            MouseTimer.Tick -= MouseTimer_Elapsed;
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
            SaveAxisLimits();
            SaveChannelNames();
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
            txtStatus.Dispatcher.BeginInvoke(() => 
            {
                RefreshTimer.Stop();
                CurrentStatus = Default.stsAcqCompleted;
                pltMainPlot.Render();
            });
        }

        private void App_NewChannelDetected(object sender, NewChannelDetectedEventArgs e)
        {
            App.AdcChannels[e.Code].ArrayChanged += AdcChannel_ArrayChanged;
            Dispatcher.Invoke(() =>
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
                PlotChannel(App.AdcChannels[e.Code]);
                App.AdcChannels[e.Code].ContextMenuItem.Click += ContextMenuItem_Click;
                App.AdcChannels[e.Code].CalculatedXColumn.ItemsLimit = Settings.Default.TableLimit;
                App.AdcChannels[e.Code].CalculatedXColumn.DropItems = Settings.Default.TableDropPoints;
                App.AdcChannels[e.Code].CalculatedYColumn.ItemsLimit = Settings.Default.TableLimit;
                App.AdcChannels[e.Code].CalculatedYColumn.DropItems = Settings.Default.TableDropPoints;
                pltMainPlot.ContextMenu.Items.Add(App.AdcChannels[e.Code].ContextMenuItem);
                pltMainPlot.plt.Legend();
                pltMainPlot.Render(skipIfCurrentlyRendering: true, lowQuality: true);
                pnlRealTimeData.Children.Add(App.AdcChannels[e.Code].CalculatedXColumn);
                pnlRealTimeData.Children.Add(App.AdcChannels[e.Code].CalculatedYColumn);
            });
        }

        private void AdcChannel_ArrayChanged(object sender, EventArgs e)
        {
            try
            {
                var channel = (AdcChannel)sender;
                pltMainPlot.Dispatcher.Invoke(() =>
                {
                    PlotChannel(channel);
                });
            }
            catch (Exception ex)
            {
                App.Logger.Error(Default.msgChannelArrayUpdateError);
                App.Logger.Info(ex.ToString());
            }
        }

        private void ContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            pltMainPlot.Dispatcher.BeginInvoke(() => { pltMainPlot.Render(); });
        }

        private void Stm32Ads1220_TerminalEvent(object sender, TerminalEventArgs e)
        {
            txtTerminal.Dispatcher.Invoke(() =>
            {
                txtTerminal.AppendText(e.Line + Environment.NewLine);
                if (txtTerminal.Text.Length > Settings.Default.TerminalLimit)
                {
                    txtTerminal.Text = txtTerminal.Text.Remove(0, Settings.Default.TerminalRemoveStep);
                }
                if (expTerminal.IsExpanded) txtTerminal.ScrollToEnd();
            });
        }

        #endregion

        #region UI Events

        private void btnAutoscrollRealTimeTable_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.AutoscrollTable = btnAutoscrollRealTimeTable.IsChecked ?? false;
            if (Settings.Default.AutoscrollTable && !App.Stm32Ads1220.AcquisitionInProgress)
                scwRealTimeData.ScrollToBottom();
        }

        private void btnConfigChannels_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ChannelSettingEditor();
            SaveChannelNames();
            dialog.SetDefaultInputValue(Settings.Default.ChannelNameMapping);
            if (dialog.ShowDialog() ?? false)
            {
                Settings.Default.ChannelNameMapping = dialog.ParsedInput;
                //ChannelNames = DictionarySaver.Parse(dialog.ParsedInput, x => x);
                LoadChannelNames();
                Settings.Default.Save();
            }
        }

        private void btnLockVerticalAxis_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.LockVerticalScale = btnLockVerticalAxis.IsChecked ?? false;
            if (Settings.Default.LockVerticalScale)
            {
                SaveAxisLimits();
            }
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
                RefreshTimer.Start();
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
            txtTerminal.Clear();
            pnlRealTimeData.Children.Clear();
            pltMainPlot.plt.Clear();
            pltMainPlot.ContextMenu.Items.Clear();
            foreach (var item in App.AdcChannels.Values)
            {
                item.ContextMenuItem.Click -= ContextMenuItem_Click;
            }
            App.AdcChannels.Clear();
            RestoreAxisLimits();
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
                    item.Value.MovingAveraging = Settings.Default.Average;
                }
                if (!App.Stm32Ads1220.AcquisitionInProgress)
                {
                    foreach (var item in App.AdcChannels)
                    {
                        item.Value.CapacityStep = (int)
                            Math.Ceiling(Settings.Default.AcquisitionDuration * Settings.Default.AcquisitionSpeed);
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

        private void txtSendCustom_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                btnSendCustom_Click(this, new RoutedEventArgs());
            }
        }

        private void btnLockHorizontalAxis_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.LockHorizontalAxis = btnLockHorizontalAxis.IsChecked ?? false;
            if (Settings.Default.LockHorizontalAxis)
            {
                SaveAxisLimits();
            }
        }

        #endregion
    }
}
