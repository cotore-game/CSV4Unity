using System;
using System.Collections.Generic;

namespace CSV4Unity
{
    /// <summary>
    /// Enum値とCSVの列番号を対応付けた不変スキーマです。
    /// </summary>
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

        public CsvDocument Document { get; }
        public int FieldCount => _columnIndices.Count;
        public ReadOnlySpan<TField> Fields => DeclaredFields;

        /// <summary>Enum名とCSVヘッダー名を対応付けます。</summary>
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

        public int GetColumnIndex(TField field)
        {
            if (_columnIndices.TryGetValue(field, out int columnIndex)) return columnIndex;
            throw new ArgumentOutOfRangeException(nameof(field), field, "The enum value is not part of this CSV schema.");
        }

        public bool TryGetColumnIndex(TField field, out int columnIndex)
        {
            return _columnIndices.TryGetValue(field, out columnIndex);
        }
    }
}
