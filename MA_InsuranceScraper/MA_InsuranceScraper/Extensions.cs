using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace MA_InsuranceScraper
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

        public static string UrlEncode(this string val)
        {
            return WebUtility.UrlEncode(val);
        }

        public static string CleanupString(this string value)
        {
            value = value.Replace("\t", " ")
                         .Replace("\r", " ")
                         .Replace("\n", " ")
                         .Replace("&nbsp;", " ")
                         .Replace(",", ".");

            var multiplySpacesReg = new Regex(@"\s+");
            return multiplySpacesReg.Replace(value, " ").Trim();
        }

        public static int? ToInt(this string value)
        {
            int intVal;
            return int.TryParse(value, out intVal)
                        ? (int?)intVal
                        : null;
        }
    }
}