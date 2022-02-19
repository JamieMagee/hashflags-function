using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Hashflags.Tests.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Hashflags.Tests;

public class ActiveHashflagsTests
{
    private readonly ILogger _logger = TestFactory.CreateLogger();

    private readonly Mock<BlockBlobClient> _mockBlockBlobClient = new();

    [Fact]
    public async Task ActiveHashflags_ReturnsContent()
    {
        _mockBlockBlobClient.Setup(x => x.UploadAsync(It.IsAny<Stream>(),
                It.IsAny<BlobHttpHeaders>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<AccessTier>(),
                It.IsAny<IProgress<long>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<Response<BlobContentInfo>>().Object);

        await ActiveHashflags.Run(null, _mockBlockBlobClient.Object, _logger);

        _mockBlockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(),
                It.IsAny<BlobHttpHeaders>(),
                default,
                default,
                default,
                default,
                default)
            , Times.Once);
    }
}
