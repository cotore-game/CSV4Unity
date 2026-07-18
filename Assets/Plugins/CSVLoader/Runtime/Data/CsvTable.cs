using System;

namespace CSV4Unity
{
    /// <summary>
    /// CsvDocumentとEnumスキーマを組み合わせ、Enumによる列アクセスを提供します。
    /// </summary>
    /// <typeparam name="TField">CSVヘッダーと同名のフィールドを持つEnum型。</typeparam>
    public sealed class CsvTable<TField> where TField : struct, Enum
    {
        internal CsvTable(CsvDocument document)
            : this(document, CsvEnumSchema<TField>.Bind(document))
        {
        }

        /// <summary>ドキュメントと対応済みスキーマを組み合わせてテーブルを生成します。</summary>
        /// <param name="document">セルデータを所有するドキュメント。</param>
        /// <param name="schema">同じドキュメントへ対応付けられたEnumスキーマ。</param>
        /// <exception cref="ArgumentNullException"><paramref name="document"/>または<paramref name="schema"/>が<see langword="null"/>です。</exception>
        /// <exception cref="CsvSchemaException"><paramref name="schema"/>が別のドキュメントへ対応付けられています。</exception>
        public CsvTable(CsvDocument document, CsvEnumSchema<TField> schema)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            if (!ReferenceEquals(document, schema.Document))
            {
                throw new CsvSchemaException("The enum schema was bound to a different CSV document.");
            }
        }

        /// <summary>セルデータを所有するドキュメントを取得します。</summary>
        public CsvDocument Document { get; }

        /// <summary>Enumフィールドと列番号の対応を取得します。</summary>
        public CsvEnumSchema<TField> Schema { get; }

        /// <summary>ヘッダーを除くデータ行数を取得します。</summary>
        public int RowCount => Document.RowCount;

        /// <summary>ドキュメント全体の列数を取得します。</summary>
        /// <remarks>Enumフィールド数が必要な場合は<see cref="CsvEnumSchema{TField}.FieldCount"/>を使用します。</remarks>
        public int ColumnCount => Document.ColumnCount;

        /// <summary>指定した行を参照するEnum対応ビューを返します。</summary>
        /// <param name="rowIndex">ヘッダーを除くゼロ始まりの行番号。</param>
        /// <returns>指定行を参照する軽量なビュー。</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="rowIndex"/>が範囲外です。</exception>
        public CsvRow<TField> Row(int rowIndex)
        {
            Document.Row(rowIndex);
            return new CsvRow<TField>(this, rowIndex);
        }

        /// <summary>Enumフィールドから列ビューを返します。</summary>
        /// <param name="field">検索するEnumフィールド。</param>
        /// <returns>指定列を参照する軽量なビュー。</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="field"/>がスキーマに含まれません。</exception>
        public CsvColumn<TField> Column(TField field)
        {
            return new CsvColumn<TField>(this, field, GetColumnIndex(field));
        }

        /// <summary>行番号とEnumフィールドからセルを返します。</summary>
        /// <param name="rowIndex">ヘッダーを除くゼロ始まりの行番号。</param>
        /// <param name="field">検索するEnumフィールド。</param>
        /// <returns>指定位置のセル。</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="rowIndex"/>が範囲外、または<paramref name="field"/>がスキーマに含まれません。</exception>
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
    /// <typeparam name="TField">列を指定するEnum型。</typeparam>
    /// <remarks>この型はセルを所有せず、生成元の<see cref="CsvTable{TField}"/>を参照します。</remarks>
    public readonly struct CsvRow<TField> where TField : struct, Enum
    {
        private readonly CsvTable<TField> _table;

        internal CsvRow(CsvTable<TField> table, int index)
        {
            _table = table;
            Index = index;
        }

        /// <summary>ヘッダーを除くゼロ始まりの行番号を取得します。</summary>
        public int Index { get; }

        /// <summary>ドキュメント全体の列数を取得します。</summary>
        public int Count => _table.ColumnCount;

        /// <summary>Enumフィールドからセルを取得します。</summary>
        /// <param name="field">検索するEnumフィールド。</param>
        /// <returns>指定列のセル。</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="field"/>がスキーマに含まれません。</exception>
        public CsvCell this[TField field] => _table.Cell(Index, field);
    }

    /// <summary>Enumで選択された列を参照するビューです。</summary>
    /// <typeparam name="TField">列を指定するEnum型。</typeparam>
    /// <remarks>この型はセルを所有せず、生成元の<see cref="CsvTable{TField}"/>を参照します。</remarks>
    public readonly struct CsvColumn<TField> where TField : struct, Enum
    {
        private readonly CsvTable<TField> _table;

        internal CsvColumn(CsvTable<TField> table, TField field, int index)
        {
            _table = table;
            Field = field;
            Index = index;
        }

        /// <summary>この列に対応するEnumフィールドを取得します。</summary>
        public TField Field { get; }

        /// <summary>ゼロ始まりの列番号を取得します。</summary>
        public int Index { get; }

        /// <summary>ヘッダーを除くデータ行数を取得します。</summary>
        public int Count => _table.RowCount;

        /// <summary>行番号からセルを取得します。</summary>
        /// <param name="rowIndex">ヘッダーを除くゼロ始まりの行番号。</param>
        /// <returns>指定行のセル。</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="rowIndex"/>が範囲外です。</exception>
        public CsvCell this[int rowIndex] => _table.Document.Cell(rowIndex, Index);
        internal CsvDocument Document => _table.Document;
    }
}
