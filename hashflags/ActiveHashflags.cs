using System;
using System.IO;
using System.Net;
using System.Net.Http;
using HtmlAgilityPack;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace hashflags
{
    public static class ActiveHashflags
    {
        [FunctionName("ActiveHashflags")]
        public static void Run(
            [TimerTrigger("0 0 * * * *")]TimerInfo timer,
            [Blob("json/activeHashflags", FileAccess.ReadWrite, Connection = "AzureWebJobsStorage")] CloudBlockBlob blob,
            TraceWriter log)
        {
            log.Info($"Function executed at: {DateTime.Now}");

            var client = new HttpClient();
            var response = client.GetAsync("https://twitter.com/").Result;
            var content = response.Content.ReadAsStringAsync().Result;

            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var initDataInput = doc.DocumentNode.SelectSingleNode("//*[@id=\"init-data\"]");
            var initDataJson = WebUtility.HtmlDecode(initDataInput.GetAttributeValue("value", ""));
            var initData = JObject.Parse(initDataJson);

            log.Info($"There are currently {((JObject)initData["activeHashflags"]).Count} active hashflags");

            var hashflags = new JObject(
                    new JProperty("hashflagBaseUrl", initData["hashflagBaseUrl"]),
                    new JProperty("activeHashflags", initData["activeHashflags"])
                );

            blob.Properties.ContentType = "application/json";
            blob.UploadText(hashflags.ToString(Formatting.None), System.Text.Encoding.UTF8);
        }
    }
}
