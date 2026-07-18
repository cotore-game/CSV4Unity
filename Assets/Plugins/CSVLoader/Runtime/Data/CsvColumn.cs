namespace CSV4Unity
{
    /// <summary>
    /// CsvDocument内の1列を参照する軽量なビューです。
    /// </summary>
    /// <remarks>この型はセルを所有せず、生成元の<see cref="CsvDocument"/>を参照します。</remarks>
    public readonly struct CsvColumn
    {
        private readonly CsvDocument _document;

        internal CsvColumn(CsvDocument document, int index)
        {
            _document = document;
            Index = index;
        }

        /// <summary>ゼロ始まりの列番号を取得します。</summary>
        public int Index { get; }

        /// <summary>ヘッダー名を取得します。</summary>
        /// <value>ヘッダーなしで解析した場合は空文字列。</value>
        public string Name => _document.GetHeader(Index);

        /// <summary>ヘッダーを除くデータ行数を取得します。</summary>
        public int Count => _document.RowCount;

        /// <summary>行番号からセルを取得します。</summary>
        /// <param name="rowIndex">ヘッダーを除くゼロ始まりの行番号。</param>
        /// <returns>指定行のセル。</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="rowIndex"/>が範囲外です。</exception>
        public CsvCell this[int rowIndex] => _document.Cell(rowIndex, Index);
        internal CsvDocument Document => _document;
    }
}
