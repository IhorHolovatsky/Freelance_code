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
using RestSharp;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace Leads_scraper
{
    public partial class Form1 : Form
    {

        public List<string> FailedLogins = new List<string>();

        public Form1()
        {
            InitializeComponent();
            lb_filePath.Text = ConfigurationManager.AppSettings["LoginsFilePath"];
        }

        private void btn_Search_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }
            lb_logs.Clear();
            FailedLogins = new List<string>();


            new Thread(() =>
                {
                    var logins = File.ReadAllText(lb_filePath.Text).Split(',', ';').ToList();

                    //STEP 1: Perform Search
                    LogMessage($"\n Found {logins.Count} logins.");
                    LogMessage("\n Perform downloading files: ");

                    foreach (var login in logins)
                    {
                        LogMessage($"\n\t Login to {login}...");
                        var client = CreateRestClient();

                        //Login via API
                        var request = new RestRequest("https://us-api.knack.com/v1/session/", Method.POST);
                        request.AddHeader("Content-Type", "application/json");
                        request.AddHeader("X-Knack-Application-Id", "54ecd35edeb1a25e09c06210");
                        request.AddHeader("x-knack-new-builder", "true");
                        request.AddHeader("X-Knack-REST-API-Key", "renderer");
                        request.AddHeader("X-Requested-With", "XMLHttpRequest");
                        request.AddJsonBody(new
                        {
                            email = login,
                            password = "pwdjeff",
                            remember = false,
                            view_key = "view_540"
                        });

                        var response = client.Execute(request);

                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            LogMessage($"\n\t Login failed for '{login}'");
                            LogMessage("\n-----------------------------");
                            FailedLogins.Add(login);
                            continue;
                        }

                        //Download file via API
                        LogMessage($"\n\t Download file for {login}...");

                        var data = new Dictionary<string,string>()
                        {
                            { "type", "csv" },
                            { "format", "both" },
                            { "page", "1" },
                            { "rows_per_page", "25" },
                            { "sort_field", "field_248" },
                            { "sort_order", "desc" },
                            { "filters", @"[{""field"":""field_90"",""operator"":""is any"",""text"":""All"",""value"":""Not Contacted""}]" }
                        };
                        var queryString = data.FormUrlEncodedSerialize();
                        request = new RestRequest("https://us-api.knack.com/v1/scenes/scene_339/views/view_539/records/export/applications/54ecd35edeb1a25e09c06210?"
                                                  + queryString, Method.GET);

                        response = client.Execute(request);

                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            LogMessage($"\n\t Downloading file failed for '{login}'");
                            LogMessage("\n-----------------------------");
                            FailedLogins.Add(login);
                            continue;
                        }

                        //Save to file
                        var rootFolder = Path.GetDirectoryName(lb_filePath.Text);
                        var outputFilePath = $@"{rootFolder}\Files\{login.Split('@')[0]}.csv";
                        LogMessage($"\n\t Save to file - {outputFilePath}");

                        if (!Directory.Exists($@"{rootFolder}\Files"))
                        {
                            Directory.CreateDirectory($@"{rootFolder}\Files");
                        }


                        if (File.Exists(outputFilePath))
                        {
                            LogMessage($"\n\tFile {outputFilePath} already exists.");
                            outputFilePath = $@"{rootFolder}\Files\{login.Split('@')[0]}_{Guid.NewGuid()}.csv";
                            LogMessage($"\n\tSaving as {outputFilePath}.");
                        }
                        File.WriteAllBytes(outputFilePath, response.RawBytes);
                        LogMessage("\n-----------------------------");
                    }

                    LogMessage("\nDONE!");
                    LogMessage("\n-----------------------------");

                    if (FailedLogins.Any())
                    {
                        LogMessage($"\n Failed Logins: {string.Join(", ", FailedLogins)}");
                    }
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

            if (!File.Exists(lb_filePath.Text))
            {
                MessageBox.Show($"File {lb_filePath.Text} is not exists");
                return false;
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
                BaseUrl = new Uri("http://us-api.knack.com/"),
                CookieContainer = new CookieContainer(),
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
