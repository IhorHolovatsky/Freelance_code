using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using HtmlAgilityPack;
using RestSharp;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace Leeclerk_Scrapper
{
    class Program
    {
        public static CookieContainer Cookies { get; set; }

        public static struct_Resort_Array[] Resort_Array = new struct_Resort_Array[10000];
        public struct struct_Resort_Array
        {
            public string detailsId;

            //"e" is grantee.  "o" is grantor
            public string mortgage;

            public string efname1;
            public string emname1;
            public string elname1;
            public string efullname1;
            public string efname2;
            public string emname2;
            public string elname2;
            public string efullname2;

            public string ofname1;
            public string omname1;
            public string olname1;
            public string ofullname1;
            public string ofname2;
            public string omname2;
            public string olname2;
            public string ofullname2;

            public string recdate;
            public string mortgageamount;
            public string legal;
        }


        static void Main(string[] args)
        {

            Console.WriteLine("Step0: Bypassing https://www.incapsula.com/ protection");
            Task.WaitAll(StartSTATask(() =>
            {
                Start();
                return true;
            }));

            clear_Resort_Array(true, 0);
            var startdate = ConfigUtils.GetAppSettingValue<string>(AppSettings.SearchStartDate);//"2/2/2018";
            var enddate = ConfigUtils.GetAppSettingValue<string>(AppSettings.SearchEndDate); //"3/2/2018";
            var documentTypes = ConfigUtils.GetAppSettingValue<string>(AppSettings.DocumentTypes); //"AD, ADC"
            var recdate = DateTime.Now.ToString("M/d/yyy"); //"3/2/2018";//today's date

            string baseurl = "https://or.leeclerk.org/OR/Search.aspx";
            // Setup browserless client with cookies capability
            var client = GetRestClient(baseurl);

            // Step1: Open Search Page
            //start here : https://or.leeclerk.org/OR/Search.aspx
            Console.WriteLine("Step1: Open https://or.leeclerk.org/OR/Search.aspx");
            string url = "https://or.leeclerk.org/OR/Search.aspx";
           
            var request = new RestRequest(url, Method.GET);
            var response = client.Execute(request);
            
            //StartSTATask(() =>
            //{

            //});

            // Step3: Perform Search
            Console.WriteLine("Step2: Performing Search...");
            var htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(response.Content);
            int page_count = 0;
            var records_details_links = new List<string>();
            while (true)
            {
                page_count++;
                Console.WriteLine($"\tPage {page_count}...");
                request = new RestRequest(url, Method.POST);

                // Prepare Post Query Parameters
                request.AddQueryParameter("bd", startdate);
                request.AddQueryParameter("ed", enddate);
                request.AddQueryParameter("bt", "O");
                request.AddQueryParameter("d", recdate);
                request.AddQueryParameter("pt", "-1");
                request.AddQueryParameter("dt", documentTypes);
                request.AddQueryParameter("vbt", "D");
                request.AddQueryParameter("st", "documenttype");

                // Prepare Post Data
                IDictionary<string, string> data = new Dictionary<string, string>();

                /*///////////////////////////
                //HERE IS WHERE IT BREAKS!!!!
                ///////////////////////////*/

                data.Add("__EVENTTARGET", "");

                data.Add("__EVENTARGUMENT", "");

                data.Add("__VIEWSTATE", htmldoc.DocumentNode.SelectNodes("//form[@id='Form1']//input[@name='__VIEWSTATE']").First().Attributes["value"].Value);

                data.Add("__VIEWSTATEGENERATOR", htmldoc.DocumentNode.SelectNodes("//form[@id='Form1']//input[@name='__VIEWSTATEGENERATOR']")
                        .First().Attributes["value"].Value);

                data.Add("__EVENTVALIDATION", htmldoc.DocumentNode.SelectNodes("//form[@id='Form1']//input[@name='__EVENTVALIDATION']").First()
                        .Attributes["value"].Value);



                data.Add("SearchType", "documenttype");
                data.Add("SearchTypeDesc", "Search By Document Type");
                data.Add("ddPartyType", "-1");
                data.Add("ddVitalsBook", "D");
                data.Add("ddBookType", "O");
                data.Add("txtName", "");
                data.Add("txtBook", "");
                data.Add("txtPage", "");
                data.Add("txtInstrumentNumber", "");
                data.Add("txtLowerBound", "");
                data.Add("txtUpperBound", "");
                data.Add("ddParcelChoice", "0");
                data.Add("txtParcelId", "");
                data.Add("ddCommentsChoice", "0");
                data.Add("txtComments", "");
                data.Add("txtLegalFields", "");
                data.Add("txtLegalDesc", "");
                data.Add("ddCaseNumberChoice", "0");
                data.Add("txtCaseNumber", "");
                data.Add("txtDocTypes", documentTypes);
                data.Add("cboCategories", "n/a");
                data.Add("txtRecordDate", recdate);
                data.Add("txtBeginDate", startdate);
                data.Add("txtEndDate", enddate);
                data.Add("cmdSubmit", "Search Records");

                // Change EventTarget if page >= 2
                if (page_count >= 2)
                {
                    data.Remove("cmdSubmit");
                    string event_target = htmldoc.DocumentNode.SelectNodes("//tr[@class='stdFontPager'][1]//span/following-sibling::a").First().Attributes["href"].Value;
                    event_target = Regex.Match(event_target, "__doPostBack\\('(.+)',").Groups[1].Value.Replace('$', ':');
                    data["__EVENTTARGET"] = event_target;
                }

                string postdata = "";
                foreach (KeyValuePair<string, string> item in data)
                    postdata = postdata + Uri.EscapeDataString(item.Key) + "=" + Uri.EscapeDataString(item.Value) + '&';
                postdata = postdata.TrimEnd('&');

                request.AddParameter("application/x-www-form-urlencoded", postdata, ParameterType.RequestBody);
                response = client.Execute(request);
                htmldoc.LoadHtml(response.Content);

                // Read Ids
                int i = 0;
                HtmlNodeCollection trNodes = htmldoc.DocumentNode.SelectNodes("//table[@id='dgResults']//tr");
                foreach (HtmlNode tr in trNodes)
                {
                    i++;
                    if ((i <= 2) || (i == trNodes.Count))
                        continue;
                    string recordid = tr.SelectNodes(".//a").First().Attributes["href"].Value;
                    recordid = recordid.Split('?')[1];
                    //recordid = Regex.Match(recordid, "id=(\\d+)").Groups[1].Value;
                    records_details_links.Add(HttpUtility.HtmlDecode(recordid));
                }

                if (htmldoc.DocumentNode.SelectNodes("//tr[@class='stdFontPager'][1]//span/following-sibling::a") == null)
                    break;
            }

            // Step3: Fetch Details of all records
            Console.WriteLine("Step3: Fetching Records Details...");

            Parallel.For(0, records_details_links.Count, i =>
            //for (var i = 0; i < records_ids.Count; i++)
            {
                Console.WriteLine($"\t{(i + 1).ToString()}/{records_details_links.Count.ToString()}: {records_details_links[i]}");
                var clientForDetails = GetRestClient(baseurl);
                //HttpUtility.UrlEncode()
                var detailsUrl = $"https://or.leeclerk.org/OR/details.aspx?{records_details_links[i]}";
                var detailsRequest = new RestRequest(detailsUrl, Method.GET);
                var detailsResponse = clientForDetails.Execute(detailsRequest);
                var detailsHtmldoc = new HtmlDocument();
                detailsHtmldoc.LoadHtml(detailsResponse.Content);

                string grantor = detailsHtmldoc.DocumentNode.SelectNodes("//tr[@id='trGrantor']/td[@class='DetailValue']").First().InnerText.Trim();
                string grantee = detailsHtmldoc.DocumentNode.SelectNodes("//tr[@id='trGrantee']/td[@class='DetailValue']").First().InnerText.Trim();
                string deed = detailsHtmldoc.DocumentNode.SelectNodes("//tr[@id='trConsideration']/td[@class='DetailValue']").First().InnerText.Trim();
                string legal = detailsHtmldoc.DocumentNode.SelectNodes("//tr[@id='trLegal']/td[@class='DetailValue']").First().InnerText.Trim();

                getset_grantor(i, grantor);
                getset_grantee(i, grantee);
                getset_deed(i, deed);
                getset_legal(i, legal);
                Resort_Array[i].detailsId = records_details_links[i];


                Console.WriteLine("\tGrantor: " + grantor);
                Console.WriteLine("\tGrantee: " + grantee);
                Console.WriteLine("\tDeed: " + deed);
                Console.WriteLine("\tLegal: " + legal);
                Console.WriteLine();
            });
            //}

            /*
            ///////////////////////////////////////
            // Put your own local hardrive path here to save the output
            ////////////////////////////////////////*/

            string check = Resort_Array[0].efname1;
            string csvpath = ConfigUtils.GetAppSettingValue<string>(AppSettings.OutputFilePath); //@"G:\";//@"C:\Users\jeff\Dropbox\BIZ\CSV\Timeshares\HorryTimeShares\";
            dumptocsv(csvpath, "leeclerk", "", startdate, enddate);

            Console.WriteLine("Done! Press Enter to Exit!");
            Console.ReadLine();
        }

        private static void Start()
        {
            var webBrowser = new HtmlWeb {UseCookies = true};

            webBrowser.LoadFromBrowser("https://or.leeclerk.org/OR/Search.aspx", delegate(object o)
            {
                var browser = (WebBrowser) o;

                var cookies = browser.Document.Cookie;

                Cookies = new CookieContainer();
                cookies.Split(new[] {";"}, StringSplitOptions.None)
                                        .Select(c =>
                                                {
                                                    var index = c.IndexOf("=", StringComparison.Ordinal);
                                                    return new KeyValuePair<string, string>(c.Substring(0, index), c.Substring(index + 1, c.Length - index - 1));
                                                })
                                        .ToList()
                                        .ForEach(c =>
                    {
                        Cookies.Add(new Cookie(c.Key.Trim(), c.Value, "/", ".leeclerk.org"));
                    });
                return true;
            });
            
        }

        public static void dumptocsv(string csvpath, string county, string lookingfor, string startdate, string enddate)
        {
            //string county = "Orlando";
            string fileName = county + "_" + lookingfor + "_" + startdate + "_" + enddate + ".csv";
            fileName = fileName.Replace("/", "");
            string fullPath = csvpath + fileName;

            using (var stream = File.CreateText(fullPath))
            {
                var line1 = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21}",
                    "County", "Looking For", "Grantee FN1", "Grantee MN1", "Grantee LN1", "Grantee Full1", "Grantee FN2", "Grantee MN2", "Grantee LN2", "Grantee Full2", "Grantor FN1", "Grantor MN1", "Grantor LN1", "Grantor Full1", "Grantor FN2", "Grantor MN2", "Grantor LN2", "Grantor Full2", "Record Date", "Mortgage Num", "Mtg Amount", "Legal");

                stream.WriteLine(line1);

                for (int i = 0; i < 10000; i++)
                {
                    //qualify here
                    //"e" is grantee.  "o" is grantor
                    var efname1 = Resort_Array[i].efname1.Replace(",", " ");
                    var emname1 = Resort_Array[i].emname1.Replace(",", " ");
                    var elname1 = Resort_Array[i].elname1.Replace(",", " ");
                    var efullname1 = Resort_Array[i].efullname1.Replace(",", " ");

                    var efname2 = Resort_Array[i].efname2.Replace(",", " ");
                    var emname2 = Resort_Array[i].emname2.Replace(",", " ");
                    var elname2 = Resort_Array[i].elname2.Replace(",", " ");
                    var efullname2 = Resort_Array[i].efullname2.Replace(",", " ");

                    var ofname1 = Resort_Array[i].ofname1.Replace(",", " ");
                    var omname1 = Resort_Array[i].omname1.Replace(",", " ");
                    var olname1 = Resort_Array[i].olname1.Replace(",", " ");
                    var ofullname1 = Resort_Array[i].ofullname1.Replace(",", " ");

                    var ofname2 = Resort_Array[i].ofname2.Replace(",", " ");
                    var omname2 = Resort_Array[i].omname2.Replace(",", " ");
                    var olname2 = Resort_Array[i].olname2.Replace(",", " ");
                    var ofullname2 = Resort_Array[i].ofullname2.Replace(",", " ");

                    var recdate = Resort_Array[i].recdate.Replace(",", " ");
                    var mortgagenum = Resort_Array[i].mortgage.Replace(",", " ");
                    var mortgageamount = Resort_Array[i].mortgageamount.Replace(",", "");
                    var legal = Resort_Array[i].legal.Replace(",", "");

                    if ((efname1.Trim().Length == 0) && (elname1.Trim().Length == 0) && (efullname1.Trim().Length == 0))
                        continue;
                    if ((ofname1.Trim().Length == 0) && (olname1.Trim().Length == 0) && (ofullname1.Trim().Length == 0))
                        continue;

                    var line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21}",
                        county, lookingfor, efname1, emname1, elname1, efullname1, efname2, emname2, elname2, efullname2, ofname1, omname1, olname1, ofullname1, ofname2, omname2, olname2, ofullname2, recdate, mortgagenum, mortgageamount, legal);
                    stream.WriteLine(line);
                }
            }
        }
        public static void clear_Resort_Array(bool all, int j)
        {
            //"e" is grantee.  "o" is grantor
            if (all)
            {
                for (int i = 0; i < 10000; i++)
                {

                    Resort_Array[i].efname1 = "";
                    Resort_Array[i].emname1 = "";
                    Resort_Array[i].elname1 = "";
                    Resort_Array[i].efullname1 = "";
                    Resort_Array[i].efname2 = "";
                    Resort_Array[i].emname2 = "";
                    Resort_Array[i].elname2 = "";
                    Resort_Array[i].efullname2 = "";

                    Resort_Array[i].ofname1 = "";
                    Resort_Array[i].omname1 = "";
                    Resort_Array[i].olname1 = "";
                    Resort_Array[i].ofullname1 = "";
                    Resort_Array[i].ofname2 = "";
                    Resort_Array[i].omname2 = "";
                    Resort_Array[i].olname2 = "";
                    Resort_Array[i].ofullname2 = "";


                    Resort_Array[i].mortgage = "";
                    Resort_Array[i].mortgageamount = "";
                    Resort_Array[i].recdate = "";
                    Resort_Array[i].legal = "";

                }
            }
            else
            {
                Resort_Array[j].efname1 = "";
                Resort_Array[j].emname1 = "";
                Resort_Array[j].elname1 = "";
                Resort_Array[j].efullname1 = "";
                Resort_Array[j].efname2 = "";
                Resort_Array[j].emname2 = "";
                Resort_Array[j].elname2 = "";
                Resort_Array[j].efullname2 = "";

                Resort_Array[j].ofname1 = "";
                Resort_Array[j].omname1 = "";
                Resort_Array[j].olname1 = "";
                Resort_Array[j].ofullname1 = "";
                Resort_Array[j].ofname2 = "";
                Resort_Array[j].omname2 = "";
                Resort_Array[j].olname2 = "";
                Resort_Array[j].ofullname2 = "";


                Resort_Array[j].mortgage = "";
                Resort_Array[j].mortgageamount = "";
                Resort_Array[j].recdate = "";
                Resort_Array[j].legal = "";
            }
        }
        public static void getset_grantee(int i, string grantee)
        {
            string ef1 = "";
            string ef2 = "";
            string stripe = grantee;
            if (grantee.IndexOf("(") > -1)
            {
                string g3 = stripe.Substring(0, stripe.IndexOf("("));
                ef1 = g3.Trim();
                stripe = stripe.Substring(stripe.IndexOf(")") + 1);
            }
            else
            {
                ef1 = grantee;
            }
            if (stripe.IndexOf("(") > -1)
            {
                string g4 = stripe.Substring(0, stripe.IndexOf("("));
                ef2 = g4.Trim();
                //strip = strip.Substring(strip.IndexOf(")") + 1);
            }
            else
            {
                ef2 = grantee;
            }

            Resort_Array[i].efullname1 = ef1;
            Resort_Array[i].efullname2 = ef2;
        }
        public static void getset_grantor(int i, string grantor)
        {
            string of1 = "";
            string of2 = "";
            string stripo = grantor;
            if (grantor.IndexOf("(") > -1)
            {
                string g1 = stripo.Substring(0, stripo.IndexOf("("));
                of1 = g1.Trim();
                stripo = stripo.Substring(stripo.IndexOf(")") + 1);
            }
            else
            {
                of1 = grantor;
            }
            if (stripo.IndexOf("(") > -1)
            {
                string g2 = stripo.Substring(0, stripo.IndexOf("("));
                of2 = g2.Trim();
                //strip = strip.Substring(strip.IndexOf(")") + 1);
            }
            else
            {
                of2 = grantor;
            }

            Resort_Array[i].ofullname1 = of1;
            Resort_Array[i].ofullname2 = of2;
        }
        public static void getset_deed(int i, string deed)
        {
            Resort_Array[i].mortgageamount = deed;
        }
        public static void getset_legal(int i, string legal)
        {
            Resort_Array[i].legal = legal;
        }

        private static RestClient GetRestClient(string baseurl)
        {
            var client = new RestClient();
            client.BaseUrl = new Uri(baseurl);
            client.CookieContainer = Cookies;

            client.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.186 Safari/537.36";

           
            return client;
        }

        public static Task<T> StartSTATask<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            Thread thread = new Thread(() =>
            {
                try
                {
                    tcs.SetResult(func());
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }
    }
}
