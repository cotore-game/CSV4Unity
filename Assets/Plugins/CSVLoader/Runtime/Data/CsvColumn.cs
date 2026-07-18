namespace CSV4Unity
{
    /// <summary>
    /// CsvDocument内の1列を参照する軽量なビューです。
    /// </summary>
    public readonly struct CsvColumn
    {
        private readonly CsvDocument _document;

        internal CsvColumn(CsvDocument document, int index)
        {
            _document = document;
            Index = index;
        }

        public int Index { get; }
        public string Name => _document.GetHeader(Index);
        public int Count => _document.RowCount;
        public CsvCell this[int rowIndex] => _document.Cell(rowIndex, Index);
        internal CsvDocument Document => _document;
    }
}
