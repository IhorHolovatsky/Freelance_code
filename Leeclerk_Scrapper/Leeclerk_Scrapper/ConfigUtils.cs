using System;
using System.Configuration;
using System.Linq;

namespace Leeclerk_Scrapper
{
    public static class ConfigUtils
    {
        public static T GetAppSettingValue<T>(AppSettings key, T defaultValue = default(T))
        {
            return GetAppSettingValue(key.ToString(), defaultValue);
        }


        public static T GetAppSettingValue<T>(string key, T defaultValue = default(T))
        {
            if (ConfigurationManager.AppSettings.AllKeys.Contains(key))
            {
                var value = ConfigurationManager.AppSettings[key];

                return (T)Convert.ChangeType(value, typeof(T));
            }

            return defaultValue;
        }
    }
}