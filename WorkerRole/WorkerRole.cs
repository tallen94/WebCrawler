using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using ClassLibrary;
using HtmlAgilityPack;
using System.IO;
using System.Text.RegularExpressions;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private static Crawler crawler = new Crawler();
        private static DateTime lastPing = DateTime.Now;

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole is running");
            while(true) {
                CloudQueueMessage command = AzureStorage.CommandQueue.GetMessage(TimeSpan.FromMinutes(5));
                if (command != null) {
                    switch (command.AsString) {
                        case "New Dashboard":
                            crawler.NewDashboard();
                            break;
                        case "New Crawl":
                            crawler.NewCrawl();
                            break;
                        case "Write Visited":
                            crawler.WriteVisited();
                            break;
                        case "Stop":
                            crawler.Stop();
                            break;
                        case "Start":
                            crawler.Start();
                            break;
                        case "Clear Index":
                            // This is commented so the index wont get cleared unless manually
                            crawler.ClearIndex();
                            break;
                        default:
                            break;
                    }
                    AzureStorage.CommandQueue.DeleteMessage(command);
                }
                if (crawler.CurrentState().Equals("Crawling")) {
                    CloudQueueMessage link = AzureStorage.LinkQueue.GetMessage(TimeSpan.FromMinutes(5));
                    if (link != null) {
                        try {
                            AzureStorage.LinkQueue.DeleteMessage(link);
                            crawler.CrawlUrl(link.AsString);
                        } catch { }
                    }
                }
            }
        }

        public void MonitorDashboard() {
            while (true) {
                Task.Delay(100);
                crawler.UpdateDashboard();
                if ((DateTime.Now - lastPing) >= new TimeSpan(0, 15, 0)) {
                    PingSearchSuggestions();
                    lastPing = DateTime.Now;
                }
            }
        }

        private void PingSearchSuggestions() {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://tallensearchengine.cloudapp.com/SearchSuggestions.asmx/BuildTrie");
            WebResponse response = request.GetResponse();
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            bool result = base.OnStart();
            ThreadPool.QueueUserWorkItem(o => MonitorDashboard());
            if (crawler.WasCrawling()) {
                crawler.RebuildVisited();
            }
            Trace.TraceInformation("WorkerRole has been started");
            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100);
            }
        }        
    }
}
