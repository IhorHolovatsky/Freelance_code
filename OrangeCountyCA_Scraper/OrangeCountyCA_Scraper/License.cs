using System.Collections.Generic;

namespace OrangeCountyCA_Scraper
{
    public class PersonLicense
    {
        public string DocNumber { get; set; }
        public string DetailsUrl { get; set; }
        public string RecordingDate { get; set; }

        public List<Document> DocumentTypes { get; set; } = new List<Document>();

    }

    public class Document
    {
        public string DocumentType { get; set; }
        public List<string> Grantors { get; set; } = new List<string>();
        public List<string> Grantees { get; set; } = new List<string>();
    }

}