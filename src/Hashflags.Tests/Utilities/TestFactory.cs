using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hashflags.Tests.Utilities;

public static class TestFactory
{
    public static ILogger CreateLogger()
    {
        return NullLoggerFactory.Instance.CreateLogger("Null Logger");
    }
}
