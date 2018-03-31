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
using OK_RealState.Models;
using RestSharp;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace OK_RealState
{
    public partial class Form1 : Form
    {
        public static CookieContainer Cookies { get; set; } = new CookieContainer();
        public static RestClient Client { get; set; } = CreateRestClient();

        private const string BASE_URL = "https://lic.ok.gov/";
        private const string SEARCH_URL = "/PublicPortal/OREC/FindAssociateEntity.jsp";

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
            
            new Thread(() =>
                {
                    //STEP 1: Perform Search
                    LogMessage("\n Perform search: ");

                    var client = Client;
                    var request = new RestRequest(SEARCH_URL, Method.GET);
                    client.Execute(request);

                    request = new RestRequest(SEARCH_URL, Method.POST);

                    var data = new Dictionary<string,string>()
                    {
                        {"lid",""            },
                        {"ReferenceFile",""  },
                        {"AddrCity",""       },
                        {"AddrCountyCode","" },
                        {"ZipCode",""        },
                        {"StatReq", "A"      },
                        {"NameFirst",""      },
                        {"NameLast",""       },
                        {"OrgName",""        },
                        {"query", "Yes"      }
                    };

                    request.AddParameter("application/x-www-form-urlencoded", data.FormUrlEncodedSerialize(), ParameterType.RequestBody);

                    var response = client.Execute(request);
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(response.Content);

                    var root = htmlDoc.DocumentNode;
                    LogMessage($"\n Found {root.SelectNodes("//tr[contains(@class,'FormtableData')]")?.Count ?? 0} licences!");

                    var rows = root.SelectNodes("//tr[contains(@class,'FormtableData')]")?
                                   .Select(row => new PersonLicense()
                                   {
                                       LicenseNumber = row.SelectSingleNode(".//td[1]//a")?.InnerText,
                                       Name = row.SelectSingleNode(".//td[2]")?.InnerText.CleanupString() ?? row.SelectSingleNode("..//td[2]//a")?.InnerText.CleanupString(),
                                       Address = row.SelectSingleNode(".//td[6]")?.InnerText.CleanupString(),
                                       City = row.SelectSingleNode(".//td[7]")?.InnerText.CleanupString(),
                                       State = row.SelectSingleNode(".//td[8]")?.InnerText.CleanupString(),
                                       ZipCode = row.SelectSingleNode(".//td[9]")?.InnerText.CleanupString(),
                                   })
                                   .ToList();



                    //STEP 3: Save to file
                    LogMessage("\n");
                    LogMessage($"\n Save to file - {lb_filePath.Text}");

                    SaveToCsv(lb_filePath.Text, rows ?? new List<PersonLicense>());

                    LogMessage("\n-----------------------------");
                    LogMessage("\nDONE!");
                    LogMessage("\n-----------------------------");
                })
                .Start();
        }

        private bool ValidateInput()
        {
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

        private void SaveToCsv(string filePath, List<PersonLicense> personLicenses)
        {
            var fullPath = filePath + $"\\OK_RealEstate_{DateTime.Now.Ticks}.csv".Replace("/", "");

            using (var stream = File.CreateText(fullPath))
            {
                const string columnsLine = "licenseNumber,name,city,state,zip,address";
                stream.WriteLine(columnsLine);

                foreach (var l in personLicenses)
                {
                    stream.WriteLine($"{l.LicenseNumber},{l.Name},{l.City},{l.State},{l.ZipCode},{l.Address}");
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
