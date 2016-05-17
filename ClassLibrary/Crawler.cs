using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Azure;
using System.Threading;
using Newtonsoft.Json;

namespace ClassLibrary {
    public class Crawler {

        private static CloudStorageAccount StorageAccount;
        private CloudQueueClient QueueClient;
        private CloudTableClient TableClient;
        public CloudQueue LinkQueue;
        public CloudQueue StateQueue;
        public CloudTable LinkTable;
        public CloudTable DashboardTable;
        
        private Queue<LinkEntity> LinkEntityQueue;
        private TrieTree visitedLinks;
        private Dashboard dashboard;

        private Int64 maxBatch;

        public Crawler() {
            StorageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=tallenwebcrawler;AccountKey=ot+8RNOFolyYrjnXD46GvOh9xQGmvFQVul6XhuBQ2ZbN5mYoJRPSt6xQ6dhmX1Q71IU6QFRJKhGgKWHAU+982w==");
            QueueClient = StorageAccount.CreateCloudQueueClient();
            TableClient = StorageAccount.CreateCloudTableClient();
            LinkQueue = QueueClient.GetQueueReference("linkqueue");
            StateQueue = QueueClient.GetQueueReference("statequeue");
            LinkTable = TableClient.GetTableReference("sitetable");
            DashboardTable = TableClient.GetTableReference("dashboardtable");

            PullDashboard();

            LinkQueue.CreateIfNotExists();
            LinkTable.CreateIfNotExists();
            StateQueue.CreateIfNotExists();
            DashboardTable.CreateIfNotExists();
            maxBatch = 10;
            LinkEntityQueue = new Queue<LinkEntity>();
            visitedLinks = new TrieTree();
        }

        public string CurrentState() {
            return dashboard.CrawlingState;
        }

        public Dictionary<string, string> GetDashboard() {
            Dictionary<string, string> temp = new Dictionary<string, string>();
            temp.Add("crawlingstate", dashboard.CrawlingState);
            temp.Add("cpuusage", dashboard.CpuUsage.ToString());
            temp.Add("ramavailable", dashboard.RamAvailable.ToString());
            temp.Add("threadcount", dashboard.ThreadCount.ToString());
            temp.Add("last10", dashboard.last10Urls);
            temp.Add("sizeofqueue", dashboard.SizeOfQueue.ToString());
            temp.Add("sizeoftable", dashboard.SizeOfTable.ToString());
            temp.Add("errorurls", dashboard.errorUris);

            return temp;
        }

        public string GetTitleForUrl(string url) {
            Uri uri = new Uri(url);
            TableOperation retrieve = TableOperation.Retrieve<Dashboard>(uri.Authority, uri.AbsolutePath);
            TableResult result = LinkTable.Execute(retrieve);
            LinkEntity entity = ((LinkEntity)result.Result);
            return entity.Title;
        }

        public void Start() {
            dashboard.CrawlingState = "Crawling";
        }

        public void Stop() {
            dashboard.CrawlingState = "Stopped";
        }

        public void SendCommand(string cmd) {
            CloudQueueMessage msg = new CloudQueueMessage(cmd);
            StateQueue.AddMessage(msg);
        }

        public void CrawlUrl(string url) {
            Uri uri = new Uri(url);
            if (uri.Segments[1].Equals("robots.txt")) {
                CrawlRobots(uri);
            } else {
                DecrementQueueCount();
                ThreadPool.QueueUserWorkItem(o => CrawlLink(uri));
            }
        }

        public void IncrementQueueCount() {
            dashboard.SizeOfQueue = dashboard.SizeOfQueue + 1;
        }

        public void DecrementQueueCount() {
            dashboard.SizeOfQueue = dashboard.SizeOfQueue - 1;
        }

        private void EnqueueCrawlingSitemap(string url) {
            if (url != null) {
                Uri uri = new Uri(url);
                if (uri.Segments[uri.Segments.Length - 1].Contains("-index")) {
                    CrawlSitemapIndex(uri);
                } else {
                    CrawlSitemap(uri);
                }
            }
        }

        private void CrawlSitemap(Uri uri) {
            HtmlDocument page = GetHtmlDocumentFromUri(uri);
            HtmlNode.ElementsFlags["loc"] = HtmlElementFlag.Closed;
            HtmlNode.ElementsFlags["url"] = HtmlElementFlag.Closed;
            if (page != null) {
                HtmlNodeCollection sitemaps = page.DocumentNode.SelectNodes("//url");
                foreach (HtmlNode sitemap in sitemaps) {
                    HtmlNode loc = sitemap.SelectSingleNode("//loc");
                    if (visitedLinks.FindOrAdd(new Uri(loc.InnerText))) {
                        CloudQueueMessage msg = new CloudQueueMessage(loc.InnerText);
                        LinkQueue.AddMessage(msg);
                        dashboard.SizeOfQueue = dashboard.SizeOfQueue + 1;
                    }
                }
            }
        }

