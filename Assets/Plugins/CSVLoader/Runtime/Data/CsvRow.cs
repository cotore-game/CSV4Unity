namespace CSV4Unity
{
    /// <summary>
    /// CsvDocument内の1行を参照する軽量なビューです。
    /// </summary>
    public readonly struct CsvRow
    {
        private readonly CsvDocument _document;

        internal CsvRow(CsvDocument document, int index)
        {
            _document = document;
            Index = index;
        }

        public int Index { get; }
        public int Count => _document.ColumnCount;
        public CsvCell this[int columnIndex] => _document.Cell(Index, columnIndex);
        public CsvCell this[string header] => _document.Cell(Index, header);
    }
}
