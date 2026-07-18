using System;
using System.Collections.Generic;
using System.Globalization;

namespace CSV4Unity.Validation
{
    /// <summary>
    /// Enumに定義された制約属性を使ってCSVテーブルを検証します。
    /// </summary>
    public static class CsvValidator
    {
        /// <summary>
        /// Enumの制約属性に従ってテーブルを検証します。読み込んだデータ自体は変更しません。
        /// </summary>
        public static CsvValidationResult Validate<TField>(
            CsvTable<TField> table,
            CsvValidationSchema<TField> validationSchema = null,
            CsvValidationContext context = null,
            IFormatProvider formatProvider = null)
            where TField : struct, Enum
        {
            if (table == null) throw new ArgumentNullException(nameof(table));

            CsvValidationSchema<TField> schema = validationSchema ?? CsvValidationSchema<TField>.Default;
            IFormatProvider provider = formatProvider ?? CultureInfo.InvariantCulture;
            var result = new CsvValidationResult();

            IReadOnlyList<CsvFieldValidationRule<TField>> rules = schema.Rules;
            for (int i = 0; i < rules.Count; i++)
            {
                ValidateField(table, rules[i], context, provider, result);
            }

            return result;
        }

        private static void ValidateField<TField>(
            CsvTable<TField> table,
            CsvFieldValidationRule<TField> rule,
            CsvValidationContext context,
            IFormatProvider formatProvider,
            CsvValidationResult result)
            where TField : struct, Enum
        {
            CsvColumn<TField> column = table.Column(rule.Field);

            // 列全体の制約は行ループの外で一度だけ検証する。
            if (rule.IsPrimaryKey)
            {
                ValidateDistinct(column, rule.FieldName, true, result);
            }
            else if (rule.IsUnique)
            {
                ValidateDistinct(column, rule.FieldName, false, result);
            }

            HashSet<string> referenceValues = PrepareForeignKeyValues(table, rule, context, result);

            for (int rowIndex = 0; rowIndex < column.Count; rowIndex++)
            {
                CsvCell cell = column[rowIndex];

                if (rule.IsRequired && !rule.IsPrimaryKey && cell.IsEmpty)
                {
                    result.AddError(rowIndex, rule.FieldName, "Value cannot be empty.");
                }

                if (cell.IsEmpty) continue;

                ValidateCell(cell, rowIndex, rule, formatProvider, referenceValues, result);
            }
        }

        private static void ValidateCell<TField>(
            CsvCell cell,
            int rowIndex,
            CsvFieldValidationRule<TField> rule,
            IFormatProvider formatProvider,
            HashSet<string> referenceValues,
            CsvValidationResult result)
            where TField : struct, Enum
        {
            if (rule.ExpectedType != null && !cell.CanGet(rule.ExpectedType, formatProvider))
            {
                result.AddError(
                    rowIndex,
                    rule.FieldName,
                    $"Value '{cell.GetString()}' cannot be converted to {rule.ExpectedType.Name}.");
            }

            if (rule.RangeMin.HasValue)
            {
                if (!cell.TryGet(out double value, formatProvider))
                {
                    if (rule.ExpectedType == null)
                    {
                        result.AddError(rowIndex, rule.FieldName, "Range validation requires a numeric value.");
                    }
                }
                else if (value < rule.RangeMin.Value || value > rule.RangeMax.Value)
                {
                    result.AddError(
                        rowIndex,
                        rule.FieldName,
                        $"Value {value} is outside the range [{rule.RangeMin.Value}, {rule.RangeMax.Value}].");
                }
            }

            bool requiresString = rule.Pattern != null || rule.AllowedValues != null ||
                                  rule.MinLength.HasValue || rule.MaxLength.HasValue ||
                                  referenceValues != null;
            if (!requiresString) return;

            string text = cell.GetString();
            if (rule.Pattern != null && !rule.Pattern.IsMatch(text))
            {
                result.AddError(rowIndex, rule.FieldName, $"Value '{text}' does not match the required pattern.");
            }

            if (rule.AllowedValues != null && !rule.AllowedValues.Contains(text))
            {
                result.AddError(rowIndex, rule.FieldName, $"Value '{text}' is not allowed.");
            }

            if (rule.MinLength.HasValue && text.Length < rule.MinLength.Value)
            {
                result.AddError(
                    rowIndex,
                    rule.FieldName,
                    $"Length {text.Length} is less than the minimum {rule.MinLength.Value}.");
            }

            if (rule.MaxLength.HasValue && text.Length > rule.MaxLength.Value)
            {
                result.AddError(
                    rowIndex,
                    rule.FieldName,
                    $"Length {text.Length} exceeds the maximum {rule.MaxLength.Value}.");
            }

            if (referenceValues != null && !referenceValues.Contains(text))
            {
                result.AddError(rowIndex, rule.FieldName, $"Referenced value '{text}' was not found.");
            }
        }

        private static void ValidateDistinct<TField>(
            CsvColumn<TField> column,
            string fieldName,
            bool requireValue,
            CsvValidationResult result)
            where TField : struct, Enum
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (int rowIndex = 0; rowIndex < column.Count; rowIndex++)
            {
                CsvCell cell = column[rowIndex];
                if (cell.IsEmpty)
                {
                    if (requireValue)
                    {
                        result.AddError(rowIndex, fieldName, "Primary key cannot be empty.");
                    }

                    continue;
                }

                string value = cell.GetString();
                if (!seen.Add(value))
                {
                    string constraint = requireValue ? "primary key" : "unique";
                    result.AddError(rowIndex, fieldName, $"Duplicate {constraint} value: '{value}'.");
                }
            }
        }

        private static HashSet<string> PrepareForeignKeyValues<TField>(
            CsvTable<TField> table,
            CsvFieldValidationRule<TField> rule,
            CsvValidationContext context,
            CsvValidationResult result)
            where TField : struct, Enum
        {
            if (rule.ForeignKey == null) return null;

            CsvColumn referenceColumn;
            if (rule.ForeignKey.ReferenceEnumType == typeof(TField))
            {
                try
                {
                    referenceColumn = table.Document.Column(rule.ForeignKey.ReferenceField);
                }
                catch (KeyNotFoundException)
                {
                    result.AddWarning(-1, rule.FieldName, "Foreign key reference column was not found.");
                    return null;
                }
            }
            else if (context == null || !context.TryGetColumn(
                         rule.ForeignKey.ReferenceEnumType,
                         rule.ForeignKey.ReferenceField,
                         out referenceColumn))
            {
                result.AddWarning(-1, rule.FieldName, "Foreign key reference table is not registered.");
                return null;
            }

            var values = new HashSet<string>(StringComparer.Ordinal);
            for (int rowIndex = 0; rowIndex < referenceColumn.Count; rowIndex++)
            {
                CsvCell cell = referenceColumn[rowIndex];
                if (!cell.IsEmpty) values.Add(cell.GetString());
            }

            return values;
        }
    }
}
