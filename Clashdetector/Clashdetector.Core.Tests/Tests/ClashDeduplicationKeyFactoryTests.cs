using Clashdetector.Core.Utilities;

namespace Clashdetector.Core.Tests.Tests;

public sealed class ClashDeduplicationKeyFactoryTests
{
    [Fact]
    public void Create_NormalizesElementAndModelOrder()
    {
        var configId = Guid.NewGuid();
        var key1 = ClashDeduplicationKeyFactory.Create(configId, "host", 100, "link:5", 44);
        var key2 = ClashDeduplicationKeyFactory.Create(configId, "link:5", 44, "host", 100);

        Assert.Equal(key1, key2);
    }
}
