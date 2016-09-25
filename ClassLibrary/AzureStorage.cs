using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary {
    public static class AzureStorage {

        private static CloudStorageAccount StorageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=talleninfo344;AccountKey=+gSIkx8cWQ27OiqnNvoR3xeYyzyg25ivyqm685czFgjiYbq9IB29kar687B8hN+uqwUD5kGZgKXErH6P/Ljv+Q==");
        private static CloudQueueClient QueueClient = StorageAccount.CreateCloudQueueClient();
        private static CloudTableClient TableClient = StorageAccount.CreateCloudTableClient();
        private static CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();
        public static CloudBlobContainer VisitedLinksContainer = BlobClient.GetContainerReference("visitedlinks");
        public static CloudQueue LinkQueue = QueueClient.GetQueueReference("linkqueue");
        public static CloudQueue CommandQueue = QueueClient.GetQueueReference("commandqueue");
        public static CloudQueue TweetQueue = QueueClient.GetQueueReference("tweetqueue");
        public static CloudTable LinkTable = TableClient.GetTableReference("sitetable");
        public static CloudTable DashboardTable = TableClient.GetTableReference("dashboardtable");
        public static CloudTable AllThatJazzTable = TableClient.GetTableReference("allthatjazz");


        public static void CreateAll() {
            LinkQueue.CreateIfNotExists();
            LinkTable.CreateIfNotExists();
            TweetQueue.CreateIfNotExists();
            CommandQueue.CreateIfNotExists();
            DashboardTable.CreateIfNotExists();
            VisitedLinksContainer.CreateIfNotExists();

            /* ONLY USE FOR RELEASE VERSIONS
            LinkQueue = QueueClient.GetQueueReference("testlinkqueue");
            CommandQueue = QueueClient.GetQueueReference("testcommandqueue");
            TweetQueue = QueueClient.GetQueueReference("testtweetqueue");
            LinkTable = TableClient.GetTableReference("testsitetable");
            DashboardTable = TableClient.GetTableReference("testdashboardtable");
            */
        }
    }
}
