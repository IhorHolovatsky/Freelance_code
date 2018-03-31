using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using HtmlAgilityPack;
using MA_InsuranceScraper.Models;
using RestSharp;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace MA_InsuranceScraper
{
    public partial class Form1 : Form
    {
        public static CookieContainer Cookies { get; set; } = new CookieContainer();
        public static RestClient Client { get; set; } = CreateRestClient();

        private const string BASE_URL = "https://agentfinder.doi.state.ma.us/";
        private const string SEARCH_URL = "/AgentSearchResults.aspx?";

        public Form1()
        {
            InitializeComponent();

            lbStates.SelectionMode= SelectionMode.MultiExtended;
            lbLines.SelectionMode = SelectionMode.MultiExtended;

            var states = "AK,AL,AR,AS,AZ,CA,CO,CT,DC,DE,FL,FM,GA,GU,HI,IA,ID,IL,IN,KS,KY,LA,MA,MD,ME,MH,MI,MN,MO,MP,MS,MT,NC,ND,NE,NH,NJ,NM,NV,NY,OH,OK,OR,PA,PR,PW,RI,SC,SD,TN,TX,UT,VA,VI,VT,WA,WI,WV,WY"
                        .Split(',').ToList();
            
            states.ForEach(s => lbStates.Items.Add(s));
            for (var i = 0; i < lbStates.Items.Count; i++)
            {
                lbStates.SetSelected(i, true);
            }


            lbLines.Items.Add(new AgencyLine("Accident and Health or Sickness", "H"));
            lbLines.Items.Add(new AgencyLine("Life", "L"));
            lbLines.Items.Add(new AgencyLine("Variable Life and Variable Annuity Products", "V"));
            lbLines.Items.Add(new AgencyLine("Property", "P"));
            lbLines.Items.Add(new AgencyLine("Casualty", "C"));
            lbLines.Items.Add(new AgencyLine("Personal Lines", "PL"));
            lbLines.Items.Add(new AgencyLine("Credit (Limited Line)", "CR"));
            lbLines.Items.Add(new AgencyLine("Travel Accident and Baggage", "T"));

            lb_filePath.Text = ConfigurationManager.AppSettings["OutputFilePath"];
        }

        private void btn_Search_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }
            lb_logs.Clear();

            var searchContext = new SearchContext()
            {
                LastName = tb_licenseName.Text,
                States = lbStates.SelectedItems.Cast<string>().ToList(),
                AgencyLines = lbLines.SelectedItems.Cast<AgencyLine>().Select(a => a.Value).ToList()
            };

            new Thread(() =>
                {
                    //STEP 1: Perform Search
                    LogMessage("\n Perform search: ");

                    var licenseDetailUrls = new List<string>();
                    try
                    {
                        licenseDetailUrls = FindActiveLicences(searchContext);
                    }
                    catch (IndexOutOfRangeException ee)
                    {
                        MessageBox.Show(ee.Message);
                        return;
                    }

                    LogMessage($"\n Totally found {licenseDetailUrls.Count} rows.");

                    if (cbDuplicates.Checked)
                    {
                        //Remove duplicated IDs
                        licenseDetailUrls = licenseDetailUrls.Select(u => new
                        {
                            ID = Regex.Match(u, @"ID=(\w*)").Groups[1].Value,
                            url = u
                        })
                                                             .GroupBy(x => x.ID)
                                                             .Select(x => x.First().url)
                                                             .ToList();

                        LogMessage($"\n Removed duplicates. Now we have {licenseDetailUrls.Count} unique rows.");
                    }

                    //STEP 2: Fetch details
                    LogMessage("\n");
                    LogMessage("\n Fetch license details: ");
                    
                    var licenses = FetchLicenseDetails(licenseDetailUrls);

                    //STEP 3: Save to file
                    LogMessage("\n");
                    LogMessage($"\n Save to file - {lb_filePath.Text}");

                    SaveToCsv(lb_filePath.Text, searchContext.LastName, licenses);

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
                MessageBox.Show("Input last name!");
                return false;
            }


            if (tb_licenseName.Text.Length < 2)
            {
                MessageBox.Show("Last name should have minimum 2 digits!");
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

        private List<string> FindActiveLicences(SearchContext context)
        {
            var pageNumber = 0;
            var licenseDetailLinks = new List<string>();

            foreach (var state in context.States)
            {
                foreach (var line in context.AgencyLines)
                {
                    //PERFORM SEARCH:
                    var results = DoSearch(SEARCH_URL,
                                           context,
                                           state,
                                           line).DocumentNode;

                    var pageCountNode = results.SelectSingleNode("//span[@id='ContentPlaceHolder1_lblResultCounts']");
                    if (pageCountNode == null)
                    {
                        throw new IndexOutOfRangeException("No records found!");
                    }

                    var totalRows = Regex.Match(pageCountNode.InnerText, @"of (\d+)").Groups[1].Value.ToInt() ?? 0;
                    var pageCount = totalRows % 20 == 0
                                            ? totalRows / 20
                                            : totalRows / 20 + 1;
                    LogMessage($"\n Search state - '{state}' and line '{line}'. Found {totalRows} rows.");

                    for (var i = 1; i <= pageCount;)
                    {
                        LogMessage($"\n \t Page: {i}");

                        var rows = results.SelectNodes("//tr[contains(@class,'search_result')]");

                        if (rows == null)
                        {
                            if (results.InnerText.Contains("No Records returned"))
                                throw new IndexOutOfRangeException("No records found.. try another string");

                            throw new IndexOutOfRangeException("A lot of rows... try another string");
                        }

                        //Loop through rows
                        licenseDetailLinks.AddRange(from row in rows
                                                    select row.SelectSingleNode(".//td[1]//a")
                                                    into aLink
                                                    where aLink != null
                                                    select aLink.Attributes["href"].Value);
                        
                        i++;

                        //Go to Next page
                        results = DoSearch(SEARCH_URL,
                                           context,
                                           state,
                                           line,
                                           i,
                                           results).DocumentNode;
                    }
                }
            }



            return licenseDetailLinks;
        }

        private List<PersonLicense> FetchLicenseDetails(List<string> licenseDetailUrls)
        {
            var licenses = new List<PersonLicense>();

            //Parallel.ForEach(licenseDetailUrls, licenseDetailsUrl =>
            foreach (var licenseDetailsUrl in licenseDetailUrls)
            {
                var client = Client;
                var request = new RestRequest($"{WebUtility.HtmlDecode(licenseDetailsUrl)}", Method.GET);
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
                                      SearchContext context,
                                      string state,
                                      string line,
                                      int? pageNumber = null,
                                      HtmlNode docHtmlNode = null)
        {
            var data = new Dictionary<string, string>
            {
                {"PATH", "1"},
                {"TYPE", "QUERY"},
                {"LNAME", context.LastName},
                {"STATE", state},
                {"PROD", line}
            };

            var request = new RestRequest(url + data.FormUrlEncodedSerialize(), Method.GET);

            //if are moving to next page, we need other set of params
            if (pageNumber.HasValue)
            {
                request.Method = Method.POST;

                var postData = new Dictionary<string, string>
                {
                    { "__EVENTTARGET", "ctl00$ContentPlaceHolder1$gridResults"     },
                    { "__EVENTARGUMENT", $"Page${pageNumber.Value}"                                 },
                    { "__VIEWSTATE" , docHtmlNode?.SelectSingleNode("//input[@id='__VIEWSTATE']").Attributes["value"].Value  },
                    { "__VIEWSTATEGENERATOR" , docHtmlNode?.SelectSingleNode("//input[@id='__VIEWSTATEGENERATOR']").Attributes["value"].Value                                 },
                    { "ctl00$ContentPlaceHolder1$hLNAME", $"{context.LastName}"},
                    { "ctl00$ContentPlaceHolder1$hFNAME", ""                     },
                    { "ctl00$ContentPlaceHolder1$hTYPE", "0"                     },
                    { "ctl00$ContentPlaceHolder1$hLICENSE",""                    },
                    { "ctl00$ContentPlaceHolder1$hFEIN", ""                      },
                    { "ctl00$ContentPlaceHolder1$hPROD", $"{line}"                   },
                    { "ctl00$ContentPlaceHolder1$hNPN",""                        },
                    { "ctl00$ContentPlaceHolder1$hPATH"," 1"                     },
                    { "ctl00$ContentPlaceHolder1$hAGENCY",""                     },
                    { "ctl00$ContentPlaceHolder1$hCOMPANIES",""                  },
                    { "ctl00$ContentPlaceHolder1$hCITY",""                       },
                    { "ctl00$ContentPlaceHolder1$hSTATE", $"{state}"                   },
                    { "ctl00$ContentPlaceHolder1$hZIP",""                        },
                    { "ctl00$ContentPlaceHolder1$hPROX", "0"                     },
                    { "ctl00$ContentPlaceHolder1$hINSTYPES",""                   },
                    { "ctl00$ContentPlaceHolder1$hSORT",""                       },
                    { "ctl00$ContentPlaceHolder1$hSORTDIR",""                    },
                    { "ctl00$ContentPlaceHolder1$hSORTVALUE",""                  },
                    { "ctl00$ContentPlaceHolder1$hDISPLAYDISTANCE"," 0"          },
                    { "ctl00$hJavaScriptEnabled", "1"                            },
                };

                request.AddParameter("application/x-www-form-urlencoded", postData.FormUrlEncodedSerialize(), ParameterType.RequestBody);
            }

            var response = Client.Execute(request);

            var htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(response.Content);

            return htmldoc;
        }

        private PersonLicense ParseLicenseDetails(HtmlDocument doc)
        {
            var root = doc.DocumentNode;

            var licenseNumber = root.SelectSingleNode("//span[contains(@id,'lblLicenseNumber')]")?.InnerText;
            var linesOfInsurance = root.SelectSingleNode("//span[contains(@id,'lblLinesOfInsurance')]")?.InnerText;
            var address1 = root.SelectSingleNode("//span[contains(@id,'lblAddress1Value')]")?.InnerText;
            var address2 = root.SelectSingleNode("//span[contains(@id,'lblAddress2Value')]")?.InnerText;
            var city = root.SelectSingleNode("//span[contains(@id,'lblBusinessCityValue')]")?.InnerText;
            var state = root.SelectSingleNode("//span[contains(@id,'lblBusinessStateValue')]")?.InnerText;
            var zip = root.SelectSingleNode("//span[contains(@id,'lblZipValue')]")?.InnerText;
            var phone = root.SelectSingleNode("//span[contains(@id,'lblBusinessPhoneValue')]")?.InnerText;
            var email = root.SelectSingleNode("//a[contains(@id,'hlnkEmail')]")?.InnerText;
            var doingAs = root.SelectSingleNode("//span[contains(@id,'lblDBAValue')]")?.InnerText;
            var companyAppointments = root.SelectNodes("//span[contains(@id,'lstCompanyAppointments')]")?.Select(n => n?.InnerText).ToList();
            var fullName = root.SelectSingleNode("//span[contains(@id,'lblNameHeaderValue')]")?.InnerText ?? "a b c";



            var license = new PersonLicense
            {
                LicenseNumber = licenseNumber,
                LinesOfInsurance = linesOfInsurance,
                Address1 = address1,
                Address2 = address2,
                City = city,
                State = state,
                Zip = zip,
                BusinessPhone = phone,
                Email = email,
                DoingAs = doingAs,
                CompanyNames = companyAppointments,
                FirstName = fullName.Split(' ')[0],
                LastName = fullName.Split(' ')[1].EndsWith(".") ? fullName.Split(' ')[2] : fullName.Split(' ')[1],
                MiddleName = fullName.Split(' ')[1].EndsWith(".") ? fullName.Split(' ')[1].Replace(".", string.Empty) : string.Empty,
            };


            LogMessage($"\n\t License: {license.LicenseNumber}" +
                       $"\n\t Email: {license.Email}");

            return license;
        }

        private static RestClient CreateRestClient()
        {
            var restClient = new RestClient
            {
                CookieContainer = Cookies,
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
            var fullPath = filePath + $"\\MA_insurance_{licenseName}.csv".Replace("/", "");

            using (var stream = File.CreateText(fullPath))
            {
                const string columnsLine = "licenseNumber,firstName,middleName,lastName,city,state,zip,email,phone,address1,address2,doingAs,linesOfInsurance,companyAppointments";
                stream.WriteLine(columnsLine);

                foreach (var l in personLicenses)
                {
                    var linesOfInsurance = l.LinesOfInsurance.Replace(",", ";");
                    var companyNames = string.Join(";", l.CompanyNames ?? new List<string>());

                    stream.WriteLine($"{l.LicenseNumber},{l.FirstName},{l.MiddleName},{l.LastName},{l.City},{l.State},{l.Zip},{l.Email},{l.BusinessPhone},{l.Address1},{l.Address2},{l.DoingAs},{linesOfInsurance},{companyNames}");
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

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
