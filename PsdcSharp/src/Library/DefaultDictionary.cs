using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Scover.Psdc.Library;

public class DefaultDictionary<TKey, TValue>(TValue defaultValue) : IDictionary<TKey, TValue> where TKey : notnull
{
    readonly Dictionary<TKey, TValue> _dic = [];

    public TValue DefaultValue { get; set; } = defaultValue;

    public ICollection<TKey> Keys => _dic.Keys;

    public ICollection<TValue> Values => _dic.Values;

    public int Count => _dic.Count;

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)_dic).IsReadOnly;

    public TValue this[TKey key] {
        get => TryGetValue(key, out var t) ? t : DefaultValue;
        set => _dic[key] = value; }

    public void Add(TKey key, TValue value) => _dic.Add(key, value);
    public bool ContainsKey(TKey key) => _dic.ContainsKey(key);
    public bool Remove(TKey key) => _dic.Remove(key);
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _dic.TryGetValue(key, out value);
    public void Clear() => _dic.Clear();
    public bool Contains(KeyValuePair<TKey, TValue> item) => _dic.Contains(item);
    
    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_dic).Add(item);
    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)_dic).CopyTo(array, arrayIndex);
    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_dic).Remove(item);
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dic.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_dic).GetEnumerator();
}
