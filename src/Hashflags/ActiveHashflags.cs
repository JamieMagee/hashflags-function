using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Hashflags;

public static class ActiveHashflags
{
    [FunctionName("ActiveHashflags")]
    [StorageAccount("AzureWebJobsStorage")]
    public static async Task Run(
        [TimerTrigger("0 0 * * * *")] TimerInfo timer,
        [Blob("json/activeHashflags", FileAccess.Write)]
        BlockBlobClient blobClient,
        ILogger log)
    {
        log.LogInformation($"Function executed at: {DateTime.Now}");

        var timeString = DateTime.UtcNow.ToString("yyyy-MM-dd-HH");

        var client = new HttpClient();
        var hashflagConfig =
            (await client.GetFromJsonAsync<IEnumerable<HashflagConfig>>($"https://pbs.twimg.com/hashflag/config-{timeString}.json") ?? Array.Empty<HashflagConfig>())
            .GroupBy(config => config.Hashtag.Trim())
            .Select(config => config.First())
            .ToList();

        log.LogInformation($"There are currently {hashflagConfig.Count} active hashflags");

        var hashflags = hashflagConfig.ToDictionary(
            config => config.Hashtag,
            config => config.AssetUrl
        );

        await using var ms = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(hashflags)));
        await blobClient.UploadAsync(ms, new BlobHttpHeaders { ContentType = "application/json" });
    }
}

internal sealed record HashflagConfig
{
    [JsonPropertyName("campaignName")]
    public string CampaignName { get; set; }

    [JsonPropertyName("hashtag")]
    public string Hashtag { get; set; }

    [JsonPropertyName("assetUrl")]
    public string AssetUrl { get; set; }

    [JsonPropertyName("startingTimestampMs")]
    public string StartingTimestampMs { get; set; }

    [JsonPropertyName("endingTimestampMs")]
    public string EndingTimestampMs { get; set; }
}
