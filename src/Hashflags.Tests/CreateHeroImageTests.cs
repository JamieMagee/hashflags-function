using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Hashflags.Tests.Utilities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using Xunit;

namespace Hashflags.Tests
{
    public class CreateHeroImageTests
    {
        private static readonly Mock<CloudBlobContainer> mockHashflagsontainer =
            new Mock<CloudBlobContainer>(new Uri("http://tempuri.org/container"));

        private static readonly Mock<CloudBlobContainer> mockHeroContainer =
            new Mock<CloudBlobContainer>(new Uri("http://tempuri.org/container"));

        private static readonly Mock<ICollector<KeyValuePair<string, string>>> tweetCollector =
            new Mock<ICollector<KeyValuePair<string, string>>>();

        private static readonly KeyValuePair<string, string> hashtagUrlPair =
            new KeyValuePair<string, string>("Test", "");

        private static readonly Mock<CloudBlockBlob> mockCloudBlockBlob =
            new Mock<CloudBlockBlob>(new Uri("http://tempuri.org/blob"));

        private readonly ILogger _logger = TestFactory.CreateLogger();

        [Fact]
        public void CreateHeroImage_ReturnsContent()
        {
            mockHashflagsontainer
                .Setup(x => x.GetBlockBlobReference(It.IsAny<string>()))
                .Returns(mockCloudBlockBlob.Object);
            mockCloudBlockBlob.Setup(x => x.DownloadToStreamAsync(It.IsAny<Stream>()))
                .Returns<MemoryStream>(stream =>
                {
                    var file = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "doritos.png");
                    File.OpenRead(file).CopyTo(stream);
                    return Task.CompletedTask;
                });
            mockHeroContainer.Setup(x => x.GetBlockBlobReference(It.IsAny<string>()))
                .Returns(mockCloudBlockBlob.Object);
            mockCloudBlockBlob.Setup(x => x.UploadFromStreamAsync(It.IsAny<Stream>()))
                .Returns<MemoryStream>(stream =>
                {
                    var directory = Path.Combine(Directory.GetCurrentDirectory(), "tmp");
                    Directory.CreateDirectory(directory);
                    var file = Path.Combine(directory, $"{DateTime.Now.ToString("s")}.png");
                    var fileStream = File.Create(file);
                    stream.WriteTo(fileStream);
                    fileStream.Close();
                    return Task.CompletedTask;
                });
            CreateHeroImage.Run(hashtagUrlPair, mockHeroContainer.Object, mockHashflagsontainer.Object,
                tweetCollector.Object, _logger);
        }
    }
}