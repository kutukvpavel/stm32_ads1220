using System;
using System.Configuration;
using System.ComponentModel;
using AdcControl.Resources;

namespace AdcControl.Properties
{
    [Serializable]
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class ExportSettings
    {
        [Browsable(false)]
        public static class ResourceKeys
        {
            public static class Names
            {
                public const string Path = "namPath";
                public const string RussianCompatibility = "namRussianCompatibility";
                public const string Period = "namPeriod";
                public const string Limit = "namLimit";
                public const string RawY = "namExportRawY";
                public const string RawX = "namExportRawX";
                public const string CalculatedY = "namExportCalculatedY";
                public const string CalculatedX = "namExportCalculatedX";
                public const string ChannelInfoFormat = "namChannelInfoFormat";
            }
            public static class Descriptions
            {
                public const string AutosavePeriod = "desAutosavePeriod";
                public const string AutosaveLimit = "desAutosaveLimit";
                public const string RelativePath = "desRelativePath";
                public const string RussianCompatibility = "desRussianCompatibility";
                public const string ChannelInfoFormat = "desChannelInfoFormat";
            }
            public static class Categories
            {
                public const string General = "catGeneral";
                public const string Autosave = "catAutosave";
            }
        }

        public ExportSettings()
        {
            CsvSavePath = @"\AdcControlExports";
            CsvAutosavePath = @"\AdcControlAutosaves";
            RussianExcelCompatible = false;
            AutosaveFileLimit = 4;
            AutosaveInterval = 60;
            RawXName = Default.strDefaultRawX;
            RawYName = Default.strDefaultRawY;
            CalculatedXName = Default.strDefaultCalcX;
            CalculatedYName = Default.strDefaultCalcY;
            ChannelInfoFormat = Default.strDefaultChannelInfoFormat;
        }

        [LocalizedCategory(ResourceKeys.Categories.General)]
        [LocalizedDescription(ResourceKeys.Descriptions.RelativePath)]
        [LocalizedDisplayName(ResourceKeys.Names.Path)]
        public string CsvSavePath { get; set; }
        [LocalizedCategory(ResourceKeys.Categories.General)]
        [LocalizedDescription(ResourceKeys.Descriptions.RussianCompatibility)]
        [LocalizedDisplayName(ResourceKeys.Names.RussianCompatibility)]
        public bool RussianExcelCompatible { get; set; }
        [LocalizedDescription(ResourceKeys.Descriptions.RelativePath)]
        [LocalizedCategory(ResourceKeys.Categories.Autosave)]
        [LocalizedDisplayName(ResourceKeys.Names.Path)]
        public string CsvAutosavePath { get; set; }
        [LocalizedCategory(ResourceKeys.Categories.Autosave)]
        [LocalizedDescription(ResourceKeys.Descriptions.AutosavePeriod)]
        [LocalizedDisplayName(ResourceKeys.Names.Period)]
        public int AutosaveInterval { get; set; }
        [LocalizedCategory(ResourceKeys.Categories.Autosave)]
        [LocalizedDescription(ResourceKeys.Descriptions.AutosaveLimit)]
        [LocalizedDisplayName(ResourceKeys.Names.Limit)]
        public int AutosaveFileLimit { get; set; }
        [LocalizedCategory(ResourceKeys.Categories.General)]
        [LocalizedDisplayName(ResourceKeys.Names.RawX)]
        public string RawXName { get; set; }
        [LocalizedCategory(ResourceKeys.Categories.General)]
        [LocalizedDisplayName(ResourceKeys.Names.RawY)]
        public string RawYName { get; set; }
        [LocalizedCategory(ResourceKeys.Categories.General)]
        [LocalizedDisplayName(ResourceKeys.Names.CalculatedX)]
        public string CalculatedXName { get; set; }
        [LocalizedCategory(ResourceKeys.Categories.General)]
        [LocalizedDisplayName(ResourceKeys.Names.CalculatedY)]
        public string CalculatedYName { get; set; }
        [LocalizedCategory(ResourceKeys.Categories.General)]
        [LocalizedDescription(ResourceKeys.Descriptions.ChannelInfoFormat)]
        [LocalizedDisplayName(ResourceKeys.Names.ChannelInfoFormat)]
        public string ChannelInfoFormat { get; set; }
    }
}