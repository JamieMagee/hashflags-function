using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Hashflags;

public static class StoreHashflagImage
{
    [FunctionName("StoreHashflagImage")]
    [StorageAccount("AzureWebJobsStorage")]
    public static async Task Run(
        [QueueTrigger("save-hashflags")] KeyValuePair<string, string> hf,
        [Blob("hashflags")] BlobContainerClient hashflagsContainerClient,
        [Queue("create-hero")] ICollector<KeyValuePair<string, string>> createHeroCollector,
        ILogger log)
    {
        log.LogInformation($"Function executed at: {DateTime.Now}");

        await hashflagsContainerClient.CreateIfNotExistsAsync();

        var imageClient = hashflagsContainerClient.GetBlobClient(hf.Value);

        using var client = new HttpClient();
        var image = await client.GetStreamAsync(new Uri(hf.Value));
        await imageClient.UploadAsync(image, new BlobHttpHeaders { ContentType = "image/png" });

        createHeroCollector.Add(hf);
    }
}
