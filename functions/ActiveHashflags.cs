using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using HtmlAgilityPack;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace hashflags
{
    public static class ActiveHashflags
    {
        [FunctionName("ActiveHashflags")]
        [StorageAccount("AzureWebJobsStorage")]
        public static void Run(
            [TimerTrigger("0 0 * * * *")] TimerInfo timer,
            [Blob("json/activeHashflags", FileAccess.ReadWrite)]
            CloudBlockBlob blob,
            ILogger log)
        {
            log.LogInformation($"Function executed at: {DateTime.Now}");

            var client = new HttpClient();
            var response = client.GetAsync("https://twitter.com/").Result;
            var content = response.Content.ReadAsStringAsync().Result;

            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var initDataInput = doc.DocumentNode.SelectSingleNode("//*[@id=\"init-data\"]");
            var initDataJson = WebUtility.HtmlDecode(initDataInput.GetAttributeValue("value", ""));
            var initData = JObject.Parse(initDataJson);

            log.LogInformation($"There are currently {((JObject) initData["activeHashflags"]).Count} active hashflags");

            var hashflags = new JObject(
                new JProperty("activeHashflags", initData["activeHashflags"])
            );

            blob.Properties.ContentType = "application/json";
            blob.UploadTextAsync(hashflags.ToString(Formatting.None));
        }
    }
}