using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using CNA_Scraper.Enums;
using CNA_Scraper.Extensions;
using CNA_Scraper.Models;
using HtmlAgilityPack;
using OfficeOpenXml;
using RestSharp;

namespace CNA_Scraper
{
    class Program
    {
        public static string OutputFilePath { get; set; }
        public static Dictionary<string, string> Data = new Dictionary<string, string>()
        {
            {"almGUID", ""},
            {"almCurrentPageH", "1"},
            {"almPageCacheMinH", "0"},
            {"almPageCacheMaxH", "50"},
            {"almLatitude", ""},
            {"almLongitude", ""},
            {"almIndustryH", "CNADesign/TAX-CNAcom/CAT-Industries/CAT-AnyIndustry"},
            {"almSizeofBusinessH", "CNADesign/TAX-CNAcom/CAT-IndustrySize/CAT-AnySize"},
            {"almZipcode", ""},
            {"almState", ""},
            {"almIndustry", "Select+Industry"},
            {"almSizeofBusiness", "Select+Business+Size"},
        };

        protected static CookieContainer Cookies = new CookieContainer();
        protected static RestClient Client = CreateRestClient();

        private const int RETRY_ATTEMPTS = 7;
        private const string BASE_URL = "https://www.cna.com/";
        private const string SEARCH_PAGE_URL = "https://www.cna.com/web/guest/cna/findanagent/!ut/p/b1/jc7JDoJADAbgZ_EJpsCgcBzRhB2RGYS5kNEYBdkkaAhPL3rx5NJDkybf3xZxlCBei3t-En3e1KJ8znyeYXAc6mImL31LAhIEU_NV8ObSBNLPgEXSf3n4UAR-5XeIfyOvD17gywnfbKojSie2eO-xZUkHEq6osVTMiSmIogRwFhXQelSDTjuUG5qPtqu47rjuB2os7NHLaKGF51Ufbav4Fl7U6rA7OvtadUQz6JyVkbi2escEtogxBNuYzFBbsQQKLB4KZDvm/";
        private const string AJAX_GET_RESULTS_URL = "https://www.cna.com/web/guest/cna/findanagent/!ut/p/b1/jY_NboMwEISfJU9gEyCBo5NUSsBAABN-Lsi1LAIFTImJEE9fyiWn0O5hpZG-2ZkFGUhA1tJnWVBZipbWvzrb5Rq0bYK1aHtwLwpEnjcvV4fOTpmB9D0Qhcr__PDNIPiXPwbZGrI0WICVCPcsGg7SGdu_7lhbxYTIP5HjQT3PmAoISKCWhxXsHGLA3mD1lZSThVWMpw85kuPempycVIZ_P8kwaG6D_6U3LOb2Z6vbVIxmFtUh_e7MPqLaBR1HL7ihzfxCuh4c9_whhp5xEBRcooK3EgtGpegD_hhqeaUFBz6j7M4xf_J60V0TJbDSaO6gzQ_qVMLh/";

        static Program()
        {
            //Needed to establish secure conneciton..
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            Data["almState"] = ConfigurationManager.AppSettings["State"];
            OutputFilePath = ConfigurationManager.AppSettings["OutputFilePath"];
        }

