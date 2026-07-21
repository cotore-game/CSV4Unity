using System;
using System.Collections.Generic;
using System.Globalization;

namespace CSV4Unity.Validation
{
    /// <summary>
    /// Enumに定義された制約属性を使ってCSVテーブルを検証します。
    /// </summary>
    /// <remarks>Validationは入力テーブルを変更せず、検出内容を新しい<see cref="CsvValidationResult"/>へ格納します。</remarks>
    public static class CsvValidator
    {
        /// <summary>
        /// Enumの制約属性と行条件に従ってテーブルを検証します。読み込んだデータ自体は変更しません。
        /// </summary>
        /// <typeparam name="TField">列と制約属性を定義したEnum型。</typeparam>
        /// <param name="table">検証するEnum対応テーブル。</param>
        /// <param name="validationSchema">
        /// 使用するValidationスキーマ。<see langword="null"/>の場合は<see cref="CsvValidationSchema{TField}.Default"/>を使用します。
        /// </param>
        /// <param name="context">ForeignKeyの参照先テーブル。参照先が同じテーブルだけの場合は<see langword="null"/>にできます。</param>
        /// <param name="formatProvider">型変換、数値比較、数値範囲検証に使用する形式。<see langword="null"/>の場合は<see cref="CultureInfo.InvariantCulture"/>を使用します。</param>
        /// <returns>すべてのエラーとWarningを格納したValidation結果。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="table"/>が<see langword="null"/>です。</exception>
        /// <remarks>
        /// 同じConditionグループの条件はANDとして評価され、条件が成立した行だけ対応するValidation属性を適用します。
        /// 条件を持たない属性は従来どおりすべての行へ適用されます。
        /// </remarks>
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
                ValidateRule(table, rules[i], context, provider, result);
            }

            return result;
        }

        private static void ValidateRule<TField>(
            CsvTable<TField> table,
            CsvFieldValidationRule<TField> rule,
            CsvValidationContext context,
            IFormatProvider formatProvider,
            CsvValidationResult result)
            where TField : struct, Enum
        {
            CsvColumn<TField> column = table.Column(rule.Field);

            if (rule.Kind == CsvValidationRuleKind.PrimaryKey)
            {
                ValidateDistinct(table, column, rule, true, formatProvider, result);
                return;
            }

            if (rule.Kind == CsvValidationRuleKind.Unique)
            {
                ValidateDistinct(table, column, rule, false, formatProvider, result);
                return;
            }

            HashSet<string> referenceValues = rule.Kind == CsvValidationRuleKind.ForeignKey
                ? PrepareForeignKeyValues(table, rule, context, result)
                : null;

            for (int rowIndex = 0; rowIndex < column.Count; rowIndex++)
            {
                if (!CsvConditionEvaluator.Matches(table, rowIndex, rule.Conditions, formatProvider)) continue;

                CsvCell cell = column[rowIndex];
                if (rule.Kind == CsvValidationRuleKind.NotNull)
                {
                    if (cell.IsEmpty && !rule.SuppressRequiredError)
                    {
                        result.AddError(rowIndex, rule.FieldName, "Value cannot be empty.");
                    }

                    continue;
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
            switch (rule.Kind)
            {
                case CsvValidationRuleKind.TypeConstraint:
                    if (!cell.CanGet(rule.ExpectedType, formatProvider))
                    {
                        result.AddError(
                            rowIndex,
                            rule.FieldName,
                            $"Value '{cell.GetString()}' cannot be converted to {rule.ExpectedType.Name}.");
                    }

                    break;

                case CsvValidationRuleKind.Range:
                    if (!cell.TryGet(out double value, formatProvider))
                    {
                        if (!rule.SuppressNumericError)
                        {
                            result.AddError(rowIndex, rule.FieldName, "Range validation requires a numeric value.");
                        }
                    }
                    else if (value < rule.RangeMin || value > rule.RangeMax)
                    {
                        result.AddError(
                            rowIndex,
                            rule.FieldName,
                            $"Value {value} is outside the range [{rule.RangeMin}, {rule.RangeMax}].");
                    }

                    break;

                case CsvValidationRuleKind.Regex:
                {
                    string text = cell.GetString();
                    if (!rule.Pattern.IsMatch(text))
                    {
                        result.AddError(rowIndex, rule.FieldName, $"Value '{text}' does not match the required pattern.");
                    }

                    break;
                }

                case CsvValidationRuleKind.AllowedValues:
                {
                    string text = cell.GetString();
                    if (!rule.AllowedValues.Contains(text))
                    {
                        result.AddError(rowIndex, rule.FieldName, $"Value '{text}' is not allowed.");
                    }

                    break;
                }

                case CsvValidationRuleKind.MinLength:
                {
                    int length = cell.GetString().Length;
                    if (length < rule.MinLength)
                    {
                        result.AddError(
                            rowIndex,
                            rule.FieldName,
                            $"Length {length} is less than the minimum {rule.MinLength}.");
                    }

                    break;
                }

                case CsvValidationRuleKind.MaxLength:
                {
                    int length = cell.GetString().Length;
                    if (length > rule.MaxLength)
                    {
                        result.AddError(
                            rowIndex,
                            rule.FieldName,
                            $"Length {length} exceeds the maximum {rule.MaxLength}.");
                    }

                    break;
                }

                case CsvValidationRuleKind.ForeignKey:
                    if (referenceValues != null)
                    {
                        string text = cell.GetString();
                        if (!referenceValues.Contains(text))
                        {
                            result.AddError(rowIndex, rule.FieldName, $"Referenced value '{text}' was not found.");
                        }
                    }

                    break;
            }
        }

        private static void ValidateDistinct<TField>(
            CsvTable<TField> table,
            CsvColumn<TField> column,
            CsvFieldValidationRule<TField> rule,
            bool requireValue,
            IFormatProvider formatProvider,
            CsvValidationResult result)
            where TField : struct, Enum
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (int rowIndex = 0; rowIndex < column.Count; rowIndex++)
            {
                if (!CsvConditionEvaluator.Matches(table, rowIndex, rule.Conditions, formatProvider)) continue;

                CsvCell cell = column[rowIndex];
                if (cell.IsEmpty)
                {
                    if (requireValue)
                    {
                        result.AddError(rowIndex, rule.FieldName, "Primary key cannot be empty.");
                    }

                    continue;
                }

                string value = cell.GetString();
                if (!seen.Add(value))
                {
                    string constraint = requireValue ? "primary key" : "unique";
                    result.AddError(rowIndex, rule.FieldName, $"Duplicate {constraint} value: '{value}'.");
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
