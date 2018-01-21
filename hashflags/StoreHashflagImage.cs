using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;

namespace hashflags
{
    public static class StoreHashflagImage
    {
        private const string BaseUrl = "https://abs.twimg.com/hashflags/";

        [FunctionName("StoreHashflagImage")]
        [StorageAccount("AzureWebJobsStorage")]
        public static void Run(
            [QueueTrigger("save-hashflags")] KeyValuePair<string, string> hf,
            [Blob("hashflags")] CloudBlobContainer hashflagsContainer,
            TraceWriter log)
        {
            log.Info($"Function executed at: {DateTime.Now}");

            hashflagsContainer.CreateIfNotExists();

            var imageBlob = hashflagsContainer.GetBlockBlobReference(hf.Value);
            imageBlob.Properties.ContentType = "image/png";

            using (var client = new WebClient())
            {
                var image = client.DownloadData(new Uri(BaseUrl + hf.Value));
                imageBlob.UploadFromByteArray(image, 0, image.Length);
            }
        }
    }
}