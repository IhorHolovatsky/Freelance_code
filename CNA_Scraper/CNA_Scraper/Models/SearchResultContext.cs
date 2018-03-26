using HtmlAgilityPack;

namespace CNA_Scraper.Models
{
    public class SearchResultContext
    {
        public bool IsSuccess { get; set; }
        public HtmlDocument Document { get; set; }

        public int PageCount { get; set; }
    }
}