using System.Collections.Generic;
using System.Threading.Tasks;
using TurfClubScrapper.Models;
using TurfClubScrapper.Scrappers;
using TurfClubScrapper.Scrappers.Implementations;

namespace TurfClubScrapper.Services.Implementations
{
    public class MeetingService : IMeetingService
    {
        private static MeetingService _instance;
        /// <summary>
        /// Singleton pattern (will be removed if project will be big with DI)
        /// </summary>
        public static MeetingService Instance => _instance ?? (_instance = new MeetingService());


        private readonly ISearchMeetingsScrapper _searchMeetingsScrapper = new SearchMeetingsScrapper();
        private readonly IMeetingsScrapper _meetingsScrapper = new MeetingsScrapper();

        /// <inheritdoc />
        public async Task<List<Meeting>> GetPastMeetingsAsync()
        {
            var searchResult = await _searchMeetingsScrapper.FindAllPastResults();

            return await _meetingsScrapper.ScrapMeetings(searchResult);
        }

        public Task<List<Meeting>> GetCurrentYearMeetingsAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<List<Meeting>> GetUpcomingMeetingsAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}