        static void Main(string[] args)
        {
            Console.WriteLine($"STEP 1. Init search");
            var searchResult = Init();

            Console.WriteLine($"\t {(searchResult.IsSuccess ? "'Success!'" : "'Failed!'")}. Found {searchResult.PageCount} pages");

            #region Retry Logic 
            if (!searchResult.IsSuccess)
            {
                searchResult = RetrySearch();

                //check success after retries..
                if (!searchResult.IsSuccess)
                {
                    Console.WriteLine($"\t Something went wrong... {RETRY_ATTEMPTS} attempts were failed..");
                    Console.ReadLine();
                    return;
                }
            }
            #endregion

            //This will not possible if we are searching only by States, but let it be
            if (searchResult.PageCount == 0)
            {
                Console.WriteLine($"\t No Agents found...");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("STEP 2. Loop through pages...");
            var agents = ScrapAllPages(searchResult);

            Console.WriteLine("STEP 3. Save to file...");
            OutputFilePath = OutputFilePath.EndsWith(@"\") ? OutputFilePath : $@"{OutputFilePath}\";
            SaveToFile($@"{OutputFilePath}{Data["almState"]}.xlsx", agents);

            Console.WriteLine($"File was saved! {agents.Count} agents were scraped");

            Console.ReadLine();
        }

        #region Private methods

        private static SearchResultContext Init()
        {
            var context = new SearchResultContext();

            //STEP 0: Init all cookies
            var client = Client;
            var request = new RestRequest(SEARCH_PAGE_URL, Method.GET);

            var response = client.Execute(request);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(response.Content);

            //STEP 1: POST Search
            var postUrl = htmlDoc.DocumentNode.SelectSingleNode("//form[@id='agentLocatorSearchForm']")?.Attributes["action"]?.Value;

            //Post url should be found in form...
            if (string.IsNullOrEmpty(postUrl))
            {
                context.IsSuccess = false;
                return context;
            }

            request = new RestRequest(postUrl, Method.POST);
            request.AddParameter("application/x-www-form-urlencoded", Data.FormUrlEncodedSerialize(), ParameterType.RequestBody);

            response = client.Execute(request);

            //STEP 2: Get redirect location, do redirect
            var contentUrl = response.Headers.FirstOrDefault(h => h.Name.Equals("content-location", StringComparison.CurrentCultureIgnoreCase))?.Value.ToString();

            //Post search should reduct content-location... it's a way how this site is working..
            if (string.IsNullOrEmpty(contentUrl))
            {
                context.IsSuccess = false;
                return context;
            }

            request = new RestRequest(contentUrl, Method.GET);
            response = client.Execute(request);

            htmlDoc.LoadHtml(response.Content);

            //everything is ok, agencies were found
            var agentsCount = htmlDoc.DocumentNode.SelectSingleNode("//span[@id='almTotlaRecordsH']")?.InnerText.ToInt();

            //if no page count means that something went wrong..
            if (!agentsCount.HasValue)
            {
                context.IsSuccess = false;
                return context;
            }

            context.PageCount = agentsCount.Value % 10 != 0
                                    ? agentsCount.Value / 10 + 1
                                    : agentsCount.Value / 10;

            //STEP 3: Get search GUID, and fetch search results
            var requestGuid = htmlDoc.DocumentNode.SelectSingleNode("//input[@id='agentlocatornamespace_almGUID']")?.Attributes["value"]?.Value;
            Data["almGUID"] = requestGuid;

            //requestGuid should be present here
            if (string.IsNullOrEmpty(requestGuid))
            {
                context.IsSuccess = false;
                return context;
            }

            request = new RestRequest(AJAX_GET_RESULTS_URL + $"?{Data.FormUrlEncodedSerialize()}", Method.GET);
            response = client.Execute(request);

            htmlDoc.LoadHtml(response.Content);

            context.IsSuccess = true;
            context.Document = htmlDoc;

            return context;
        }

        private static SearchResultContext RetrySearch()
        {
            var i = 0;
            while (i != RETRY_ATTEMPTS)
            {
                Console.WriteLine($"\t Retry {i}...");

                //Just retry it again (reset cookies)
                Client.CookieContainer = new CookieContainer();
                var searchResult = Init();

                Console.WriteLine($"\t Search - {(searchResult.IsSuccess ? "'success'" : "'failed'")}. Found {searchResult.PageCount} pages");

                if (searchResult.IsSuccess)
                {
                    return searchResult;
                }

                i++;

                //2 seconds wait between retries
                var waitTimeInSeconds = 3 * i;
                Console.WriteLine($"\t Wait {waitTimeInSeconds} seconds...");
                Thread.Sleep(waitTimeInSeconds * 1000);
            }

            return new SearchResultContext() {IsSuccess = false};
        }

        private static List<Agent> ScrapAllPages(SearchResultContext searchResult)
        {
            var agents = new List<Agent>();

            //Scrap current page
            Console.WriteLine("\t Page 1: ");
            ParsePage(agents, searchResult.Document);

            //Loop through other pages
            for (var i = 2; i <= searchResult.PageCount; i++)
            {
                Console.WriteLine($"\t Page {i}: ");
                Data["almCurrentPageH"] = i.ToString();

                var client = Client;
                var request = new RestRequest(AJAX_GET_RESULTS_URL + $"?{Data.FormUrlEncodedSerialize()}", Method.GET);
                var response = client.Execute(request);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(response.Content);

                //Probably for cache reasons
                Data["almPageCacheMinH"] = htmlDoc.DocumentNode.SelectSingleNode("//span[@id='almAjaxPageCacheMinH']")?.InnerText;
                Data["almPageCacheMaxH"] = htmlDoc.DocumentNode.SelectSingleNode("//span[@id='almAjaxPageCacheMaxH']")?.InnerText;

                var success = ParsePage(agents, htmlDoc);

                if (!success)
                {
                    Console.WriteLine($"\t\tPage - {i}. Scrap failed... Retry...'");
                    var retryResult = RetrySearch();

                    if (!retryResult.IsSuccess)
                    {
                        Console.WriteLine($"\t\t Something went wrong... Page - {i} was not scrapped... go to next page'");
                        continue;;
                    }

                    ParsePage(agents, retryResult.Document);
                }

                var rand = new Random();
                Thread.Sleep(rand.Next(500, 2000));
            }

            return agents;
        }

        private static void SaveToFile(string filePath, List<Agent> agents)
        {
            using (var package = new ExcelPackage())
            {
                // Add a new worksheet to the empty workbook
                var worksheet = package.Workbook.Worksheets.Add("Agents");

                //Add the headers
                worksheet.Cells[1, 1].Value = "State";
                worksheet.Cells[1, 2].Value = "CompanyName";
                worksheet.Cells[1, 3].Value = "Street Address";
                worksheet.Cells[1, 4].Value = "City";
                worksheet.Cells[1, 5].Value = "StateFromData";
                worksheet.Cells[1, 6].Value = "ZipCode";
                worksheet.Cells[1, 7].Value = "PhoneNumber";
                worksheet.Cells[1, 8].Value = "FaxNumber";
                worksheet.Cells[1, 9].Value = "WebSiteUrl";

                //Add some items...
                for (var i = 0; agents.Count > i; i++)
                {
                    var agent = agents[i];
                    var cellNumber = i + 2;

                    worksheet.Cells[$"A{cellNumber}"].Value = agent.State;
                    worksheet.Cells[$"B{cellNumber}"].Value = agent.CompanyName;
                    worksheet.Cells[$"C{cellNumber}"].Value = agent.StreetAddress;
                    worksheet.Cells[$"D{cellNumber}"].Value = agent.City;
                    worksheet.Cells[$"E{cellNumber}"].Value = agent.StateFromData;
                    worksheet.Cells[$"F{cellNumber}"].Value = agent.ZipCode;
                    worksheet.Cells[$"G{cellNumber}"].Value = agent.PhoneNumber;
                    worksheet.Cells[$"H{cellNumber}"].Value = agent.FaxNumber;
                    worksheet.Cells[$"I{cellNumber}"].Value = agent.WebSiteUrl;
                }


                // save our new workbook in the output directory and we are done!
                var file = File.Create(filePath);
                package.SaveAs(file);
            }
        }

        private static bool ParsePage(List<Agent> agents, HtmlDocument document)
        {
            var root = document.DocumentNode;
            var resultsDivs = root.SelectNodes("//div[@id='mainLeftCol']//div[@class='rowWrapper']");

            if (resultsDivs == null)
            {
                return false;
            }

            foreach (var agentNode in resultsDivs)
            {
                var agent = new Agent
                {
                    State = Data["almState"],
                    CompanyName = agentNode.SelectSingleNode(".//span[contains(@id, 'googleAgentName')]")?.InnerText.CleanupString(),
                    StreetAddress = agentNode.SelectSingleNode(".//span[@id='distributorAddressLine']")?.InnerText.CleanupString(),
                    City = agentNode.SelectSingleNode(".//span[@id='distributorCity']")?.InnerText.CleanupString(),
                    StateFromData = agentNode.SelectSingleNode(".//span[@id='distributorStatecode']")?.InnerText.CleanupString(),
                    ZipCode = agentNode.SelectSingleNode(".//span[@id='distributorPostalcode']")?.InnerText.CleanupString(),
                    WebSiteUrl = agentNode.SelectSingleNode(".//p[@class='companyWebsite']//a")?.Attributes["href"]?.Value,
                    PhoneNumber = agentNode.SelectSingleNode(".//div[@class='companyPhone']//span[1]")?.InnerText.CleanupString(),
                    FaxNumber = agentNode.SelectSingleNode(".//div[@class='companyPhone']//span[2]")?.InnerText.CleanupString(),
                };

                Console.WriteLine($"\t\t CompanyName: {agent.CompanyName}");
                agents.Add(agent);
            }

            return true;
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

        #endregion
    }
}
