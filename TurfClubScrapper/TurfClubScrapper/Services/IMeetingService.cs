using System.Collections.Generic;
using System.Threading.Tasks;
using TurfClubScrapper.Models;

namespace TurfClubScrapper.Services
{
    public interface IMeetingService
    {
        /// <summary>
        /// Get all past meetings asynchronously
        /// </summary>
        Task<List<Meeting>> GetPastMeetingsAsync();

        /// <summary>
        /// Get all current year meetings asynchronously
        /// </summary>
        Task<List<Meeting>> GetCurrentYearMeetingsAsync();

        /// <summary>
        /// Get upcoming meetings asynchronously
        /// </summary>
        Task<List<Meeting>> GetUpcomingMeetingsAsync();
    }
}