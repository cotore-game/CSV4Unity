using System;

namespace CSV4Unity
{
    /// <summary>
    /// CsvDocumentとEnumスキーマを組み合わせ、Enumによる列アクセスを提供します。
    /// </summary>
    public sealed class CsvTable<TField> where TField : struct, Enum
    {
        internal CsvTable(CsvDocument document)
            : this(document, CsvEnumSchema<TField>.Bind(document))
        {
        }

        public CsvTable(CsvDocument document, CsvEnumSchema<TField> schema)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            if (!ReferenceEquals(document, schema.Document))
            {
                throw new CsvSchemaException("The enum schema was bound to a different CSV document.");
            }
        }

        public CsvDocument Document { get; }
        public CsvEnumSchema<TField> Schema { get; }
        public int RowCount => Document.RowCount;
        public int ColumnCount => Document.ColumnCount;

        public CsvRow<TField> Row(int rowIndex)
        {
            Document.Row(rowIndex);
            return new CsvRow<TField>(this, rowIndex);
        }

        public CsvColumn<TField> Column(TField field)
        {
            return new CsvColumn<TField>(this, field, GetColumnIndex(field));
        }

        public CsvCell Cell(int rowIndex, TField field)
        {
            return Document.Cell(rowIndex, GetColumnIndex(field));
        }

        internal int GetColumnIndex(TField field)
        {
            return Schema.GetColumnIndex(field);
        }
    }

    /// <summary>Enumで列を指定できる行ビューです。</summary>
    public readonly struct CsvRow<TField> where TField : struct, Enum
    {
        private readonly CsvTable<TField> _table;

        internal CsvRow(CsvTable<TField> table, int index)
        {
            _table = table;
            Index = index;
        }

        public int Index { get; }
        public int Count => _table.ColumnCount;
        public CsvCell this[TField field] => _table.Cell(Index, field);
    }

    /// <summary>Enumで選択された列を参照するビューです。</summary>
    public readonly struct CsvColumn<TField> where TField : struct, Enum
    {
        private readonly CsvTable<TField> _table;

        internal CsvColumn(CsvTable<TField> table, TField field, int index)
        {
            _table = table;
            Field = field;
            Index = index;
        }

        public TField Field { get; }
        public int Index { get; }
        public int Count => _table.RowCount;
        public CsvCell this[int rowIndex] => _table.Document.Cell(rowIndex, Index);
        internal CsvDocument Document => _table.Document;
    }
}
