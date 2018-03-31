using System.Collections.Generic;

namespace OK_RealState.Models
{
    public class SearchContext
    {
        public string LastName { get; set; }
        public List<string> States { get; set; }
        public List<string> AgencyLines{ get; set; }
    }
}
