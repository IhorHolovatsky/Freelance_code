using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Windows.Forms;
using RestSharp;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace WebInsurance_Scraper
{
    public partial class Form1 : Form
    {
        public static RestClient Client { get; set; } = CreateRestClient();

        private const string BASE_URL = "https://interactive.web.insurance.ca.gov/";
        private const string SEARCH_URL = "/webuser/licw_name_search$lic_name_qry.actionquery";
        private const string NEXT_PAGE_URL = "/webuser/licw_name_search$lic_name_qry.querylist";

        public Form1()
        {
            InitializeComponent();
        }

        private void btn_Search_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }
            lb_logs.Clear();
            
            var licenseName = tb_licenseName.Text.ToUpper();

            new Thread(() =>
                {
                    //STEP 1: Perform Search
                    LogMessage("\n Perform search: ");

                    var licenseDetailUrls = new List<string>();
                    try
                    {
                        licenseDetailUrls = FindActiveLicences(licenseName);
                    }
                    catch (IndexOutOfRangeException ee)
                    {
                        MessageBox.Show(ee.Message);
                        return;
                    }

                    //STEP 2: Fetch details
                    LogMessage("\n");
                    LogMessage("\n Fetch license details: ");

                    var licenses = FetchLicenseDetails(licenseDetailUrls);

                    //STEP 3: Save to file
                    LogMessage("\n");
                    LogMessage($"\n Save to file - {lb_filePath.Text}");

                    SaveToCsv(lb_filePath.Text, licenseName, licenses);

                    LogMessage("\n-----------------------------");
                    LogMessage("\nDONE!");
                    LogMessage("\n-----------------------------");
                })
                .Start();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrEmpty(tb_licenseName.Text))
            {
                MessageBox.Show("Input license name!");
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

        private List<string> FindActiveLicences(string licenseName)
        {
            var pageNumber = 0;
            var licenseDetailLinks = new List<string>();

            //PERFORM SEARCH:
            var results = DoSearch(SEARCH_URL, licenseName).DocumentNode;

            var pageCount = 0;

            while (true)
            {

                LogMessage($"\n \t Page: {pageCount++}");

                var rows = results.SelectNodes("//tr[@class='cgrldatarow']");

                if (rows == null)
                {
                    if (results.InnerText.Contains("No Records returned"))
                        throw new IndexOutOfRangeException("No records found.. try another string");

                    throw new IndexOutOfRangeException("A lot of rows... try another string");
                }

                //Loop through rows and filter not needed rows
                foreach (var row in rows)
                {
                    var tdStatus = row.SelectNodes(".//td")[2];

                    //Skip not active licences
                    if (tdStatus.InnerText.IndexOf("inactive", StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        continue;
                    }

                    var tdLicenseNumber = row.SelectNodes(".//td")[1];
                    var licenseLinkNode = tdLicenseNumber.SelectSingleNode(".//a");

                    //Skip empty license numbers
                    if (licenseLinkNode == null)
                    {
                        continue;
                    }

                    licenseDetailLinks.Add(licenseLinkNode.Attributes["href"].Value);
                }

                //if no more results -> stop cycle
                if (results.SelectNodes("//input[@value='Next']") == null)
                {
                    break;
                }

                //Strange paging... 
                pageNumber = pageNumber == 0 ? 1 : pageNumber + 100;

                //Go to Next page
                results = DoSearch(NEXT_PAGE_URL, licenseName, pageNumber).DocumentNode;
            }

            return licenseDetailLinks;
        }

        private List<PersonLicense> FetchLicenseDetails(List<string> licenseDetailUrls)
        {
            var licenses = new List<PersonLicense>();

            //Parallel.ForEach(licenseDetailUrls, licenseDetailsUrl =>
            foreach (var licenseDetailsUrl in licenseDetailUrls)
            {
                var client = CreateRestClient();
                var request = new RestRequest($"webuser/{licenseDetailsUrl}", Method.GET);
                var response = client.Execute(request);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(response.Content);

                LogMessage("\n------------------");
                var license = ParseLicenseDetails(htmlDoc);
                if (license != null)
                {
                    licenses.Add(license);
                }
            }
            //);

            return licenses;
        }

        private HtmlDocument DoSearch(string url,
                                      string licenseName,
                                      int? pageNumber = null)
        {
            var request = new RestRequest(url, Method.POST);
            var data = new Dictionary<string, string>
            {
                {"P_CAP_NAME", WebUtility.UrlEncode($"{licenseName.Replace(" ", "_")}%")},
                {"Z_ACTION", "QUERY"},
                {"Z_CHK", "0"}
            };

            //if are moving to next page, we need other set of params
            if (pageNumber.HasValue)
            {
                data["P_IL_LIC_NBR"] = "";
                data["P_IL_LIC_NBR"] = "";
                data["Z_START"] = pageNumber.Value.ToString();
                data["Z_ACTION"] = "NEXT";
            }

            var formData = string.Join("&", data.Select(d => $"{d.Key}={d.Value}"));

            request.AddParameter("application/x-www-form-urlencoded", formData, ParameterType.RequestBody);

            var response = Client.Execute(request);

            var htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(response.Content);

            return htmldoc;
        }

        private PersonLicense ParseLicenseDetails(HtmlDocument doc)
        {
            var license = new PersonLicense();

            var root = doc.DocumentNode;

            var rows = root.SelectNodes("//tr");

            if (rows == null || !rows.Any())
            {
                return null;
            }

            var isCompanyAppointmentsRows = false;

            foreach (var row in rows)
            {
                //name + license row
                if (row.SelectNodes(".//td").Any(tdNode => tdNode.InnerText.Contains("Name: ")))
                {
                    var fullName = row.SelectNodes(".//td")?[1]?.FirstChild?.InnerText.Replace("Name: ", string.Empty);
                    var regex = new Regex("[ ]{2,}", RegexOptions.None);
                    fullName = regex.Replace(fullName, " ");

                    license.FirstName = fullName.Split(' ').Count() > 1 ? fullName.Split(' ')[0] : fullName;
                    license.LastName = fullName.Split(' ').Count() > 1 ? fullName.Split(' ')[1] : string.Empty;
                    license.MiddleName = fullName.Split(' ').Count() > 2 ? fullName.Split(' ')[2] : string.Empty;

                    license.LicenseNumber = row.SelectNodes(".//td")?[3]?.InnerText.Replace("License#: ", string.Empty);
                }

                //license type + exp date
                if (row.SelectNodes(".//td").Any(tdNode => tdNode.InnerText.Contains("License type: ")))
                {
                    license.Licences.Add(new License()
                    {
                        LicenseType = row.SelectNodes(".//td")?[1]?.FirstChild?.InnerText.Replace("License type: ", string.Empty),
                        ExpirationDate = row.SelectNodes(".//td")?[4]?.InnerText.Replace("Expiration Date: ", string.Empty).Trim()
                    });
                }

                //address row
                if (row.SelectNodes(".//td").Any(tdNode => tdNode.InnerText.Contains("Business Address: ")))
                {
                    license.BusinessAddress = row.SelectNodes(".//td")?[1]?.InnerText.Replace("Business Address: ", string.Empty).Replace(",", "");
                }

                //phone row
                if (row.SelectNodes(".//td").Any(tdNode => tdNode.InnerText.Contains("Business Phone: ")))
                {
                    license.BusinessPhone = row.SelectNodes(".//td")?[1]?.InnerText.Replace("Business Phone: ", string.Empty);
                }

                //indicator that next rows will be Company
                if (row.SelectNodes(".//td").Any(tdNode => tdNode.InnerText.Contains("Company Appointments")))
                {
                    isCompanyAppointmentsRows = true;
                }

                //reset indicator
                if (row.SelectNodes(".//td").Any(tdNode => tdNode.InnerText.Contains("Agencies or Organizations")))
                {
                    isCompanyAppointmentsRows = false;
                }

                //gather company names
                if (isCompanyAppointmentsRows
                    && row.SelectNodes(".//td").Any(tdNode => tdNode.InnerText.Contains("For: ")))
                {
                    var companyName = row.SelectNodes(".//td")?[1]?.InnerText;
                    license.CompanyNames.Add(companyName);
                }
            }

            LogMessage($"\n\t License: {license.LicenseNumber}" +
                       $"\n\t Name: {license.Name}" +
                       $"\n\t License Types: {string.Join(";",license.Licences.Select(l => $"{l.LicenseType}|{l.ExpirationDate}"))}" +
                       $"\n\t Address: {license.BusinessAddress}" +
                       $"\n\t Phone: {license.BusinessPhone}" +
                       $"\n\t Company Names: {string.Join(",", license.CompanyNames)}");

            return license;
        }

        private static RestClient CreateRestClient()
        {
            var restClient = new RestClient
            {
                BaseUrl = new Uri(BASE_URL),
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

        private void SaveToCsv(string filePath, string licenseName, List<PersonLicense> personLicenses)
        {
            var fullPath = filePath + $"\\insurance_{licenseName}.csv".Replace("/", "");

            using (var stream = File.CreateText(fullPath))
            {
                const string columnsLine = "name,licenseNumber,licenseTypes,address,phone,companyNames";
                stream.WriteLine(columnsLine);

                foreach (var l in personLicenses)
                {
                    var licenseTypes = string.Join(";", l.Licences.Select(lt => $"{lt.LicenseType}|{lt.ExpirationDate}"));
                    var companyNames = string.Join(";", l.CompanyNames);

                    stream.WriteLine($"{l.Name},{l.LicenseNumber},{licenseTypes},{l.BusinessAddress},{l.BusinessPhone},{companyNames}");
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
    }
}
