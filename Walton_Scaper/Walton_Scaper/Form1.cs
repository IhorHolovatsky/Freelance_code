using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace Walton_Scaper
{
    public partial class Form1 : Form
    {
        public static RestClient Client { get; set; } = CreateRestClient();
        public static CookieContainer Cookies { get; set; } = new CookieContainer();

        private const string BASE_URL = "http://orsearch.clerkofcourts.co.walton.fl.us";
        private const string SEARCH_URL = "/Search/DocumentTypeSearch";
        private const string DISCLAIMER_URL = "/LandmarkWeb/Search/SetDisclaimer";
        private const string HOME_URL = "/LandmarkWeb/home/index";
        private const string DETAILS_URL = "/LandmarkWeb/Document/Index";

        public Form1()
        {
            InitializeComponent();
            lb_filePath.Text = ConfigurationManager.AppSettings["OutputFilePath"];
            tbDocTypes.Text = ConfigurationManager.AppSettings["DocTypes"];
            tbStartDate.Text = ConfigurationManager.AppSettings["StartDate"];
            tbEndDate.Text = ConfigurationManager.AppSettings["EndDate"];
        }

        private void btn_Search_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }
            lb_logs.Clear();

            var docTypes = tbDocTypes.Text;
            var startDate = tbStartDate.Text;
            var endDate = tbEndDate.Text;

            new Thread(() =>
                {
                    //STEP 0: Accept Disclaimer
                    LogMessage("\n Accept Disclaimer: ");
                    AcceptDisclaimer();
                    LogMessage("\n\t ACCEPTED! ");

                    //STEP 1: Perform Search
                    LogMessage("\n Perform search: ");

                    var licenseAndDetailUrls = new List<PersonLicense>();
                    try
                    {
                        licenseAndDetailUrls = FindActiveLicences(docTypes, startDate, endDate);
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

                    SaveToCsv(lb_filePath.Text, startDate, endDate, licenseAndDetailUrls);

                    LogMessage("\n-----------------------------");
                    LogMessage("\nDONE!");
                    LogMessage("\n-----------------------------");
                })
                .Start();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrEmpty(tbDocTypes.Text))
            {
                MessageBox.Show("Input document types!");
                return false;
            }

            if (string.IsNullOrEmpty(tbStartDate.Text))
            {
                MessageBox.Show("Input start date!");
                return false;
            }
            
            if (string.IsNullOrEmpty(tbEndDate.Text))
            {
                MessageBox.Show("Input end date!");
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

        #region Accepting Disclaimer 

        private void AcceptDisclaimer()
        {
            var client = CreateRestClient();
            
            //GET initial cookies
            var request = new RestRequest(HOME_URL, Method.GET);
            client.Execute(request);

            //Accept disclaimer
            request = new RestRequest(DISCLAIMER_URL, Method.POST);
            client.Execute(request);
        }

        #endregion

        private List<PersonLicense> FindActiveLicences(string docTypes,
                                                       string startDate,
                                                       string endDate)
        {
            //This site has client-side paging, so we get all needed data by only one request
            var licenseDetailLinks = new List<PersonLicense>();

            //PERFORM SEARCH:
            var results = DoSearch(docTypes, startDate, endDate).DocumentNode;

            var dataJsonRegex = new Regex("\"aaData\": (.*)");
            var licensesJsonString = dataJsonRegex.Match(results.InnerHtml).Groups?[1].Value;

            if (string.IsNullOrEmpty(licensesJsonString))
            {
                throw new IndexOutOfRangeException("No results found!");
            }

            var licensesJson = JArray.Parse(licensesJsonString);

            LogMessage($"\n\t Found {licensesJson.Count} records!");

            licenseDetailLinks = licensesJson.Select(j => new PersonLicense
            {
                DocumentId = j["DT_RowId"].Value<string>().Replace("doc_", ""),
                RecordDate = j["7"].Value<string>().Replace("nobreak_", ""),
                DocType = j["8"].Value<string>().Replace("nobreak_", "").Trim(),
                Grantees = j["6"].Value<string>().Contains("<div class='nameSeperator'></div>")
                                    ? j["6"].Value<string>().Split(new[] { "<div class='nameSeperator'></div>" }, StringSplitOptions.RemoveEmptyEntries).ToList()
                                    : new List<string> { j["6"].Value<string>() },

                Grantors = j["5"].Value<string>().Contains("<div class='nameSeperator'></div>")
                                    ? j["5"].Value<string>().Split(new[] { "<div class='nameSeperator'></div>" }, StringSplitOptions.RemoveEmptyEntries).ToList()
                                    : new List<string> { j["5"].Value<string>() }
            }).ToList();

            return licenseDetailLinks;
        }

        private void FetchLicenseDetails(List<PersonLicense> licenseDetailUrls)
        {
            var i = 1;

            Parallel.ForEach(licenseDetailUrls, licenseDetailsUrl =>
            //foreach (var licenseDetailsUrl in licenseDetailUrls)
            {
                var client = CreateRestClient();
                var request = new RestRequest(DETAILS_URL, Method.POST);
                
                var data = new Dictionary<string, string>() { {"id", licenseDetailsUrl.DocumentId} };
                var formData = string.Join("&", data.Select(d => $"{d.Key}={d.Value}"));
                request.AddParameter("application/x-www-form-urlencoded", formData, ParameterType.RequestBody);

                var response = client.Execute(request);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(response.Content);

                LogMessage("\n------------------");

                ParseLicenseDetails(htmlDoc, licenseDetailsUrl);

                lock (Client)
                {
                    lbPercentDone.InvokeEx(lbl => lbl.Text = $"{100 * i++ / licenseDetailUrls.Count}%");
                }
            }
            );

        }

        private HtmlDocument DoSearch(string docTypes,
                                      string startDate,
                                      string endDate)
        {
            var data = new Dictionary<string, string>
            {
                {"doctype", docTypes},
                {"beginDate", startDate},
                {"endDate", endDate},
                {"recordCount", "2000"},
                {"townName", ""}
            };

            var formData = string.Join("&", data.Select(d => $"{d.Key}={d.Value}"));

            var request = new RestRequest(SEARCH_URL, Method.POST);
            request.AddParameter("application/x-www-form-urlencoded", formData, ParameterType.RequestBody);

            var response = CreateRestClient().Execute(request);

            var htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(response.Content);

            return htmldoc;
        }

        private void ParseLicenseDetails(HtmlDocument doc, PersonLicense license)
        {
            var root = doc.DocumentNode;
            
            license.Consideration = root.SelectSingleNode("//label[@for='Consideration1 ']//..//..//td[2]")?.InnerText.Replace("\t", " ").Replace("\n", " ").Trim();
            license.FullLegalName = root.SelectSingleNode("//label[@for='Full Legal ']//..//..//td[2]")?.InnerText.Replace("\t", " ").Replace("\n", " ").Trim();

            LogMessage($"\n\t Doc Id: {license.DocumentId}" +
                       $"\n\t Doc Type: {license.DocType}" +
                       $"\n\t Record Date: {license.RecordDate}" +
                       $"\n\t Consideration: {license.Consideration}" +
                       $"\n\t Grantees: {string.Join(",", license.Grantees)}" +
                       $"\n\t Grantors: {string.Join(",",license.Grantors)}");
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

        private void SaveToCsv(string filePath, string startDate, string endDate, List<PersonLicense> personLicenses)
        {
            var fullPath = filePath + $"\\walton_{startDate}-{endDate}.csv".Replace("/", " ");

            using (var stream = File.CreateText(fullPath))
            {
                const string columnsLine = "recordDate,docType,grantors,grantees,consideration,fullLegalName";
                stream.WriteLine(columnsLine);

                foreach (var l in personLicenses)
                {
                    var grantorsString = string.Join(";", l.Grantors);
                    var grenteesString = string.Join(";", l.Grantees);
                    stream.WriteLine($"{l.RecordDate},{l.DocType},{grantorsString.Replace(",", "")},{grenteesString.Replace(",", "")},{l.Consideration.Replace(",", "")},{l.FullLegalName.Replace(",", "")}");
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
