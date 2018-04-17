using HtmlAgilityPack;

namespace TurfClubScrapper.Extensions
{
    public static class HtmlNodeExtensions
    {
        public static string GetInputValueByIdOrName(this HtmlNode node, string id)
        {
            var returnValue = node?.SelectSingleNode($"//input[@id='{id}']")?.Attributes["value"].Value;

            return string.IsNullOrEmpty(returnValue)
                ? node?.SelectSingleNode($"//input[@name='{id}']")?.Attributes["value"].Value
                : returnValue;
        }
    }
}