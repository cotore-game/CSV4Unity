using System;
using System.Collections.Generic;

namespace CSV4Unity
{
    /// <summary>
    /// 元のCSV文字列とセル位置を所有する、読み取り専用のCSVドキュメントです。
    /// </summary>
    /// <remarks>
    /// 行番号と列番号はゼロ始まりです。ヘッダーを使用する場合、ヘッダーレコードは<see cref="RowCount"/>と行番号に含まれません。
    /// この型が元CSV文字列とセル位置を所有し、<see cref="CsvRow"/>、<see cref="CsvColumn"/>、<see cref="CsvCell"/>はその内容を参照します。
    /// </remarks>
    public sealed class CsvDocument
    {
        private readonly string _source;
        private readonly string[] _headers;
        private readonly CsvCellRange[] _cells;
        private readonly Dictionary<string, int> _headerIndices;

        internal CsvDocument(string name, string source, string[] headers, CsvCellRange[] cells, int rowCount, int columnCount)
        {
            Name = name;
            _source = source;
            _headers = headers;
            _cells = cells;
            RowCount = rowCount;
            ColumnCount = columnCount;

            _headerIndices = new Dictionary<string, int>(headers.Length, StringComparer.Ordinal);
            for (int i = 0; i < headers.Length; i++)
            {
                _headerIndices.Add(headers[i], i);
            }
        }

        /// <summary>ログやValidation結果で使用できるドキュメントの識別名を取得します。</summary>
        public string Name { get; }

        /// <summary>ヘッダーを除いたデータ行数を取得します。</summary>
        public int RowCount { get; }

        /// <summary>各レコードの列数を取得します。</summary>
        public int ColumnCount { get; }

        /// <summary>宣言順のヘッダー名を取得します。</summary>
        /// <value>ヘッダーなしで解析した場合は空のリスト。</value>
        public IReadOnlyList<string> Headers => _headers;

        /// <summary>CSVをヘッダー付きとして解析したかを取得します。</summary>
        public bool HasHeader => _headers.Length > 0;

        /// <summary>指定した行を参照するビューを返します。</summary>
        /// <param name="rowIndex">ヘッダーを除くゼロ始まりの行番号。</param>
        /// <returns>指定行を参照する軽量なビュー。</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="rowIndex"/>が範囲外です。</exception>
        public CsvRow Row(int rowIndex)
        {
            ValidateRowIndex(rowIndex);
            return new CsvRow(this, rowIndex);
        }

        /// <summary>列番号から列ビューを返します。</summary>
        /// <param name="columnIndex">ゼロ始まりの列番号。</param>
        /// <returns>指定列を参照する軽量なビュー。</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="columnIndex"/>が範囲外です。</exception>
        public CsvColumn Column(int columnIndex)
        {
            ValidateColumnIndex(columnIndex);
            return new CsvColumn(this, columnIndex);
        }

        /// <summary>ヘッダー名から列ビューを返します。</summary>
        /// <param name="header">検索するヘッダー名。大文字小文字を区別します。</param>
        /// <returns>指定列を参照する軽量なビュー。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="header"/>が<see langword="null"/>です。</exception>
        /// <exception cref="KeyNotFoundException">指定したヘッダーが存在しません。</exception>
        public CsvColumn Column(string header)
        {
            return Column(GetColumnIndex(header));
        }

        /// <summary>行番号と列番号からセルを返します。</summary>
        /// <param name="rowIndex">ヘッダーを除くゼロ始まりの行番号。</param>
        /// <param name="columnIndex">ゼロ始まりの列番号。</param>
        /// <returns>指定位置のセル。</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="rowIndex"/>または<paramref name="columnIndex"/>が範囲外です。</exception>
        public CsvCell Cell(int rowIndex, int columnIndex)
        {
            ValidateRowIndex(rowIndex);
            ValidateColumnIndex(columnIndex);
            return new CsvCell(_source, _cells[(rowIndex * ColumnCount) + columnIndex]);
        }

        /// <summary>行番号とヘッダー名からセルを返します。</summary>
        /// <param name="rowIndex">ヘッダーを除くゼロ始まりの行番号。</param>
        /// <param name="header">検索するヘッダー名。大文字小文字を区別します。</param>
        /// <returns>指定位置のセル。</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="rowIndex"/>が範囲外です。</exception>
        /// <exception cref="ArgumentNullException"><paramref name="header"/>が<see langword="null"/>です。</exception>
        /// <exception cref="KeyNotFoundException">指定したヘッダーが存在しません。</exception>
        public CsvCell Cell(int rowIndex, string header)
        {
            return Cell(rowIndex, GetColumnIndex(header));
        }

        /// <summary>Enumとヘッダーを対応付けたテーブルを生成します。</summary>
        /// <typeparam name="TField">CSVヘッダーと同名のフィールドを持つEnum型。</typeparam>
        /// <returns>Enumで列を指定できるテーブル。</returns>
        /// <exception cref="CsvSchemaException">ヘッダーがないか、Enumの各フィールドをヘッダーへ一意に対応付けられません。</exception>
        public CsvTable<TField> WithFields<TField>() where TField : struct, Enum
        {
            return new CsvTable<TField>(this);
        }

        /// <summary>ヘッダー名に対応する列番号を取得します。</summary>
        /// <param name="header">検索するヘッダー名。大文字小文字を区別します。</param>
        /// <returns>ゼロ始まりの列番号。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="header"/>が<see langword="null"/>です。</exception>
        /// <exception cref="KeyNotFoundException">指定したヘッダーが存在しません。</exception>
        public int GetColumnIndex(string header)
        {
            if (header == null) throw new ArgumentNullException(nameof(header));
            if (_headerIndices.TryGetValue(header, out int columnIndex)) return columnIndex;
            throw new KeyNotFoundException($"Header '{header}' was not found.");
        }

        internal string GetHeader(int columnIndex)
        {
            ValidateColumnIndex(columnIndex);
            return HasHeader ? _headers[columnIndex] : string.Empty;
        }

        private void ValidateRowIndex(int rowIndex)
        {
            if ((uint)rowIndex >= (uint)RowCount) throw new ArgumentOutOfRangeException(nameof(rowIndex));
        }

        private void ValidateColumnIndex(int columnIndex)
        {
            if ((uint)columnIndex >= (uint)ColumnCount) throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }
    }
}
