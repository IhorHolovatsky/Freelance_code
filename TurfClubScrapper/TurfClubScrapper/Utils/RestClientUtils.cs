using System;
using System.Net;
using RestSharp;
using TurfClubScrapper.Models.Constants;

namespace TurfClubScrapper.Utils
{
    public static class RestClientUtils
    {
        public static RestClient Create()
        {
            return new RestClient
            {
                BaseUrl = new Uri(TurfClubConstants.Url.BASE_URL),
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.186 Safari/537.36",
                CookieContainer = new CookieContainer()
            };
        }
    }
}