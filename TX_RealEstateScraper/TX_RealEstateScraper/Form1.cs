using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
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

        private const string BASE_URL = "https://www.trec.texas.gov/";
        private const string SEARCH_URL = "/apps/license-holder-search/index.php";

        public Form1()
        {
            InitializeComponent();
            lb_filePath.Text = ConfigurationManager.AppSettings["OutputFilePath"];
        }

        private void btn_Search_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }
            lb_logs.Clear();
            
            var licenseName = tb_licenseName.Text;

            new Thread(() =>
                {
                    //STEP 1: Perform Search
                    LogMessage("\n Perform search: ");

                    var licenseAndDetailUrls = new List<PersonLicense>();
                    try
                    {
                        licenseAndDetailUrls = FindActiveLicences(licenseName);
                    }
                    catch (IndexOutOfRangeException ee)
                    {
                        MessageBox.Show(ee.Message);
                        return;
                    }

                    //STEP 2: Fetch details
                    LogMessage("\n");
                    LogMessage("\n Fetch license details: ");

                    FetchLicenseDetails(licenseAndDetailUrls);

                    //STEP 3: Save to file
                    LogMessage("\n");
                    LogMessage($"\n Save to file - {lb_filePath.Text}");

                    SaveToCsv(lb_filePath.Text, licenseName, licenseAndDetailUrls);

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

        private List<PersonLicense> FindActiveLicences(string licenseName)
        {
            //Start paging from second page, since first request give us first page
            var pageNumber = 2;
            var licenseDetailLinks = new List<PersonLicense>();

            //PERFORM SEARCH:
            var results = DoSearch(SEARCH_URL, licenseName).DocumentNode;

            var pageCountElement = results.SelectNodes("//div[@class='paginator-description']")?[0];

            if (pageCountElement == null)
            {
                throw new IndexOutOfRangeException("No results found!");
            }

            var pageCount = new Regex(@"of (\w+)").Match(pageCountElement.InnerText).Groups?[1].Value.ToInt();

            LogMessage($"\n Found {pageCount} pages.");
            do
                //while (true)
            {

                LogMessage($"\n \t Page: {pageNumber - 1}");

                var rows = results.SelectNodes("//div[@class='record-fluid']");

                //Loop through rows and filter not needed rows
                foreach (var row in rows)
                {
                    var licenseLinkNode = row.SelectNodes(".//div[@class='panel-heading']//a");
                    
                    //Skip empty license numbers
                    if (licenseLinkNode == null)
                    {
                        continue;
                    }

                    var personLicense = new PersonLicense
                    {
                        DetailsUrl = licenseLinkNode[0].Attributes["href"].Value,
                        FullName = licenseLinkNode[0].InnerText.Replace(",", " "),
                        LicenseNumber = row.SelectNodes(".//div[contains(@class, 'field-fluid')]/div[@class='data-fluid']")?[0].InnerText,
                        LicenseType = row.SelectNodes(".//div[contains(@class, 'field-fluid')]/div[@class='data-fluid']")?[1].InnerText,
                        Status = row.SelectNodes(".//div[contains(@class, 'field-fluid')]/div[@class='data-fluid']")?[2].InnerText,
                        City = row.SelectNodes(".//div[contains(@class, 'field-fluid')]/div[@class='data-fluid']")?[4].InnerText,
                        State = row.SelectNodes(".//div[contains(@class, 'field-fluid')]/div[@class='data-fluid']")?[5].InnerText,
                        Zip = row.SelectNodes(".//div[contains(@class, 'field-fluid')]/div[@class='data-fluid']")?[6].InnerText,
                    };

                    licenseDetailLinks.Add(personLicense);
                }

                //if page number > page count do no need make call
                if (pageNumber > pageCount.Value)
                {
                    break;
                }

                //Go to Next page
                results = DoSearch(SEARCH_URL, licenseName, pageNumber).DocumentNode;
            } while (pageNumber++ <= pageCount);

            return licenseDetailLinks;
        }

        private void FetchLicenseDetails(List<PersonLicense> licenseDetailUrls)
        {
            Parallel.ForEach(licenseDetailUrls, licenseDetailsUrl =>
            //foreach (var licenseDetailsUrl in licenseDetailUrls)
            {
                var client = CreateRestClient();
                var request = new RestRequest($"/apps/license-holder-search/{licenseDetailsUrl.DetailsUrl}", Method.GET);
                var response = client.Execute(request);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(response.Content);

                LogMessage("\n------------------");

                ParseLicenseDetails(htmlDoc, licenseDetailsUrl);
            }
            );

        }

        private HtmlDocument DoSearch(string url,
                                      string licenseName,
                                      int? pageNumber = null)
        {
            var data = new Dictionary<string, string>
            {
                {"lic_name", WebUtility.UrlEncode(licenseName)},
                {"license_search", WebUtility.UrlEncode("Search")},
                {"industry", WebUtility.UrlEncode("Real Estate")},
                {"lic_hp", ""},
                {"display_status", "active"}
            };

            //if are moving to next page, we need other set of params
            if (pageNumber.HasValue)
            {
                data["showpage"] = pageNumber.Value.ToString();
            }

            var queryStringData = string.Join("&", data.Select(d => $"{d.Key}={d.Value}"));
            var request = new RestRequest(url + $"?{queryStringData}", Method.GET);
            
            var response = Client.Execute(request);

            var htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(response.Content);

            return htmldoc;
        }

        private void ParseLicenseDetails(HtmlDocument doc, PersonLicense license)
        {
            var root = doc.DocumentNode;

            const string removeHtmlTagsRegex = @"<span.*?>(.|\n)*?</span>";

            var dataAria = root.SelectSingleNode("//div[@aria-data]")?.Attributes?["aria-data"]?.Value.Split('-')?[0];

            license.Phone = Regex.Replace(root.SelectSingleNode($"//div[@id='{dataAria}-1']")?.InnerHtml ?? string.Empty, removeHtmlTagsRegex, string.Empty);
            license.Email = Regex.Replace(root.SelectSingleNode($"//div[@id='{dataAria}-0']")?.InnerHtml ?? string.Empty, removeHtmlTagsRegex, string.Empty);
        
            LogMessage($"\n\t License: {license.LicenseNumber}" +
                       $"\n\t Name: {license.FullName}" +
                       $"\n\t Status: {license.Status}" +
                       $"\n\t License Type: {license.LicenseType}" +
                       $"\n\t Email: {license.Email}" +
                       $"\n\t Phone: {license.Phone}");
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
            var fullPath = filePath + $"\\tx_RealEstate_{licenseName}.csv".Replace("/", "");

            using (var stream = File.CreateText(fullPath))
            {
                const string columnsLine = "fullName,licenseNumber,licenseType,status,city,state,zip,email,phone";
                stream.WriteLine(columnsLine);

                foreach (var l in personLicenses)
                {
                    stream.WriteLine($"{l.FullName},{l.LicenseNumber},{l.LicenseType},{l.Status},{l.City},{l.State},{l.Zip},{l.Email},{l.Phone}");
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
