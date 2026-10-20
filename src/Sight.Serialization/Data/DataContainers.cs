using System;
using System.Collections;
using System.Collections.Generic;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Data;

/// <summary>
/// List value; a list of <c>u8</c> created from bytes stores them as a byte array until it is modified
/// </summary>
public sealed class DataList : DataValue, IList<DataValue>
{
    private List<DataValue>? _items;
    private byte[]? _bytes;

    /// <summary>
    /// Initialize a new instance of <see cref="DataList"/>
    /// </summary>
    /// <param name="elementType">Element type, required only when the list is stored in an <c>any</c> position (defaults to <c>any</c>)</param>
    public DataList(TypeReference? elementType = null)
    {
        ElementType = elementType;
        _items = new List<DataValue>();
    }

    /// <summary>
    /// Initialize a new instance of <see cref="DataList"/> with items
    /// </summary>
    public DataList(IEnumerable<DataValue> items, TypeReference? elementType = null)
    {
        ElementType = elementType;
        _items = new List<DataValue>(items);
    }

    /// <summary>
    /// Initialize a new instance of <see cref="DataList"/> holding bytes (<c>list&lt;u8&gt;</c>), without copying them
    /// </summary>
    /// <param name="bytes">Content of the list, owned by the list from now on</param>
    /// <param name="elementType">Element type: <c>u8</c> (default), possibly through an alias</param>
    public DataList(byte[] bytes, TypeReference? elementType = null)
    {
        elementType ??= TypeReference.U8;
        if (elementType.Kind != TypeKind.U8 || elementType.IsNullable)
            throw new ArgumentException($"A list created from bytes must have u8 elements, not {elementType}", nameof(elementType));

        ElementType = elementType;
        _bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
    }

    /// <summary>
    /// Element type, if known
    /// </summary>
    public TypeReference? ElementType { get; set; }

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.List;

    /// <inheritdoc cref="DataValue.Count" />
    public override int Count => _bytes?.Length ?? _items!.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <summary>
    /// Bytes stored without conversion to values (lists read or created from bytes), otherwise <c>null</c>
    /// </summary>
    internal byte[]? RawBytes => _bytes;

    /// <inheritdoc cref="DataValue.this[int]" />
    public override DataValue this[int index]
    {
        get => _bytes != null ? DataScalar.FromUInt64(TypeKind.U8, _bytes[index]) : _items![index];
        set => Items[index] = value ?? Null;
    }

    private List<DataValue> Items
    {
        get
        {
            if (_bytes != null)
            {
                _items = new List<DataValue>(_bytes.Length);
                foreach (var item in _bytes)
                    _items.Add(DataScalar.FromUInt64(TypeKind.U8, item));

                _bytes = null;
            }

            return _items!;
        }
    }

    /// <inheritdoc />
    public override ReadOnlyMemory<byte> AsBytes()
    {
        if (_bytes != null)
            return _bytes;

        var bytes = new byte[_items!.Count];
        for (var i = 0; i < bytes.Length; i++)
        {
            var item = _items[i];
            if (item.Kind is not TypeKind.U8 and not TypeKind.U16 and not TypeKind.U32 and not TypeKind.U64 and not TypeKind.S8 and not TypeKind.S16 and not TypeKind.S32 and not TypeKind.S64)
                throw new InvalidOperationException($"Item {i} of the list is not an integer ({item.Kind})");

            var value = item.AsInt64();
            if (value is < 0 or > byte.MaxValue)
                throw new InvalidOperationException($"Item {i} of the list is out of range for u8 ({value})");

            bytes[i] = (byte)value;
        }

        return bytes;
    }

    /// <inheritdoc />
    public void Add(DataValue item) => Items.Add(item ?? Null);

    /// <inheritdoc />
    public void Clear() => Items.Clear();

    /// <inheritdoc />
    public bool Contains(DataValue item) => IndexOf(item) >= 0;

    /// <inheritdoc />
    public void CopyTo(DataValue[] array, int arrayIndex)
    {
        for (var i = 0; i < Count; i++)
            array[arrayIndex + i] = this[i];
    }

    /// <inheritdoc />
    public int IndexOf(DataValue item) => _bytes != null ? IndexOfByte(item) : _items!.IndexOf(item);

    /// <inheritdoc />
    public void Insert(int index, DataValue item) => Items.Insert(index, item ?? Null);

    /// <inheritdoc />
    public bool Remove(DataValue item) => Items.Remove(item);

    /// <inheritdoc />
    public void RemoveAt(int index) => Items.RemoveAt(index);

    /// <inheritdoc />
    public IEnumerator<DataValue> GetEnumerator()
    {
        if (_items != null)
            return _items.GetEnumerator();

        return EnumerateBytes(_bytes!);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static IEnumerator<DataValue> EnumerateBytes(byte[] bytes)
    {
        foreach (var item in bytes)
            yield return DataScalar.FromUInt64(TypeKind.U8, item);
    }

    private int IndexOfByte(DataValue item)
    {
        for (var i = 0; i < _bytes!.Length; i++)
        {
            if (this[i].Equals(item))
                return i;
        }

        return -1;
    }
}

/// <summary>
/// Map value: ordered pairs with unique keys
/// </summary>
public sealed class DataMap : DataValue, IEnumerable<KeyValuePair<DataValue, DataValue>>
{
    private readonly List<KeyValuePair<DataValue, DataValue>> _entries = new();
    private readonly Dictionary<DataValue, int> _indexes = new();

