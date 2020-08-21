using System;
using System.Threading.Tasks;
using FluentAssertions;
using Hashflags.Tests.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using Xunit;

namespace Hashflags.Tests
{
    public class ActiveHashflagsTests
    {
        private readonly ILogger _logger = TestFactory.CreateLogger();

        private readonly Mock<CloudBlockBlob> _mockCloudBlob =
            new Mock<CloudBlockBlob>(new Uri("http://tempuri.org/blob"));

        [Fact]
        public void ActiveHashflags_ReturnsContent()
        {
            var content = "";
            _mockCloudBlob.Setup(x => x.UploadTextAsync(It.IsAny<string>()))
                .Callback<string>(x => content = x)
                .Returns(Task.CompletedTask);

            ActiveHashflags.Run(null, _mockCloudBlob.Object, _logger);

            _mockCloudBlob.Verify(x => x.UploadTextAsync(It.IsAny<string>()), Times.Once);
            content.Should().NotBeNullOrWhiteSpace();
        }
    }
}