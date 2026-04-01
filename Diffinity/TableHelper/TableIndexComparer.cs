namespace Diffinity.TableHelper;

public static class TableIndexComparer
{
    public static bool AreEqual(List<IndexDto>? sourceIndexes, List<IndexDto>? targetIndexes)
    {
        sourceIndexes ??= new List<IndexDto>();
        targetIndexes ??= new List<IndexDto>();

        if (sourceIndexes.Count != targetIndexes.Count)
        {
            return false;
        }

        var sourceMap = sourceIndexes.ToDictionary(i => i.IndexName, StringComparer.OrdinalIgnoreCase);
        var targetMap = targetIndexes.ToDictionary(i => i.IndexName, StringComparer.OrdinalIgnoreCase);

        if (sourceMap.Count != targetMap.Count)
        {
            return false;
        }

        foreach (var sourceIndex in sourceMap)
        {
            if (!targetMap.TryGetValue(sourceIndex.Key, out var targetIndex))
            {
                return false;
            }

            if (!AreDefinitionsEqual(sourceIndex.Value, targetIndex))
            {
                return false;
            }
        }

        return true;
    }

    public static List<string> GetDifferenceMarkers(List<IndexDto>? sourceIndexes, List<IndexDto>? targetIndexes)
    {
        sourceIndexes ??= new List<IndexDto>();
        targetIndexes ??= new List<IndexDto>();

        var markers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sourceMap = sourceIndexes.ToDictionary(i => i.IndexName, StringComparer.OrdinalIgnoreCase);
        var targetMap = targetIndexes.ToDictionary(i => i.IndexName, StringComparer.OrdinalIgnoreCase);

        foreach (var sourceIndex in sourceMap)
        {
            if (!targetMap.TryGetValue(sourceIndex.Key, out var targetIndex) || !AreDefinitionsEqual(sourceIndex.Value, targetIndex))
            {
                markers.Add(ToMarker(sourceIndex.Key));
            }
        }

        foreach (var targetIndex in targetMap)
        {
            if (!sourceMap.TryGetValue(targetIndex.Key, out var sourceIndex) || !AreDefinitionsEqual(targetIndex.Value, sourceIndex))
            {
                markers.Add(ToMarker(targetIndex.Key));
            }
        }

        return markers.ToList();
    }

    public static bool AreDefinitionsEqual(IndexDto? left, IndexDto? right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }

        return string.Equals(left.IndexType, right.IndexType, StringComparison.OrdinalIgnoreCase)
            && left.IsUnique == right.IsUnique
            && left.IsUniqueConstraint == right.IsUniqueConstraint
            && string.Equals(Normalize(left.FilterDefinition), Normalize(right.FilterDefinition), StringComparison.OrdinalIgnoreCase)
            && string.Equals(Normalize(left.KeyColumns), Normalize(right.KeyColumns), StringComparison.OrdinalIgnoreCase)
            && string.Equals(Normalize(left.IncludedColumns), Normalize(right.IncludedColumns), StringComparison.OrdinalIgnoreCase);
    }

    public static string ToMarker(string indexName) => $"INDEX:{indexName}";

    private static string Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
