using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AdcControl.Properties;
using AdcControl.Resources;

namespace AdcControl
{
    public static class CsvExporter
    {
        public static async Task<bool> Export(int channel, AdcChannel data)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string path =
                        Path.Combine(Path.GetFullPath(Environment.CurrentDirectory + Settings.Default.CsvSavePath),
                        string.Format("{0}_{1}.csv", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"), channel.ToString()));
                    string dir = Path.GetDirectoryName(path);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    data.Plot.SaveCSV(path, Settings.Default.RussianExcelCompatible ? ";" : ",", Environment.NewLine);
                    return true;
                }
                catch (Exception ex)
                {
                    App.Logger.Error(Default.msgCsvSaveFailed);
                    App.Logger.Info(ex);
                }
                return false;
            });
        }
    }
}
