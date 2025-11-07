using System.Collections.Generic;
using System;

namespace CSV4Unity
{
    // ジェネリック版
    /// <summary>
    /// 任意の Enum をキーに、各セルの値を保持する汎用的な 1 行データ
    /// </summary>
    /// <typeparam name="TEnum">列を識別するEnum型</typeparam>
    public sealed class LineData<TEnum> where TEnum : struct, Enum
    {
        private readonly Dictionary<TEnum, object> _values = new();

        /// <summary>
        /// Enumをキーとして値を取得または設定します
        /// </summary>
        /// <param name="field">列を識別するEnum値</param>
        /// <returns>セルの値。存在しない場合はnull</returns>
        public object this[TEnum field]
        {
            get => _values.TryGetValue(field, out var val) ? val : null;
            set => _values[field] = value;
        }

        /// <summary>
        /// 指定された型で値を取得します
        /// </summary>
        /// <typeparam name="T">取得する値の型</typeparam>
        /// <param name="field">列を識別するEnum値</param>
        /// <returns>型変換された値</returns>
        /// <exception cref="KeyNotFoundException">フィールドが存在しない場合</exception>
        /// <exception cref="InvalidCastException">型変換に失敗した場合</exception>
        public T Get<T>(TEnum field)
        {
            if (_values.TryGetValue(field, out var val))
            {
                if (val == null) return default;
                if (val is T t) return t;

                // 型変換を試みる
                try
                {
                    return (T)Convert.ChangeType(val, typeof(T));
                }
                catch
                {
                    throw new InvalidCastException($"Field {field} cannot be converted to type {typeof(T)}. Current type: {val.GetType()}");
                }
            }
            throw new KeyNotFoundException($"Field {field} not found");
        }

