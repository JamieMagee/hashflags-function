using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;

namespace hashflags
{
    class TweetHashflag
    {

        [FunctionName("CreateHeroImage")]
        [StorageAccount("AzureWebJobsStorage")]
        public static void Run(
            [QueueTrigger("tweet")] KeyValuePair<string, string> hf,
            [Blob("heroimages")] CloudBlobContainer heroContainer,
            TraceWriter log)
        {
            log.Info($"Function executed at: {DateTime.Now}");

            
        }
}
