namespace Scover.Psdc.Library;

/// <summary>
/// Aggregates a read-only map and an equivalent list usable for sequential access
/// </summary>
/// <typeparam name="TKey"/>The map key type</typeparam>
/// <typeparam name="TValue"/>The map value type</typeparam>
sealed class OrderedMap<TKey, TValue>
where TKey : notnull
{
    public IReadOnlyDictionary<TKey, TValue> Map { get; }
    public IReadOnlyList<KeyValuePair<TKey, TValue>> List { get; }

    public OrderedMap(IReadOnlyDictionary<TKey, TValue> map,
    IReadOnlyList<KeyValuePair<TKey, TValue>> list)
    {
        if (!list.All(map.Contains) || !map.All(list.Contains)) {
            throw new ArgumentException("The provided map and list must contain equal items");
        }
        (Map, List) = (map, list);
    }

    // Map.Count and List.Count would be equivalent
    public int Count => Map.Count;
}
