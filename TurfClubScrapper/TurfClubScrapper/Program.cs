using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp.Serializers;
using TurfClubScrapper.Extensions;
using TurfClubScrapper.Models;
using TurfClubScrapper.Models.Constants;
using TurfClubScrapper.Models.Enums;
using TurfClubScrapper.Services;
using TurfClubScrapper.Services.Implementations;

namespace TurfClubScrapper
{
    public class Program
    {
        private static readonly IMeetingService _meetingService = MeetingService.Instance;

        static void Main(string[] args)
        {
            ApplicationMode appMode;

            //Try to parse appMode, if any value, just exit...
            if (args.IsNullOrEmpty()
                || !Enum.TryParse(args[0], out appMode))
            {
                return;
            }

            List<Meeting> meetings;

            switch (appMode)
            {
                case ApplicationMode.PastResultsMode:
                    meetings =  _meetingService.GetPastMeetingsAsync().Result;
                    break;
                case ApplicationMode.DailyMode:
                    meetings = _meetingService.GetCurrentYearMeetingsAsync().Result;
                    break;
                case ApplicationMode.UpcomingRacesMode:
                    meetings =_meetingService.GetUpcomingMeetingsAsync().Result;
                    break;
                default:
                    throw new InvalidOperationException($"Unknow app mode: {(int)appMode}");
            }


            //TODO: Save it to anywhere
            var jsonValue = new JsonSerializer().Serialize(meetings);
        }
    }
}
