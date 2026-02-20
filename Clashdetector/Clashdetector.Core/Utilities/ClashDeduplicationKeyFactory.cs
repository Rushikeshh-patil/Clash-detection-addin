namespace Clashdetector.Core.Utilities;

public static class ClashDeduplicationKeyFactory
{
    public static string Create(
        Guid configId,
        string modelAStableKey,
        int elementAId,
        string modelBStableKey,
        int elementBId)
    {
        var left = $"{modelAStableKey}:{elementAId}";
        var right = $"{modelBStableKey}:{elementBId}";

        if (string.Compare(left, right, StringComparison.Ordinal) > 0)
        {
            (left, right) = (right, left);
        }

        return $"{configId:N}|{left}|{right}";
    }
}
