using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace hashflags
{
    public static class StoreHashflagImage
    {
        [FunctionName("StoreHashflagImage")]
        [StorageAccount("AzureWebJobsStorage")]
        public static void Run(
            [QueueTrigger("save-hashflags")] KeyValuePair<string, string> hf,
            [Blob("hashflags")] CloudBlobContainer hashflagsContainer,
            [Queue("create-hero")] ICollector<KeyValuePair<string, string>> createHeroCollector,
            ILogger log)
        {
            log.LogInformation($"Function executed at: {DateTime.Now}");

            hashflagsContainer.CreateIfNotExistsAsync();

            var imageBlob = hashflagsContainer.GetBlockBlobReference(hf.Value);
            imageBlob.Properties.ContentType = "image/png";

            using (var client = new WebClient())
            {
                var image = client.DownloadData(new Uri(hf.Value));
                imageBlob.UploadFromByteArrayAsync(image, 0, image.Length);
            }

            createHeroCollector.Add(hf);
        }
    }
}