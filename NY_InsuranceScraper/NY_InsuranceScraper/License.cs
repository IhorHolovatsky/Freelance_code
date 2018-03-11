using System.Collections.Generic;

namespace NY_InsuranceScraper
{
    public class PersonLicense
    {
        public string DetailsUrl { get; set; }

        public string Name { get; set; }
        public string BusinessType { get; set; }
        public string LicenseNo { get; set; }
        public string Email { get; set; }
        public string HomeState { get; set; }
        public List<CompanyAppointment> CompanyAppointments { get; set; } = new List<CompanyAppointment>();
        public List<Address> Addresses { get; set; } = new List<Address>();
        public List<License> Licenses { get; set; } = new List<License>();


        public string LicenseId { get; set; }
        public string HashedLicenseId { get; set; }
    }

    public class License
    {
        public string Class { get; set; }
        public string Status { get; set; }
        public string StatusDate { get; set; }
        public string EffectiveDate { get; set; }
        public string ExpDate { get; set; }
        public string LicensedSince { get; set; }
    }

    public class Address
    {
        public string AddressType { get; set; }
        public string AddressLine1 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Phone { get; set; }
    }

    public class CompanyAppointment
    {
        public string NAIC { get; set; }
        public string Name { get; set; }
        public string AppointmentDate { get; set; }
        public string TermDate { get; set; }
    }
}