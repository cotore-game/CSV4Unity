using System;
using System.Collections.Generic;

namespace CSV4Unity
{
    /// <summary>
    /// Enum値とCSVの列番号を対応付けた不変スキーマです。
    /// </summary>
    /// <typeparam name="TField">CSVヘッダーと同名のフィールドを持つEnum型。</typeparam>
    /// <remarks>Enum名とヘッダー名は<see cref="StringComparer.Ordinal"/>相当で比較し、大文字小文字を区別します。</remarks>
    public sealed class CsvEnumSchema<TField> where TField : struct, Enum
    {
        private static readonly TField[] DeclaredFields = (TField[])Enum.GetValues(typeof(TField));
        private static readonly string[] DeclaredNames = Enum.GetNames(typeof(TField));

        private readonly Dictionary<TField, int> _columnIndices;

        private CsvEnumSchema(CsvDocument document, Dictionary<TField, int> columnIndices)
        {
            Document = document;
            _columnIndices = columnIndices;
        }

        /// <summary>このスキーマを対応付けたドキュメントを取得します。</summary>
        public CsvDocument Document { get; }

        /// <summary>対応付けられたEnumフィールド数を取得します。</summary>
        public int FieldCount => _columnIndices.Count;

        /// <summary>宣言順のEnumフィールドを割り当てなしで参照します。</summary>
        public ReadOnlySpan<TField> Fields => DeclaredFields;

        /// <summary>Enum名とCSVヘッダー名を対応付けます。</summary>
        /// <param name="document">ヘッダー付きで解析されたドキュメント。</param>
        /// <returns>指定ドキュメントに対応付けられた不変スキーマ。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="document"/>が<see langword="null"/>です。</exception>
        /// <exception cref="CsvSchemaException">
        /// ヘッダーがない、必要なヘッダーが存在しない、またはEnumに同じ値を持つ別名フィールドがあります。
        /// </exception>
        public static CsvEnumSchema<TField> Bind(CsvDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (!document.HasHeader)
            {
                throw new CsvSchemaException("Enum field access requires a CSV header record.");
            }

            var columnIndices = new Dictionary<TField, int>(DeclaredFields.Length);
            for (int i = 0; i < DeclaredFields.Length; i++)
            {
                TField field = DeclaredFields[i];
                string header = DeclaredNames[i];

                if (columnIndices.ContainsKey(field))
                {
                    throw new CsvSchemaException(
                        $"Enum '{typeof(TField).FullName}' contains aliases. Aliased enum values cannot define an unambiguous CSV schema.");
                }

                try
                {
                    columnIndices.Add(field, document.GetColumnIndex(header));
                }
                catch (KeyNotFoundException exception)
                {
                    throw new CsvSchemaException(
                        $"CSV header '{header}' required by enum '{typeof(TField).Name}' was not found.",
                        exception);
                }
            }

            return new CsvEnumSchema<TField>(document, columnIndices);
        }

        /// <summary>Enumフィールドに対応する列番号を取得します。</summary>
        /// <param name="field">検索するEnumフィールド。</param>
        /// <returns>ゼロ始まりの列番号。</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="field"/>がスキーマに含まれません。</exception>
        public int GetColumnIndex(TField field)
        {
            if (_columnIndices.TryGetValue(field, out int columnIndex)) return columnIndex;
            throw new ArgumentOutOfRangeException(nameof(field), field, "The enum value is not part of this CSV schema.");
        }

        /// <summary>Enumフィールドに対応する列番号を取得します。</summary>
        /// <param name="field">検索するEnumフィールド。</param>
        /// <param name="columnIndex">対応付けが存在する場合のゼロ始まり列番号。</param>
        /// <returns>対応付けが存在する場合は<see langword="true"/>、それ以外は<see langword="false"/>。</returns>
        public bool TryGetColumnIndex(TField field, out int columnIndex)
        {
            return _columnIndices.TryGetValue(field, out columnIndex);
        }
    }
}
