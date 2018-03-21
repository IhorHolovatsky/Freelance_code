using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using HtmlAgilityPack;
using RestSharp;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace MiamiDade_Scraper
{
    public partial class Form1 : Form
    {
        public static HtmlDocument SearchPage { get; set; }
        public static RestClient Client { get; set; } = CreateRestClient();
        public static CookieContainer Cookies { get; set; } = new CookieContainer();

        public bool IsCaptchaPosted { get; set; }
        public string AuthId { get; set; }
        public int PageNumber = 1;
        public int TotalPages = 0;
        public List<PersonLicense> Licenses { get; set; } = new List<PersonLicense>();

        private const string BASE_URL = "https://www2.miami-dadeclerk.com";
        private const string HOME_URL = "/officialrecords/StandardSearch.aspx";
        private const string SEARCH_URL = "/officialrecords/StandardSearch.aspx";
        private const string DETAILS_URL = "/nylinxext/elprsmain.alice";
        private const string AUTH_ID_URL = "/nylinxext/elfetchid.alice";

        public Form1()
        {
            InitializeComponent();
            lb_filePath.Text = ConfigurationManager.AppSettings["OutputFilePath"];
            tbStartDate.Text = ConfigurationManager.AppSettings["StartDate"];
            tbEndDate.Text = ConfigurationManager.AppSettings["EndDate"];

            ShowCaptcha();
        }

        private void btn_Search_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }

            var lastName = tbName.Text;

            new Thread(() =>
                {
                    //STEP 1: Perform Search
                    LogMessage("\n Perform search: ");
                    PageNumber = 1;
                    var licenseAndDetailUrls = new List<PersonLicense>();
                    try
                    {
                        licenseAndDetailUrls = FindActiveLicences(lastName);
                    }
                    catch (IndexOutOfRangeException ee)
                    {
                        MessageBox.Show(ee.Message);
                        return;
                    }

                    //STEP 2: Fetch details
                    LogMessage("\n");
                    LogMessage("\n Fetch license details: ");

                    FetchLicenseDetails(lastName, licenseAndDetailUrls);

                    //STEP 3: Save to file
                    LogMessage("\n");
                    LogMessage($"\n Save to file - {lb_filePath.Text}");

                    var filteredRows = licenseAndDetailUrls.Where(l => l.Rows.Count > 0).ToList();
                    SaveToCsv(lb_filePath.Text, lastName, filteredRows);

                    LogMessage("\n-----------------------------");
                    LogMessage("\nDONE!");
                    LogMessage("\n-----------------------------");


                    ShowCaptcha();
                })
                .Start();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrEmpty(tbName.Text))
            {
                MessageBox.Show("Input Last Name!");
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
            var response = client.Execute(request);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(response.Content);
            SearchPage = htmlDoc;

            var captchaUrl = htmlDoc.DocumentNode.SelectSingleNode("//img[@id='c_standardsearch_samplecaptcha_CaptchaImage']")?.Attributes["src"].Value;

            if (string.IsNullOrEmpty(captchaUrl))
            {
                MessageBox.Show("Error during loading captcha!");
                return;
            }

            //GET Patcha
            request = new RestRequest(captchaUrl, Method.GET);
            response = client.Execute(request);

            pbCaptcha.Image = Image.FromStream(new MemoryStream(response.RawBytes));

            pbCaptcha.InvokeEx(b => b.Visible = true);
            tbCaptcha.InvokeEx(b => b.Visible = true);
        }

        #endregion

        private List<PersonLicense> FindActiveLicences(string lastName)
        {
            //This site has client-side paging, so we get all needed data by only one request
            var licenseDetailLinks = new List<PersonLicense>();

            //PERFORM SEARCH:
            var results = DoSearch(lastName,SearchPage).DocumentNode;

          
           
            while (true)
            {
                LogMessage($"\n\t Page {PageNumber}...");
                var resultTable = results.SelectSingleNode("//table[@class='table table-condensed ']");

                if (results.SelectSingleNode("//input[@id='LBD_VCID_c_standardsearch_samplecaptcha']") != null)
                {
                    ShowCaptcha();
                    throw new IndexOutOfRangeException("Invalid entered captcha code!");
                }

                if (resultTable == null)
                {
                    ShowCaptcha();
                    throw new IndexOutOfRangeException("No results found! Or something went wrong on server... click 'Search' again please");
                }

                var rows = resultTable.SelectNodes(".//tr");

                //if something strange .. just skip
                if (rows == null)
                {
                    return licenseDetailLinks;
                }

                var paramRegex = new Regex("newWindow\\(\'(.+)\'");

                foreach (var row in rows)
                {
                    var detailsLink = paramRegex.Match(HttpUtility.HtmlDecode(row?.Attributes?["onclick"].Value ?? string.Empty)).Groups[1].Value;
                    var docNumber = row?.SelectSingleNode(".//td[1]//a")?.InnerText;

                    if (licenseDetailLinks.Any(l => l.LicenseNo.Equals(docNumber, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        continue;
                    }

                    var license = new PersonLicense()
                    {
                        DetailsUrl = detailsLink,
                        LicenseNo = docNumber
                    };
                    licenseDetailLinks.Add(license);
                }
                
                

                //Condition for ending loop
                PageNumber++;
                if (results.SelectSingleNode($"//ul[@class='pagination']") == null
                    || results.SelectSingleNode($"//ul[@class='pagination']//li") == null
                    || results.SelectSingleNode($"//ul[@class='pagination']//li[last()]//a").InnerText.ToInt() < PageNumber)
                {
                    break;
                }
                var pageHref= results.SelectSingleNode($"//ul[@class='pagination']//li[{PageNumber}]//a").Attributes["href"].Value;
                
                results = NextPage(Regex.Match(HttpUtility.HtmlDecode(pageHref ?? string.Empty), "WebForm_PostBackOptions\\(\"(.+btnPage)\"").Groups[1].Value, results).DocumentNode;
            }

            return licenseDetailLinks;
        }

        private void FetchLicenseDetails(string lastName, List<PersonLicense> licenseDetailUrls)
        {
            var progress = 1;

            //Parallel.ForEach(licenseDetailUrls, licenseDetailsUrl =>
            foreach (var licenseDetailsUrl in licenseDetailUrls.Where(l => !string.IsNullOrEmpty(l.LicenseNo)).ToList())
            {
                var response = GetDocumentDetails(licenseDetailsUrl.DetailsUrl);
                
                ParseLicenseDetails(response, licenseDetailsUrl);

                lock (Client)
                {
                    lbPercentDone.InvokeEx(lbl => lbl.Text = $"{100 * progress++ / licenseDetailUrls.Count}%");
                }
            }
            //);

        }

        private HtmlDocument DoSearch(string lastName,
                                      HtmlDocument doc,
                                      PersonLicense license = null)
        {
            var data = new Dictionary<string, string>
            {
               {"__EVENTTARGET", WebUtility.UrlEncode(doc.DocumentNode.SelectSingleNode("//input[@id='__EVENTTARGET']").Attributes["value"].Value)},
               {"__EVENTARGUMENT", WebUtility.UrlEncode(doc.DocumentNode.SelectSingleNode("//input[@id='__EVENTARGUMENT']").Attributes["value"].Value)},
               {"__VIEWSTATE", WebUtility.UrlEncode(doc.DocumentNode.SelectSingleNode("//input[@id='__VIEWSTATE']").Attributes["value"].Value)},
               {"__VIEWSTATEGENERATOR", WebUtility.UrlEncode(doc.DocumentNode.SelectSingleNode("//input[@id='__VIEWSTATEGENERATOR']").Attributes["value"].Value)},
               {"__EVENTVALIDATION", WebUtility.UrlEncode(doc.DocumentNode.SelectSingleNode("//input[@id='__EVENTVALIDATION']").Attributes["value"].Value)},
               {"ctl00$ContentPlaceHolder1$pfirst_party", WebUtility.UrlEncode(tbName.Text)},
               {"ctl00$ContentPlaceHolder1$prec_date_from", tbStartDate.Text},
               {"ctl00$ContentPlaceHolder1$prec_date_to", tbEndDate.Text},
               {"ctl00$ContentPlaceHolder1$pdoc_type", "MOR"},
               {"ctl00$ContentPlaceHolder1$prec_booktype", "O"},
               {"ctl00$ContentPlaceHolder1$btnNameSearch", "Search"},
               {"LBD_VCID_c_standardsearch_samplecaptcha", WebUtility.UrlEncode(doc.DocumentNode.SelectSingleNode("//input[@id='LBD_VCID_c_standardsearch_samplecaptcha']").Attributes["value"].Value)},
               {"LBD_BackWorkaround_c_standardsearch_samplecaptcha", WebUtility.UrlEncode(doc.DocumentNode.SelectSingleNode("//input[@id='LBD_BackWorkaround_c_standardsearch_samplecaptcha']").Attributes["value"].Value)},
               {"ctl00$ContentPlaceHolder1$CaptchaCodeTextBox", tbCaptcha.Text},
            };

            //if (pageNumber.HasValue)
            //{
            //    //TODO:
            //}
            

            var formData = string.Join("&", data.Select(d => $"{d.Key}={d.Value}"));

            var request = new RestRequest(license == null ? SEARCH_URL : DETAILS_URL, Method.POST);
            request.AddParameter("application/x-www-form-urlencoded", formData, ParameterType.RequestBody);

            var response = CreateRestClient().Execute(request);

            var htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(response.Content);

            return htmldoc;
        }

        private HtmlDocument NextPage(string btnName,
                                      HtmlNode doc)
        {
            var data = new Dictionary<string, string>
            {
               {"__EVENTTARGET", WebUtility.UrlEncode(btnName)},
               {"__EVENTARGUMENT", WebUtility.UrlEncode(doc.SelectSingleNode("//input[@id='__EVENTARGUMENT']").Attributes["value"].Value)},
               {"__VIEWSTATE", WebUtility.UrlEncode(doc.SelectSingleNode("//input[@id='__VIEWSTATE']").Attributes["value"].Value)},
               {"__VIEWSTATEGENERATOR", WebUtility.UrlEncode(doc.SelectSingleNode("//input[@id='__VIEWSTATEGENERATOR']").Attributes["value"].Value)},
               {"__EVENTVALIDATION", WebUtility.UrlEncode(doc.SelectSingleNode("//input[@id='__EVENTVALIDATION']").Attributes["value"].Value)},
            };

            //if (pageNumber.HasValue)
            //{
            //    //TODO:
            //}


            var formData = string.Join("&", data.Select(d => $"{d.Key}={d.Value}"));

            var request = new RestRequest(SEARCH_URL, Method.POST);
            request.AddParameter("application/x-www-form-urlencoded", formData, ParameterType.RequestBody);

            var response = CreateRestClient().Execute(request);

            var htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(response.Content);

            return htmldoc;
        }


        private HtmlDocument GetDocumentDetails(string url)
        {
            var request = new RestRequest("https://www2.miami-dadeclerk.com/officialrecords/" + url, Method.GET);
            var response = CreateRestClient().Execute(request);

            var hDoc = new HtmlDocument();
            hDoc.LoadHtml(response.Content);
            var doc = hDoc.DocumentNode;

            var data = new Dictionary<string, string>
            {
               {"__EVENTTARGET", WebUtility.UrlEncode("btnCFNDetails")},
               {"__EVENTARGUMENT", WebUtility.UrlEncode(doc.SelectSingleNode("//input[@id='__EVENTARGUMENT']").Attributes["value"].Value)},
               {"__VIEWSTATE", WebUtility.UrlEncode(doc.SelectSingleNode("//input[@id='__VIEWSTATE']").Attributes["value"].Value)},
               {"__VIEWSTATEGENERATOR", WebUtility.UrlEncode(doc.SelectSingleNode("//input[@id='__VIEWSTATEGENERATOR']").Attributes["value"].Value)},
               {"__EVENTVALIDATION", WebUtility.UrlEncode(doc.SelectSingleNode("//input[@id='__EVENTVALIDATION']").Attributes["value"].Value)},
               {"txtDocPages", WebUtility.UrlEncode(doc.SelectSingleNode("//input[@id='txtDocPages']").Attributes["value"].Value)},
               {"txtPages", WebUtility.UrlEncode(doc.SelectSingleNode("//input[@id='txtPages']").Attributes["value"].Value)},
               {"txtZoom", WebUtility.UrlEncode(doc.SelectSingleNode("//input[@id='txtZoom']").Attributes["value"].Value)},
               {"txtPageUrls", WebUtility.UrlEncode(doc.SelectSingleNode("//input[@id='txtPageUrls']").Attributes["value"].Value)},
            };
            var formData = string.Join("&", data.Select(d => $"{d.Key}={d.Value}"));

            request = new RestRequest("https://www2.miami-dadeclerk.com/officialrecords/" +url, Method.POST);
            request.AddParameter("application/x-www-form-urlencoded", formData, ParameterType.RequestBody);

            response = CreateRestClient().Execute(request);

            var htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(response.Content);

            return htmldoc;
        }

        private void ParseLicenseDetails(HtmlDocument doc, PersonLicense license)
        {
            var root = doc.DocumentNode;

            var records = root.SelectNodes("//table//tr[1]//td[2]")?.Where(node => !node.InnerText.Contains("(R)")).ToList();

            foreach (var record in records ?? Enumerable.Empty<HtmlNode>())
            {
                var docRow = new DocumentRow
                {
                    FirstParty = record.SelectSingleNode("../../tr[1]//td[2]")?.InnerText.CleanupString(),
                    SecondParty = record.SelectSingleNode("../../tr[1]//td[4]")?.InnerText.CleanupString(),
                    RecDate = record.SelectSingleNode("../../tr[1]//td[8]")?.InnerText.CleanupString(),

                    SubdivisionName = record.SelectSingleNode("../../tr[4]//td[2]")?.InnerText.CleanupString(),
                    LegalDescription = record.SelectSingleNode("../../tr[4]//td[4]")?.InnerText.CleanupString()
                };

                if (new CultureInfo("en-US").CompareInfo.IndexOf(docRow.SecondParty, 
                                                                 tbName.Text,
                                                                 CompareOptions.IgnoreCase) >= 0)
                {
                    license.Rows.Add(docRow);
                }
            }

            if (license.Rows.Count > 0)
            {
                LogMessage("\n------------------");
                LogMessage($"\n\t DocNumber: {license.LicenseNo}" +
                           $"\n\t Rows: {string.Join(";", license.Rows.Select(r => $"{r.FirstParty}|{r.SecondParty}|{r.RecDate}|{r.SubdivisionName}|{r.LegalDescription}"))}");
            }
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
            var fullPath = filePath + $"\\MiamiDade_{lastName}_{tbStartDate.Text}-{tbEndDate.Text}.csv".Replace("/", " ");

            using (var stream = File.CreateText(fullPath))
            {
                const string columnsLine = "docNumber,firstParty,secondParty,recDate,subdivisionName,legalDescription";
                stream.WriteLine(columnsLine);

                foreach (var l in personLicenses)
                {
                    foreach (var r in l.Rows)
                    {
                        stream.WriteLine($"{l.LicenseNo},{r.FirstParty},{r.SecondParty},{r.RecDate},{r.SubdivisionName},{r.LegalDescription}");
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
