using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Tweetinvi;
using Tweetinvi.Logic;
using Tweetinvi.Models;
using User = Tweetinvi.User;

namespace hashflags
{
    class TweetHashflag
    {

        [FunctionName("TweetHashflag")]
        [StorageAccount("AzureWebJobsStorage")]
        public static void Run(
            [TimerTrigger("0 * * * * *")] TimerInfo timer,
            [Queue("tweet")] KeyValuePair<string, string> hf,
            [Blob("heroimages")] CloudBlobContainer heroContainer,
            TraceWriter log)
        {
            log.Info($"Function executed at: {DateTime.Now}");

            var authenticatedUser = InitialiseTwitter();

            var hashflagBlob = heroContainer.GetBlockBlobReference(hf.Key);
            IMedia media;
            using (var stream = new MemoryStream())
            {
                hashflagBlob.DownloadToStream(stream);
                media = Upload.UploadImage(stream.ToArray());
            }
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

        private static string GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }
}
