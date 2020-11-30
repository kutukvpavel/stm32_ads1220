using AdcControl.Properties;
using AdcControl.Resources;
using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AdcControl
{
    public static class CsvExporter
    {
        static CsvExporter()
        {
            Configuration = new CsvConfiguration(CultureInfo.InvariantCulture);
        }

        //Public

        public static CsvConfiguration Configuration { get; set; }

        public static bool CheckIfAlreadyExists(string experimentName)
        {
            return File.Exists(ComputePath(Settings.ExportSettings.CsvSavePath, experimentName, false));
        }
        public static bool CheckIfAlreadyExists(string experimentName, AdcChannel channel)
        {
            return File.Exists(ComputePath(
                Settings.ExportSettings.CsvSavePath, 
                FormatSingleChannel(experimentName, channel), 
                false
                ));
        }

        public static async Task<bool> Export(string experimentName, IEnumerable<AdcChannel> data)
        {
            return await Task.Run(() => 
            {
                return ExportEngine(
                    Settings.ExportSettings.CsvSavePath,
                    experimentName,
                    true,
                    data.Where(x => x.IsVisible).ToArray()
                    );
            });
        }

        public static async Task<bool> ExportSingleChannel(string experimentName, AdcChannel data)
        {
            return await Task.Run(() =>
            {
                return ExportEngine(
                    Settings.ExportSettings.CsvSavePath,
                    FormatSingleChannel(experimentName, data),
                    true,
                    data
                    );
            });
        }

        public static bool AutoSave(IEnumerable<AdcChannel> data)
        {
            Trace("Autosave invoked");
            try
            {
                IEnumerable<FileSystemInfo> files = new DirectoryInfo(Path.GetFullPath(
                        Environment.CurrentDirectory + Settings.ExportSettings.CsvAutosavePath)
                    ).GetFileSystemInfos().OrderByDescending(x => x.CreationTime)
                    .Skip(Settings.ExportSettings.AutosaveFileLimit);
                foreach (var item in files)
                {
                    File.Delete(item.FullName);
                    App.Logger.Info(Default.msgDeletedOldAutosave);
                    App.Logger.Info(item.FullName);
                }
            }
            catch (Exception ex)
            {
                App.Logger.Error(Default.msgCantDeleteOldAutosave);
                App.Logger.Info(ex.ToString());
            }
            return ExportEngine(
                Settings.ExportSettings.CsvAutosavePath,
                "Autosave_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"),
                false,
                data.ToArray()
                );
        }

        public static double OADateToSeconds(double oa)
        {
            return DateTime.FromOADate(oa).TimeOfDay.TotalSeconds;
        }

        //Private
#if TRACE
        private static readonly BlockingCollectionQueue TraceQueue = new BlockingCollectionQueue();
#endif

        private static void Trace(string s)
        {
#if TRACE
            TraceQueue.Enqueue(() => { System.Diagnostics.Trace.WriteLine(string.Format("{0:mm.ss.ff} {1}", DateTime.UtcNow, s)); });
#endif
        }
        private static string FormatSingleChannel(string experimentName, AdcChannel channel)
        {
            return string.Format("{0}_{1}", experimentName, channel.Name);
        }
        private static string ComputePath(string relativePath, string experimentName, bool autoDir = true, string extension = ".csv")
        {
            try
            {
                string path = Path.Combine(
                    Path.GetFullPath(Environment.CurrentDirectory + relativePath),
                    experimentName + extension
                    );
                string dir = Path.GetDirectoryName(path);
                if (autoDir && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return path;
            }
            catch (Exception ex)
            {
                App.Logger.Error(Default.msgCantCreateCsvDirectory);
                App.Logger.Info(ex);
                return null;
            }
        }
        private static bool ExportEngine(string relativePath, string experimentName, bool exportInfo, params AdcChannel[] data)
        {
            //Get access to the file
            string path = ComputePath(relativePath, experimentName);
            if (path == null) return false;
            //Build required structures
            string[] columnsPerChannel = new string[]
            {
                Settings.ExportSettings.RawXName,
                Settings.ExportSettings.RawYName,
                Settings.ExportSettings.CalculatedXName,
                Settings.ExportSettings.CalculatedYName
            };
            string[] topHeaders = new string[data.Length * columnsPerChannel.Length];
            for (int i = 0; i < topHeaders.Length; i++)
            {
                topHeaders[i] = i % columnsPerChannel.Length == 0 ? data[i / columnsPerChannel.Length].Name : null;
            }
            int maxCount = Math.Max(data.Max(x => x.CalculatedCount), data.Max(x => x.RawCount));
            //Write to the file
            try
            {
                using var writer = new StreamWriter(path, false);
                using var csvWriter = new CsvWriter(writer, Configuration);
                //Write headers
                for (int i = 0; i < topHeaders.Length; i++)
                {
                    csvWriter.WriteField(topHeaders[i]);
                }
                csvWriter.NextRecord();
                for (int i = 0; i < topHeaders.Length; i++)
                {
                    csvWriter.WriteField(columnsPerChannel[i % columnsPerChannel.Length]);
                }
                csvWriter.NextRecord();
                //Write data
                for (int i = 0; i < maxCount; i++)
                {
                    for (int j = 0; j < data.Length; j++)
                    {
                        if (data[j].RawCount > i)
                        {
                            csvWriter.WriteField(OADateToSeconds(data[j].RawX[i]));
                            csvWriter.WriteField(data[j].RawY[i]);
                        }
                        else
                        {
                            csvWriter.WriteField(null);
                            csvWriter.WriteField(null);
                        }
                        if (data[j].CalculatedCount > i)
                        {
                            csvWriter.WriteField(OADateToSeconds(data[j].CalculatedX[i]));
                            csvWriter.WriteField(data[j].CalculatedY[i]);
                        }
                        else
                        {
                            csvWriter.WriteField(null);
                            csvWriter.WriteField(null);
                        }
                    }
                    csvWriter.NextRecord();
                }
            }
            catch (Exception ex)
            {
                App.Logger.Error(Default.msgCsvSaveFailed);
                App.Logger.Info(ex);
                return false;
            }
            App.Logger.Info(Default.msgSuccessfullyExportedToFile);
            App.Logger.Info(path);
            if (exportInfo)
            {
                try
                {
                    path = ComputePath(relativePath, experimentName, false, ".txt");
                    if (path == null) return false;
                    using TextWriter writer = new StreamWriter(path);
                    foreach (var item in data)
                    {
                        writer.WriteLine(string.Format(Settings.ExportSettings.ChannelInfoFormat,
                            item.Code, item.Name,
                            DateTime.FromOADate(item.StartTime),
                            item.MovingAveraging, item.DropPoints));
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.Error(Default.msgFailedToSaveInfo);
                    App.Logger.Info(ex.ToString());
                    return false;
                }
            }
            return true;
        }
    }
}
