using System;
using System.Collections.Generic;

namespace CSV4Unity
{
    /// <summary>
    /// 指定列の値から行番号を検索する、明示生成型のインデックスです。
    /// </summary>
    /// <typeparam name="TKey">検索キーへ変換する型。</typeparam>
    /// <remarks>
    /// 生成時に指定列の全データ行を走査し、同じキーに一致する複数の行番号を入力順で保持します。
    /// 元の<see cref="CsvDocument"/>は読み取り専用のため、生成後にIndexを同期する処理はありません。
    /// </remarks>
    public sealed class CsvIndex<TKey>
    {
        private readonly Dictionary<TKey, int> _firstRows;
        private readonly Dictionary<TKey, List<int>> _additionalRows;

        private CsvIndex(Dictionary<TKey, int> firstRows, Dictionary<TKey, List<int>> additionalRows)
        {
            _firstRows = firstRows;
            _additionalRows = additionalRows;
        }

        /// <summary>重複を除いたキー数を取得します。</summary>
        public int KeyCount => _firstRows.Count;

        /// <summary>非ジェネリック列からインデックスを作成します。</summary>
        /// <param name="column">検索対象の列。</param>
        /// <param name="skipEmpty">空セルをIndexへ含めない場合は<see langword="true"/>。</param>
        /// <param name="formatProvider">キー変換に使用する形式。<see langword="null"/>の場合はInvariantCultureを使用します。</param>
        /// <param name="comparer">キーの等価比較。<see langword="null"/>の場合は<see cref="EqualityComparer{TKey}.Default"/>を使用します。</param>
        /// <returns>指定列から生成されたIndex。</returns>
        /// <exception cref="CsvConversionException">セルを<typeparamref name="TKey"/>へ変換できません。</exception>
        /// <exception cref="InvalidOperationException"><paramref name="skipEmpty"/>が<see langword="false"/>で、変換結果が<see langword="null"/>です。</exception>
        public static CsvIndex<TKey> Create(
            CsvColumn column,
            bool skipEmpty = true,
            IFormatProvider formatProvider = null,
            IEqualityComparer<TKey> comparer = null)
        {
            return Build(column.Document, column.Index, skipEmpty, formatProvider, comparer);
        }

        /// <summary>Enumで選択された列からインデックスを作成します。</summary>
        /// <typeparam name="TField">列を指定するEnum型。</typeparam>
        /// <param name="column">検索対象のEnum対応列。</param>
        /// <param name="skipEmpty">空セルをIndexへ含めない場合は<see langword="true"/>。</param>
        /// <param name="formatProvider">キー変換に使用する形式。<see langword="null"/>の場合はInvariantCultureを使用します。</param>
        /// <param name="comparer">キーの等価比較。<see langword="null"/>の場合は<see cref="EqualityComparer{TKey}.Default"/>を使用します。</param>
        /// <returns>指定列から生成されたIndex。</returns>
        /// <exception cref="CsvConversionException">セルを<typeparamref name="TKey"/>へ変換できません。</exception>
        /// <exception cref="InvalidOperationException"><paramref name="skipEmpty"/>が<see langword="false"/>で、変換結果が<see langword="null"/>です。</exception>
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

        /// <summary>キーに最初に一致する行番号を検索します。</summary>
        /// <param name="key">検索キー。</param>
        /// <param name="rowIndex">一致した最初のゼロ始まり行番号。</param>
        /// <returns>一致する行が存在する場合は<see langword="true"/>、それ以外は<see langword="false"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/>が<see langword="null"/>で、基になるDictionaryがnullキーを許可しません。</exception>
        public bool TryFindFirst(TKey key, out int rowIndex)
        {
            return _firstRows.TryGetValue(key, out rowIndex);
        }

        /// <summary>キーに一致するすべての行番号を検索します。</summary>
        /// <param name="key">検索キー。</param>
        /// <returns>入力順の行番号を参照する検索結果。一致しない場合は空の結果。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/>が<see langword="null"/>で、基になるDictionaryがnullキーを許可しません。</exception>
        public CsvIndexMatches FindAll(TKey key)
        {
            if (!_firstRows.TryGetValue(key, out int firstRow)) return CsvIndexMatches.Empty;
            _additionalRows.TryGetValue(key, out List<int> additionalRows);
            return new CsvIndexMatches(firstRow, additionalRows);
        }
    }

    /// <summary>インデックス検索に一致した行番号を参照します。</summary>
    /// <remarks>行番号はゼロ始まりで、元CSVの入力順に並びます。</remarks>
    public readonly struct CsvIndexMatches
    {
        private readonly int _firstRow;
        private readonly IReadOnlyList<int> _additionalRows;

        internal CsvIndexMatches(int firstRow, IReadOnlyList<int> additionalRows)
        {
            _firstRow = firstRow;
            _additionalRows = additionalRows;
        }

        /// <summary>一致する行がない検索結果を取得します。</summary>
        public static CsvIndexMatches Empty => new CsvIndexMatches(-1, null);

        /// <summary>一致した行数を取得します。</summary>
        public int Count => _firstRow < 0 ? 0 : 1 + (_additionalRows?.Count ?? 0);

        /// <summary>入力順の行番号を取得します。</summary>
        /// <param name="index">検索結果内のゼロ始まり位置。</param>
        /// <returns>元ドキュメント内のゼロ始まり行番号。</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/>が範囲外です。</exception>
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
