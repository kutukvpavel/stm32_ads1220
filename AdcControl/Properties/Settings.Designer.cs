﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.42000
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AdcControl.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.7.0.0")]
    public sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("COM47")]
        public string PortName {
            get {
                return ((string)(this["PortName"]));
            }
            set {
                this["PortName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public int Average {
            get {
                return ((int)(this["Average"]));
            }
            set {
                this["Average"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("5")]
        public int AcquisitionDuration {
            get {
                return ((int)(this["AcquisitionDuration"]));
            }
            set {
                this["AcquisitionDuration"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("4")]
        public float AcquisitionSpeed {
            get {
                return ((float)(this["AcquisitionSpeed"]));
            }
            set {
                this["AcquisitionSpeed"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ArrayOfString xmlns:xsi=\"http://www.w3." +
            "org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <s" +
            "tring>0=AIN0_AIN1</string>\r\n  <string>50=AIN2_AIN3</string>\r\n</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection ChannelNameMapping {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["ChannelNameMapping"]));
            }
            set {
                this["ChannelNameMapping"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ArrayOfString xmlns:xsi=\"http://www.w3." +
            "org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <s" +
            "tring>0=True</string>\r\n  <string>50=True</string>\r\n</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection ChannelEnableMapping {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["ChannelEnableMapping"]));
            }
            set {
                this["ChannelEnableMapping"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("-1")]
        public int AcqDropPoints {
            get {
                return ((int)(this["AcqDropPoints"]));
            }
            set {
                this["AcqDropPoints"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::AdcControl.Properties.ViewSettings ViewSettingsBuffer {
            get {
                return ((global::AdcControl.Properties.ViewSettings)(this["ViewSettingsBuffer"]));
            }
            set {
                this["ViewSettingsBuffer"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::AdcControl.Properties.ExportSettings ExportSettingsBuffer {
            get {
                return ((global::AdcControl.Properties.ExportSettings)(this["ExportSettingsBuffer"]));
            }
            set {
                this["ExportSettingsBuffer"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ArrayOfString xmlns:xsi=\"http://www.w3." +
            "org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <s" +
            "tring>0=-47872</string>\r\n  <string>50=-16777011</string>\r\n</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection Colorset {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["Colorset"]));
            }
            set {
                this["Colorset"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ArrayOfString xmlns:xsi=\"http://www.w3." +
            "org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <s" +
            "tring>0=y</string>\r\n  <string>50=y</string>\r\n</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection ChannelMathYMapping {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["ChannelMathYMapping"]));
            }
            set {
                this["ChannelMathYMapping"] = value;
            }
        }
    }
}
