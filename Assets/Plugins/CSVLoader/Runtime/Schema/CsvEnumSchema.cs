using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CSV4Unity
{
    /// <summary>
    /// Enum値とCSVの列番号を対応付けた不変スキーマです。
    /// </summary>
    /// <typeparam name="TField">CSVの列を表すEnum型。</typeparam>
    /// <remarks>
    /// 属性を指定しないEnum名は、ヘッダー名と大文字小文字を区別して比較します。
    /// 別名には<see cref="CsvHeaderAttribute"/>、正規表現には<see cref="CsvHeaderPatternAttribute"/>を使用します。
    /// </remarks>
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

        /// <summary>EnumフィールドとCSVヘッダー名を対応付けます。</summary>
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
            var mappedColumns = new Dictionary<int, string>(DeclaredFields.Length);
            for (int i = 0; i < DeclaredFields.Length; i++)
            {
                TField field = DeclaredFields[i];
                string fieldName = DeclaredNames[i];

                if (columnIndices.ContainsKey(field))
                {
                    throw new CsvSchemaException(
                        $"Enum '{typeof(TField).FullName}' contains aliases. Aliased enum values cannot define an unambiguous CSV schema.");
                }

                int columnIndex = ResolveColumnIndex(document, fieldName);
                if (mappedColumns.TryGetValue(columnIndex, out string mappedFieldName))
                {
                    throw new CsvSchemaException(
                        $"Enum fields '{mappedFieldName}' and '{fieldName}' both map to CSV header '{document.Headers[columnIndex]}'.");
                }

                columnIndices.Add(field, columnIndex);
                mappedColumns.Add(columnIndex, fieldName);
            }

            return new CsvEnumSchema<TField>(document, columnIndices);
        }

        private static int ResolveColumnIndex(CsvDocument document, string fieldName)
        {
            FieldInfo fieldInfo = typeof(TField).GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
            var header = fieldInfo.GetCustomAttribute<CsvHeaderAttribute>();
            var pattern = fieldInfo.GetCustomAttribute<CsvHeaderPatternAttribute>();

            if (header != null && pattern != null)
            {
                throw new CsvSchemaException(
                    $"Enum field '{typeof(TField).Name}.{fieldName}' cannot use both CsvHeader and CsvHeaderPattern.");
            }

            if (pattern != null)
            {
                Regex regex;
                try
                {
                    regex = new Regex($@"\A(?:{pattern.Pattern})\z", pattern.Options);
                }
                catch (ArgumentException exception)
                {
                    throw new CsvSchemaException(
                        $"CsvHeaderPattern on enum field '{typeof(TField).Name}.{fieldName}' is invalid.",
                        exception);
                }

                return FindUniqueColumn(document, fieldName, candidate => regex.IsMatch(candidate), pattern.Pattern);
            }

            string expectedHeader = header?.Name ?? fieldName;
            StringComparison comparison = header?.IgnoreCase == true
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return FindUniqueColumn(
                document,
                fieldName,
                candidate => string.Equals(candidate, expectedHeader, comparison),
                expectedHeader);
        }

        private static int FindUniqueColumn(
            CsvDocument document,
            string fieldName,
            Predicate<string> matches,
            string description)
        {
            int matchedColumn = -1;
            for (int columnIndex = 0; columnIndex < document.Headers.Count; columnIndex++)
            {
                if (!matches(document.Headers[columnIndex])) continue;
                if (matchedColumn >= 0)
                {
                    throw new CsvSchemaException(
                        $"Header mapping '{description}' for enum field '{typeof(TField).Name}.{fieldName}' matches multiple CSV headers.");
                }

                matchedColumn = columnIndex;
            }

            if (matchedColumn >= 0) return matchedColumn;
            throw new CsvSchemaException(
                $"CSV header matching '{description}' required by enum field '{typeof(TField).Name}.{fieldName}' was not found.");
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
