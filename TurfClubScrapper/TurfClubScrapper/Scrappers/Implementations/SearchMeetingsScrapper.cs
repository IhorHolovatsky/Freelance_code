using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using RestSharp;
using TurfClubScrapper.Extensions;
using TurfClubScrapper.Models.Constants;
using TurfClubScrapper.Scrappers.Models;
using TurfClubScrapper.Utils;

namespace TurfClubScrapper.Scrappers.Implementations
{
    public class SearchMeetingsScrapper : ISearchMeetingsScrapper
    {
        private readonly List<string> _allowedLocations = new List<string> { ApplicationConstants.Location.SINGAPORE };

        /// <inheritdoc />
        public async Task<SearchMeetingsResult> FindAllPastResults()
        {
            var context = new SearchMeetingsResult();
            var htmlDoc = await InitPage();

            foreach (var year in Enumerable.Range(ApplicationConstants.MINIMUM_YEAR, DateTime.Now.Year - ApplicationConstants.MINIMUM_YEAR))
            {
                foreach (var month in Enumerable.Range(1, 12))
                {
                    htmlDoc = await PerformSearch(htmlDoc, month, year);

                    var meetings = ParseMeetings(htmlDoc);
                    context.Results.AddRange(meetings);
                }
            }
            
            //Filter out not needed meetings
            context.Results = context.Results.Where(r => _allowedLocations.Contains(r.Location)).ToList();
            context.IsSuccess = true;

            return context;
        }


        #region Requests to server

        /// <summary>
        /// Firstly, need to do GET request (to have search form)
        /// </summary>
        private Task<HtmlDocument> InitPage()
        {
            var client = RestClientUtils.Create();
            var request = new RestRequest(TurfClubConstants.Url.PAST_RESULTS_SEARCH_URL, Method.GET);
            var response = client.Execute(request);
            return Task.FromResult(response.ToHtmlDocument());
        }

        /// <summary>
        /// Search meetings by year and month
        /// </summary>
        /// <param name="page">init page (which we have form GET request) </param>
        /// <returns>Page with search results</returns>
        private Task<HtmlDocument> PerformSearch(HtmlDocument page,
                                                 int month,
                                                 int year)
        {
            var rootNode = page.DocumentNode;
            var client = RestClientUtils.Create();
            var request = new RestRequest(TurfClubConstants.Url.PAST_RESULTS_SEARCH_URL, Method.POST);

            #region Needed POST data 
            var data = new Dictionary<string,string>()
            {
                {"ctl00$m$g_542b56b6_d56b_4811_bc77_e5af4b70499e$ctl00$ddlMonth", month.ToString() },
                {"ctl00$m$g_542b56b6_d56b_4811_bc77_e5af4b70499e$ctl00$ddlYear", year.ToString() },
                {"ctl00$m$g_542b56b6_d56b_4811_bc77_e5af4b70499e$ctl00$ddlRegion", "All" },
                {"ctl00$m$g_542b56b6_d56b_4811_bc77_e5af4b70499e$ctl00$ddlVenue", "Australia" }, //Hardcoded on site...

                {"ctl00$ScriptManager","" },
                {"MSOTlPn_View", rootNode.GetInputValueByIdOrName("MSOTlPn_View")},
                {"MSOTlPn_ShowSettings", rootNode.GetInputValueByIdOrName("MSOTlPn_ShowSettings") },
                {"MSOTlPn_Button", rootNode.GetInputValueByIdOrName("MSOTlPn_Button") },
                {"__REQUESTDIGEST",rootNode.GetInputValueByIdOrName("__REQUESTDIGEST") },
                {"MSOSPWebPartManager_DisplayModeName", rootNode.GetInputValueByIdOrName("MSOSPWebPartManager_DisplayModeName") },
                {"MSOSPWebPartManager_ExitingDesignMode", rootNode.GetInputValueByIdOrName("MSOSPWebPartManager_ExitingDesignMode") },
                {"MSOSPWebPartManager_OldDisplayModeName", rootNode.GetInputValueByIdOrName("MSOSPWebPartManager_OldDisplayModeName") },
                {"MSOSPWebPartManager_StartWebPartEditingName", rootNode.GetInputValueByIdOrName("MSOSPWebPartManager_StartWebPartEditingName") },
                {"MSOSPWebPartManager_EndWebPartEditing", rootNode.GetInputValueByIdOrName("MSOSPWebPartManager_EndWebPartEditing") },
                {"_maintainWorkspaceScrollPosition", rootNode.GetInputValueByIdOrName("_maintainWorkspaceScrollPosition") },
                {"InputKeywords", "Search this site..." },
                {"ctl00$PlaceHolderSearchArea$ctl01$ctl03", rootNode.GetInputValueByIdOrName("ctl00$PlaceHolderSearchArea$ctl01$ctl03")},
                {"__VIEWSTATE", rootNode.GetInputValueByIdOrName("__VIEWSTATE") },
                {"__VIEWSTATEGENERATOR", rootNode.GetInputValueByIdOrName("__VIEWSTATEGENERATOR")},
                {"__EVENTVALIDATION", rootNode.GetInputValueByIdOrName("__EVENTVALIDATION") },
                {"__VIEWSTATEENCRYPTED", "" },
                {"__LASTFOCUS", "" },


                {"MSOWebPartPage_PostbackSource", "" },
                {"MSOTlPn_SelectedWpId", "" },
                {"MSOGallery_SelectedLibrary", "" },
                {"MSOGallery_FilterString", "" },
                {"MSOWebPartPage_Shared", "" },
                {"MSOLayout_LayoutChanges", "" },
                {"MSOLayout_InDesignMode", "" },
                {"__spText1", "" },
                {"__spText2", "" },
                {"_wpcmWpid", "" },
                {"wpcmVal", "" },
                {"_wpSelected", "" },
                {"_wzSelected", "" },
            };
            #endregion

            request.AddParameter("application/x-www-form-urlencoded", data.UrlEncodedSerialize(), ParameterType.RequestBody);

            var response = client.Execute(request);

            return Task.FromResult(response.ToHtmlDocument());
        }

        #endregion

        #region Parsing data

        /// <summary>
        /// Gather all info from page
        /// </summary>
        public List<MeetingContext> ParseMeetings(HtmlDocument doc)
        {
            var root = doc.DocumentNode;

            var rows = root.SelectNodes("//table[@class='STC_Gdv_WP']//tr[contains(@class, 'STC_Gdv_Row')]");

            if (rows == null)
            {
                //TODO: log this...
                return new List<MeetingContext>();
            }

            return rows.Select(r =>
                               {
                                   var detailsUrl = r.SelectSingleNode(".//td[1]//a")?.Attributes["href"].Value ?? string.Empty;

                                   var meeting = new MeetingContext
                                   {
                                       MeetingDate = r.SelectSingleNode(".//td[1]//a")?.InnerText,
                                       Location = r.SelectSingleNode(".//td[2]//span")?.InnerText,
                                       MeetingId = HttpUtility.ParseQueryString(detailsUrl)[TurfClubConstants.MEETING_ID_QUERY_STRING].ParseInt()
                                   };
                                   return meeting;
                               })
                       .ToList();
        }

        #endregion
    }
}