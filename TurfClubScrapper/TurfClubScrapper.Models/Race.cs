using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurfClubScrapper.Models
{
    public class Race
    {
        public Race()
        {
            Starters = new List<Starter>();
        }

        public int RaceId { get; set; }

        public int? MeetingId { get; set; }

        public int Number { get; set; }

        public string Description { get; set; }

        public string VideoLink { get; set; }

        public string StripeReport { get; set; }


        public List<Starter> Starters { get; set; }
        public List<Payout> Payouts { get; set; }
    }
}
