using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Hashflags
{
    public static class StoreHashflagImage
    {
        [FunctionName("StoreHashflagImage")]
        [StorageAccount("AzureWebJobsStorage")]
        public static async Task Run(
            [QueueTrigger("save-hashflags")] KeyValuePair<string, string> hf,
            [Blob("hashflags")] CloudBlobContainer hashflagsContainer,
            [Queue("create-hero")] ICollector<KeyValuePair<string, string>> createHeroCollector,
            ILogger log)
        {
            log.LogInformation($"Function executed at: {DateTime.Now}");

            await hashflagsContainer.CreateIfNotExistsAsync();

            var imageBlob = hashflagsContainer.GetBlockBlobReference(hf.Value);
            imageBlob.Properties.ContentType = "image/png";

            using (var client = new WebClient())
            {
                var image = client.DownloadData(new Uri(hf.Value));
                await imageBlob.UploadFromByteArrayAsync(image, 0, image.Length);
            }

            createHeroCollector.Add(hf);
        }
    }
}