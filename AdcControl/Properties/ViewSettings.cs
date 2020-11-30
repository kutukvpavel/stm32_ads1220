using System;
using System.ComponentModel;
using System.Configuration;
using AdcControl.Resources;

namespace AdcControl.Properties
{
    [Serializable]
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class ViewSettings
    {
        [Browsable(false)]
        public static class ResourseKeys
        {
            public static class Categories
            {
                public const string MainWindow = "catMainWindow";
                public const string Axes = "catAxes";
                public const string Terminal = "catTerminal";
                public const string Plot = "catPlot";
                public const string RealtimeTable = "catRealTimeTable";

            }
            public static class Descriptions
            {
                public const string WindowState = "desWindowState";
                public const string WindowLocation = "desWindowCoords";
                public const string TerminalLimit = "desTerminalLimit";
                public const string TerminalRemoveStep = "desTerminalRemoveStep";
                public const string RealtimeTableLimit = "desRealTimeTableLimit";
                public const string PlotRefreshPeriod = "desPlotRefreshPeriod";
                public const string MouseRefreshPeriod = "desMouseRefreshPeriod";
                public const string RealtimeTableDrop = "desDropPoints";
            }
            public static class Names
            {
                public const string TerminalLimit = "namLimit";
                public const string TerminalRemoveStep = "namTerminalRemoveStep";
                public const string WindowState = "namWindowMaximized";
                public const string WindowLocation = "namWindowCoords";
                public const string WindowSize = "namWindowSize";
                public const string EnableAutoscaling = "namEnableAutoscaling";
                public const string LockVerticalAxis = "strLockVerticalAxis";
                public const string LockHorizontalAxis = "strLockHorizontalAxis";
                public const string YMax = "namYMax";
                public const string YMin = "namYMin";
                public const string XMax = "namXMax";
                public const string XMin = "namXMin";
                public const string LineWidth = "namLineWidth";
                public const string YAxisLabel = "namYAxisLabel";
                public const string XAxisLabel = "namXAxisLabel";
                public const string AutoscrollTable = "namEnableAutoscroll";
                public const string TableLimit = "namLimit";
                public const string PlotRefreshPeriod = "namPlotRefreshPeriod";
                public const string MouseRefreshPeriod = "namMouseRefreshPeriod";
                public const string TableDropPoints = "namDropPoints";
            }
        }

        public ViewSettings()
        {
            MainWindowSize = new System.Drawing.Size(750, 200);
            Maximized = false;
            MainWindowLocation = new System.Drawing.Point(10, 10);
            TerminalLimit = 1500;
            TerminalRemoveStep = 100;
            EnableAutoscaling = true;
            LockVerticalScale = false;
            YMax = 2;
            YMin = -2;
            LineWidth = 1;
            LockHorizontalAxis = false;
            XAxisLabel = Default.strDefaultXAxisLabel;
            YAxisLabel = Default.strDefaultYAxisLabel;
            XMax = 1;
            XMin = 0;
            AutoscrollTable = true;
            TableLimit = 1200;
            RefreshPeriod = 500;
            MouseRefreshPeriod = 20;
            TableDropPoints = 2;
        }

        [LocalizedCategory(ResourseKeys.Categories.MainWindow)]
        [LocalizedDisplayName(ResourseKeys.Names.WindowSize)]
        public System.Drawing.Size MainWindowSize { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.MainWindow)]
        [LocalizedDescription(ResourseKeys.Descriptions.WindowState)]
        [LocalizedDisplayName(ResourseKeys.Names.WindowState)]
        public bool Maximized { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.MainWindow)]
        [LocalizedDescription(ResourseKeys.Descriptions.WindowLocation)]
        [LocalizedDisplayName(ResourseKeys.Names.WindowLocation)]
        public System.Drawing.Point MainWindowLocation { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.Terminal)]
        [LocalizedDescription(ResourseKeys.Descriptions.TerminalLimit)]
        [LocalizedDisplayName(ResourseKeys.Names.TerminalLimit)]
        public int TerminalLimit { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.Terminal)]
        [LocalizedDescription(ResourseKeys.Descriptions.TerminalRemoveStep)]
        [LocalizedDisplayName(ResourseKeys.Names.TerminalRemoveStep)]
        public int TerminalRemoveStep { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.Axes)]
        [LocalizedDisplayName(ResourseKeys.Names.EnableAutoscaling)]
        public bool EnableAutoscaling { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.Axes)]
        [LocalizedDisplayName(ResourseKeys.Names.LockVerticalAxis)]
        public bool LockVerticalScale { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.Axes)]
        [LocalizedDisplayName(ResourseKeys.Names.YMax)]
        public double YMax { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.Axes)]
        [LocalizedDisplayName(ResourseKeys.Names.YMin)]
        public double YMin { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.Plot)]
        [LocalizedDisplayName(ResourseKeys.Names.LineWidth)]
        public double LineWidth { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.Axes)]
        [LocalizedDisplayName(ResourseKeys.Names.LockHorizontalAxis)]
        public bool LockHorizontalAxis { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.Axes)]
        [LocalizedDisplayName(ResourseKeys.Names.XAxisLabel)]
        public string XAxisLabel { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.Axes)]
        [LocalizedDisplayName(ResourseKeys.Names.YAxisLabel)]
        public string YAxisLabel { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.Axes)]
        [LocalizedDisplayName(ResourseKeys.Names.XMax)]
        public double XMax { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.Axes)]
        [LocalizedDisplayName(ResourseKeys.Names.XMin)]
        public double XMin { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.RealtimeTable)]
        [LocalizedDisplayName(ResourseKeys.Names.AutoscrollTable)]
        public bool AutoscrollTable { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.RealtimeTable)]
        [LocalizedDescription(ResourseKeys.Descriptions.RealtimeTableLimit)]
        [LocalizedDisplayName(ResourseKeys.Names.TableLimit)]
        public int TableLimit { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.Plot)]
        [LocalizedDescription(ResourseKeys.Descriptions.PlotRefreshPeriod)]
        [LocalizedDisplayName(ResourseKeys.Names.PlotRefreshPeriod)]
        public int RefreshPeriod { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.Plot)]
        [LocalizedDescription(ResourseKeys.Descriptions.MouseRefreshPeriod)]
        [LocalizedDisplayName(ResourseKeys.Names.MouseRefreshPeriod)]
        public int MouseRefreshPeriod { get; set; }
        [LocalizedCategory(ResourseKeys.Categories.RealtimeTable)]
        [LocalizedDescription(ResourseKeys.Descriptions.RealtimeTableDrop)]
        [LocalizedDisplayName(ResourseKeys.Names.TableDropPoints)]
        public int TableDropPoints { get; set; }
    }
}
