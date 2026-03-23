using System;
using System.Configuration;
using System.Globalization;

namespace NetEaseLyricsBar.Properties
{
    internal sealed partial class Settings : ApplicationSettingsBase
    {
        private static Settings defaultInstance = ((Settings)(ApplicationSettingsBase.Synchronized(new Settings())));

        public static Settings Default
        {
            get
            {
                return defaultInstance;
            }
        }

        [UserScopedSettingAttribute()]
        [DefaultSettingValueAttribute("0")]
        public double WindowLeft
        {
            get
            {
                return ((double)(this["WindowLeft"]));
            }
            set
            {
                this["WindowLeft"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [DefaultSettingValueAttribute("0")]
        public double WindowTop
        {
            get
            {
                return ((double)(this["WindowTop"]));
            }
            set
            {
                this["WindowTop"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [DefaultSettingValueAttribute("600")]
        public double WindowWidth
        {
            get
            {
                return ((double)(this["WindowWidth"]));
            }
            set
            {
                this["WindowWidth"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [DefaultSettingValueAttribute("80")]
        public double WindowHeight
        {
            get
            {
                return ((double)(this["WindowHeight"]));
            }
            set
            {
                this["WindowHeight"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [DefaultSettingValueAttribute("false")]
        public bool IsLocked
        {
            get
            {
                return ((bool)(this["IsLocked"]));
            }
            set
            {
                this["IsLocked"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [DefaultSettingValueAttribute("Fade")]
        public string? AnimationMode
        {
            get
            {
                return ((string)(this["AnimationMode"]));
            }
            set
            {
                this["AnimationMode"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [DefaultSettingValueAttribute("SemiBold")]
        public string? FontWeight
        {
            get
            {
                return ((string)(this["FontWeight"]));
            }
            set
            {
                this["FontWeight"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [DefaultSettingValueAttribute("false")]
        public bool FontItalic
        {
            get
            {
                return ((bool)(this["FontItalic"]));
            }
            set
            {
                this["FontItalic"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [DefaultSettingValueAttribute("true")]
        public bool IsTopmost
        {
            get
            {
                return ((bool)(this["IsTopmost"]));
            }
            set
            {
                this["IsTopmost"] = value;
            }
        }
    }
}
