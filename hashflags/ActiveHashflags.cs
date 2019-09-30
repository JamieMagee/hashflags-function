using System;
using System.IO;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

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
            TraceWriter log)
        {
            log.Info($"Function executed at: {DateTime.Now}");

            var timeString = DateTime.UtcNow.ToString("yyyy-MM-dd-HH");

            var client = new HttpClient();
            var response = client.GetAsync($"https://pbs.twimg.com/hashflag/config-{timeString}.json").Result;
            var content = response.Content.ReadAsStringAsync().Result;
            var hashflagConfig = JArray.Parse(content).GroupBy(c => c["hashtag"]).Select(c => c.First()).ToList();

            log.Info($"There are currently {hashflagConfig.Count} active hashflags");

            var hashflags = hashflagConfig.Select(c =>
                new JProperty(c["hashtag"].ToString(), c["assetUrl"])
            );

            blob.Properties.ContentType = "application/json";
            blob.UploadText(new JObject(hashflags).ToString(Formatting.None), Encoding.UTF8);
        }
    }
}