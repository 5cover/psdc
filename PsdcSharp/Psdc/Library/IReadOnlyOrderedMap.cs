namespace Scover.Psdc.Library;

/// <summary>
/// Aggregates a read-only map and an equivalent list usable for sequential access
/// </summary>
/// <typeparam name="TKey">The map key type</typeparam>
/// <typeparam name="TValue">The map value type</typeparam>
sealed record ReadOnlyOrderedMap<TKey, TValue>(
    IReadOnlyDictionary<TKey, TValue> Map,
    IReadOnlyList<(TKey Key, TValue Value)> List
)
where TKey : notnull;
