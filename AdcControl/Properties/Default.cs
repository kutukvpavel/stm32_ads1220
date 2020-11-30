using System;

namespace AdcControl.Properties
{
    public sealed partial class Settings : System.Configuration.ApplicationSettingsBase
    {
        public static ExportSettings ExportSettings
        {
            get
            {
                if (defaultInstance.ExportSettingsBuffer == null)
                {
                    defaultInstance.ExportSettingsBuffer = new ExportSettings();
                }
                return defaultInstance.ExportSettingsBuffer;
            }
        }

        public static ViewSettings ViewSettings
        {
            get
            {
                if (defaultInstance.ViewSettingsBuffer == null)
                {
                    defaultInstance.ViewSettingsBuffer = new ViewSettings();
                }
                return defaultInstance.ViewSettingsBuffer;
            }
        }
    }
}