        /// <summary>
        /// 指定された型で値を取得します。失敗した場合はデフォルト値を返します
        /// </summary>
        /// <typeparam name="T">取得する値の型</typeparam>
        /// <param name="field">列を識別するEnum値</param>
        /// <param name="defaultValue">取得に失敗した場合のデフォルト値</param>
        /// <returns>型変換された値、または失敗時はdefaultValue</returns>
        public T GetOrDefault<T>(TEnum field, T defaultValue = default)
        {
            if (_values.TryGetValue(field, out var val))
            {
                if (val == null) return defaultValue;
                if (val is T t) return t;

                try
                {
                    return (T)Convert.ChangeType(val, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 指定された型で値を安全に取得します
        /// </summary>
        /// <typeparam name="T">取得する値の型</typeparam>
        /// <param name="field">列を識別するEnum値</param>
        /// <param name="value">取得された値（失敗時はdefault）</param>
        /// <returns>取得に成功した場合はtrue</returns>
        public bool TryGet<T>(TEnum field, out T value)
        {
            if (_values.TryGetValue(field, out var val))
            {
                if (val == null)
                {
                    value = default;
                    return true;
                }

                if (val is T t)
                {
                    value = t;
                    return true;
                }

                try
                {
                    value = (T)Convert.ChangeType(val, typeof(T));
                    return true;
                }
                catch
                {
                    value = default;
                    return false;
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 指定されたフィールドが存在するかを確認します
        /// </summary>
        /// <param name="field">確認する列のEnum値</param>
        /// <returns>フィールドが存在する場合はtrue</returns>
        public bool HasField(TEnum field)
        {
            return _values.ContainsKey(field);
        }

        /// <summary>
        /// この行に含まれる全てのフィールドを取得します
        /// </summary>
        /// <returns>フィールドのEnum値のコレクション</returns>
        public IEnumerable<TEnum> GetFields()
        {
            return _values.Keys;
        }
    }

    // 非ジェネリック
    /// <summary>
    /// 行列インデックスまたはヘッダー名を使用して各セルの値を保持する汎用的な 1 行データ
    /// </summary>
    public sealed class LineData
    {
        private readonly Dictionary<string, object> _valuesByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, object> _valuesByIndex = new();
        private readonly List<string> _headers = new();

        /// <summary>
        /// ヘッダー名をキーとして値を取得または設定します
        /// </summary>
        /// <param name="field">列のヘッダー名</param>
        /// <returns>セルの値。存在しない場合はnull</returns>
        public object this[string field]
        {
            get => _valuesByName.TryGetValue(field, out var val) ? val : null;
            set
            {
                if (!_valuesByName.ContainsKey(field))
                {
                    _headers.Add(field);
                }
                _valuesByName[field] = value;
            }
        }

        /// <summary>
        /// 列インデックスをキーとして値を取得または設定します（ヘッダーなしCSV用の高速アクセス）
        /// </summary>
        /// <param name="index">列のインデックス（0始まり）</param>
        /// <returns>セルの値</returns>
        /// <exception cref="IndexOutOfRangeException">インデックスが範囲外の場合</exception>
        public object this[int index]
        {
            get
            {
                if (_valuesByIndex.TryGetValue(index, out var val))
                {
                    return val;
                }

                // ヘッダーありの場合のフォールバック
                if (index >= 0 && index < _headers.Count)
                {
                    return _valuesByName[_headers[index]];
                }

                throw new IndexOutOfRangeException($"Column index {index} is out of range.");
            }
            set
            {
                _valuesByIndex[index] = value;

                // ヘッダーありの場合は両方に設定
                if (index >= 0 && index < _headers.Count)
                {
                    _valuesByName[_headers[index]] = value;
                }
            }
        }

        /// <summary>
        /// ヘッダー名で指定された型で値を取得します
        /// </summary>
        /// <typeparam name="T">取得する値の型</typeparam>
        /// <param name="field">列のヘッダー名</param>
        /// <returns>型変換された値</returns>
        /// <exception cref="KeyNotFoundException">フィールドが存在しない場合</exception>
        /// <exception cref="InvalidCastException">型変換に失敗した場合</exception>
        public T Get<T>(string field)
        {
            if (_valuesByName.TryGetValue(field, out var val))
            {
                if (val == null) return default;
                if (val is T t) return t;

                try
                {
                    return (T)Convert.ChangeType(val, typeof(T));
                }
                catch
                {
                    throw new InvalidCastException($"Field '{field}' cannot be converted to type {typeof(T)}. Current type: {val.GetType()}");
                }
            }
            throw new KeyNotFoundException($"Field '{field}' not found");
        }

        /// <summary>
        /// インデックスで指定された型で値を取得します（ヘッダーなしCSV用の高速アクセス）
        /// </summary>
        /// <typeparam name="T">取得する値の型</typeparam>
        /// <param name="index">列のインデックス（0始まり）</param>
        /// <returns>型変換された値</returns>
        /// <exception cref="IndexOutOfRangeException">インデックスが範囲外の場合</exception>
        /// <exception cref="InvalidCastException">型変換に失敗した場合</exception>
        public T Get<T>(int index)
        {
            object val = this[index]; // インデクサを使用

            if (val == null) return default;
            if (val is T t) return t;

            try
            {
                return (T)Convert.ChangeType(val, typeof(T));
            }
            catch
            {
                throw new InvalidCastException($"Field at index {index} cannot be converted to type {typeof(T)}. Current type: {val.GetType()}");
            }
        }

        /// <summary>
        /// ヘッダー名で指定された型で値を取得します。失敗した場合はデフォルト値を返します
        /// </summary>
        /// <typeparam name="T">取得する値の型</typeparam>
        /// <param name="field">列のヘッダー名</param>
        /// <param name="defaultValue">取得に失敗した場合のデフォルト値</param>
        /// <returns>型変換された値、または失敗時はdefaultValue</returns>
        public T GetOrDefault<T>(string field, T defaultValue = default)
        {
            if (_valuesByName.TryGetValue(field, out var val))
            {
                if (val == null) return defaultValue;
                if (val is T t) return t;

                try
                {
                    return (T)Convert.ChangeType(val, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// インデックスで指定された型で値を取得します。失敗した場合はデフォルト値を返します
        /// </summary>
        /// <typeparam name="T">取得する値の型</typeparam>
        /// <param name="index">列のインデックス（0始まり）</param>
        /// <param name="defaultValue">取得に失敗した場合のデフォルト値</param>
        /// <returns>型変換された値、または失敗時はdefaultValue</returns>
        public T GetOrDefault<T>(int index, T defaultValue = default)
        {
            try
            {
                object val = this[index];
                if (val == null) return defaultValue;
                if (val is T t) return t;

                try
                {
                    return (T)Convert.ChangeType(val, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// ヘッダー名で指定された型で値を安全に取得します
        /// </summary>
        /// <typeparam name="T">取得する値の型</typeparam>
        /// <param name="field">列のヘッダー名</param>
        /// <param name="value">取得された値（失敗時はdefault）</param>
        /// <returns>取得に成功した場合はtrue</returns>
        public bool TryGet<T>(string field, out T value)
        {
            if (_valuesByName.TryGetValue(field, out var val))
            {
                if (val == null)
                {
                    value = default;
                    return true;
                }

                if (val is T t)
                {
                    value = t;
                    return true;
                }

                try
                {
                    value = (T)Convert.ChangeType(val, typeof(T));
                    return true;
                }
                catch
                {
                    value = default;
                    return false;
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// インデックスで指定された型で値を安全に取得します
        /// </summary>
        /// <typeparam name="T">取得する値の型</typeparam>
        /// <param name="index">列のインデックス（0始まり）</param>
        /// <param name="value">取得された値（失敗時はdefault）</param>
        /// <returns>取得に成功した場合はtrue</returns>
        public bool TryGet<T>(int index, out T value)
        {
            try
            {
                object val = this[index];

                if (val == null)
                {
                    value = default;
                    return true;
                }

                if (val is T t)
                {
                    value = t;
                    return true;
                }

                try
                {
                    value = (T)Convert.ChangeType(val, typeof(T));
                    return true;
                }
                catch
                {
                    value = default;
                    return false;
                }
            }
            catch
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// 指定されたヘッダー名のフィールドが存在するかを確認します
        /// </summary>
        /// <param name="field">確認する列のヘッダー名</param>
        /// <returns>フィールドが存在する場合はtrue</returns>
        public bool HasField(string field)
        {
            return _valuesByName.ContainsKey(field);
        }

        /// <summary>
        /// 指定されたインデックスのフィールドが存在するかを確認します
        /// </summary>
        /// <param name="index">確認する列のインデックス</param>
        /// <returns>フィールドが存在する場合はtrue</returns>
        public bool HasField(int index)
        {
            return _valuesByIndex.ContainsKey(index) || (index >= 0 && index < _headers.Count);
        }

        /// <summary>
        /// この行に含まれる全てのヘッダー名を取得します
        /// </summary>
        /// <returns>ヘッダー名のコレクション</returns>
        public IEnumerable<string> GetFields()
        {
            return _headers;
        }

        /// <summary>
        /// この行のフィールド数を取得します
        /// </summary>
        public int FieldCount => _valuesByIndex.Count > 0 ? _valuesByIndex.Count : _headers.Count;

        internal void Add(string header, object value, int index)
        {
            if (!_valuesByName.ContainsKey(header))
            {
                _headers.Add(header);
            }
            _valuesByName[header] = value;
            _valuesByIndex[index] = value;
        }

        internal void AddByIndex(int index, object value)
        {
            _valuesByIndex[index] = value;
        }
    }
}
