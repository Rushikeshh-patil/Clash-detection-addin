using System.Numerics;
using Clashdetector.Core.Utilities;

namespace Clashdetector.Core.Tests.Tests;

public sealed class LinkTransformNormalizerTests
{
    [Fact]
    public void ToHostCoordinates_AppliesNormalizedTransform()
    {
        var transform = new Matrix4x4(
            0.99999994f, 0f, 0f, 0f,
            0f, 1f, 0f, 0f,
            0f, 0f, 1f, 0f,
            10f, 20f, 30f, 1f);

        var point = new Vector3(2f, 3f, 4f);
        var mapped = LinkTransformNormalizer.ToHostCoordinates(point, transform);

        Assert.Equal(12f, mapped.X, 5);
        Assert.Equal(23f, mapped.Y, 5);
        Assert.Equal(34f, mapped.Z, 5);
    }
}