        private void CrawlRobots(Uri uri) {
            StreamReader reader = GetStreamFromUri(uri);
            string line = reader.ReadLine();
            while (line != null) {
                string[] split = Regex.Split(line, ": ");
                if (split[0].Equals("Sitemap")) {
                    switch (uri.Authority) {
                        case "www.cnn.com":
                            ThreadPool.QueueUserWorkItem(o => EnqueueCrawlingSitemap(split[1]));
                            break;
                        case "bleacherreport.com":
                            if (split[1].Contains("nba")) {
                                ThreadPool.QueueUserWorkItem(o => EnqueueCrawlingSitemap(split[1]));
                            }
                            break;
                        default:
                            break;
                    }
                } else if (split[0].Equals("Disallow")) {
                    visitedLinks.AddDisallow(new Uri("http://" + uri.Authority + split[1]));
                }
                line = reader.ReadLine();
            }
        }

        private void CrawlSitemapIndex(Uri uri) {
            HtmlDocument page = GetHtmlDocumentFromUri(uri);
            HtmlNode.ElementsFlags["loc"] = HtmlElementFlag.Closed;
            HtmlNode.ElementsFlags["sitemap"] = HtmlElementFlag.Closed;
            if (page != null) {
                HtmlNode[] sitemaps = page.DocumentNode.SelectNodes("//sitemap").ToArray();
                foreach (HtmlNode sitemap in sitemaps) {
                    HtmlNode loc = sitemap.FirstChild;
                    switch (uri.Authority) {
                        case "www.cnn.com":
                            try {
                                DateTime sitemapDate = Convert.ToDateTime(sitemap.LastChild.InnerText);
                                if (sitemapDate.AddMonths(3).Month >= DateTime.Now.Month && sitemapDate.Year == DateTime.Now.Year) {
                                    ThreadPool.QueueUserWorkItem(o => EnqueueCrawlingSitemap(loc.InnerText));
                                }
                            } catch (Exception e) { };
                            break;
                        case "bleacherreport.com":
                            ThreadPool.QueueUserWorkItem(o => EnqueueCrawlingSitemap(loc.InnerText));
                            break;
                        default:
                            break;
                    } 
                }
            }
        }

        private void CrawlLink(Uri uri) {
            LinkEntity entity = new LinkEntity(uri.Authority, string.Join("@420;", uri.LocalPath.Split('/')));
            HtmlDocument page = GetHtmlDocumentFromUri(uri);
            if(page == null) {
                return;
            }
            HtmlNode title = page.DocumentNode.SelectSingleNode("//title");
            HtmlNodeCollection linkNodes = page.DocumentNode.SelectNodes("//a");
            if (linkNodes == null) {
                return;
            }
            foreach (HtmlNode linkNode in linkNodes) {
                if (!dashboard.CrawlingState.Equals("Crawling")) {
                    return;
                }
                HtmlAttribute att = linkNode.Attributes["href"];
                if (att != null && !att.Value.Equals("")) {
                    string link = att.Value;
                    link = CleanUrl(link, uri.Authority);
                    if (!link.Equals("")) {
                        Uri newUri = new Uri(link);
                        CloudQueueMessage msg = new CloudQueueMessage(link);
                       
                        if (visitedLinks.FindOrAdd(newUri)) {
                            if (newUri.Authority.Equals("bleacherreport.com")) {
                                if (newUri.Segments.Length > 1) {
                                    if (newUri.Segments[1].Equals("nba/") || newUri.Segments[1].Equals("nba")) {
                                        LinkQueue.AddMessage(msg);
                                        IncrementQueueCount();
                                    }
                                } else {
                                    LinkQueue.AddMessage(msg);
                                    IncrementQueueCount();
                                }
                            } else {
                                LinkQueue.AddMessage(msg);
                                IncrementQueueCount();
                            }
                        }
                    }
                }
            }
            if (title != null) {
                entity.Title = title.InnerText;
                entity.link = uri.AbsoluteUri;
                entity.LinkTimestamp = DateTime.Now;
                LinkEntityQueue.Enqueue(entity);
                AddToLast10(uri.AbsoluteUri);
                if (LinkEntityQueue.Count >= maxBatch) {
                    BatchAddToTable().Wait();
                }
            }
        }

        private string CleanUrl(string url, string authority) {
            string cleanUrl = "";
            if (url.Substring(0, 1).Equals("/")) {
                cleanUrl = "http://" + authority + url;
            } else if (url.Contains("www.cnn.com") || url.Contains("bleacherreport.com")) {
                if (!url.Contains("http")) {
                    cleanUrl = "http://" + url;
                } else {
                    cleanUrl = url;
                }
            }
            return cleanUrl;
        }

