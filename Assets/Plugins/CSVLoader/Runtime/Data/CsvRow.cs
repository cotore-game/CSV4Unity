namespace CSV4Unity
{
    /// <summary>
    /// CsvDocument内の1行を参照する軽量なビューです。
    /// </summary>
    /// <remarks>この型はセルを所有せず、生成元の<see cref="CsvDocument"/>を参照します。</remarks>
    public readonly struct CsvRow
    {
        private readonly CsvDocument _document;

        internal CsvRow(CsvDocument document, int index)
        {
            _document = document;
            Index = index;
        }

        /// <summary>ヘッダーを除くゼロ始まりの行番号を取得します。</summary>
        public int Index { get; }

        /// <summary>行に含まれる列数を取得します。</summary>
        public int Count => _document.ColumnCount;

        /// <summary>列番号からセルを取得します。</summary>
        /// <param name="columnIndex">ゼロ始まりの列番号。</param>
        /// <returns>指定列のセル。</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="columnIndex"/>が範囲外です。</exception>
        public CsvCell this[int columnIndex] => _document.Cell(Index, columnIndex);

        /// <summary>ヘッダー名からセルを取得します。</summary>
        /// <param name="header">検索するヘッダー名。大文字小文字を区別します。</param>
        /// <returns>指定列のセル。</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="header"/>が<see langword="null"/>です。</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">指定したヘッダーが存在しません。</exception>
        public CsvCell this[string header] => _document.Cell(Index, header);
    }
}
