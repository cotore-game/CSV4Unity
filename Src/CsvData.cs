using System.Collections.Generic;
using System;
using System.Linq;

namespace CSV4Unity
{
    /// <summary>
    /// 汎用的な CSV 全体を保持するクラス（行・列の両方向アクセスに最適化）
    /// </summary>
    /// <typeparam name="TEnum">CSVのヘッダーに対応するEnum型</typeparam>
    public sealed class CsvData<TEnum> where TEnum : struct, Enum
    {
        /// <summary>
        /// CSVデータの名前
        /// </summary>
        public string DataName { get; set; }

        /// <summary>
        /// 全ての行データ
        /// </summary>
        public IReadOnlyList<LineData<TEnum>> Rows => _rows;

        /// <summary>
        /// 行数
        /// </summary>
        public int RowCount => _rows.Count;

        /// <summary>
        /// 列数
        /// </summary>
        public int ColumnCount => _columnCount;

        private readonly List<LineData<TEnum>> _rows = new();
        private readonly Dictionary<TEnum, List<object>> _columns = new();
        private int _columnCount;

        internal void Initialize()
        {
            _columnCount = Enum.GetValues(typeof(TEnum)).Length;
            foreach (TEnum field in Enum.GetValues(typeof(TEnum)))
            {
                _columns[field] = new List<object>();
            }
        }

        internal void Add(LineData<TEnum> row)
        {
            _rows.Add(row);

            // 列データも同時に構築（行追加時に一度だけ処理）
            foreach (TEnum field in Enum.GetValues(typeof(TEnum)))
            {
                _columns[field].Add(row[field]);
            }
        }

        /// <summary>
        /// 指定された列の全データを取得（高速な列アクセス）
        /// </summary>
        /// <param name="field">取得する列のEnum値</param>
        /// <returns>列の全データのリスト</returns>
        /// <exception cref="KeyNotFoundException">指定された列が存在しない場合</exception>
        public IReadOnlyList<object> GetColumn(TEnum field)
        {
            if (!_columns.TryGetValue(field, out var column))
            {
                throw new KeyNotFoundException($"Column {field} not found");
            }
            return column;
        }

        /// <summary>
        /// 指定された列の全データを型付きで取得
        /// </summary>
        /// <typeparam name="T">取得するデータの型</typeparam>
        /// <param name="field">取得する列のEnum値</param>
        /// <returns>型変換された列データのリスト</returns>
        /// <exception cref="KeyNotFoundException">指定された列が存在しない場合</exception>
        /// <exception cref="InvalidCastException">型変換に失敗した場合</exception>
        public IReadOnlyList<T> GetColumn<T>(TEnum field)
        {
            var column = GetColumn(field);
            var result = new List<T>(column.Count);

            for (int i = 0; i < column.Count; i++)
            {
                if (column[i] is T value)
                {
                    result.Add(value);
                }
                else if (column[i] != null)
                {
                    try
                    {
                        result.Add((T)Convert.ChangeType(column[i], typeof(T)));
                    }
                    catch
                    {
                        throw new InvalidCastException($"Cannot convert value at row {i} in column {field} to type {typeof(T)}");
                    }
                }
                else
                {
                    result.Add(default);
                }
            }

            return result;
        }

        /// <summary>
        /// 条件に一致する行をフィルタリング
        /// </summary>
        /// <param name="predicate">フィルタリング条件</param>
        /// <returns>条件に一致する行のコレクション</returns>
        public IEnumerable<LineData<TEnum>> Where(Func<LineData<TEnum>, bool> predicate)
        {
            return _rows.Where(predicate);
        }

        /// <summary>
        /// 指定フィールドでグループ化
        /// </summary>
        /// <param name="field">グループ化のキーとなる列</param>
        /// <returns>グループ化された行のコレクション</returns>
        public IEnumerable<IGrouping<object, LineData<TEnum>>> GroupBy(TEnum field)
        {
            return _rows.GroupBy(row => row[field]);
        }

