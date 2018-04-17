using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace TurfClubScrapper.Extensions
{
    public static class StringExtensions
    {
        public static string UrlEncodedSerialize(this Dictionary<string, string> data)
        {
            return string.Join("&", data.Select(d => $"{WebUtility.UrlEncode(d.Key)}={WebUtility.UrlEncode(d.Value)}"));
        }

        public static DateTime? ParseDateTime(this string data)
        {
            DateTime dateTime;
            return DateTime.TryParse(data, out dateTime) 
                            ? (DateTime?)dateTime 
                            : null;
        }

        public static int? ParseInt(this string data)
        {
            int intValue;
            return int.TryParse(data, out intValue)
                            ? (int?)intValue
                            : null;
        }
    }
}