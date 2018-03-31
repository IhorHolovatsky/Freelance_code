using System.Collections.Generic;

namespace MA_InsuranceScraper.Models
{
    public class PersonLicense
    {
        public string LinesOfInsurance { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string DoingAs { get; set; }
        public string LicenseNumber { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string BusinessPhone { get; set; }
        public string Email { get; set; }
        public List<string> CompanyNames { get; set; } = new List<string>();
    }

}