using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;

namespace Leads_scraper
{
    public static class Extensions
    {
        public static void InvokeEx<T>(this T @this, Action<T> action) where T : ISynchronizeInvoke
        {
            if (@this.InvokeRequired)
            {
                @this.Invoke(action, new object[] { @this });
            }
            else
            {
                action(@this);
            }
        }

        public static string FormUrlEncodedSerialize(this Dictionary<string, string> data)
        {
            return string.Join("&", data.Select(d => $"{WebUtility.UrlEncode(d.Key)}={WebUtility.UrlEncode(d.Value)}"));
        }

        public static int ToInt(this string value)
        {
            var intValue = 0;
            int.TryParse(value, out intValue);

            return intValue;
        }
    }
}