using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurfClubScrapper.Models
{
    public class Meeting
    {
        public Meeting()
        {
            Races = new List<Race>();
        }

        public int MeetingId { get; set; }

        public DateTime? Date { get; set; }

        public string Country { get; set; }

        public string MeetingName { get; set; }

        public int RacesCount { get; set; }

        public int StartersCount { get; set; }

        public string TurfClub_URL { get; set; }


        public List<Race> Races { get; set; }
    }
}
