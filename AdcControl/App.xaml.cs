using AdcControl.Properties;
using AdcControl.Resources;
using LLibrary;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;

namespace AdcControl
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static L Logger { get; } = new L(
            deleteOldFiles: TimeSpan.FromDays(3),
            directory: Path.Combine(Environment.CurrentDirectory, "AdcControlLogs"),
            enabledLabels: new string[] { "INFO", "WARN", "ERROR", "FATAL"
#if DEBUG
                , "DEBUG"
#endif
            }
            );

        public static event EventHandler<NewChannelDetectedEventArgs> NewChannelDetected;

        /// <summary>
        /// Do not make this property automatic (possible data binding reasons)!
        /// </summary>
        public static Controller Stm32Ads1220 { get { return _Stm32Ads1220; } }

        public static System.Timers.Timer AutosaveTimer { get; set; }

        public static List<ConcurrentDictionary<int, AdcChannel>> ArchivedChannels { get; set; }
        public static ConcurrentDictionary<int, AdcChannel> AdcChannels { get; set; }
        
        public static void InitGlobals()
        {
            AdcChannels = new ConcurrentDictionary<int, AdcChannel>();
            ArchivedChannels = new List<ConcurrentDictionary<int, AdcChannel>>();
            _Stm32Ads1220 = new Controller(new RJCP.IO.Ports.SerialPortStream());
            _Stm32Ads1220.Port.PortName = Settings.Default.PortName;
            _Stm32Ads1220.LogEvent += (sender, e) => { Logger.Error(e.Message); if (e.Exception != null) Logger.Info(e.Exception); };
            _Stm32Ads1220.TerminalEvent += (sender, e) => { Logger.Debug(e.Line); };
            _Stm32Ads1220.AcquisitionDataReceived += Stm32Ads1220_AcquisitionDataReceived;
            _Stm32Ads1220.DeviceError += Stm32Ads1220_DeviceError;
            _Stm32Ads1220.DataError += Stm32Ads1220_DataError;
            AutosaveTimer = new System.Timers.Timer(Settings.Default.AutosaveInterval * 1000)
            {
                AutoReset = true,
                Enabled = false
            };
            AutosaveTimer.Elapsed += AutosaveTimer_Elapsed;
        }

        public static void ConfigureCsvExporter()
        {
            var c = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            c.NumberFormat.NumberDecimalSeparator = Settings.Default.RussianExcelCompatible ? "," : ".";
            CsvExporter.Configuration.CultureInfo = c;
            CsvExporter.Configuration.Delimiter = Settings.Default.RussianExcelCompatible ? ";" : ",";
            CsvExporter.Configuration.NewLine = CsvHelper.Configuration.NewLine.Environment;
        }

        private static Controller _Stm32Ads1220;
#if TRACE
        private static BlockingCollectionQueue TraceQueue = new BlockingCollectionQueue();
#endif

        private static void Trace(string s)
        {
#if TRACE
            TraceQueue.Enqueue(() => { System.Diagnostics.Trace.WriteLine(string.Format("{0:mm.ss.ff} {1}", DateTime.UtcNow, s)); });
#endif
        }

        private static void AutosaveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!Stm32Ads1220.AcquisitionInProgress) AutosaveTimer.Stop();
            if (AdcChannels.Values.Count > 0)
            {
                new Thread(() => { CsvExporter.AutoSave(AdcChannels.Values); }).Start();
            }
        }

        private static void Stm32Ads1220_DataError(object sender, DataErrorEventArgs e)
        {
            Logger.Error(Default.msgParsingProblem);
            Logger.Warn(e.Data);
            Logger.Info(e.Exception);
        }

        private static void Stm32Ads1220_DeviceError(object sender, TerminalEventArgs e)
        {
            Logger.Warn(e.Line);
        }

        private static void Stm32Ads1220_AcquisitionDataReceived(object sender, AcquisitionEventArgs e)
        {
            Trace("App DataReceived");
            if (!AdcChannels.ContainsKey(e.Channel))
            {
                bool added = AdcChannels.TryAdd(
                    e.Channel,
                    new AdcChannel(e.Channel,
                        (int)Math.Ceiling(Settings.Default.AcquisitionDuration * Settings.Default.AcquisitionSpeed),
                        Settings.Default.Average, Settings.Default.SampleRate
                        )
                    );
                if (added)
                {
                    AdcChannels[e.Channel].DropPoints = Settings.Default.AcqDropPoints;
                    new Thread(() =>
                    {
                        NewChannelDetected?.Invoke(Stm32Ads1220, new NewChannelDetectedEventArgs(e.Channel));
                    }).Start();
                }
                else
                {
                    Logger.Error(Default.msgAdcChannelConcurrency);
                    return;
                }
            }
            AdcChannels[e.Channel].AddPoint(e.Value);
            if (!AutosaveTimer.Enabled) AutosaveTimer.Start();
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Logger.Fatal(e.Exception);
            }
            catch (Exception) { }
            finally
            {
                if (MessageBox.Show(e.Exception.ToString(), Default.msgFatalContinue, MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    Environment.Exit(-1);
                }
                e.Handled = true;
                try
                {
                    Logger.Info(Default.msgExceptionOverride);
                }
                catch (Exception) { }
            }
        }
    }
    
    public class NewChannelDetectedEventArgs : EventArgs
    {
        public int Code { get; }
        public NewChannelDetectedEventArgs(int code)
        {
            Code = code;
        }
    }
}
