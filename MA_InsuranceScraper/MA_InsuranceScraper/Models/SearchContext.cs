using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MA_InsuranceScraper.Models
{
    public class SearchContext
    {
        public string LastName { get; set; }
        public List<string> States { get; set; }
        public List<string> AgencyLines{ get; set; }
    }
}