        /// <summary>
        /// 指定フィールドの値で行を検索（最初の一致）
        /// </summary>
        /// <param name="field">検索対象の列</param>
        /// <param name="value">検索する値</param>
        /// <returns>最初に一致した行。見つからない場合はnull</returns>
        public LineData<TEnum> FindFirst(TEnum field, object value)
        {
            var comparer = EqualityComparer<object>.Default;
            for (int i = 0; i < _rows.Count; i++)
            {
                if (comparer.Equals(_rows[i][field], value))
                {
                    return _rows[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 指定フィールドの値で行を検索（全ての一致）
        /// </summary>
        /// <param name="field">検索対象の列</param>
        /// <param name="value">検索する値</param>
        /// <returns>一致した全ての行のコレクション</returns>
        public IEnumerable<LineData<TEnum>> FindAll(TEnum field, object value)
        {
            var comparer = EqualityComparer<object>.Default;
            var results = new List<LineData<TEnum>>();

            for (int i = 0; i < _rows.Count; i++)
            {
                if (comparer.Equals(_rows[i][field], value))
                {
                    results.Add(_rows[i]);
                }
            }

            return results;
        }

        /// <summary>
        /// 列の値で高速検索用のインデックスを作成
        /// </summary>
        /// <param name="field">インデックス化する列</param>
        /// <returns>値から行インデックスへのマッピング</returns>
        public Dictionary<object, List<int>> CreateIndex(TEnum field)
        {
            var index = new Dictionary<object, List<int>>();
            var column = _columns[field];

            for (int i = 0; i < column.Count; i++)
            {
                var value = column[i];
                if (!index.TryGetValue(value, out var list))
                {
                    list = new List<int>();
                    index[value] = list;
                }
                list.Add(i);
            }

            return index;
        }
    }

    /// <summary>
    /// 汎用的な CSV 全体を保持する非ジェネリック版クラス（インデックスベースアクセスに最適化）
    /// </summary>
    public sealed class CsvData
    {
        /// <summary>
        /// CSVデータの名前
        /// </summary>
        public string DataName { get; set; }

        /// <summary>
        /// 全ての行データ
        /// </summary>
        public IReadOnlyList<LineData> Rows => _rows;

        /// <summary>
        /// ヘッダー名のリスト（ヘッダーがない場合は空）
        /// </summary>
        public IReadOnlyList<string> Headers => _headers;

        /// <summary>
        /// 行数
        /// </summary>
        public int RowCount => _rows.Count;

        /// <summary>
        /// 列数
        /// </summary>
        public int ColumnCount => _columnCount;

        private readonly List<LineData> _rows = new();
        private readonly List<string> _headers = new();
        private readonly Dictionary<string, List<object>> _columnsByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, List<object>> _columnsByIndex = new();
        private int _columnCount;

        internal void SetHeaders(List<string> headers)
        {
            _headers.Clear();
            _headers.AddRange(headers);
            _columnCount = headers.Count;

            _columnsByName.Clear();
            _columnsByIndex.Clear();

            for (int i = 0; i < headers.Count; i++)
            {
                _columnsByName[headers[i]] = new List<object>();
                _columnsByIndex[i] = new List<object>();
            }
        }

        internal void SetColumnCount(int count)
        {
            _columnCount = count;
            _columnsByIndex.Clear();

            for (int i = 0; i < count; i++)
            {
                _columnsByIndex[i] = new List<object>();
            }
        }

        internal void Add(LineData row)
        {
            _rows.Add(row);

            // ヘッダーがある場合
            if (_headers.Count > 0)
            {
                for (int i = 0; i < _headers.Count; i++)
                {
                    var header = _headers[i];
                    var value = row[header];
                    _columnsByName[header].Add(value);
                    _columnsByIndex[i].Add(value);
                }
            }
            // ヘッダーがない場合（インデックスベース）
            else
            {
                for (int i = 0; i < _columnCount; i++)
                {
                    var value = row[i];
                    _columnsByIndex[i].Add(value);
                }
            }
        }

        /// <summary>
        /// ヘッダー名で列の全データを取得（高速な列アクセス）
        /// </summary>
        /// <param name="header">取得する列のヘッダー名</param>
        /// <returns>列の全データのリスト</returns>
        /// <exception cref="KeyNotFoundException">指定された列が存在しない場合</exception>
        public IReadOnlyList<object> GetColumn(string header)
        {
            if (!_columnsByName.TryGetValue(header, out var column))
            {
                throw new KeyNotFoundException($"Column '{header}' not found");
            }
            return column;
        }

        /// <summary>
        /// ヘッダー名で列の全データを型付きで取得
        /// </summary>
        /// <typeparam name="T">取得するデータの型</typeparam>
        /// <param name="header">取得する列のヘッダー名</param>
        /// <returns>型変換された列データのリスト</returns>
        /// <exception cref="KeyNotFoundException">指定された列が存在しない場合</exception>
        /// <exception cref="InvalidCastException">型変換に失敗した場合</exception>
        public IReadOnlyList<T> GetColumn<T>(string header)
        {
            var column = GetColumn(header);
            return ConvertColumn<T>(column, $"column '{header}'");
        }

        /// <summary>
        /// インデックスで列の全データを取得（ヘッダーなしCSV用の高速アクセス）
        /// </summary>
        /// <param name="index">列のインデックス（0始まり）</param>
        /// <returns>列の全データのリスト</returns>
        /// <exception cref="IndexOutOfRangeException">インデックスが範囲外の場合</exception>
        public IReadOnlyList<object> GetColumnByIndex(int index)
        {
            if (!_columnsByIndex.TryGetValue(index, out var column))
            {
                throw new IndexOutOfRangeException($"Column index {index} is out of range (0-{_columnCount - 1})");
            }
            return column;
        }

        /// <summary>
        /// インデックスで列の全データを型付きで取得（ヘッダーなしCSV用の高速アクセス）
        /// </summary>
        /// <typeparam name="T">取得するデータの型</typeparam>
        /// <param name="index">列のインデックス（0始まり）</param>
        /// <returns>型変換された列データのリスト</returns>
        /// <exception cref="IndexOutOfRangeException">インデックスが範囲外の場合</exception>
        /// <exception cref="InvalidCastException">型変換に失敗した場合</exception>
        public IReadOnlyList<T> GetColumnByIndex<T>(int index)
        {
            var column = GetColumnByIndex(index);
            return ConvertColumn<T>(column, $"column index {index}");
        }

        private IReadOnlyList<T> ConvertColumn<T>(IReadOnlyList<object> column, string columnName)
        {
            var result = new List<T>(column.Count);

            for (int i = 0; i < column.Count; i++)
            {
                if (column[i] is T value)
                {
                    result.Add(value);
                }
                else if (column[i] != null)
                {
                    try
                    {
                        result.Add((T)Convert.ChangeType(column[i], typeof(T)));
                    }
                    catch
                    {
                        throw new InvalidCastException($"Cannot convert value at row {i} in {columnName} to type {typeof(T)}");
                    }
                }
                else
                {
                    result.Add(default);
                }
            }

            return result;
        }

        /// <summary>
        /// 条件に一致する行をフィルタリング
        /// </summary>
        /// <param name="predicate">フィルタリング条件</param>
        /// <returns>条件に一致する行のコレクション</returns>
        public IEnumerable<LineData> Where(Func<LineData, bool> predicate)
        {
            return _rows.Where(predicate);
        }

        /// <summary>
        /// 指定フィールドでグループ化
        /// </summary>
        /// <param name="header">グループ化のキーとなる列のヘッダー名</param>
        /// <returns>グループ化された行のコレクション</returns>
        public IEnumerable<IGrouping<object, LineData>> GroupBy(string header)
        {
            return _rows.GroupBy(row => row[header]);
        }

        /// <summary>
        /// インデックスでグループ化（ヘッダーなしCSV用）
        /// </summary>
        /// <param name="index">グループ化のキーとなる列のインデックス</param>
        /// <returns>グループ化された行のコレクション</returns>
        public IEnumerable<IGrouping<object, LineData>> GroupByIndex(int index)
        {
            return _rows.GroupBy(row => row[index]);
        }

        /// <summary>
        /// 指定フィールドの値で行を検索（最初の一致）
        /// </summary>
        /// <param name="header">検索対象の列のヘッダー名</param>
        /// <param name="value">検索する値</param>
        /// <returns>最初に一致した行。見つからない場合はnull</returns>
        public LineData FindFirst(string header, object value)
        {
            var comparer = EqualityComparer<object>.Default;
            for (int i = 0; i < _rows.Count; i++)
            {
                if (comparer.Equals(_rows[i][header], value))
                {
                    return _rows[i];
                }
            }
            return null;
        }

        /// <summary>
        /// インデックスで行を検索（最初の一致、ヘッダーなしCSV用）
        /// </summary>
        /// <param name="index">検索対象の列のインデックス</param>
        /// <param name="value">検索する値</param>
        /// <returns>最初に一致した行。見つからない場合はnull</returns>
        public LineData FindFirstByIndex(int index, object value)
        {
            var comparer = EqualityComparer<object>.Default;
            for (int i = 0; i < _rows.Count; i++)
            {
                if (comparer.Equals(_rows[i][index], value))
                {
                    return _rows[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 指定フィールドの値で行を検索（全ての一致）
        /// </summary>
        /// <param name="header">検索対象の列のヘッダー名</param>
        /// <param name="value">検索する値</param>
        /// <returns>一致した全ての行のコレクション</returns>
        public IEnumerable<LineData> FindAll(string header, object value)
        {
            var comparer = EqualityComparer<object>.Default;
            var results = new List<LineData>();

            for (int i = 0; i < _rows.Count; i++)
            {
                if (comparer.Equals(_rows[i][header], value))
                {
                    results.Add(_rows[i]);
                }
            }

            return results;
        }

        /// <summary>
        /// インデックスで行を検索（全ての一致、ヘッダーなしCSV用）
        /// </summary>
        /// <param name="index">検索対象の列のインデックス</param>
        /// <param name="value">検索する値</param>
        /// <returns>一致した全ての行のコレクション</returns>
        public IEnumerable<LineData> FindAllByIndex(int index, object value)
        {
            var comparer = EqualityComparer<object>.Default;
            var results = new List<LineData>();

            for (int i = 0; i < _rows.Count; i++)
            {
                if (comparer.Equals(_rows[i][index], value))
                {
                    results.Add(_rows[i]);
                }
            }

            return results;
        }

        /// <summary>
        /// 列の値で高速検索用のインデックスを作成
        /// </summary>
        /// <param name="header">インデックス化する列のヘッダー名</param>
        /// <returns>値から行インデックスへのマッピング</returns>
        public Dictionary<object, List<int>> CreateIndex(string header)
        {
            var column = GetColumn(header);
            return CreateIndexFromColumn(column);
        }

        /// <summary>
        /// 列インデックスで高速検索用のインデックスを作成（ヘッダーなしCSV用）
        /// </summary>
        /// <param name="index">インデックス化する列のインデックス</param>
        /// <returns>値から行インデックスへのマッピング</returns>
        public Dictionary<object, List<int>> CreateIndexByColumnIndex(int index)
        {
            var column = GetColumnByIndex(index);
            return CreateIndexFromColumn(column);
        }

        private Dictionary<object, List<int>> CreateIndexFromColumn(IReadOnlyList<object> column)
        {
            var index = new Dictionary<object, List<int>>();

            for (int i = 0; i < column.Count; i++)
            {
                var value = column[i];
                if (!index.TryGetValue(value, out var list))
                {
                    list = new List<int>();
                    index[value] = list;
                }
                list.Add(i);
            }

            return index;
        }
    }
}
