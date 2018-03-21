using System.Collections.Generic;

namespace MiamiDade_Scraper
{
    public class PersonLicense
    {
        public string DetailsUrl { get; set; }

        public string LicenseNo { get; set; }

        public List<DocumentRow> Rows { get; set; } = new List<DocumentRow>();
    }

    public class DocumentRow
    {
        public string FirstParty { get; set; }
        public string SecondParty { get; set; }
        public string RecDate { get; set; }
        public string SubdivisionName { get; set; }
        public string LegalDescription { get; set; }
    }

}