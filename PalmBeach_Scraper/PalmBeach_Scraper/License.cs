using System.Collections.Generic;

namespace PalmBeach_Scraper
{
    public class PersonLicense
    {
        public string DetailsUrl { get; set; }

        public string DocType { get; set; }
        public string Consideration { get; set; }
        public List<string> Party1 { get; set; }
        public List<string> Party2 { get; set; }
        public string Legal { get; set; }
        public string DateTime { get; set; }
    }
}