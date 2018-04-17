using System.Collections.Generic;

namespace TurfClubScrapper.Scrappers.Models
{
    public class SearchMeetingsResult
    {
        public SearchMeetingsResult()
        {
            Results = new List<MeetingContext>();
        }

        public List<MeetingContext> Results { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class MeetingContext
    {
        public string DetailsUrl { get; set; }
        public string Location { get; set; }
        public string MeetingDate { get; set; }
        public int? MeetingId { get; set; }
    }
}