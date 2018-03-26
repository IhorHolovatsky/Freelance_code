using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace CNA_Scraper.Extensions
{
    public static class DictionaryExtensions
    {
        public static string FormUrlEncodedSerialize(this Dictionary<string, string> data)
        {
            return string.Join("&", data.Select(d => $"{d.Key}={WebUtility.UrlEncode(d.Value)}"));
        }
    }
}