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
using ClassLibrary;
using Microsoft.WindowsAzure.Storage.Queue;
using TweetSharp;
using Microsoft.WindowsAzure.Storage.Table;

namespace Tweeter {
    public class TweeterWorker : RoleEntryPoint {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private Crawler crawler;
        private TwitterService service;

        public override void Run() {
            Trace.TraceInformation("Tweeter is running");

            while(true) {
                CloudQueueMessage msg = AzureStorage.TweetQueue.GetMessage(TimeSpan.FromMinutes(5));
                if (msg != null) {
                    service.SendTweet(new SendTweetOptions {
                        Status = Uri.EscapeUriString(msg.AsString)
                    });
                    AzureStorage.TweetQueue.DeleteMessage(msg);
                }
            }
        }

        public override bool OnStart() {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            crawler = new Crawler();

            TableOperation get1 = TableOperation.Retrieve<AllThatJazzEntity>("1", "consumer");
            TableOperation get2 = TableOperation.Retrieve<AllThatJazzEntity>("1", "access");

            TableResult result1 = AzureStorage.AllThatJazzTable.Execute(get1);
            TableResult result2 = AzureStorage.AllThatJazzTable.Execute(get2);

            AllThatJazzEntity consumer = ((AllThatJazzEntity) result1.Result);
            AllThatJazzEntity access = ((AllThatJazzEntity) result2.Result);

            service = new TwitterService(consumer.honky, consumer.tonky);
            service.AuthenticateWith(access.honky, access.tonky);

            Trace.TraceInformation("Tweeter has been started");

            return result;
        }

        public override void OnStop() {
            Trace.TraceInformation("Tweeter is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("Tweeter has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken) {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested) {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
