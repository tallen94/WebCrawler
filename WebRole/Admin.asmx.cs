using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using ClassLibrary;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Web.Script.Services;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Web.Caching;

namespace WebRole {
    /// <summary>
    /// Summary description for Admin
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    
    public class Admin : System.Web.Services.WebService {

        private static Crawler crawler = new Crawler();
        private static Cache cache = HttpRuntime.Cache;

        [WebMethod]
        public void StartCrawling() {
            if (crawler.CurrentState().Equals("Clear")) {
                CloudQueueMessage msg = new CloudQueueMessage("http://www.cnn.com/robots.txt");
                CloudQueueMessage msg2 = new CloudQueueMessage("http://bleacherreport.com/robots.txt");
                AzureStorage.LinkQueue.AddMessage(msg);
                AzureStorage.LinkQueue.AddMessage(msg2);
                crawler.SendCommand("New Crawl");
            } else {
                crawler.SendCommand("Start");
            }
        }

        [WebMethod]
        public void SendCommand(string command) {
            crawler.SendCommand(command);
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        public void GetPages(string query, string callback, int page) {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string strip = Regex.Replace(query, @"[^a-zA-Z0-9 ]+", "").ToLower();
            LinkEntity[][] data;
            if (cache[strip] == null) {
                string[] splitQuery = strip.Split(' ');
                List<LinkEntity> results = new List<LinkEntity>();
                foreach (string word in splitQuery) {
                    var tableResults = from entity in AzureStorage.LinkTable.CreateQuery<LinkEntity>()
                                       where entity.PartitionKey == word  
                                       select entity;
                    
                    foreach (LinkEntity obj in tableResults.ToArray()) {
                        results.Add(obj);
                    }
                }

                var sorted = results.ToArray()
                    .GroupBy(entity => entity.Title)
                    .Select(grp => grp.First())
                    .OrderByDescending(entity => Rank(entity, strip))
                    .ThenByDescending(entity => entity.Lastmod)
                    .Select((entity, index) => new { entity, index })
                    .GroupBy(a => a.index / 20)
                    .Select((grp => grp.Select(g => g.entity).ToArray()))
                    .ToArray();

                if (sorted.Count() > 20) {
                    sorted = sorted.Take(20).ToArray();
                }

                if (sorted.Count() > 0 && cache[strip] == null) {
                    cache.Insert(strip, sorted, null, Cache.NoAbsoluteExpiration, new TimeSpan(0, 5, 0));
                }
                
                data = sorted as LinkEntity[][];
            } else {
                data = cache[strip] as LinkEntity[][];
            }

            var jsonData = "";
            if (data.Length > 0) {
                jsonData = serializer.Serialize(new { data = data[page], numPages = data.Length });
            }

            var response = callback + "(" + jsonData + ");";
            Context.Response.Clear();
            Context.Response.ContentType = "application/json";
            Context.Response.Write(response);
            Context.Response.End();
        }

        private float Rank(LinkEntity entity, string query) {
            float rank = 0;
            string[] pkSplit = entity.RowKey.Split('|')[0].Split(' ');
            string[] querySplit = query.Split(' ');
            foreach (string word in querySplit) {
                if (pkSplit.Contains(word)) {
                    rank++;
                }
            }

            Uri uri = new Uri(entity.Link);
            if (uri.Segments.Contains("index.html") && rank > 0) {
                rank++;
            }
            return rank / uri.Segments.Count();
        }

        [WebMethod]
        public string PullDashboard() {
            crawler.PullDashboard();
            var obj = crawler.GetDashboard();
            JavaScriptSerializer s = new JavaScriptSerializer();
            return s.Serialize(obj);
        }
    }
}
/// <summary>
/// Web Role - dashboard.html
//- status, #urls, last 10, etc (see assignment)
//- admin.asmx
//- StartCrawling - StopCrawling - ClearIndex - GetPageTitle
//Worker Role - Read URL from Queue - Crawl URL - Store title to Table
//- Add new found URLs to Queue
/// </summary>
