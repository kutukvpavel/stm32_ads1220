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

        #region Properties

        public static CsvConfiguration Configuration { get; set; }

        public static string ExportPath { get; set; } = "";

        public static string AutosavePath { get; set; } = "";

        public static int AutosaveFileLimit { get; set; } = 5;

        public static string ChannelInfoFormat { get; set; } = Default.strDefaultChannelInfoFormat;

        public static string[] ChannelColumnNames { get; set; } = new string[] 
        { 
            Default.strDefaultRawX,
            Default.strDefaultRawY,
            Default.strDefaultCalcX, 
            Default.strDefaultCalcY
        };

        #endregion

        public static bool CheckIfAlreadyExists(string experimentName)
        {
            return File.Exists(ComputePath(ExportPath, experimentName, false));
        }
        public static bool CheckIfAlreadyExists(string experimentName, AdcChannel channel)
        {
            return File.Exists(ComputePath(
                ExportPath, 
                FormatSingleChannel(experimentName, channel), 
                false
                ));
        }

        public static async Task<bool> Export(string experimentName, IEnumerable<AdcChannel> data)
        {
            return await Task.Run(() => 
            {
                return ExportEngine(
                    ExportPath,
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
                    ExportPath,
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
                        Environment.CurrentDirectory + AutosavePath)
                    ).GetFileSystemInfos().OrderByDescending(x => x.CreationTime)
                    .Skip(AutosaveFileLimit);
                foreach (var item in files)
                {
                    File.Delete(item.FullName);
                    Log(null, null, Default.msgDeletedOldAutosave, item.FullName);
                }
            }
            catch (Exception ex)
            {
                Log(ex, Default.msgCantDeleteOldAutosave);
            }
            return ExportEngine(
                AutosavePath,
                "Autosave_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"),
                false,
                data.ToArray()
                );
        }

        public static double OADateToSeconds(double oa)
        {
            return TimeSpan.FromTicks(DateTime.FromOADate(oa).Ticks - TicksInOLEDateMinValue).TotalSeconds;
        }

        //Private
        private static readonly long TicksInOLEDateMinValue = DateTime.FromOADate(0).Ticks;
#if TRACE
        private static readonly BlockingCollectionQueue TraceQueue = new BlockingCollectionQueue();
#endif

        private static void Log(Exception e, string err, params string[] s)
        {
            if (err != null) App.Logger.Error(err);
            foreach (var item in s)
            {
                App.Logger.Info(item);
            }
            if (e != null) App.Logger.Info(e);
        }

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
                Log(ex, Default.msgCantCreateCsvDirectory);
                return null;
            }
        }
        private static bool ExportEngine(string relativePath, string experimentName, bool exportInfo, params AdcChannel[] data)
        {
            //Get access to the file
            string path = ComputePath(relativePath, experimentName);
            if (path == null) return false;
            //Build required structures
            string[] topHeaders = new string[data.Length * ChannelColumnNames.Length];
            for (int i = 0; i < topHeaders.Length; i++)
            {
                topHeaders[i] = i % ChannelColumnNames.Length == 0 ? data[i / ChannelColumnNames.Length].Name : null;
            }
            int maxCount = Math.Max(data.Max(x => x.CalculatedCount), data.Max(x => x.RawCount));
            string rawTimeFormat = string.Format("HH{0}mm{0}ss{1}ff", 
                Configuration.CultureInfo.DateTimeFormat.TimeSeparator,
                Configuration.CultureInfo.NumberFormat.NumberDecimalSeparator
                );
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
                    csvWriter.WriteField(ChannelColumnNames[i % ChannelColumnNames.Length]);
                }
                csvWriter.NextRecord();
                //Write data
                int persistentError = 0;
                for (int i = 0; i < maxCount; i++)
                {
                    try
                    {
                        for (int j = 0; j < data.Length; j++)
                        {
                            if (data[j].RawCount > i)
                            {
                                csvWriter.WriteField(DateTime.FromOADate(data[j].RawX[i]).ToString(rawTimeFormat), false);
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
                        persistentError = 0;
                    }
                    catch (Exception ex)
                    {
                        Log(ex, Default.msgFailedToWriteCsvRecord);
                        if (++persistentError > 10) throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex, Default.msgCsvSaveFailed);
                return false;
            }
            Log(null, null, Default.msgSuccessfullyExportedToFile, path);
            if (exportInfo)
            {
                try
                {
                    path = ComputePath(relativePath, experimentName, false, ".txt");
                    if (path == null) return false;
                    using TextWriter writer = new StreamWriter(path);
                    foreach (var item in data)
                    {
                        writer.WriteLine(string.Format(ChannelInfoFormat,
                            item.Code, item.Name,
                            DateTime.FromOADate(item.StartTime),
                            item.MovingAveraging,
                            item.DropPoints > 0 ? (object)item.DropPoints : "none"));
                    }
                }
                catch (Exception ex)
                {
                    Log(ex, Default.msgFailedToSaveInfo);
                    return false;
                }
            }
            return true;
        }
    }
}
