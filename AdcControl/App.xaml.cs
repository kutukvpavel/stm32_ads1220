using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LLibrary;
using System.IO;
using AdcControl.Properties;
using AdcControl.Resources;

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

        public static Controller Stm32Ads1220 { get; set; }

        public static Dictionary<int, AdcChannel> AdcChannels { get; set; }

        public static void InitGlobals()
        {
            Stm32Ads1220 = new Controller(new RJCP.IO.Ports.SerialPortStream());
            Stm32Ads1220.Port.PortName = Settings.Default.PortName;
            Stm32Ads1220.LogEvent += (sender, e) => { Logger.Error(e.Message); Logger.Info(e.Exception); };
            Stm32Ads1220.TerminalEvent += (sender, e) => { Logger.Debug(e.Line); };
            Stm32Ads1220.AcquisitionDataReceived += Stm32Ads1220_AcquisitionDataReceived;
            Stm32Ads1220.DeviceError += Stm32Ads1220_DeviceError;
            Stm32Ads1220.DataError += Stm32Ads1220_DataError;
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
            if (!AdcChannels.ContainsKey(e.Channel))
                AdcChannels.Add(
                    e.Channel,
                    new AdcChannel(
                        (int)Math.Ceiling(Settings.Default.AcquisitionDuration * Settings.Default.AcquisitionSpeed),
                        Settings.Default.Average
                        )
                    );
            AdcChannels[e.Channel].AddPoint(e.Value);
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
                if (MessageBox.Show(e.ToString(), "Continue?", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    Environment.Exit(-1);
                }
                e.Handled = true;
                try
                {
                    Logger.Info(AdcControl.Resources.Default.msgExceptionOverride);
                }
                catch (Exception) { }
            }
        }
    }
}
