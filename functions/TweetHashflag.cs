using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace hashflags
{
    public static class TweetHashflag
    {
        [FunctionName("TweetHashflag")]
        [StorageAccount("AzureWebJobsStorage")]
        public static void Run(
            [TimerTrigger("0 * * * * *")] TimerInfo timer,
            [Blob("heroimages")] CloudBlobContainer heroContainer,
            TraceWriter log)
        {
            log.Info($"Function executed at: {DateTime.Now}");

            var queue = FetchQueue();
            var message = queue.GetMessageAsync().Result;
            if (message == null) return;
            var messageDict = message.AsString.ConvertJsonTo<Dictionary<string, string>>();
            var hf = new KeyValuePair<string, string>(messageDict["Key"], messageDict["Value"]);

            var authenticatedUser = InitialiseTwitter();

            IMedia media;
            using (var stream = new MemoryStream())
            {
                var hashflagBlob = heroContainer.GetBlockBlobReference(hf.Key);
                hashflagBlob.DownloadToStreamAsync(stream);
                media = Auth.ExecuteOperationWithCredentials(authenticatedUser.Credentials,
                    () => Upload.UploadImage(stream.ToArray()));
            }

            authenticatedUser.PublishTweet('#' + hf.Key, new PublishTweetOptionalParameters
            {
                Medias = new List<IMedia> {media}
            });
            queue.DeleteMessageAsync(message);
        }

        private static IAuthenticatedUser InitialiseTwitter()
        {
            var consumerKey = GetEnvironmentVariable("CONSUMER_KEY");
            var consumerSecret = GetEnvironmentVariable("CONSUMER_SECRET");
            var accessToken = GetEnvironmentVariable("ACCESS_TOKEN");
            var accessTokenSecret = GetEnvironmentVariable("ACCESS_TOKEN_SECRET");
            var userCredentials = Auth.CreateCredentials(consumerKey, consumerSecret, accessToken, accessTokenSecret);
            return User.GetAuthenticatedUser(userCredentials);
        }

        private static string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }

        private static CloudQueue FetchQueue()
        {
            var storageAccount = CloudStorageAccount.Parse(GetEnvironmentVariable("AzureWebJobsStorage"));
            var queueClient = storageAccount.CreateCloudQueueClient();
            return queueClient.GetQueueReference("tweet");
        }
    }
}
