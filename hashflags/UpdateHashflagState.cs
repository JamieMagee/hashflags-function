using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace hashflags
{
    public static class UpdateHashflagState
    {
        [FunctionName("UpdateHashflagState")]
        [StorageAccount("AzureWebJobsStorage")]
        public static void Run(
            [TimerTrigger("0 1 * * * *")] TimerInfo timer,
            [Blob("json/activeHashflags", FileAccess.ReadWrite)]
            CloudBlockBlob initDataBlob,
            [Table("hashflags")] CloudTable table,
            [Queue("save-hashflags")] ICollector<KeyValuePair<string, string>> saveHashflagsCollector,
            TraceWriter log)
        {
            log.Info($"Function executed at: {DateTime.UtcNow}");

            var activeHashflagsString = initDataBlob.DownloadText(Encoding.UTF8);
            var activeHashflags = JObject.Parse(activeHashflagsString).ToObject<Dictionary<string, string>>();

            table.CreateIfNotExists();
            var tableQuery =
                new TableQuery<HashFlag>().Where(TableQuery.GenerateFilterCondition("PartitionKey", "eq", "active"));
            var previousHashflags = table.ExecuteQuery(tableQuery);

            var previousHashtags = previousHashflags.Select(x => x.HashTag);
            var currentHashtags = activeHashflags.Select(x => x.Key);

            log.Info($"previous Hashtags: {previousHashtags.Count()}");
            log.Info($"current Hashtags: {currentHashtags.Count()}");


            foreach (var entry in previousHashtags.Except(currentHashtags))
            {
                log.Info($"INACTIVE: {entry}");
                var hf = previousHashflags.First(x => x.HashTag == entry);
                MovePartition(hf, table);
            }

            foreach (var entry in currentHashtags.Except(previousHashtags))
            {
                log.Info($"NEW: {entry}");
                var hf = activeHashflags.First(x => x.Key == entry);
                InsertNew(hf, table);
                saveHashflagsCollector.Add(hf);
            }
        }

        private static void MovePartition(HashFlag hf, CloudTable table)
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

            table.Execute(delete);
            table.Execute(insert);
        }

        private static void InsertNew(KeyValuePair<string, string> hf, CloudTable table)
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

            table.Execute(insert);
        }
    }

    public class HashFlag : TableEntity
    {
        public string HashTag { get; set; }
        public string Path { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
