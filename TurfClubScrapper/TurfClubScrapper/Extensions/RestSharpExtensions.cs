using HtmlAgilityPack;
using RestSharp;

namespace TurfClubScrapper.Extensions
{
    public static class RestSharpExtensions
    {
        public static HtmlDocument ToHtmlDocument(this IRestResponse response)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(response.Content);

            return htmlDoc;
        }
    }
}