    /// <summary>
    /// Initialize a new instance of <see cref="DataMap"/>
    /// </summary>
    /// <param name="keyType">Key type, required only when the map is stored in an <c>any</c> position (defaults to <c>string</c>)</param>
    /// <param name="valueType">Value type, required only when the map is stored in an <c>any</c> position (defaults to <c>any</c>)</param>
    public DataMap(TypeReference? keyType = null, TypeReference? valueType = null)
    {
        KeyType = keyType;
        ValueType = valueType;
    }

    /// <summary>
    /// Key type, if known
    /// </summary>
    public TypeReference? KeyType { get; set; }

    /// <summary>
    /// Value type, if known
    /// </summary>
    public TypeReference? ValueType { get; set; }

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Map;

    /// <inheritdoc />
    public override int Count => _entries.Count;

    /// <summary>
    /// Keys, in order
    /// </summary>
    public IEnumerable<DataValue> Keys
    {
        get
        {
            foreach (var entry in _entries)
                yield return entry.Key;
        }
    }

    /// <summary>
    /// Value of a key
    /// </summary>
    public DataValue this[DataValue key]
    {
        get => _indexes.TryGetValue(key, out var index) ? _entries[index].Value : throw new KeyNotFoundException($"Key '{key}' not found");
        set => Set(key, value);
    }

    /// <inheritdoc />
    public override DataValue this[string key]
    {
        get => this[(DataValue)key];
        set => Set(key, value);
    }

    /// <summary>
    /// Add a new entry
    /// </summary>
    /// <exception cref="ArgumentException">The key already exists</exception>
    public void Add(DataValue key, DataValue value)
    {
        CheckKey(key);
        if (_indexes.ContainsKey(key))
            throw new ArgumentException($"Duplicate map key '{key}'", nameof(key));

        _indexes.Add(key, _entries.Count);
        _entries.Add(new KeyValuePair<DataValue, DataValue>(key, value ?? Null));
    }

    /// <summary>
    /// Add or replace an entry
    /// </summary>
    public void Set(DataValue key, DataValue value)
    {
        CheckKey(key);
        if (_indexes.TryGetValue(key, out var index))
            _entries[index] = new KeyValuePair<DataValue, DataValue>(key, value ?? Null);
        else
            Add(key, value);
    }

    /// <summary>
    /// Try to get the value of a key
    /// </summary>
    public bool TryGetValue(DataValue key, out DataValue value)
    {
        if (_indexes.TryGetValue(key, out var index))
        {
            value = _entries[index].Value;
            return true;
        }

        value = Null;
        return false;
    }

    /// <summary>
    /// Indicates whether the map contains a key
    /// </summary>
    public bool ContainsKey(DataValue key) => _indexes.ContainsKey(key);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<DataValue, DataValue>> GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static void CheckKey(DataValue key)
    {
        if (key is not DataScalar and not DataEnum)
            throw new ArgumentException("Map keys must be booleans, integers, strings or enum values", nameof(key));
    }
}

/// <summary>
/// Struct value
/// </summary>
public sealed class DataStruct : DataValue, IEnumerable<KeyValuePair<string, DataValue>>
{
    private readonly DataValue?[] _values;

    /// <summary>
    /// Initialize a new instance of <see cref="DataStruct"/>
    /// </summary>
    public DataStruct(StructDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        if (definition.IsAbstract)
            throw new ArgumentException($"Cannot instantiate abstract struct '{definition.QualifiedName}'", nameof(definition));

        _values = new DataValue?[definition.AllFields.Count];
    }

    /// <summary>
    /// Struct definition (concrete type)
    /// </summary>
    public StructDefinition Definition { get; }

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Struct;

    /// <inheritdoc />
    public override string? TypeId => Definition.QualifiedName;

    /// <summary>
    /// Number of fields of the definition
    /// </summary>
    public override int Count => _values.Length;

    /// <summary>
    /// Value of a field; <see cref="DataValue.Null"/> if not set
    /// </summary>
    public override DataValue this[string key]
    {
        get => _values[IndexOf(key)] ?? Null;
        set => _values[IndexOf(key)] = value ?? Null;
    }

    /// <summary>
    /// Indicates whether a field has been set
    /// </summary>
    public bool IsSet(string field) => _values[IndexOf(field)] != null;

    /// <summary>
    /// Clear the value of a field
    /// </summary>
    public void Unset(string field) => _values[IndexOf(field)] = null;

    /// <summary>
    /// Fields that are set, in field sequence order
    /// </summary>
    public IEnumerator<KeyValuePair<string, DataValue>> GetEnumerator()
    {
        var fields = Definition.AllFields;
        for (var i = 0; i < _values.Length; i++)
        {
            if (_values[i] != null)
                yield return new KeyValuePair<string, DataValue>(fields[i].Name, _values[i]!);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal DataValue? GetAt(int index) => _values[index];

    internal void SetAt(int index, DataValue? value) => _values[index] = value;

    private int IndexOf(string field)
    {
        var fields = Definition.AllFields;
        for (var i = 0; i < fields.Count; i++)
        {
            if (string.Equals(fields[i].Name, field, StringComparison.Ordinal))
                return i;
        }

        throw new KeyNotFoundException($"Struct '{Definition.QualifiedName}' has no field '{field}'");
    }
}
