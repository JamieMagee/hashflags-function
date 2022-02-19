using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Hashflags.Tests.Utilities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Hashflags.Tests;

public class CreateHeroImageTests
{
    private static readonly Mock<BlobContainerClient> MockHashflagsContainer = new(new Uri("http://tempuri.org/container"));

    private static readonly Mock<BlobContainerClient> MockHeroContainer = new(new Uri("http://tempuri.org/container"));

    private static readonly Mock<ICollector<KeyValuePair<string, string>>> TweetCollector = new();

    private static readonly KeyValuePair<string, string> HashtagUrlPair = new("Test", "");

    private static readonly Mock<BlobClient> MockBlobClient = new(new Uri("http://tempuri.org/blob"));

    private readonly ILogger _logger = TestFactory.CreateLogger();

    [Fact(Skip = "")]
    public void CreateHeroImage_ReturnsContent()
    {
        // var mockBlobDownloadResult = new Mock<BlobDownloadResult>();
        //     mockBlobDownloadResult.Setup(x => x.Content.ToStream())
        //     .Returns<MemoryStream>(stream =>
        //     {
        //         var file = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "doritos.png");
        //         File.OpenRead(file).CopyTo(stream);
        //         return stream;
        //     });
        // var mockResponse = new Mock<Response<BlobDownloadResult>>();
        // mockResponse.SetupGet(x => x.Value)
        //     .Returns(mockBlobDownloadResult.Object);
        // MockHashflagsContainer
        //     .Setup(x => x.GetBlobClient(It.IsAny<string>()))
        //     .Returns(MockBlobClient.Object);
        // MockBlobClient.Setup(x => x.DownloadContentAsync())
        //     .ReturnsAsync(mockResponse.Object);
        // MockHeroContainer.Setup(x => x.GetBlobClient(It.IsAny<string>()))
        //     .Returns(MockBlobClient.Object);
        // MockBlobClient.Setup(x => x.UploadAsync(It.IsAny<Stream>(),
        //     It.IsAny<BlobHttpHeaders>(),
        //     It.IsAny<IDictionary<string, string>>(),
        //     It.IsAny<BlobRequestConditions>(),
        //     It.IsAny<IProgress<long>>(),
        //     It.IsAny<AccessTier>(),
        //     It.IsAny<StorageTransferOptions>(),
        //     It.IsAny<CancellationToken>()));
        //
        // CreateHeroImage.Run(HashtagUrlPair, MockHeroContainer.Object, MockHashflagsContainer.Object,
        //     TweetCollector.Object, _logger);
    }
}
