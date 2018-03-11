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
using Newtonsoft.Json.Linq;
using RestSharp;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace PalmBeach_Scraper
{
    public partial class Form1 : Form
    {
        public static RestClient Client { get; set; } = CreateRestClient();
        public static CookieContainer Cookies { get; set; } = new CookieContainer();

        private const string BASE_URL = "http://oris.co.palm-beach.fl.us";
        private const string SEARCH_URL = "/or_web1/new_sch.asp";
        private const string DETAILS_URL = "/or_web1/";

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

        private List<PersonLicense> FindActiveLicences(string docTypes,
                                                       string startDate,
                                                       string endDate)
        {
            //This site has client-side paging, so we get all needed data by only one request
            var licenseDetailLinks = new List<PersonLicense>();

            //PERFORM SEARCH:
            var results = DoSearch(docTypes, startDate, endDate).DocumentNode;

            var pageCountNodes = results.SelectNodes("//select[@name='PageNumber']//option");

            if (pageCountNodes == null)
            {
                throw new IndexOutOfRangeException("No results found!");
            }
            var pageCount = pageCountNodes.Count;

            LogMessage($"\n\t Found {pageCount} pages!");

            var i = 1;
            while (true)
            {
                LogMessage($"\n\t\t Page {i}...");

                var rows = results.SelectNodes("//a[@class='list_2']");

                //if something strange .. just skip
                if (rows == null)
                {
                    i++;
                    continue;
                }

                var licenses = rows.Select(node => new PersonLicense {DetailsUrl = node.Attributes["href"].Value
                                                                                                          .Replace("\t", "")
                                                                                                          .Replace("\n", "")
                                                                                                          .Replace("\r", "") })
                                   .ToList();
                licenseDetailLinks.AddRange(licenses);

                //Condition for ending loop
                i++;
                if (i > pageCount)
                {
                    break;
                }

                results = DoSearch(docTypes, startDate, endDate, i).DocumentNode;
            }

            return licenseDetailLinks;
        }

        private void FetchLicenseDetails(List<PersonLicense> licenseDetailUrls)
        {
            var progress = 1;

            Parallel.ForEach(licenseDetailUrls, licenseDetailsUrl =>
            //foreach (var licenseDetailsUrl in licenseDetailUrls)
            {
                var client = CreateRestClient();
                var request = new RestRequest(DETAILS_URL + licenseDetailsUrl.DetailsUrl.Replace("details", "details_des"), Method.GET);
                var response = client.Execute(request);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(response.Content);

                LogMessage("\n------------------");

                ParseLicenseDetails(htmlDoc, licenseDetailsUrl);

                lock (Client)
                {
                    lbPercentDone.InvokeEx(lbl => lbl.Text = $"{100 * progress++ / licenseDetailUrls.Count}%");
                }
            }
            );

        }

        private HtmlDocument DoSearch(string docTypes,
                                      string startDate,
                                      string endDate,
                                      int? pageNumber = null)
        {
            const int PageSize = 100;

            var data = new Dictionary<string, string>
            {
                {"search_by", "DocType"},
                {"search_entry", docTypes},
                {"consideration", ""},
                {"FromDate", startDate},
                {"ToDate", endDate},
                {"RecSetSize", "2000"},
                {"PageSize", PageSize.ToString()}
            };

            if (pageNumber.HasValue)
            {
                data.Add("RecSetOffset", ((pageNumber -1) * PageSize).ToString() );
                data.Add("PageNumber", pageNumber.Value.ToString());
            }

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

            license.DocType = root.SelectSingleNode("//td[text()='Type:']//..//td[@class='details_des_plain']")?.InnerText;
            license.DateTime = root.SelectSingleNode("//td[text()='Date/Time:']//..//td[@class='details_des_plain']")?.InnerText.CleanupString();
            license.Consideration = root.SelectSingleNode("//td[text()='Consideration:']//..//td[@class='details_des_plain']")?.InnerText.CleanupString();

            license.Party1 = root.SelectNodes("//td[text()='Party 1:']//..//td[@class='details_des_plain']//dl//dt")?.Select(node => node.InnerText.CleanupString()).ToList() ?? new List<string>();
            license.Party2 = root.SelectNodes("//td[text()='Party 2:']//..//td[@class='details_des_plain']//dl//dt")?.Select(node => node.InnerText.CleanupString()).ToList() ?? new List<string>();
            license.Legal = root.SelectSingleNode("//td[text()='Legal:']//..//td[@class='details_des_plain']//dl//dt")?.InnerText.CleanupString();

            LogMessage($"\n\t Doc Type: {license.DocType}" +
                       $"\n\t Record Date: {license.DateTime}" +
                       $"\n\t Consideration: {license.Consideration}" +
                       $"\n\t Legal: {license.Legal}" +
                       $"\n\t Party1: {string.Join(",", license.Party1)}" +
                       $"\n\t Party2: {string.Join(",", license.Party2)}");
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
            var fullPath = filePath + $"\\palmBeach_{startDate}-{endDate}.csv".Replace("/", " ");

            using (var stream = File.CreateText(fullPath))
            {
                const string columnsLine = "recordDate,docType,party1,party2,consideration,legal";
                stream.WriteLine(columnsLine);

                foreach (var l in personLicenses)
                {
                    var party1String = string.Join(";", l.Party1);
                    var party2String = string.Join(";", l.Party2);
                    stream.WriteLine($"{l.DateTime},{l.DocType},{party1String.Replace(",", "")},{party2String.Replace(",", "")},{l.Consideration?.Replace(",", "")},{l.Legal?.Replace(",", "")}");
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
