using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Tweetinvi;
using Tweetinvi.Logic;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using User = Tweetinvi.User;

namespace hashflags
{
    public static class TweetHashflag
    {
        [FunctionName("TweetHashflag")]
        [StorageAccount("AzureWebJobsStorage")]
        public static void Run(
            [TimerTrigger("0 0 * * * *")] TimerInfo timer,
            [Blob("heroimages")] CloudBlobContainer heroContainer,
            TraceWriter log)
        {
            log.Info($"Function executed at: {DateTime.Now}");

            var queue = FetchQueue();
            var message = queue.GetMessage();
            var messageDict = message.AsString.ConvertJsonTo<Dictionary<string, string>>();
            var hf = new KeyValuePair<string, string>(messageDict["Key"], messageDict["Value"]);

            var authenticatedUser = InitialiseTwitter();
            RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackOnly;

            TweetinviEvents.QueryBeforeExecute += (sender, args) =>
            {
                var queryRateLimits = RateLimit.GetQueryRateLimit(args.QueryURL);
                // Some methods are not RateLimited. Invoking such a method will result in the queryRateLimits to be null
                if (queryRateLimits != null)
                {
                    if (queryRateLimits.Remaining > 0)
                    {
                        // We have enough resource to execute the query
                        return;
                    }
                    log.Info("Rate limited. Retry later");
                    args.Cancel = true;
                    return;
                }
            };

            var hashflagBlob = heroContainer.GetBlockBlobReference(hf.Key);
            IMedia media;
            using (var stream = new MemoryStream())
            {
                hashflagBlob.DownloadToStream(stream);
                media = Upload.UploadImage(stream.ToArray());
            }

            var tweet = authenticatedUser.PublishTweet('#' + hf.Key, new PublishTweetOptionalParameters
            {
                Medias = new List<IMedia> { media }
            });
            queue.DeleteMessage(message);
        }

        private static AuthenticatedUser InitialiseTwitter()
        {
            var consumerKey = GetEnvironmentVariable("CONSUMER_KEY");
            var consumerSecret = GetEnvironmentVariable("CONSUMER_SECRET");
            var accessToken = GetEnvironmentVariable("ACCESS_TOKEN");
            var accessTokenSecret = GetEnvironmentVariable("ACCESS_TOKEN_SECRET");
            var userCredentials = Auth.CreateCredentials(consumerKey, consumerSecret, accessToken, accessTokenSecret);
            return User.GetAuthenticatedUser(userCredentials) as AuthenticatedUser;
        }

        private static string GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);

        private static CloudQueue FetchQueue()
        {
            var storageAccount = CloudStorageAccount.Parse(GetEnvironmentVariable("AzureWebJobsStorage"));
            var queueClient = storageAccount.CreateCloudQueueClient();
            return queueClient.GetQueueReference("tweet");
        }
    }
}
