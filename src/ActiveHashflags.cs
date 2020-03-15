using System;
using System.IO;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Hashflags
{
    public static class ActiveHashflags
    {
        [FunctionName("ActiveHashflags")]
        [StorageAccount("AzureWebJobsStorage")]
        public static async Task Run(
            [TimerTrigger("0 0 * * * *")] TimerInfo timer,
            [Blob("json/activeHashflags", FileAccess.ReadWrite)]
            CloudBlockBlob blob,
            ILogger log)
        {
            log.LogInformation($"Function executed at: {DateTime.Now}");

            var timeString = DateTime.UtcNow.ToString("yyyy-MM-dd-HH");

            var client = new HttpClient();
            var response = client.GetAsync($"https://pbs.twimg.com/hashflag/config-{timeString}.json").Result;
            var content = response.Content.ReadAsStringAsync().Result;
            var hashflagConfig = JArray.Parse(content).GroupBy(c => c["hashtag"].ToString().Trim()).Select(c => c.First()).ToList();

            log.LogInformation($"There are currently {hashflagConfig.Count} active hashflags");

            var hashflags = hashflagConfig.Select(c =>
                new JProperty(c["hashtag"].ToString(), c["assetUrl"])
            );

            blob.Properties.ContentType = "application/json";
            await blob.UploadTextAsync(new JObject(hashflags).ToString(Formatting.None));
        }
    }
}