        public void UpdateDashboard() {
            GetPerfCounters();
            dashboard.ETag = "*";
            TableOperation update = TableOperation.Replace(dashboard);
            DashboardTable.Execute(update);
        }

        public void GetPerfCounters() {
            PerformanceCounter mem = new PerformanceCounter("Memory", "Available MBytes", null);
            PerformanceCounter threadCt = new PerformanceCounter("Process", "Thread Count", "_Total");
            PerformanceCounter cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            dashboard.CpuUsage = cpu.NextValue();
            dashboard.RamAvailable = mem.NextValue();
            dashboard.ThreadCount = threadCt.NextValue();
        }

        public void NewDashboard() {
            dashboard = new Dashboard();
            visitedLinks.ClearTree();
            LinkEntityQueue.Clear();
            dashboard.CrawlingState = "Clear";
            dashboard.last10Urls = "[]";
            dashboard.SizeOfQueue = 0;
            dashboard.SizeOfTable = 0;
            dashboard.errorUris = "[]";
            LinkQueue.Clear();
        }

        public void PullDashboard() {
            TableOperation retrieve = TableOperation.Retrieve<Dashboard>("1", "1");
            TableResult result = DashboardTable.Execute(retrieve);
            dashboard = ((Dashboard)result.Result);
        }

        public void ClearIndex() {
            dashboard.CrawlingState = "Deleting Index";
            LinkTable.DeleteIfExists();
            bool created = false;
            while (!created) {
                try {
                    LinkTable.CreateIfNotExists();
                    created = true;
                } catch (StorageException e) {
                    if (e.RequestInformation.HttpStatusCode == 409) {
                        Task.Delay(1000);
                    } else {
                        throw;
                    }
                }
            }
            NewDashboard();
        }

        private void AddToLast10(string url) {
            string[] last10 = JsonConvert.DeserializeObject<string[]>(dashboard.last10Urls);
            string[] newArr = new string[10];
            newArr[0] = url;
            for (int i = 1; i < last10.Length && i < 10; i++) {
                newArr[i] = last10[i - 1];
            }
            dashboard.last10Urls = JsonConvert.SerializeObject(newArr);
        }

        private void AddErrorUrl(string url) {
            string[] errUrls = JsonConvert.DeserializeObject<string[]>(dashboard.errorUris);
            string[] newArr = new string[errUrls.Length + 1];
            newArr[0] = url;
            for (int i = 1; i < errUrls.Length; i++) {
                newArr[i] = errUrls[i - 1];
            }
            dashboard.errorUris = JsonConvert.SerializeObject(newArr);
        }

        private async Task BatchAddToTable() {
            Debug.WriteLine("Batch Add To Table {0}", LinkEntityQueue.Count);
            var tasks = new List<Task>();
            int i = 0;
            while (i <= maxBatch && LinkEntityQueue.Count > 0) {
                try {
                    LinkEntity entity = LinkEntityQueue.Dequeue();
                    entity.ETag = "*";
                    var task = Task.Factory.StartNew(() => {
                        try {
                            TableOperation insert = TableOperation.Insert(entity);
                            LinkTable.Execute(insert);
                            dashboard.SizeOfTable = dashboard.SizeOfTable + 1;
                        } catch (Exception e) {
                            TableOperation update = TableOperation.Replace(entity);
                            LinkTable.Execute(update);
                        }
                    });
                    i++;
                    tasks.Add(task);
                } catch (Exception e) {
                    return;
                }
            }
            if (maxBatch < 1000) {
                maxBatch = maxBatch + 10;
            }
            await Task.WhenAll(tasks);
        }

        private StreamReader GetStreamFromUri(Uri uri) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri.AbsoluteUri);
            request.UserAgent = "A .Net Web Crawler";
            WebResponse response = request.GetResponse();
            Stream streamResponse = response.GetResponseStream();
            StreamReader sreader = new StreamReader(streamResponse);
            return sreader;
        }

        private HtmlDocument GetHtmlDocumentFromUri(Uri url) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HtmlDocument doc = null;
            request.UserAgent = "A .Net Web Crawler";
            try {
                WebResponse response = request.GetResponse();
                Stream streamResponse = response.GetResponseStream();
                StreamReader sreader = new StreamReader(streamResponse);
                string s = "";
                s = sreader.ReadToEnd();
                doc = new HtmlDocument();
                doc.LoadHtml(s);
            } catch (WebException e) {
                AddErrorUrl(url.AbsoluteUri);
            }
            return doc;
        }
    }
}
