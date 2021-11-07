using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace TinyToml.Types
{
    public class TomlDoc
    {
        private readonly TomlTable _root;

        public TomlDoc(TomlTable root)
        {
            _root = root;
        }

        public IEnumerable<string> Keys => _root.Keys;

        public ITomlValue this[string key]
        {
            get => _root[key];
        }

        public int Count => _root.Count;
    }

    public enum TomlType
    {
        None,
        Integer,
        Float,
        DateTime,
        OffsetDate,
        OffSetDateTime,
        Array,
        Table,
        Boolean,
        String
    }

    internal interface ITomlValueContainer
    {
        void AddValue<T>(T value) where T: ITomlValue;
    }

    public interface ITomlValue
    {
        string   Key      { get; }
        TomlType TomlType { get; }

        bool TryReadTomlInteger(out long value)
        {
            value = default;
            return false;
        }

        bool TryReadTomlFloat(out double value)
        {
            value = default;
            return false;
        }

        bool TryReadTomlDateTimeOffset(out DateTimeOffset value)
        {
            value = default;
            return false;
        }

        bool TryReadTomlString(out string value)
        {
            value = string.Empty;
            return false;
        }

        bool TryReadTomlBool(out bool value)
        {
            value = false;
            return false;
        }

        bool TryReadTomlArray(out TomlArray value)
        {
            value = new TomlArray(string.Empty);
            return false;
        }

        bool TryReadTomlTable(out TomlTable value)
        {
            value = new TomlTable(string.Empty);
            return false;
        }

        public ITomlValue this[string key]
        {
            //TODO: better object model, for consummation
            get => throw new NotImplementedException();
        }
    }

    public readonly struct Value : ITomlValue
    {
        public static readonly Value    None = new Value("", TomlType.None);
        public                 string   Key      { get; }
        public                 TomlType TomlType { get; }

        private Value(string key, TomlType tomlType)
        {
            Key      = key;
            TomlType = tomlType;
        }
    }

    [DebuggerDisplay("Name = {Key}, Count = {Count}")]
    public class TomlArray : ITomlValue, IReadOnlyList<ITomlValue>, ITomlValueContainer
    {
        //internal static readonly TomlArray Empty = new TomlArray(string.Empty, 0);

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private List<ITomlValue> Values { get; }

        public   string     Key            { get; }
        public   TomlType   TomlType       { get; }
        public   int        Count          => Values.Count;
        internal ITomlValue CurrentElement { get; private set; }

        public TomlArray(string key, int initialCapacity = 16)
        {
            Key            = key;
            TomlType       = TomlType.Array;
            Values         = new List<ITomlValue>(initialCapacity);
            CurrentElement = Value.None;
        }

        public void AddValue<T>(T value) where T: ITomlValue
        {
            Values.Add(value);
            CurrentElement = value;
        }

        public bool TryReadTomlArray(out TomlArray value)
        {
            value = this;
            return true;
        }

        public ITomlValue this[int index] => Values[index];

        public IEnumerator<ITomlValue> GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    [DebuggerDisplay("Name = {Key}, Count = {Count}")]
    public class TomlTable : ITomlValue, IReadOnlyDictionary<string, ITomlValue>, ITomlValueContainer
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private Dictionary<string, ITomlValue> Values { get; }
        public string   Key      { get; }
        public TomlType TomlType { get; }

        public TomlTable(string tableName)
        {
            Key      = tableName;
            TomlType = TomlType.Table;
            Values   = new Dictionary<string, ITomlValue>(17);
        }

        public ITomlValue this[string key]
        {
            get => Values[key];
        }

        public void AddValue<T>(T value) where T: ITomlValue
        {
            Values.Add(value.Key, value);
        }

        public bool TryReadTomlTable(out TomlTable value)
        {
            value = this;
            return true;
        }

        public bool ContainsKey(string key)
        {
            return Values.ContainsKey(key);
        }

        public bool TryGetValue(string key, out ITomlValue value)
        {
            var hasValue = Values.TryGetValue(key, out var nullableValue);
            value = nullableValue ?? Value.None;
            return hasValue;
        }

        public bool TryGetValue<TTomlType>(string key, out TTomlType value) where TTomlType: ITomlValue
        {
            if (Values.TryGetValue(key, out var tmpValue) && tmpValue is TTomlType tmpOutValue)
            {
                value = tmpOutValue;
                return true;
            }

            value = default!;
            return false;
        }

        public IEnumerable<string>                                      Keys   => Values.Keys;
        IEnumerable<ITomlValue> IReadOnlyDictionary<string, ITomlValue>.Values => Values.Values;

        public IEnumerator<KeyValuePair<string, ITomlValue>> GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => Values.Count;
    }

    [DebuggerDisplay("{Key} = {Value}")]
    public readonly struct TomlInteger : ITomlValue, IEquatable<long>
    {
        public string   Key      { get; }
        public TomlType TomlType { get; }
        public long     Value    { get; }

        public TomlInteger(string key, long value)
        {
            TomlType = TomlType.Integer;
            Key      = key;
            Value    = value;
        }

        public bool TryReadTomlInteger(out long value)
        {
            value = Value;
            return true;
        }

        public static implicit operator long(TomlInteger value) => value.Value;

        public bool Equals(long other)
        {
            return other == Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    [DebuggerDisplay("{Key} = {Value}")]
    public readonly struct TomlFloat : ITomlValue, IEquatable<double>
    {
        public string   Key      { get; }
        public TomlType TomlType { get; }
        public double   Value    { get; }

        public TomlFloat(string key, double value)
        {
            TomlType = TomlType.Float;
            Key      = key;
            Value    = value;
        }

        public bool TryReadTomlFloat(out double value)
        {
            value = Value;
            return true;
        }

        public static implicit operator double(TomlFloat value) => value.Value;

        public bool Equals(double other)
        {
            return other.Equals(Value);
        }

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }
    }

    [DebuggerDisplay("{Key} = {Value}")]
    public readonly struct TomlBoolean : ITomlValue, IEquatable<bool>
    {
        public string   Key      { get; }
        public TomlType TomlType { get; }
        public bool     Value    { get; }

        public TomlBoolean(string key, bool value)
        {
            TomlType = TomlType.Boolean;
            Key      = key;
            Value    = value;
        }

        public bool TryReadTomlBool(out bool value)
        {
            value = Value;
            return true;
        }

        public bool Equals(bool other)
        {
            return Value == other;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    [DebuggerDisplay("{Key} = {Value}")]
    public readonly struct TomlString : ITomlValue, IEquatable<string>
    {
        public string   Key      { get; }
        public TomlType TomlType { get; }
        public string   Value    { get; }

        public TomlString(string key, string value)
        {
            TomlType = TomlType.String;
            Key      = key;
            Value    = value;
        }

        public bool TryReadTomlString(out string value)
        {
            value = Value;
            return true;
        }

        public bool Equals(string? other)
        {
            return string.Equals(Value, other, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return Value;
        }
    }

    [DebuggerDisplay("{Key} = {Value}")]
    public readonly struct TomlDateTimeOffset : ITomlValue, IEquatable<DateTimeOffset>
    {
        public string   Key      { get; }
        public TomlType TomlType { get; }
        public DateTimeOffset Value    { get; }

        public TomlDateTimeOffset(string key, DateTimeOffset value)
        {
            TomlType = TomlType.DateTime;
            Key      = key;
            Value    = value;
        }

        public bool TryReadTomlDateTimeOffset(out DateTimeOffset value)
        {
            value = Value;
            return true;
        }

        public bool Equals(DateTimeOffset other)
        {
            return Value == other;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}