using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;

namespace Hashflags
{
    public static class UpdateHashflagState
    {
        [FunctionName("UpdateHashflagState")]
        [StorageAccount("AzureWebJobsStorage")]
        public static async Task Run(
            [TimerTrigger("0 1 * * * *")] TimerInfo timer,
            [Blob("json/activeHashflags", FileAccess.ReadWrite)]
            CloudBlockBlob initDataBlob,
            [Table("hashflags")] CloudTable table,
            [Queue("save-hashflags")] ICollector<KeyValuePair<string, string>> saveHashflagsCollector,
            ILogger log)
        {
            log.LogInformation($"Function executed at: {DateTime.UtcNow}");

            var activeHashflagsString = await initDataBlob.DownloadTextAsync();
            var activeHashflags = JObject.Parse(activeHashflagsString).ToObject<Dictionary<string, string>>();

            await table.CreateIfNotExistsAsync();
            var tableQuery =
                new TableQuery<HashFlag>().Where(TableQuery.GenerateFilterCondition("PartitionKey", "eq", "active"));
            var previousHashflags = new List<HashFlag>();
            TableContinuationToken? token = null;

            do
            {
                var segment = await table.ExecuteQuerySegmentedAsync(tableQuery, token);
                token = segment.ContinuationToken;
                previousHashflags.AddRange(segment.Results);
            } while (token != null);

            var previousHashtags = previousHashflags.Select(x => x.HashTag);
            var currentHashtags = activeHashflags.Select(x => x.Key);

            log.LogInformation($"previous Hashtags: {previousHashtags.Count()}");
            log.LogInformation($"current Hashtags: {currentHashtags.Count()}");


            foreach (var entry in previousHashtags.Except(currentHashtags))
            {
                log.LogInformation($"INACTIVE: {entry}");
                var hf = previousHashflags.First(x => x.HashTag == entry);
                MovePartition(hf, table);
            }

            foreach (var entry in currentHashtags.Except(previousHashtags))
            {
                log.LogInformation($"NEW: {entry}");
                var hf = activeHashflags.First(x => x.Key == entry);
                InsertNew(hf, table);
                saveHashflagsCollector.Add(hf);
            }
        }

        private static async void MovePartition(HashFlag hf, CloudTable table)
        {
            var delete = TableOperation.Delete(hf);
            var insert = TableOperation.InsertOrReplace(new HashFlag
            {
                PartitionKey = "inactive",
                RowKey = hf.RowKey,
                HashTag = hf.HashTag,
                Path = hf.Path,
                FirstSeen = hf.FirstSeen,
                LastSeen = DateTime.UtcNow.Date
            });

            await table.ExecuteAsync(delete);
            await table.ExecuteAsync(insert);
        }

        private static async void InsertNew(KeyValuePair<string, string> hf, CloudTable table)
        {
            var urlParts = hf.Value.Split('/');
            var rowKey = string.Join("", new ArraySegment<string>(urlParts, urlParts.Length - 2, 2)).Split('.')[0];

            var insert = TableOperation.InsertOrReplace(new HashFlag
            {
                PartitionKey = "active",
                RowKey = hf.Key + rowKey,
                HashTag = hf.Key,
                Path = hf.Value,
                FirstSeen = DateTime.UtcNow.Date,
                LastSeen = DateTime.UtcNow.Date
            });

            await table.ExecuteAsync(insert);
        }
    }

    public class HashFlag : TableEntity
    {
        public string? HashTag { get; set; }
        public string? Path { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
