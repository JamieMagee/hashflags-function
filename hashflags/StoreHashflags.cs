using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;

namespace hashflags
{
    public static class StoreHashflags
    {
        [FunctionName("StoreHashflags")]
        [StorageAccount("AzureWebJobsStorage")]
        public static void Run(
            [TimerTrigger("0 0 * * * *")] TimerInfo timer,
            [Blob("json/activeHashflags", FileAccess.ReadWrite)] CloudBlockBlob initDataBlob,
            [Table("hashflags", "active")] CloudTable table,
            TraceWriter log)
        {
            log.Info($"Function executed at: {DateTime.Now}");

            var initDataJson = initDataBlob.DownloadText(Encoding.UTF8);
            var initData = JObject.Parse(initDataJson);
            var activeHashflags = initData["activeHashflags"].ToObject<Dictionary<string, string>>();

            var tableQuery = new TableQuery<HashFlag>();
            var previousHashflags = table.ExecuteQuery(tableQuery);

            var previousHashtags = previousHashflags.Select(x => x.HashTag);
            var currentHashtags = activeHashflags.Select(x => x.Key);

            foreach(var entry in previousHashtags.Except(currentHashtags))
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
            }

            foreach (var entry in currentHashtags.Intersect(previousHashtags))
            {
                log.Info($"ACTIVE: {entry}");
            }
        }

        private static void MovePartition(HashFlag hf, CloudTable table)
        {
            var batch = new TableBatchOperation();
            batch.Delete(hf);
            batch.Insert(new HashFlag()
            {
                PartitionKey = "Inactive",
                RowKey = hf.RowKey,
                HashTag = hf.HashTag,
                Path = hf.Path,
                FirstSeen = hf.FirstSeen,
                LastSeen = DateTime.Now.Date
            });

            table.ExecuteBatch(batch);
        }

        private static void InsertNew(KeyValuePair<string, string> hf, CloudTable table)
        {
            var insert = TableOperation.Insert(new HashFlag
            {
                RowKey = "Active",
                PartitionKey = hf.Key + hf.Value.Replace('/', '_'),
                HashTag = hf.Key,
                Path = hf.Value,
                FirstSeen = DateTime.Now.Date
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
