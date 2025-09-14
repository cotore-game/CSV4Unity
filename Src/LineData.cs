using System.Collections.Generic;
using System;

namespace CSV4Unity
{
    // ジェネリック版
    /// <summary>
    /// 任意の Enum をキーに、各セルの値を保持する汎用的な 1 行データ
    /// </summary>
    public sealed class LineData<TEnum> where TEnum : struct, Enum
    {
        private readonly Dictionary<TEnum, object> _values = new();

        public object this[TEnum field]
        {
            get => _values.TryGetValue(field, out var val) ? val : null;
            set => _values[field] = value;
        }

        public T Get<T>(TEnum field)
        {
            if (_values.TryGetValue(field, out var val))
            {
                if (val is T t) return t;
                throw new InvalidCastException($"Field {field} is not of type {typeof(T)}");
            }
            throw new KeyNotFoundException($"Field {field} not found");
        }
    }

    // 非ジェネリック
    /// <summary>
    /// 行列インデックスを使用して各セルの値を保持する汎用的な 1 行データ
    /// </summary>
    public sealed class LineData
    {
        private readonly Dictionary<string, object> _values = new(StringComparer.OrdinalIgnoreCase);

        public object this[string field]
        {
            get => _values.TryGetValue(field, out var val) ? val : null;
            set => _values[field] = value;
        }

        public T Get<T>(string field)
        {
            if (_values.TryGetValue(field, out var val))
            {
                if (val is T t) return t;
                throw new InvalidCastException($"Field '{field}' is not of type {typeof(T)}");
            }
            throw new KeyNotFoundException($"Field '{field}' not found");
        }

        public object this[int index]
        {
            get => _values.TryGetValue(GetHeaderByIndex(index), out var val) ? val : null;
            set => _values[GetHeaderByIndex(index)] = value;
        }

        // 内部でヘッダーを管理するリストを持つ
        private readonly List<string> _headers = new();
        internal void Add(string header, object value)
        {
            if (!_values.ContainsKey(header))
            {
                _headers.Add(header);
            }
            _values[header] = value;
        }

        private string GetHeaderByIndex(int index)
        {
            if (index < 0 || index >= _headers.Count)
            {
                throw new IndexOutOfRangeException($"Header index {index} is out of range.");
            }
            return _headers[index];
        }
    }
}
