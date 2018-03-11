using System.Collections.Generic;

namespace Walton_Scaper
{
    public class PersonLicense
    {
        public string DocumentId { get; set; }

        public string RecordDate { get; set; }
        public string DocType { get; set; }
        public List<string> Grantors { get; set; }
        public List<string> Grantees { get; set; }
        public string Consideration { get; set; }
        public string FullLegalName { get; set; }
    }
}