using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using RestSharp;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace OrangeCountyCA_Scraper
{
    public partial class Form1 : Form
    {
        public static CookieContainer Cookies { get; set; } = new CookieContainer();
        public static RestClient Client { get; set; } = CreateRestClient();

        private const string BASE_URL = "https://cr.ocgov.com/";
        private const string SEARCH_URL = "/recorderworks/Presentors/AjaxPresentor.aspx";
        private const string DETAILS_URL = "/recorderworks/Presentors/DetailsPresentor.aspx";

        public Form1()
        {
            InitializeComponent();

            tbStartDate.Text = ConfigurationManager.AppSettings["StartDate"];
            tbEndDate.Text = ConfigurationManager.AppSettings["EndDate"];
            lb_filePath.Text = ConfigurationManager.AppSettings["OutputFilePath"];
        }

        private void btn_Search_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }
            lb_logs.Clear();
            
            var licenseName = tbName.Text.ToUpper();

            new Thread(() =>
                {
                    //STEP : Init Session
                    LogMessage("\n Init Session: ");
                    InitSession();

                    //STEP 1: Perform Search
                    LogMessage("\n Perform search: ");

                    var licenseDetailUrls = new List<PersonLicense>();
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

                    var filteredRows = FetchLicenseDetails(licenseDetailUrls);

                    //STEP 3: Save to file
                    LogMessage("\n");
                    LogMessage($"\n Save to file - {lb_filePath.Text}");

                    SaveToCsv(lb_filePath.Text, licenseName, filteredRows);

                    LogMessage("\n-----------------------------");
                    LogMessage("\nDONE!");
                    LogMessage("\n-----------------------------");
                })
                .Start();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrEmpty(tbName.Text))
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
            var pageNumber = 1;
            var licenseDetailLinks = new List<PersonLicense>();

            //PERFORM SEARCH:
            var results = DoSearch(SEARCH_URL, licenseName).DocumentNode;

            int _;
                var totalCountNode = results.SelectSingleNode(".//span[@id='SearchResultsTitle1_resultCount']");
            if (totalCountNode == null
                || !int.TryParse(totalCountNode.InnerText, out _)
                || int.Parse(totalCountNode.InnerText) == 0)
            {
                throw new IndexOutOfRangeException("No results found!");
            }

            var lastPageNode = results.SelectSingleNode(".//td[@id='SearchResultsTitle1_paging']//td[last()]");
            var pageCount = lastPageNode == null
                ? 1
                : Regex.Match(HttpUtility.HtmlDecode(lastPageNode.Attributes["onclick"].Value ?? string.Empty), "OnPage\\(\'(\\w+)\'").Groups?[1].Value.ToInt() ?? 1;

            LogMessage($"\n Found {pageCount} pages!");

            while (true)
            {

                LogMessage($"\n \t Page: {pageNumber}");

                var rows = results.SelectNodes("//td[@class='docLinkTD resultFieldUnderline']");

                if (rows == null)
                {
                    if (results.InnerText.Contains("No Records returned"))
                        throw new IndexOutOfRangeException("No records found.. try another string");

                    throw new IndexOutOfRangeException("A lot of rows... try another string");
                }

                //Loop through rows and filter not needed rows
                foreach (var row in rows)
                {
                    var license = new PersonLicense();

                    var tdLinkValue = row?.Attributes["onclick"]?.Value;
                    var docNumber = row?.SelectSingleNode(".//span[contains(@id,'docNumber')]")?.InnerText;

                    if (string.IsNullOrEmpty(docNumber)
                        || licenseDetailLinks.Any(l => l.DocNumber.Equals(docNumber, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        continue;
                    }

                    license.DocNumber = docNumber;
                    license.DetailsUrl = Regex.Match(HttpUtility.HtmlDecode(tdLinkValue ?? string.Empty), "showDetails\\(\'(.+)\',\'ImgControlWidth")?.Groups?[1]?.Value;
                 
                    //Skip empty license numbers
                    if (string.IsNullOrEmpty(license.DetailsUrl))
                    {
                        LogMessage($"\n \t Something strange happened...");
                        continue;
                    }

                    licenseDetailLinks.Add(license);
                }
                
                pageNumber++;

                if (pageNumber > pageCount)
                {
                    break;
                }

                //Go to Next page
                results = DoSearch(SEARCH_URL, licenseName, pageNumber).DocumentNode;
            }

            return licenseDetailLinks;
        }

        private List<PersonLicense> FetchLicenseDetails(List<PersonLicense> licenseDetailUrls)
        {
            var list = new List<PersonLicense>();

            LogMessage("\n------------------");

            Parallel.ForEach(licenseDetailUrls, licenseDetailsUrl =>
            //foreach (var licenseDetailsUrl in licenseDetailUrls)
            {
                var client = CreateRestClient();
                var request = new RestRequest(DETAILS_URL, Method.POST);
                request.AddParameter("application/x-www-form-urlencoded", licenseDetailsUrl.DetailsUrl, ParameterType.RequestBody);

                var response = client.Execute(request);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(response.Content);

                var license = ParseLicenseDetails(htmlDoc, licenseDetailsUrl);

                if (license.DocumentTypes != null
                    && license.DocumentTypes.Count > 0)
                {
                    LogMessage("\n------------------");
                    list.Add(license);
                }
            }
            );

            return list;
        }

        private HtmlDocument DoSearch(string url,
                                      string licenseName,
                                      int? pageNumber = null)
        {
            var request = new RestRequest(url, Method.POST);
            var data = new Dictionary<string, string>
            {
                {"PartyType", "0"},
                {"NameForSearch", WebUtility.UrlEncode($"{licenseName}")},
                {"AllowPartial", "true"},
                {"FromDate", tbStartDate.Text},
                {"ToDate", tbEndDate.Text},
                {"ERetrievalGroup", "1"},
                {"SearchMode", "1"},
                {"IsNewSearch", "true"},
                { "DocumentTypes", "5,1,"},
                { "DocumentNames", "GRANT DEED, TRUST DEED,"}
            };

            //if are moving to next page, we need other set of params
            if (pageNumber.HasValue)
            {
                data.Add("PageNum", pageNumber.Value.ToString());
            }
			
            var formData = string.Join("&", data.Select(d => $"{d.Key}={d.Value}"));

            request.AddParameter("application/x-www-form-urlencoded", formData, ParameterType.RequestBody);

            var response = Client.Execute(request);

            var htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(response.Content);

            return htmldoc;
        }

        private PersonLicense ParseLicenseDetails(HtmlDocument doc, PersonLicense license)
        {
            var root = doc.DocumentNode;

            license.RecordingDate = root.SelectSingleNode("//div[@id='generalData']//td[last()]")?.InnerText;

            var documentTypes = root.SelectNodes("//div[@id='DocumentTitlesList']//tr//td[2]")?.Select(n => n.InnerText.CleanupString()).ToList();

            var docTypes = new List<string> {"GRANT DEED", "TRUST DEED"};

            documentTypes = documentTypes?.Where(d => docTypes.Any(dt => dt.Equals(d, StringComparison.CurrentCultureIgnoreCase))).ToList();

            if (documentTypes == null
                || documentTypes.Count == 0)
            {
                return license;
            }

            for (var i = 1; i <= documentTypes.Count; i++)
            {
                var docType = new Document()
                {
                    DocumentType = documentTypes[i-1]
                };

                var grantors = root.SelectNodes($"//div[@id='Grantors']//table[{i}]//tr//td")?.Select(n => n.InnerText.CleanupString()).ToList();
                var grantees = root.SelectNodes($"//div[@id='Grantees']//table[{i}]//tr//td")?.Select(n => n.InnerText.CleanupString()).ToList();

                docType.Grantees = grantees;
                docType.Grantors = grantors;

                license.DocumentTypes.Add(docType);
            }

            LogMessage($"\n\t RecordingDate: {license.RecordingDate}" +
                       $"\n\t DocNumber: {license.DocNumber}" +
                       $"\n\t DocumentTypes: {string.Join(";", license.DocumentTypes.Select(l => $"{l.DocumentType}|{string.Join("^", l.Grantors)}|{string.Join("^", l.Grantees)}"))}");

            return license;
        }

        private static void InitSession()
        {
            var client = CreateRestClient();
            var request = new RestRequest("https://cr.ocgov.com/recorderworks/", Method.GET);

            client.Execute(request);
        }

        private static RestClient CreateRestClient()
        {
            var restClient = new RestClient
            {
                BaseUrl = new Uri(BASE_URL),
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.186 Safari/537.36",
                CookieContainer = Cookies
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
            var fullPath = filePath + $"\\orangCountyCA_{licenseName}_{tbStartDate.Text}_{tbEndDate.Text}.csv".Replace("/", "");

            using (var stream = File.CreateText(fullPath))
            {
                const string columnsLine = "docNumber,recordDate,documentType,grantors,grantees";
                stream.WriteLine(columnsLine);

                foreach (var l in personLicenses)
                {
                    foreach (var d in l.DocumentTypes)
                    {
                        var grantors = string.Join(";", d.Grantors);
                        var grantees = string.Join(";", d.Grantees);

                        stream.WriteLine($"{l.DocNumber},{l.RecordingDate},{d.DocumentType},{grantors},{grantees}");
                    }
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
