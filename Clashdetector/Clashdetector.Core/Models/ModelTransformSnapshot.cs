using System.Numerics;

namespace Clashdetector.Core.Models;

public sealed class ModelTransformSnapshot
{
    public string ModelStableKey { get; set; } = string.Empty;

    public Matrix4x4 ModelToHost { get; set; } = Matrix4x4.Identity;
}
