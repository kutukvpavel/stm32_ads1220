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
using org.mariuszgromada.math.mxparser;
using Expression = org.mariuszgromada.math.mxparser.Expression;
using System.Linq;
using System.Drawing;

namespace AdcControl
{
    public partial class App : Application
    {
        /* Public */

        public static event EventHandler<NewChannelDetectedEventArgs> NewChannelDetected;

        #region Properties

        public static L Logger { get; } = new L(
            deleteOldFiles: TimeSpan.FromDays(3),
            directory: Path.Combine(Environment.CurrentDirectory, "AdcControlLogs"),
            enabledLabels: new string[] { "INFO", "WARN", "ERROR", "FATAL"
#if DEBUG
                , "DEBUG"
#endif
            }
            );
        
        /// <summary>
        /// Do not make this property automatic (possible data binding reasons)!
        /// </summary>
        public static Controller Stm32Ads1220 { get { return _Stm32Ads1220; } }
        public static System.Timers.Timer AutosaveTimer { get; private set; }
        public static List<ConcurrentDictionary<int, AdcChannel>> ArchivedChannels { get; private set; }
        public static ConcurrentDictionary<int, AdcChannel> AdcChannels { get; private set; }
        public static Constant[] MathConstants { get; } = new Constant[]
        {
            new Constant("F", 96485.3, Default.strFaradaysConstant),
            new Constant("R", 8.31446, Default.strIdealGasConstant)
        };
        public static Argument[] MathArguments { get; } = new Argument[]
        {
            new Argument("x"),
            new Argument("y")
        };
        public static PrimitiveElement[] MathElements
        {
            get
            {
                return MathConstants.Cast<PrimitiveElement>().Concat(MathArguments.Cast<PrimitiveElement>()).ToArray();
            }
        }
        public static ConcurrentDictionary<int, string> ChannelNames { get; private set; }
        public static ConcurrentDictionary<int, bool> ChannelEnable { get; private set; }
        public static ConcurrentDictionary<int, Expression> ChannelMathY { get; private set; }
        public static ConcurrentDictionary<int, Color?> ColorSet { get; private set; }

        #endregion

        #region Public Methods

        public static void LoadChannelNames()
        {
            ChannelNames = DictionarySerializer.Parse(Settings.Default.ChannelNameMapping, x => x);
        }

        public static void SaveChannelNames()
        {
            DictionarySerializer.Save(Settings.Default.ChannelNameMapping, ChannelNames, x => x);
        }

        public static void LoadMathSettings()
        {
            ChannelMathY = DictionarySerializer.Parse(Settings.Default.ChannelMathYMapping, (x) =>
            {
                return new Expression(x ?? "y", App.MathElements);
            }, false);
        }

        public static void SaveMathSettings()
        {
            DictionarySerializer.Save(Settings.Default.ChannelMathYMapping, ChannelMathY, (x) =>
            {
                return x != null ? x.getExpressionString() : "";
            });
        }

        public static void LoadColorSet()
        {
            ColorSet = DictionarySerializer.Parse(Settings.Default.Colorset, (x) =>
            {
                return x != null ? (Color?)Color.FromArgb(int.Parse(x)) : null;
            }, false);
        }

        public static void SaveColorSet()
        {
            DictionarySerializer.Save(Settings.Default.Colorset, ColorSet, (x) =>
            {
                return x.HasValue ? x.Value.ToArgb().ToString() : "";
            });
        }

        public static void LoadChannelEnableMapping()
        {
            ChannelEnable = DictionarySerializer.Parse(Settings.Default.ChannelEnableMapping, x => bool.Parse(x), false);
        }

        public static void SaveChannelEnableMapping()
        {
            DictionarySerializer.Save(Settings.Default.ChannelEnableMapping, ChannelEnable, x => x.ToString());
        }

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
            AutosaveTimer = new System.Timers.Timer(Settings.ExportSettings.AutosaveInterval * 1000)
            {
                AutoReset = true,
                Enabled = false
            };
            AutosaveTimer.Elapsed += AutosaveTimer_Elapsed;
        }

