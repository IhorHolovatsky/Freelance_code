using System.Collections.Generic;
using System.Threading.Tasks;
using TurfClubScrapper.Models;
using TurfClubScrapper.Scrappers.Models;

namespace TurfClubScrapper.Scrappers
{
    public interface IMeetingsScrapper
    {
        /// <summary>
        /// Fetch all info about meeting
        /// </summary>
        /// <returns>Meeting with all info</returns>
        Task<List<Meeting>> ScrapMeetings(SearchMeetingsResult searchResult);
    }
}