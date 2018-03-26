using System.Text.RegularExpressions;

namespace CNA_Scraper.Extensions
{
    public static class StringExtensions
    {
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