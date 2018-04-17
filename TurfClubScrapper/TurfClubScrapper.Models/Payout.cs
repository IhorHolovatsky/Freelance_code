using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurfClubScrapper.Models
{
    public class Payout
    {
        public int MeetingId { get; set; }
        public int RaceId { get; set; }
        public string Type { get; set; }
        public int Sequence { get; set; }
        public int Numbers { get; set; }
        public decimal Dividend { get; set; }
    }
}