        public static void ConfigureCsvExporter()
        {
            var c = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            c.NumberFormat.NumberDecimalSeparator = Settings.ExportSettings.RussianExcelCompatible ? "," : ".";
            CsvExporter.Configuration.CultureInfo = c;
            CsvExporter.Configuration.Delimiter = Settings.ExportSettings.RussianExcelCompatible ? ";" : ",";
            CsvExporter.Configuration.NewLine = CsvHelper.Configuration.NewLine.Environment;
            CsvExporter.AutosaveFileLimit = Settings.ExportSettings.AutosaveFileLimit;
            CsvExporter.AutosavePath = Settings.ExportSettings.CsvAutosavePath;
            CsvExporter.ChannelInfoFormat = Settings.ExportSettings.ChannelInfoFormat;
            CsvExporter.ExportPath = Settings.ExportSettings.CsvSavePath;
            CsvExporter.ChannelColumnNames = new string[]
            {
                Settings.ExportSettings.RawXName,
                Settings.ExportSettings.RawYName,
                Settings.ExportSettings.CalculatedXName,
                Settings.ExportSettings.CalculatedYName
            };
        }

        #endregion

        /* Private */

        private static Controller _Stm32Ads1220;
#if TRACE
        private static readonly BlockingCollectionQueue TraceQueue = new BlockingCollectionQueue();
#endif

        #region Private Methods

        private static void Trace(string s)
        {
#if TRACE
            TraceQueue.Enqueue(() => { System.Diagnostics.Trace.WriteLine(string.Format("{0:mm.ss.ff} {1}", DateTime.UtcNow, s)); });
#endif
        }

        private static void AutosaveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            AutosaveTimer.Interval = Settings.ExportSettings.AutosaveInterval * 1000;
            if (!Stm32Ads1220.AcquisitionInProgress) AutosaveTimer.Stop();
            if (AdcChannels.Values.Count > 0)
            {
                new Thread(() => { CsvExporter.AutoSave(AdcChannels.Values); }).Start();
            }
        }

        #endregion

        #region Controller Events

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
                        Settings.Default.Average, Settings.Default.AcquisitionSpeed
                        )
                    );
                if (added)
                {
                    AdcChannels[e.Channel].DropPoints = Settings.Default.AcqDropPoints;
                    AdcChannels[e.Channel].CalculatedXColumnSelector = x => CsvExporter.OADateToSeconds(x);
                    if (ChannelEnable.ContainsKey(e.Channel))
                    {
                        AdcChannels[e.Channel].IsVisible = ChannelEnable[e.Channel];
                    }
                    if (ChannelNames.ContainsKey(e.Channel))
                    {
                        AdcChannels[e.Channel].Name = ChannelNames[e.Channel];
                    }
                    if (ColorSet.ContainsKey(e.Channel))
                    {
                        AdcChannels[e.Channel].Color = ColorSet[e.Channel];
                    }
                    if (ChannelMathY.ContainsKey(e.Channel))
                    {
                        AdcChannels[e.Channel].MathExpressionY = ChannelMathY[e.Channel];
                    }
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

        #endregion

        #region Application Events

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            InitGlobals();
            ConfigureCsvExporter();
            LoadChannelNames();
            LoadMathSettings();
            LoadColorSet();
            LoadChannelEnableMapping();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            SaveChannelNames();
            SaveChannelEnableMapping();
            SaveColorSet();
            SaveMathSettings();
            Settings.Default.Save();
            Logger.Info(Default.msgApplicationExit);
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
                    try
                    {
                        Stm32Ads1220.Port.Close();
                        Stm32Ads1220.Port.Dispose();
                    }
                    catch (Exception)
                    { }
                    finally
                    {
                        Environment.Exit(-1);
                    }
                }
                e.Handled = true;
                try
                {
                    Logger.Info(Default.msgExceptionOverride);
                }
                catch (Exception) { }
            }
        }

        #endregion
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
