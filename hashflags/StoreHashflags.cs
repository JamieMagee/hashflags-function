using System;
using System.Collections.Generic;
using System.IO;
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
            [TimerTrigger("0 0 * * * *")]TimerInfo timer,
            [Blob("json/activeHashflags", FileAccess.Read] CloudBlockBlob initDataBlob,
            [Table("hashflags", "active")] CloudTable hashflagsTable,
            TraceWriter log)
        {
            log.Info($"Function executed at: {DateTime.Now}");

            var initDataJson = initDataBlob.DownloadText(Encoding.UTF8);
            var initData = JObject.Parse(initDataJson);
            var activeHashflags = initData.ToObject<Dictionary<string, object>>();

            var tableQuery = new TableQuery<HashFlag>();
            var previousHashflags = hashflagsTable.ExecuteQuery(tableQuery);
            
            foreach (var entry in activeHashflags)
            {
                log.Info($"Hashflag: {entry.Key}. Path: {entry.Value}");
            }
            
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
