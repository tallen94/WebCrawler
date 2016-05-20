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

        [WebMethod]
        public void StartCrawling() {
            if (crawler.CurrentState().Equals("Clear")) {
                CloudQueueMessage msg = new CloudQueueMessage("http://www.cnn.com/robots.txt");
                CloudQueueMessage msg2 = new CloudQueueMessage("http://bleacherreport.com/robots.txt");
                crawler.LinkQueue.AddMessage(msg);
                crawler.LinkQueue.AddMessage(msg2);
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
        public string GetPageTitle(string url) {
            return crawler.GetTitleForUrl(url);
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
