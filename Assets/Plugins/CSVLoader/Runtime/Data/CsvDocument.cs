using System;
using System.Collections.Generic;

namespace CSV4Unity
{
    /// <summary>
    /// 元のCSV文字列とセル位置を所有する、読み取り専用のCSVドキュメントです。
    /// </summary>
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

        public string Name { get; }
        public int RowCount { get; }
        public int ColumnCount { get; }
        public IReadOnlyList<string> Headers => _headers;
        public bool HasHeader => _headers.Length > 0;

        /// <summary>指定した行を参照するビューを返します。</summary>
        public CsvRow Row(int rowIndex)
        {
            ValidateRowIndex(rowIndex);
            return new CsvRow(this, rowIndex);
        }

        /// <summary>列番号から列ビューを返します。</summary>
        public CsvColumn Column(int columnIndex)
        {
            ValidateColumnIndex(columnIndex);
            return new CsvColumn(this, columnIndex);
        }

        /// <summary>ヘッダー名から列ビューを返します。</summary>
        public CsvColumn Column(string header)
        {
            return Column(GetColumnIndex(header));
        }

        /// <summary>行番号と列番号からセルを返します。</summary>
        public CsvCell Cell(int rowIndex, int columnIndex)
        {
            ValidateRowIndex(rowIndex);
            ValidateColumnIndex(columnIndex);
            return new CsvCell(_source, _cells[(rowIndex * ColumnCount) + columnIndex]);
        }

        /// <summary>行番号とヘッダー名からセルを返します。</summary>
        public CsvCell Cell(int rowIndex, string header)
        {
            return Cell(rowIndex, GetColumnIndex(header));
        }

        /// <summary>Enumとヘッダーを対応付けたテーブルを生成します。</summary>
        public CsvTable<TField> WithFields<TField>() where TField : struct, Enum
        {
            return new CsvTable<TField>(this);
        }

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
