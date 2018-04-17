using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using RestSharp;
using TurfClubScrapper.Extensions;
using TurfClubScrapper.Models;
using TurfClubScrapper.Models.Constants;
using TurfClubScrapper.Scrappers.Models;
using TurfClubScrapper.Utils;

namespace TurfClubScrapper.Scrappers.Implementations
{
    public class MeetingsScrapper : IMeetingsScrapper
    {
        /// <inheritdoc />
        public async Task<List<Meeting>> ScrapMeetings(SearchMeetingsResult searchResult)
        {
            if (searchResult == null) throw new ArgumentNullException(nameof(searchResult));

            var meetings = new List<Meeting>();

            foreach (var meetingResult in searchResult.Results)
            {
                //Skip errors (they were logged, no worry)
                if (!meetingResult.MeetingId.HasValue) continue;

                var page = await GetAllRacesPage(meetingResult.MeetingId.Value);
                var meeting = ParseMeetingDetails(meetingResult, page);

                if (meeting != null)
                {
                    meetings.Add(meeting);
                }
                else
                {
                    //TODO: Log unexpected error...
                }
            }

            return meetings;
        }


        #region Private Members
        
        /// <summary>
        /// Get page with all races info
        /// </summary>
        private Task<HtmlDocument> GetAllRacesPage(int meetingId)
        {
            var client = RestClientUtils.Create();
            var request = new RestRequest($"{TurfClubConstants.Url.RACE_RESTULTS_URL}/{meetingId}/All", Method.GET);
            var response = client.Execute(request);
            return Task.FromResult(response.ToHtmlDocument());

        }

        /// <summary>
        /// Gather all Meeting info from page
        /// </summary>
        private Meeting ParseMeetingDetails(MeetingContext meetingContext,
                                            HtmlDocument doc)
        {
            var root = doc.DocumentNode;
            var turfClubUrl = root.SelectSingleNode("//form[@id='aspnetForm']")?.Attributes["action"].Value;

            var meeting = new Meeting()
            {
                Country = meetingContext.Location.Contains(" ") //sometimes location contains MeetingName in brackets
                                ? meetingContext.Location.Split(' ')[0]
                                : meetingContext.Location,
                MeetingName = meetingContext.Location.Contains(" ")
                                ? meetingContext.Location.Split(' ')[1].Trim('(',')')
                                : meetingContext.Location,
                Date = meetingContext.MeetingDate.ParseDateTime(),
                TurfClub_URL = turfClubUrl
            };

            meeting.Races = ParseRacesDetails(meetingContext, doc);

            //Calculate total counts
            meeting.RacesCount = meeting.Races.Count;
            meeting.StartersCount = meeting.Races.Sum(r => r.Starters.Count);


            return meeting;
        }

        /// <summary>
        /// Gather all races info from page
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        private List<Race> ParseRacesDetails(MeetingContext meetingContext,
                                             HtmlDocument page)
        {
            var root = page.DocumentNode;
            var races = new List<Race>();

            var rows = root.SelectNodes("//table[@class='STC_Table_Tab']");

            if (rows == null)
            {
                //TODO: Log this
                return new List<Race>();
            }

            foreach (var raceRow in rows)
            {
                var race = new Race()
                {
                    MeetingId = meetingContext.MeetingId
                };

                race.
            }


            return races;
        }

        #endregion
    }
}