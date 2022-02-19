using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace Hashflags;

public static class TweetHashflag
{
    [FunctionName("TweetHashflag")]
    [StorageAccount("AzureWebJobsStorage")]
    public static async Task Run(
        [TimerTrigger("0 * * * * *")] TimerInfo timer,
        [Blob("heroimages")] BlobContainerClient heroClient,
        ILogger log)
    {
        log.LogInformation($"Function executed at: {DateTime.Now}");

        var queueClient = new QueueClient(GetEnvironmentVariable("AzureWebJobsStorage"), "tweet");
        var message = await queueClient.ReceiveMessageAsync();
        if (message?.Value is null)
        {
            return;
        }

        var messageDict = message.Value.Body.ToObjectFromJson<Dictionary<string, string>>();
        var (key, _) = new KeyValuePair<string, string>(messageDict["Key"], messageDict["Value"]);

        var client = InitialiseTwitterClient();

        var hashflagClient = heroClient.GetBlobClient(key);
        var result = await hashflagClient.DownloadContentAsync();
        var media = await client.Upload.UploadTweetImageAsync(result.Value.Content.ToArray());

        await client.Tweets.PublishTweetAsync(new PublishTweetParameters
        {
            Text = $"#{key}",
            Medias = new List<IMedia>
                { media }
        });
        await queueClient.DeleteMessageAsync(message.Value.MessageId, message.Value.PopReceipt);
    }

    private static ITwitterClient InitialiseTwitterClient()
    {
        var consumerKey = GetEnvironmentVariable("CONSUMER_KEY");
        var consumerSecret = GetEnvironmentVariable("CONSUMER_SECRET");
        var accessToken = GetEnvironmentVariable("ACCESS_TOKEN");
        var accessTokenSecret = GetEnvironmentVariable("ACCESS_TOKEN_SECRET");
        var userCredentials = new TwitterCredentials(consumerKey, consumerSecret, accessToken, accessTokenSecret);
        return new TwitterClient(userCredentials);
    }

    private static string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }
}
