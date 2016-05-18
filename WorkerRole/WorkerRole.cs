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

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole is running");
            while(true) {
                CloudQueueMessage command = crawler.CommandQueue.GetMessage(TimeSpan.FromMinutes(5));
                if (command != null) {
                    switch (command.AsString) {
                        case "New Dashboard":
                            //crawler.NewDashboard();
                            break;
                        case "New Crawl":
                            crawler.NewCrawl();
                            break;
                        case "Write Visited":
                            //crawler.WriteVisited();
                            break;
                        case "Stop":
                            crawler.Stop();
                            break;
                        case "Start":
                            crawler.Start();
                            break;
                        case "Clear Index":
                            //crawler.ClearIndex();
                            break;
                        default:
                            break;
                    }
                    crawler.CommandQueue.DeleteMessage(command);
                }
                if (crawler.CurrentState().Equals("Crawling")) {
                    CloudQueueMessage link = crawler.LinkQueue.GetMessage();
                    if (link != null) {
                        try {
                            crawler.LinkQueue.DeleteMessage(link);
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
                /*
                if (DateTime.Now.Hour == 9) {
                    crawler.WriteVisited();
                }*/
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            /*
            if (crawler.CurrentState().Equals("Crawling")) {
                crawler.RebuildVisited();
            }*/
            bool result = base.OnStart();
            Trace.TraceInformation("WorkerRole has been started");
            ThreadPool.QueueUserWorkItem(o => MonitorDashboard());
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
