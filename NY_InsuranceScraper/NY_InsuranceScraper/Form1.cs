using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using RestSharp;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace NY_InsuranceScraper
{
    public partial class Form1 : Form
    {
        public static RestClient Client { get; set; } = CreateRestClient();
        public static CookieContainer Cookies { get; set; } = new CookieContainer();

        public bool IsCaptchaPosted { get; set; }
        public string AuthId { get; set; }
        public int PageNumber = 1;
        public int TotalPages = 0;
        public List<PersonLicense> Licenses { get; set; } = new List<PersonLicense>();

        private const string BASE_URL = "https://myportal.dfs.ny.gov";
        private const string HOME_URL = "/nylinxext/elsearch.alis";
        private const string CAPTCHA_URL = "/nylinxext/CaptchaServlet";
        private const string SEARCH_URL = "/nylinxext/elprocesssearch.alice";
        private const string DETAILS_URL = "/nylinxext/elprsmain.alice";
        private const string AUTH_ID_URL = "/nylinxext/elfetchid.alice";

        public Form1()
        {
            InitializeComponent();
            lb_filePath.Text = ConfigurationManager.AppSettings["OutputFilePath"];

            ShowCaptcha();
        }

        private void btn_Search_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }

            var lastName = tbLastName.Text;

            new Thread(() =>
                {
                    //STEP 1: Perform Search
                    LogMessage("\n Perform search: ");

                    var licenseAndDetailUrls = new List<PersonLicense>();
                    try
                    {
                        licenseAndDetailUrls = FindActiveLicences(lastName);
                        Licenses.AddRange(licenseAndDetailUrls);
                    }
                    catch (IndexOutOfRangeException ee)
                    {
                        MessageBox.Show(ee.Message);
                        return;
                    }


                    lbRecordsFound.InvokeEx(l => l.Text = Licenses.Count.ToString());
                    if (PageNumber < TotalPages)
                    {
                        lbFromPage.InvokeEx(l => l.Text = PageNumber.ToString());
                        lbTotalPages.InvokeEx(l => l.Text = TotalPages.ToString());
                        Cookies = new CookieContainer();
                        ShowCaptcha();
                        IsCaptchaPosted = false;
                        return;
                    }
                    else
                    {
                        lbFromPage.InvokeEx(l => l.Text = TotalPages.ToString());}


                    //STEP 2: Fetch details
                    LogMessage("\n");
                    LogMessage("\n Fetch license details: ");

                    FetchLicenseDetails(lastName, Licenses);

                    //STEP 3: Save to file
                    LogMessage("\n");
                    LogMessage($"\n Save to file - {lb_filePath.Text}");

                    var filteredRows = Licenses.Where(l => l.Licenses.Any(ll => ll.Status.Equals("Active", StringComparison.CurrentCultureIgnoreCase)))
                                                           .ToList();
                    SaveToCsv(lb_filePath.Text, lastName, filteredRows);

                    LogMessage("\n-----------------------------");
                    LogMessage("\nDONE!");
                    LogMessage("\n-----------------------------");
                })
                .Start();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrEmpty(tbLastName.Text))
            {
                MessageBox.Show("Input Last Name!");
                return false;
            }

            if (!IsCaptchaPosted)
            {
                MessageBox.Show("Pass catpcha!");
                return false;
            }

            if (string.IsNullOrEmpty(lb_filePath.Text))
            {
                var path = ShowDialog();
                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                lb_filePath.Text = path;
            }

            return true;
        }

        private string ShowDialog()
        {
            var dialog = new FolderBrowserDialog();
            var dialogResult = dialog.ShowDialog();
            var folderPath = dialog.SelectedPath;

            if (dialogResult != DialogResult.OK)
            {
                return null;
            }

            return folderPath;
        }

        #region Captcha

        private void ShowCaptcha()
        {
            var client = CreateRestClient();
            var request = new RestRequest(HOME_URL, Method.GET);

            //GET sessionId
            client.Execute(request);

            //GET Patcha
            request = new RestRequest(CAPTCHA_URL, Method.GET);
            var response = client.Execute(request);

            pbCaptcha.Image = Image.FromStream(new MemoryStream(response.RawBytes));

            btnSubmitCaptcha.InvokeEx(b => b.Visible = true);
            pbCaptcha.InvokeEx(b => b.Visible = true);
            tbCaptcha.InvokeEx(b => b.Visible = true);
        }

        #endregion

        private List<PersonLicense> FindActiveLicences(string lastName)
        {
            //This site has client-side paging, so we get all needed data by only one request
            var licenseDetailLinks = new List<PersonLicense>();

            //PERFORM SEARCH:
            var results = DoSearch(lastName,PageNumber).DocumentNode;

            var pageCountNodes = results.SelectNodes("//a[@id='viewpage']");

            if (pageCountNodes == null)
            {
                throw new IndexOutOfRangeException("No results found! Or something went wrong on server... click 'Search' again please");
            }

            if (TotalPages == 0)
            {
                var totalRecords = results.SelectSingleNode("//input[@id='totalRecords']")?.Attributes["value"].Value;
                TotalPages = int.Parse(totalRecords ?? "0") / 10;
                if (int.Parse(totalRecords ?? "0") % 10 != 0)
                {
                    TotalPages++;
                }
            }


            while (true)
            {
                LogMessage($"\n\t Page {PageNumber}...");

                var rows = results.SelectNodes("//a[@class='aText']");

                //if something strange .. just skip
                if (rows == null)
                {
                    return licenseDetailLinks;
                }

                var paramRegex = new Regex("beforeFetchLicenseeDetails\\((.+)\\)");

                var licenses = rows.Select(node => new KeyValuePair<string,string>(node.InnerText.CleanupString(),node.Attributes["onclick"].Value))
                                   .Select(s => new KeyValuePair<string, string>(s.Key, paramRegex.Match(s.Value).Groups?[1].Value))
                                   .Where(s => !string.IsNullOrEmpty(s.Value))
                                   .Select(s => new PersonLicense
                                   {
                                       Name = s.Key,
                                       LicenseId = s.Value.Split(',')[0].Replace("'", string.Empty),
                                       HashedLicenseId = s.Value.Split(',')[3].Replace("'", string.Empty)
                                   })
                                   .ToList();

                licenseDetailLinks.AddRange(licenses);

                //Condition for ending loop
                PageNumber++;
                if (results.SelectSingleNode($"//a[@id='viewpage' and text()='{PageNumber}']") == null)
                {
                    break;
                }

                results = DoSearch(lastName, PageNumber).DocumentNode;
            }

            return licenseDetailLinks;
        }

        private void FetchLicenseDetails(string lastName, List<PersonLicense> licenseDetailUrls)
        {
            var progress = 1;

            //Parallel.ForEach(licenseDetailUrls, licenseDetailsUrl =>
            foreach (var licenseDetailsUrl in licenseDetailUrls.Where(l => string.IsNullOrEmpty(l.LicenseNo)).ToList())
            {
                var response = DoSearch(lastName, license: licenseDetailsUrl);
                
                LogMessage("\n------------------");

                ParseLicenseDetails(response, licenseDetailsUrl);

                lock (Client)
                {
                    lbPercentDone.InvokeEx(lbl => lbl.Text = $"{100 * progress++ / licenseDetailUrls.Count}%");
                }
            }
            //);

        }

        private HtmlDocument DoSearch(string lastName,
                                      int? pageNumber = null,
                                      PersonLicense license = null)
        {
            const int PageSize = 100;

            var data = new Dictionary<string, string>
            {
               {"pageNumber", "1"},
               {"totalRecords", "0"},
               {"paramValues", ""},
               {"licenseeId", ""},
               {"mode", ""},
               {"selectedCheck", ""},
               {"reservedStatusId", ""},
               {"incStatusId", ""},
               {"actionUrl", ""},
               {"pkcompany", ""},
               {"businessTypeId", "20726"},
               {"businessTypeCode", "I"},
               {"contextpath", "%2Fnylinxext"},
               {"providertype", ""},
               {"nystate", "21084"},
               {"type", "Search"},
               {"searchFlag", "Y"},
               {"aithentId", ""},
               {"encLicenseId", ""},
               {"businesstype", "20726%3AI"},
               {"sortBy", ""},
               {"licenseno", ""},
               {"licensee_naicnpnid", ""},
               {"lastname", WebUtility.UrlEncode(lastName)},
               {"firstname", ""},
               {"middlename", ""},
               {"name", ""},
               {"classValue", "7"},
               {"city", ""},
               {"state", "21084"},
               {"county", ""}
            };

            if (pageNumber.HasValue)
            {
                data["pageNumber"] = pageNumber.Value.ToString();
            }

            //if license was provided, means that we need find license details
            if (license != null)
            {
                data["paramValues"] = $"licenseeId%3D" + license.LicenseId;
                data["licenseeId"] = license.LicenseId;
                data["aithentId"] =  WebUtility.UrlEncode(AuthId);
                data["encLicenseId"] = WebUtility.UrlEncode(license.HashedLicenseId);
            }

            var formData = string.Join("&", data.Select(d => $"{d.Key}={d.Value}"));

            var request = new RestRequest(license == null ? SEARCH_URL : DETAILS_URL, Method.POST);
            request.AddParameter("application/x-www-form-urlencoded", formData, ParameterType.RequestBody);

            var response = CreateRestClient().Execute(request);

            var htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(response.Content);

            return htmldoc;
        }

        private void ParseLicenseDetails(HtmlDocument doc, PersonLicense license)
        {
            var root = doc.DocumentNode;

            //license.Name = root.SelectSingleNode("//table[@width='92%']//tr[2]//td[1]")?.InnerText.CleanupString();
            license.BusinessType = root.SelectSingleNode("//table[@width='92%']//tr[2]//td[2]")?.InnerText.CleanupString();
            license.LicenseNo = root.SelectSingleNode("//table[@width='92%']//tr[2]//td[3]")?.InnerText.CleanupString();
            license.Email = root.SelectSingleNode("//table[@width='92%']//tr[2]//td[4]")?.InnerText.CleanupString();
            license.HomeState = root.SelectSingleNode("//table[@width='92%']//tr[2]//td[5]")?.InnerText.CleanupString();

            var licensesRows = root.SelectNodes("//tr[contains(@id,'applForm')]");

            if (licensesRows != null)
            {
                license.Licenses = licensesRows.Select(row => new License
                {
                    Class = row.SelectSingleNode(".//td[2]//a")?.InnerText.CleanupString(),
                    Status = row.SelectSingleNode(".//td[3]")?.InnerText.CleanupString(),
                    StatusDate = row.SelectSingleNode(".//td[4]")?.InnerText.CleanupString(),
                    EffectiveDate = row.SelectSingleNode(".//td[5]")?.InnerText.CleanupString(),
                    ExpDate = row.SelectSingleNode(".//td[6]")?.InnerText.CleanupString(),
                    LicensedSince = row.SelectSingleNode(".//td[7]")?.InnerText.CleanupString()
                }).ToList();
            }

            var iframeSrc = root.SelectSingleNode("//iframe")?.Attributes?["src"].Value;

            if (iframeSrc == null)
            {
                return;
            }

            var client = CreateRestClient();
            var request = new RestRequest(iframeSrc, Method.GET);

            var response = client.Execute(request);
            doc.LoadHtml(response.Content);
            root = doc.DocumentNode;

            var addressRows = root.SelectNodes("//table[@id='addressList']//tr[contains(@id,'add')]");

            if (addressRows != null)
            {
                license.Addresses = addressRows.Select(row => new Address()
                {
                    AddressType = row.SelectSingleNode(".//td[1]")?.InnerText.CleanupString(),
                    AddressLine1 = row.SelectSingleNode(".//td[2]")?.InnerText.CleanupString(),
                    City = row.SelectSingleNode(".//td[3]")?.InnerText.CleanupString(),
                    Phone = row.SelectSingleNode(".//td[4]")?.InnerText.CleanupString(),
                    State = row.SelectSingleNode(".//td[5]")?.InnerText.CleanupString(),
                    Zip = row.SelectSingleNode(".//td[6]")?.InnerText.CleanupString(),
                }).ToList();
            }

            var companyAppRows = root.SelectNodes("//span[contains(text(),'Active Company Appointment Section')]//..//..//..//..//..//..//tr[2]//tr[contains(@id,'compAgent')]");

            if (companyAppRows != null)
            {
                license.CompanyAppointments = companyAppRows.Select(row => new CompanyAppointment()
                {
                    NAIC = row.SelectSingleNode(".//td[1]")?.InnerText.CleanupString(),
                    Name = row.SelectSingleNode(".//td[2]")?.InnerText.CleanupString(),
                    AppointmentDate = row.SelectSingleNode(".//td[3]")?.InnerText.CleanupString(),
                    TermDate = row.SelectSingleNode(".//td[4]")?.InnerText.CleanupString()
                }).ToList();
            }

            var licenses = string.Join(";", license.Licenses.Select(ll => $"{ll.Class}|{ll.Status}|{ll.StatusDate}|{ll.EffectiveDate}|{ll.ExpDate}|{ll.LicensedSince}"));
            var addresses = string.Join(";", license.Addresses.Select(ll => $"{ll.AddressType}|{ll.AddressLine1}|{ll.City}|{ll.State}|{ll.Zip}|{ll.Phone}"));
            var companyAppointments = string.Join(";", license.CompanyAppointments.Select(ll => $"{ll.NAIC}|{ll.Name}|{ll.AppointmentDate}|{ll.TermDate}"));

            LogMessage($"\n\t Name: {license.Name}" +
                       $"\n\t BusinessType: {license.BusinessType}" +
                       $"\n\t Email: {license.Email}" +
                       $"\n\t LicenseNo: {license.LicenseNo}" +
                       $"\n\t HomeState: {license.HomeState}" +
                       $"\n\t companyAppointments: {companyAppointments}" +
                       $"\n\t licenses: {licenses}" +
                       $"\n\t addresses: {addresses}");
        }

        private static RestClient CreateRestClient()
        {
            var restClient = new RestClient
            {
                BaseUrl = new Uri(BASE_URL),
                CookieContainer = Cookies,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.186 Safari/537.36"
            };

            return restClient;
        }

        private void LogMessage(string text)
        {
            // InvokeRequired required compares the thread ID of the  
            // calling thread to the thread ID of the creating thread.  
            // If these threads are different, it returns true.  
            lb_logs.InvokeEx(rtb => rtb.AppendText(text));
        }

        private void SaveToCsv(string filePath, string lastName, List<PersonLicense> personLicenses)
        {
            var fullPath = filePath + $"\\NY_Insurance_{lastName}_{1}-{PageNumber}.csv".Replace("/", " ");

            using (var stream = File.CreateText(fullPath))
            {
                const string columnsLine = "name,licenseNo,businessType,email,homeState,licenses,addresses,companyAppointments";
                stream.WriteLine(columnsLine);

                foreach (var l in personLicenses)
                {
                    var licenses = string.Join(";", l.Licenses.Select(ll => $"{ll.Class}|{ll.Status}|{ll.StatusDate}|{ll.EffectiveDate}|{ll.ExpDate}|{ll.LicensedSince}"));
                    var addresses = string.Join(";", l.Addresses.Select(ll => $"{ll.AddressType}|{ll.AddressLine1}|{ll.City}|{ll.State}|{ll.Zip}|{ll.Phone}"));
                    var companyAppointments = string.Join(";", l.CompanyAppointments.Select(ll => $"{ll.NAIC}|{ll.Name}|{ll.AppointmentDate}|{ll.TermDate}"));

                    stream.WriteLine($"{l.Name},{l.LicenseNo},{l.BusinessType},{l.Email},{l.HomeState},{licenses},{addresses},{companyAppointments}");
                }
            }
        }

        private void lb_logs_TextChanged(object sender, EventArgs e)
        {
            // set the current caret position to the end
            lb_logs.SelectionStart = lb_logs.Text.Length;
            // scroll it automatically
            lb_logs.ScrollToCaret();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            lb_filePath.Text = ShowDialog();
        }

        private void btnSubmitCaptcha_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbCaptcha.Text))
            {
                MessageBox.Show("Enter captcha!");
            }

            var data = new Dictionary<string, string>()
            {
                {"captcha", "true"},
                {"captchaResponse", tbCaptcha.Text}
            };


            var client = CreateRestClient();
            var request = new RestRequest(HOME_URL, Method.POST);

            var formData = string.Join("&", data.Select(d => $"{d.Key}={d.Value}"));
            request.AddParameter("application/x-www-form-urlencoded", formData, ParameterType.RequestBody);

            //Post captcha
            var response = client.Execute(request);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(response.Content);

            IsCaptchaPosted = htmlDoc.DocumentNode.SelectSingleNode("//input[@id='captchaResponse']") == null;

            if (!IsCaptchaPosted)
            {
                ShowCaptcha();
                MessageBox.Show("Invalid captcha. Please enter new one!");
            }
            else
            {
                btnSubmitCaptcha.Visible = false;
                pbCaptcha.Visible = false;
                tbCaptcha.Visible = false;

                request = new RestRequest(AUTH_ID_URL, Method.POST);
                response = client.Execute(request);

                var json = JObject.Parse(response.Content);
                AuthId = json["aithentId"].Value<string>();
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            PageNumber = 1;
            Licenses.Clear();
            lb_logs.Clear();
            TotalPages = 0;
        }

        private void tbSave_Click(object sender, EventArgs e)
        {
            //STEP 2: Fetch details
            LogMessage("\n");
            LogMessage("\n Fetch license details: ");

            FetchLicenseDetails(tbLastName.Text, Licenses);

            //STEP 3: Save to file
            LogMessage("\n");
            LogMessage($"\n Save to file - {lb_filePath.Text}");

            var filteredRows = Licenses.Where(l => l.Licenses.Any(ll => ll.Status.Equals("Active", StringComparison.CurrentCultureIgnoreCase)))
                                                   .ToList();
            SaveToCsv(lb_filePath.Text, tbLastName.Text, filteredRows);

            LogMessage("\n-----------------------------");
            LogMessage("\nDONE!");
            LogMessage("\n-----------------------------");
        }
    }
}
