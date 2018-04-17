using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurfClubScrapper.Models
{
    public class Starter
    {
        public int MeetingId { get; set; }
        public int RaceId { get; set; }
        public int HorseNumber { get; set; }
        public string HorseName { get; set; }
        public string Gear { get; set; }
        public string HorseRating { get; set; }
        public string HorseWeight { get; set; }
        public string HandicappedWeight { get; set; }
        public string CarriedWeight { get; set; }
        public string Barrier { get; set; }
        public string JockeyIsApprentice { get; set; }
        public string RunningPos { get; set; }
        public string Placing { get; set; }
        public string LBW { get; set; }
        public TimeSpan Time { get; set; }
        public string WinDiv { get; set; }
        public string StripeReport { get; set; }
        public bool IsScratched { get; set; }


        public int JockeyId { get; set; }
        public int TrainerId { get; set; }
        public int HorseId { get; set; }

        public Horse Horse { get; set; }
        public Jockey Jockey { get; set; }
        public Trainer Trainer { get; set; }
        public Sectional Sectional { get; set; }
    }
}
