using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurfClubScrapper.Models
{
    public class Horse
    {
        public int HorseId { get; set; }
        public string AgeSex { get; set; }
        public string Country { get; set; }
        public string MRA_Brand { get; set; }
        public string DateFoaled { get; set; }
        public string Sire { get; set; }
        public string Dam { get; set; }
        public string Owner { get; set; }
        public string Total { get; set; }
        public string Stats { get; set; }
        public string Rating { get; set; }
        public string TrainerName { get; set; }
    }
}
