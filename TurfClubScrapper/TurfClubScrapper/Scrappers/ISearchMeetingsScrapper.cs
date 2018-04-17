using System.Threading.Tasks;
using TurfClubScrapper.Scrappers.Models;

namespace TurfClubScrapper.Scrappers
{
    public interface ISearchMeetingsScrapper
    {
        /// <summary>
        /// Find all past meetings (loop through all years and months)
        /// </summary>
        Task<SearchMeetingsResult> FindAllPastResults();
    }
}