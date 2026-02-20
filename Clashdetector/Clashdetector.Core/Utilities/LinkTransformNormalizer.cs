using System.Numerics;

namespace Clashdetector.Core.Utilities;

public static class LinkTransformNormalizer
{
    public static Matrix4x4 Normalize(Matrix4x4 matrix, float epsilon = 1e-5f)
    {
        var values = new[]
        {
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44,
        };

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = NormalizeValue(values[i], epsilon);
        }

        return new Matrix4x4(
            values[0], values[1], values[2], values[3],
            values[4], values[5], values[6], values[7],
            values[8], values[9], values[10], values[11],
            values[12], values[13], values[14], values[15]);
    }

    public static Vector3 ToHostCoordinates(Vector3 modelPoint, Matrix4x4 modelToHostTransform)
    {
        var normalized = Normalize(modelToHostTransform);
        return Vector3.Transform(modelPoint, normalized);
    }

    private static float NormalizeValue(float value, float epsilon)
    {
        if (MathF.Abs(value) < epsilon)
        {
            return 0f;
        }

        if (MathF.Abs(value - 1f) < epsilon)
        {
            return 1f;
        }

        if (MathF.Abs(value + 1f) < epsilon)
        {
            return -1f;
        }

        return value;
    }
}
