using System;
using System.ComponentModel;

namespace AdcControl.Properties
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.Delegate | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter)]
    class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        static string Localize(string key)
        {
            return Resources.Default.ResourceManager.GetString(key) ?? "";
        }
        public LocalizedDescriptionAttribute(string key)
            : base(Localize(key))
        { }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event)]
    class LocalizedDisplayNameAttribute : DisplayNameAttribute
    {
        static string Localize(string key)
        {
            return Resources.Default.ResourceManager.GetString(key) ?? key;
        }
        public LocalizedDisplayNameAttribute(string key)
            : base(Localize(key))
        { }
    }
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.Delegate | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter)]
    public class LocalizedCategoryAttribute : CategoryAttribute
    {
        public LocalizedCategoryAttribute(string resourceName)
            : base(resourceName)
        { }

        protected override string GetLocalizedString(string value)
        {
            return Resources.Default.ResourceManager.GetString(value) ?? base.GetLocalizedString(value);
        }
    }
}