using System.Collections.Immutable;

namespace Scover.Psdc.Library;

/// <summary>
/// Aggregates an immutable dictionary and an equivalent list, allowing for both key-based and sequential access to the data.
/// </summary>
/// <typeparam name="TKey">The map key type</typeparam>
/// <typeparam name="TValue">The map value type</typeparam>
readonly struct ImmutableOrderedMap<TKey, TValue>
where TKey : notnull
{
    private readonly IImmutableDictionary<TKey, TValue> _map;
    private readonly IImmutableList<KeyValuePair<TKey, TValue>> _list;
    public IReadOnlyDictionary<TKey, TValue> Map => _map;
    public IReadOnlyList<KeyValuePair<TKey, TValue>> List => _list;

    public ImmutableOrderedMap(IImmutableList<KeyValuePair<TKey, TValue>> list) : this(ImmutableDictionary.CreateRange(list), list) { }
    ImmutableOrderedMap(IImmutableDictionary<TKey, TValue> map, IImmutableList<KeyValuePair<TKey, TValue>> list) => (_map, _list) = (map, list);

    public static ImmutableOrderedMap<TKey, TValue> Empty { get; } = new(ImmutableDictionary<TKey, TValue>.Empty, ImmutableList<KeyValuePair<TKey, TValue>>.Empty);

    // _map.Count and _list.Count would be equivalent
    public int Count => _map.Count;

    public bool TryAdd(TKey key, TValue value, out ImmutableOrderedMap<TKey, TValue> map)
    {
        if (_map.ContainsKey(key)) {
            map = this;
            return false;
        }
        map = Add(key, value);
        return true;
    }

    public ImmutableOrderedMap<TKey, TValue> Add(TKey key, TValue value) => new(
        _map.Add(key, value),
        _list.Add(new(key, value))
    );
    public ImmutableOrderedMap<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs) => new(
        _map.AddRange(pairs),
        _list.AddRange(pairs)
    );
    public ImmutableOrderedMap<TKey, TValue> Clear() => new(
        _map.Clear(),
        _list.Clear()
    );
    public ImmutableOrderedMap<TKey, TValue> Remove(TKey key) => new(
        _map.Remove(key),
        _list.RemoveAt(IndexOfKey(_list, key))
    );
    public ImmutableOrderedMap<TKey, TValue> RemoveRange(IEnumerable<TKey> keys)
    {
        var newList = _list;
        foreach (var key in keys) {
            newList = newList.RemoveAt(IndexOfKey(_list, key));
        }
        return new(_map.RemoveRange(keys), newList);
    }

    public ImmutableOrderedMap<TKey, TValue> SetItem(TKey key, TValue value) => new(
        _map.SetItem(key, value),
        _list.SetItem(IndexOfKey(_list, key), new(key, value))
    );

    public ImmutableOrderedMap<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        var newList = _list;
        foreach (var item in items) {
            newList = newList.SetItem(IndexOfKey(_list, item.Key), item);
        }
        return new(_map.SetItems(items), _list);
    }

    static int IndexOfKey(IImmutableList<KeyValuePair<TKey, TValue>> list, TKey key)
     => list.IndexOfFirst(kvp => EqualityComparer<TKey>.Default.Equals(kvp.Key, key)).Unwrap();
}
