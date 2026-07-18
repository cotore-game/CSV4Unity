using System;
using System.Collections.Generic;

namespace CSV4Unity
{
    /// <summary>
    /// 指定列の値から行番号を検索する、明示生成型のインデックスです。
    /// </summary>
    public sealed class CsvIndex<TKey>
    {
        private readonly Dictionary<TKey, int> _firstRows;
        private readonly Dictionary<TKey, List<int>> _additionalRows;

        private CsvIndex(Dictionary<TKey, int> firstRows, Dictionary<TKey, List<int>> additionalRows)
        {
            _firstRows = firstRows;
            _additionalRows = additionalRows;
        }

        public int KeyCount => _firstRows.Count;

        /// <summary>非ジェネリック列からインデックスを作成します。</summary>
        public static CsvIndex<TKey> Create(
            CsvColumn column,
            bool skipEmpty = true,
            IFormatProvider formatProvider = null,
            IEqualityComparer<TKey> comparer = null)
        {
            return Build(column.Document, column.Index, skipEmpty, formatProvider, comparer);
        }

        /// <summary>Enumで選択された列からインデックスを作成します。</summary>
        public static CsvIndex<TKey> Create<TField>(
            CsvColumn<TField> column,
            bool skipEmpty = true,
            IFormatProvider formatProvider = null,
            IEqualityComparer<TKey> comparer = null)
            where TField : struct, Enum
        {
            return Build(column.Document, column.Index, skipEmpty, formatProvider, comparer);
        }

        private static CsvIndex<TKey> Build(
            CsvDocument document,
            int columnIndex,
            bool skipEmpty,
            IFormatProvider formatProvider,
            IEqualityComparer<TKey> comparer)
        {
            var firstRows = new Dictionary<TKey, int>(comparer);
            var additionalRows = new Dictionary<TKey, List<int>>(comparer);

            for (int rowIndex = 0; rowIndex < document.RowCount; rowIndex++)
            {
                CsvCell cell = document.Cell(rowIndex, columnIndex);
                if (skipEmpty && cell.IsEmpty) continue;

                if (!cell.TryGet(out TKey key, formatProvider))
                {
                    throw new CsvConversionException(cell.GetString(), typeof(TKey));
                }

                if (!typeof(TKey).IsValueType && ReferenceEquals(key, null))
                {
                    if (skipEmpty) continue;
                    throw new InvalidOperationException("CSV indices do not support null keys.");
                }

                if (firstRows.ContainsKey(key))
                {
                    if (!additionalRows.TryGetValue(key, out List<int> rows))
                    {
                        rows = new List<int>();
                        additionalRows.Add(key, rows);
                    }

                    rows.Add(rowIndex);
                }
                else
                {
                    firstRows.Add(key, rowIndex);
                }
            }

            return new CsvIndex<TKey>(firstRows, additionalRows);
        }

        public bool TryFindFirst(TKey key, out int rowIndex)
        {
            return _firstRows.TryGetValue(key, out rowIndex);
        }

        public CsvIndexMatches FindAll(TKey key)
        {
            if (!_firstRows.TryGetValue(key, out int firstRow)) return CsvIndexMatches.Empty;
            _additionalRows.TryGetValue(key, out List<int> additionalRows);
            return new CsvIndexMatches(firstRow, additionalRows);
        }
    }

    /// <summary>インデックス検索に一致した行番号を参照します。</summary>
    public readonly struct CsvIndexMatches
    {
        private readonly int _firstRow;
        private readonly IReadOnlyList<int> _additionalRows;

        internal CsvIndexMatches(int firstRow, IReadOnlyList<int> additionalRows)
        {
            _firstRow = firstRow;
            _additionalRows = additionalRows;
        }

        public static CsvIndexMatches Empty => new CsvIndexMatches(-1, null);
        public int Count => _firstRow < 0 ? 0 : 1 + (_additionalRows?.Count ?? 0);

        public int this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
                return index == 0 ? _firstRow : _additionalRows[index - 1];
            }
        }
    }
}
