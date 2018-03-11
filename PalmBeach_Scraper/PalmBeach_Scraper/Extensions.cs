using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace PalmBeach_Scraper
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

        public static string CleanupString(this string value)
        {
            value = value.Replace("\t", " ")
                         .Replace("\r", " ")
                         .Replace("\n", " ")
                         .Replace("&nbsp;", " ");

            var multiplySpacesReg = new Regex(@"\s+");
            return multiplySpacesReg.Replace(value, " ").Trim();
        }
    }
}