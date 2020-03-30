using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json.Linq;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace Hashflags
{
    public static class TweetHashflag
    {
        [FunctionName("TweetHashflag")]
        [StorageAccount("AzureWebJobsStorage")]
        public static async Task Run(
            [TimerTrigger("0 * * * * *")] TimerInfo timer,
            [Blob("heroimages")] CloudBlobContainer heroContainer,
            ILogger log)
        {
            log.LogInformation($"Function executed at: {DateTime.Now}");

            var queue = FetchQueue();
            var message = await queue.GetMessageAsync();
            if (message == null) return;
            var messageDict = JObject.Parse(message.AsString).ToObject<Dictionary<string, string>>();
            var (key, _) = new KeyValuePair<string, string>(messageDict["Key"], messageDict["Value"]);

            var authenticatedUser = InitialiseTwitter();

            IMedia media;
            await using (var stream = new MemoryStream())
            {
                var hashflagBlob = heroContainer.GetBlockBlobReference(key);
                await hashflagBlob.DownloadToStreamAsync(stream);
                media = Auth.ExecuteOperationWithCredentials(authenticatedUser.Credentials,
                    () => Upload.UploadBinary(stream.ToArray()));
            }

            authenticatedUser.PublishTweet('#' + key, new PublishTweetOptionalParameters
            {
                Medias = new List<IMedia> {media}
            });
            await queue.DeleteMessageAsync(message);
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

        private static string? GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);

        private static CloudQueue FetchQueue()
        {
            var storageAccount = CloudStorageAccount.Parse(GetEnvironmentVariable("AzureWebJobsStorage"));
            var queueClient = storageAccount.CreateCloudQueueClient();
            return queueClient.GetQueueReference("tweet");
        }
    }
}
