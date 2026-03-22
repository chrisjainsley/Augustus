namespace Augustus.AI;

/// <summary>
/// Merges global simulator dynamic field names with per-route names for stable cache keys.
/// </summary>
internal static class DynamicContentFieldsMerger
{
    public static IReadOnlyList<string> Merge(IEnumerable<string>? globalSimulatorFields, IReadOnlyList<string>? routeFields)
    {
        if (routeFields is not { Count: > 0 })
            return AsReadOnlyList(globalSimulatorFields);

        if (globalSimulatorFields is null)
            return routeFields;

        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var g in globalSimulatorFields)
        {
            if (!string.IsNullOrWhiteSpace(g))
                set.Add(g);
        }

        foreach (var r in routeFields)
        {
            if (!string.IsNullOrWhiteSpace(r))
                set.Add(r);
        }

        return set.Count == 0 ? Array.Empty<string>() : set.ToArray();
    }

    private static IReadOnlyList<string> AsReadOnlyList(IEnumerable<string>? globalSimulatorFields)
    {
        if (globalSimulatorFields is null)
            return Array.Empty<string>();
        if (globalSimulatorFields is IReadOnlyList<string> list)
            return list;
        return globalSimulatorFields.ToArray();
    }
}
