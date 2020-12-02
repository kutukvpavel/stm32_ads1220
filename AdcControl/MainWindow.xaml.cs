using AdcControl.Properties;
using AdcControl.Resources;
using RJCP.IO.Ports;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using org.mariuszgromada.math.mxparser;
using Expression = org.mariuszgromada.math.mxparser.Expression;

namespace AdcControl
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            //Internal events
            RefreshTimer.Tick += RefreshTimer_Tick;
            MouseTimer.Tick += MouseTimer_Elapsed;
            //External events
            App.Stm32Ads1220.TerminalEvent += Stm32Ads1220_TerminalEvent;
            App.Stm32Ads1220.AcquisitionFinished += Stm32Ads1220_AcquisitionFinished;
            App.Stm32Ads1220.CommandCompleted += Stm32Ads1220_CommandCompleted;
            App.Stm32Ads1220.UnexpectedDisconnect += Stm32Ads1220_UnexpectedDisconnect;
            App.Stm32Ads1220.AcquisitionDataReceived += Stm32Ads1220_AcquisitionDataReceived;
            App.Stm32Ads1220.PropertyChanged += Stm32Ads1220_PropertyChanged;
            App.NewChannelDetected += App_NewChannelDetected;
            //Window Settings (can't make a TwoWay binding or bind to ActualXX, so no point in doing this in XAML at all)
            if (Settings.Default.MainWindowMaximized) WindowState = WindowState.Maximized;
            Top = Settings.Default.MainWindowLocation.Y;
            Left = Settings.Default.MainWindowLocation.X;
            Height = Settings.Default.MainWindowSize.Height;
            Width = Settings.Default.MainWindowSize.Width;
        }

        //Public

        public event PropertyChangedEventHandler PropertyChanged;

        #region Properties

        public string CurrentStatus
        {
            get { return _CurrentStatus; }
            private set
            {
                _CurrentStatus = value;
                OnPropertyChanged("CurrentStatus");
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

        public bool ReadyToExportData
        {
            get => _ReadyToExportData;
            set
            {
                _ReadyToExportData = value;
                OnPropertyChanged("ReadyToExportData");
            }
        }

        public bool CanDisconnect
        {
            get => App.Stm32Ads1220.IsConnected && 
                !App.Stm32Ads1220.AcquisitionInProgress && 
                App.Stm32Ads1220.CommandExecutionCompleted;
        }

        public bool IsRecalculating
        {
            get => _IsRecalculating;
            set
            {
                _IsRecalculating = value;
                OnPropertyChanged("IsRecalculating");
            }
        }

        #endregion

        //Private

        private bool _IsRecalculating = false;
        private bool _ReadyToExportData = false;
        private string _CurrentStatus = Default.stsReady;
        private bool TableRenderingDefered = false;
        private readonly DispatcherTimer MouseTimer = new DispatcherTimer() { IsEnabled = false };
        private readonly DispatcherTimer RefreshTimer = new DispatcherTimer() { IsEnabled = false };
#if TRACE
        private static readonly BlockingCollectionQueue TraceQueue = new BlockingCollectionQueue();
#endif

        #region Functions

        private async Task RecalculateChannels()
        {
            if (App.Stm32Ads1220.AcquisitionInProgress) return;
            CurrentStatus = Default.stsRecalculating;
            IsRecalculating = true;
            prgAcquisitionProgress.Minimum = 0;
            prgAcquisitionProgress.Value = 0;
            prgAcquisitionProgress.Maximum = App.AdcChannels.Count;
            foreach (var item in App.AdcChannels.Values)
            {
                prgAcquisitionProgress.Value++;
                await AdcChannel.Recalculate(item);
            }
            prgAcquisitionProgress.Value++;
            pltMainPlot.Render();
            IsRecalculating = false;
            CurrentStatus = Default.stsRecalculated;
        }

        private static void Trace(string s)
        {
#if TRACE
            TraceQueue.Enqueue(() => { System.Diagnostics.Trace.WriteLine(string.Format("{0:mm.ss.ff} {1}", DateTime.UtcNow, s)); });
#endif
        }

        private void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void PlotChannel(AdcChannel channel)
        {
            Trace("Replotting");
            pltMainPlot.plt.Remove(channel.Plot);
            channel.Plot = pltMainPlot.plt.PlotSignal( //This will automatically apply all properties defined in AdcChannel
                channel.CalculatedY,
                lineWidth: Settings.ViewSettings.LineWidth,
                color: channel.Color); //Somehow signalPlot doesn't support color change (only markers change color after the field was modified)
        }

        private void SaveAxisLimits()
        {
            var s = pltMainPlot.plt.GetSettings();
            Settings.ViewSettings.YMax = s.axes.y.max;
            Settings.ViewSettings.YMin = s.axes.y.min;
            double xMin = s.axes.x.min;
            double xMax = s.axes.x.max;
            if (Settings.ViewSettings.LockHorizontalAxis)
            {
                xMax -= xMin;
                xMin = 0;
            }
            Settings.ViewSettings.XMax = xMax;
            Settings.ViewSettings.XMin = xMin;
        }

        private void RestoreAxisLimits()
        {
            pltMainPlot.plt.Axis(y1: Settings.ViewSettings.YMin, y2: Settings.ViewSettings.YMax);
        }

        private void LoadPlotSettings()
        {
            pltMainPlot.plt.YLabel(Settings.ViewSettings.YAxisLabel);
            pltMainPlot.plt.XLabel(Settings.ViewSettings.XAxisLabel);
            pltMainPlot.plt.Ticks(numericFormatStringY: Settings.ViewSettings.CalculatedYNumberFormat);
            RestoreAxisLimits();
        }

        private void LoadTimerSettings()
        {
            RefreshTimer.Interval = new TimeSpan(0, 0, 0, 0, Settings.ViewSettings.RefreshPeriod);
            MouseTimer.Interval = new TimeSpan(0, 0, 0, 0, Settings.ViewSettings.MouseRefreshPeriod);
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPlotSettings();
            pltMainPlot.Render();
            LoadTimerSettings();
            MouseTimer.Start();
            App.Logger.Info(Default.msgLoadedMainWindow);
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (Settings.ViewSettings.EnableAutoscaling)
            {
                pltMainPlot.plt.AxisAutoX(0);
                if (Settings.ViewSettings.LockHorizontalAxis)
                {
                    var d = pltMainPlot.plt.GetSettings().axes.x.max - Settings.ViewSettings.XMax;
                    pltMainPlot.plt.Axis(Settings.ViewSettings.XMin + (d > 0 ? d : 0));
                }
                if (!Settings.ViewSettings.LockVerticalScale)
                {
                    pltMainPlot.plt.AxisAutoY();
                }
            }
            else
            {
                var a = pltMainPlot.plt.GetSettings().axes;
                if (Settings.ViewSettings.LockHorizontalAxis)
                {
                    a.x.max = Settings.ViewSettings.XMax;
                    a.x.min = Settings.ViewSettings.XMin;
                }
                if (Settings.ViewSettings.LockVerticalScale)
                {
                    a.y.max = Settings.ViewSettings.YMax;
                    a.y.min = Settings.ViewSettings.YMin;
                }
            }
            pltMainPlot.Render(skipIfCurrentlyRendering: true, lowQuality: true);
            if (expTable.IsExpanded)
            {
                if (Settings.ViewSettings.AutoscrollTable)
                {
                    if (TableRenderingDefered)
                    {
                        foreach (var item in App.AdcChannels.Values)
                        {
                            item.CalculatedXColumn.DeferRendering = false;
                            item.CalculatedYColumn.DeferRendering = false;
                        }
                        TableRenderingDefered = false;
                    }
                    scwRealTimeData.ScrollToBottom();
                }
                else if (!TableRenderingDefered)
                {
                    foreach (var item in App.AdcChannels.Values)
                    {
                        item.CalculatedXColumn.DeferRendering = true;
                        item.CalculatedYColumn.DeferRendering = true;
                    }
                    TableRenderingDefered = true;
                }
            }
            prgAcquisitionProgress.Value = DateTime.UtcNow.Ticks;
        }

        private void MouseTimer_Elapsed(object sender, EventArgs e)
        {
            if (!pltMainPlot.IsMouseOver) return;
            (double mouseX, double mouseY) = pltMainPlot.GetMouseCoordinates();
            txtCoordinates.Text = string.Format("{0} @ {1:F2}", 
                mouseY.ToString(Settings.ViewSettings.CalculatedYNumberFormat), 
                mouseX
                );
        }

        private void MainWindow_DebugLogEvent(object sender, LogEventArgs e)
        {
            App.Logger.Debug(e.Message);
            App.Logger.Debug(e.Exception.Message);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (App.Stm32Ads1220.AcquisitionInProgress)
            {
                e.Cancel = true;
                MessageBox.Show(
                    Default.strStopAcquisitionBeforeExiting,
                    Default.strApplicationNameCaption,
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation
                    );
                return;
            }
            MouseTimer.Stop();
            MouseTimer.Tick -= MouseTimer_Elapsed;
            if (App.Stm32Ads1220.IsConnected)
            {
                App.Stm32Ads1220.Disconnect();
            }
            OnPropertyChanged();
            Settings.Default.MainWindowMaximized = WindowState == WindowState.Maximized;
            Settings.Default.MainWindowLocation = new System.Drawing.Point((int)Left, (int)Top);
            Settings.Default.MainWindowSize = new System.Drawing.Size((int)ActualWidth, (int)ActualHeight);
            SaveAxisLimits();
        }

        #endregion

        #region UI-related Application Events

        private void Stm32Ads1220_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged();
        }

        private void Stm32Ads1220_AcquisitionDataReceived(object sender, AcquisitionEventArgs e)
        {
            if (!ReadyToExportData) ReadyToExportData = true;
        }

        private void Stm32Ads1220_CommandCompleted(object sender, EventArgs e)
        {
            if (!App.Stm32Ads1220.AcquisitionInProgress)
            {
                CurrentStatus = Default.stsCommandCompleted;
            }
        }

        private void Stm32Ads1220_AcquisitionFinished(object sender, EventArgs e)
        {
            txtStatus.Dispatcher.BeginInvoke(() => 
            {
                RefreshTimer.Stop();
                CurrentStatus = Default.stsAcqCompleted;
                pltMainPlot.Render();
                NativeMethods.AllowSleep();
            });
        }

        private void Stm32Ads1220_UnexpectedDisconnect(object sender, EventArgs e)
        {
            Stm32Ads1220_AcquisitionFinished(this, null);
            CurrentStatus = Default.stsUnexpectedDisconnect;
        }

        private void App_NewChannelDetected(object sender, NewChannelDetectedEventArgs e)
        {
            App.AdcChannels[e.Code].ArrayChanged += AdcChannel_ArrayChanged;
            Dispatcher.Invoke(() =>
            {
                PlotChannel(App.AdcChannels[e.Code]);
                //First access creates the controls, further accesses to my own properties don't have to be through UI thread
                App.AdcChannels[e.Code].ContextMenuItem.Click += ContextMenuItem_Click;
                //This property has to be set upon creation, because this event is being executed asynchronously
                App.AdcChannels[e.Code].CalculatedYColumn.ItemStringFormat = Settings.ViewSettings.CalculatedYNumberFormat;
                App.AdcChannels[e.Code].CalculatedYColumn.ItemsLimit = Settings.ViewSettings.TableLimit;
                pltMainPlot.ContextMenu.Items.Add(App.AdcChannels[e.Code].ContextMenuItem);
                pltMainPlot.plt.Legend(location: ScottPlot.legendLocation.upperLeft);
                pltMainPlot.Render(skipIfCurrentlyRendering: true, lowQuality: true);
                pnlRealTimeData.Children.Add(App.AdcChannels[e.Code].CalculatedXColumn);
                pnlRealTimeData.Children.Add(App.AdcChannels[e.Code].CalculatedYColumn);
            });
            App.AdcChannels[e.Code].CalculatedXColumn.ItemsLimit = Settings.ViewSettings.TableLimit;
            App.AdcChannels[e.Code].CalculatedXColumn.DropItems = Settings.ViewSettings.TableDropPoints;
            App.AdcChannels[e.Code].CalculatedYColumn.DropItems = Settings.ViewSettings.TableDropPoints;
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
                if (txtTerminal.Text.Length > Settings.ViewSettings.TerminalLimit)
                {
                    txtTerminal.Text = txtTerminal.Text.Remove(0, Settings.ViewSettings.TerminalRemoveStep);
                }
                if (expTerminal.IsExpanded) txtTerminal.ScrollToEnd();
            });
        }

        #endregion

        #region UI Events

        private void btnExportConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ExportSettingsWindow();
            dialog.ShowDialog();
            App.ConfigureCsvExporter();
        }

        private void btnPlotConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ViewSettingsWindow();
            dialog.ShowDialog();
            LoadPlotSettings();
            LoadTimerSettings();
            pltMainPlot.Render();
        }

        private void btnConfigChannels_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ChannelSettingEditor();
            App.SaveChannelNames();
            dialog.SetDefaultInputValue(Settings.Default.ChannelNameMapping);
            if (dialog.ShowDialog() ?? false)
            {
                Settings.Default.ChannelNameMapping = dialog.ParsedInput;
                App.LoadChannelNames();
                Settings.Default.Save();
                foreach (var item in App.AdcChannels.Values)
                {
                    if (App.ChannelNames.ContainsKey(item.Code)) item.Name = App.ChannelNames[item.Code];
                }
                pltMainPlot.Render();
            }
        }

        private void btnOpenExportFolder_Click(object sender, RoutedEventArgs e)
        {
            string p = null;
            try
            {
                p ='"' + Path.GetFullPath(Environment.CurrentDirectory + Settings.ExportSettings.CsvSavePath) + '"';
                var s = new ProcessStartInfo(p)
                {
                    UseShellExecute = true
                };
                Process.Start(s);
            }
            catch (Exception ex)
            {
                App.Logger.Error(Default.msgCantOpenExportFolder);
                App.Logger.Info(p ?? "N/A");
                App.Logger.Info(ex.ToString());
            }
        }

        private async void btnStartAcquisition_Click(object sender, RoutedEventArgs e)
        {
            CurrentStatus = Default.stsStartingAcq;
            if (await App.Stm32Ads1220.StartAcquisition(Settings.Default.AcquisitionDuration))
            {
                prgAcquisitionProgress.Minimum = DateTime.UtcNow.Ticks;
                prgAcquisitionProgress.Maximum = prgAcquisitionProgress.Minimum + Settings.Default.AcquisitionDuration * 10E6;
                RefreshTimer.Start();
                App.Logger.Info(NativeMethods.PreventSleep() ? Default.msgPowerManagementOK : Default.msgPowerManagementFail);
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
            ReadyToExportData = false;
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
                        Default.strApplicationNameCaption,
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
                    item.Value.CapacityStep = (int)
                        Math.Ceiling(Settings.Default.AcquisitionDuration * Settings.Default.AcquisitionSpeed);
                }
                await RecalculateChannels();
                OnPropertyChanged();
            }
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

        private void btnAutoscrollRealTimeTable_Checked(object sender, RoutedEventArgs e)
        {
            if (!App.Stm32Ads1220.AcquisitionInProgress && IsLoaded)
                scwRealTimeData.ScrollToBottom();
        }

        private void btnLockHorizontalAxis_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded) SaveAxisLimits();
        }

        private void btnLockVerticalAxis_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded) SaveAxisLimits();
        }

        private async void btnMathConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ChannelSettingEditor()
            {
                ValueValidator = (x) =>
                {
                    if (x == null) return false;
                    return new Expression(x, App.MathElements).checkSyntax();
                },
                HelpText = string.Format(Default.strMathEditingHelp,
                    string.Join(
                        ", ", App.MathConstants.Select(x =>
                        string.Format("{0} - {1}", x.getConstantName(), x.getDescription()))
                        ),
                    string.Join(
                        ", ", App.MathArguments.Select(x => x.getArgumentName())
                        )
                    )
            };
            dialog.SetDefaultInputValue(Settings.Default.ChannelMathYMapping);
            if (dialog.ShowDialog() ?? false)
            {
                Settings.Default.ChannelMathYMapping = dialog.ParsedInput;
                App.LoadMathSettings();
                foreach (var item in App.AdcChannels.Values)
                {
                    item.MathExpressionY = App.ChannelMathY[item.Code];
                }
                await RecalculateChannels();
            }
        }

        #endregion
    }
}
