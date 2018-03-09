using System.Collections.Generic;

namespace WebInsurance_Scraper
{
    public class PersonLicense
    {
        public string FullName
        {
            get { return $"{FirstName} {LastName} {MiddleName}"; }
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string LicenseNumber { get; set; }
        public string BusinessAddress { get; set; }
        public string BusinessPhone { get; set; }
        public List<string> CompanyNames { get; set; } = new List<string>();
        public List<License> Licences { get; set; } = new List<License>();
    }

    public class License
    {
        public string LicenseType { get; set; }
        public string ExpirationDate { get; set; }
    }
}