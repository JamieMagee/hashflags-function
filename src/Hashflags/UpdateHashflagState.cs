using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Hashflags;

public static class UpdateHashflagState
{
    [FunctionName("UpdateHashflagState")]
    [StorageAccount("AzureWebJobsStorage")]
    public static async Task Run(
        [TimerTrigger("0 1 * * * *")] TimerInfo timer,
        [Blob("json/activeHashflags", FileAccess.Read)]
        string activeHashflagsString,
        [Table("hashflags")] TableClient tableClient,
        [Queue("save-hashflags")] IAsyncCollector<KeyValuePair<string, string>> saveHashflagsCollector,
        ILogger log)
    {
        log.LogInformation($"Function executed at: {DateTime.UtcNow}");

        var activeHashflags = JsonSerializer.Deserialize<Dictionary<string, string>>(activeHashflagsString);

        await tableClient.CreateIfNotExistsAsync();

        var previousHashflags = await tableClient.QueryAsync<HashFlag>("PartitionKey eq 'active'").ToListAsync();

        var previousHashtags = previousHashflags.Select(x => x.HashTag).ToList();
        var currentHashtags = activeHashflags.Select(x => x.Key).ToList();

        log.LogInformation($"previous Hashtags: {previousHashtags.Count}");
        log.LogInformation($"current Hashtags: {currentHashtags.Count}");

        foreach (var entry in previousHashtags.Except(currentHashtags))
        {
            log.LogInformation($"INACTIVE: {entry}");
            var hf = previousHashflags.First(x => x.HashTag == entry);
            await MovePartition(hf, tableClient);
        }

        foreach (var entry in currentHashtags.Except(previousHashtags))
        {
            log.LogInformation($"NEW: {entry}");
            var hf = activeHashflags.First(x => x.Key == entry);
            await InsertNew(hf, tableClient);
            await saveHashflagsCollector.AddAsync(hf);
        }
    }

    private static async Task MovePartition(HashFlag hf, TableClient tableClient)
    {
        var delete = new TableTransactionAction(TableTransactionActionType.Delete, hf);
        var insert = new TableTransactionAction(TableTransactionActionType.UpsertReplace, new HashFlag
        {
            PartitionKey = "inactive",
            RowKey = hf.RowKey,
            HashTag = hf.HashTag,
            Path = hf.Path,
            FirstSeen = hf.FirstSeen,
            LastSeen = DateTime.UtcNow.Date
        });

        await tableClient.SubmitTransactionAsync(new[] { insert, delete });
    }

    private static async Task InsertNew(KeyValuePair<string, string> hf, TableClient tableClient)
    {
        var (key, value) = hf;
        var urlParts = value.Split('/');
        var rowKey = string.Join("", new ArraySegment<string>(urlParts, urlParts.Length - 2, 2)).Split('.')[0];

        var insert = new TableTransactionAction(TableTransactionActionType.UpsertReplace, new HashFlag
        {
            PartitionKey = "active",
            RowKey = key + rowKey,
            HashTag = key,
            Path = value,
            FirstSeen = DateTime.UtcNow.Date,
            LastSeen = DateTime.UtcNow.Date
        });

        await tableClient.SubmitTransactionAsync(new[] { insert });
    }
}

public sealed record HashFlag : ITableEntity
{
    public string? HashTag { get; init; }

    public string? Path { get; init; }

    public DateTime FirstSeen { get; init; }

    public DateTime LastSeen { get; set; }

    public string PartitionKey { get; set; } = null!;

    public string RowKey { get; set; } = null!;

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }
}
