using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurfClubScrapper.Models
{
    public class Sectional
    {
        public int MeetingId { get; set; }
        public int RaceId { get; set; }

        public string HorseNumber { get; set; }
        public string HorseName { get; set; }
        public TimeSpan FinishTime { get; set; }

        public string SectionalPos1 { get; set; }
        public string SectionalMgn1 { get; set; }
        public string SectionalTime1 { get; set; }

        public string SectionalPos2 { get; set; }
        public string SectionalMgn2 { get; set; }
        public string SectionalTime2 { get; set; }

        public string SectionalPos3 { get; set; }
        public string SectionalMgn3 { get; set; }
        public string SectionalTime3 { get; set; }

        public string SectionalPos4 { get; set; }
        public string SectionalMgn4 { get; set; }
        public string SectionalTime4 { get; set; }

        public string SectionalPos5 { get; set; }
        public string SectionalMgn5 { get; set; }
        public string SectionalTime5 { get; set; }

        public string SectionalPos6 { get; set; }
        public string SectionalMgn6 { get; set; }
        public string SectionalTime6 { get; set; }
    }
}
