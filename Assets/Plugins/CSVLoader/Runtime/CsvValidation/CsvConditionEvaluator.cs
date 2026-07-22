using System;

namespace CSV4Unity.Validation
{
    /// <summary>
    /// コンパイル済みの行条件を評価します。
    /// </summary>
    internal static class CsvConditionEvaluator
    {
        public static bool Matches<TField>(
            CsvTable<TField> table,
            int rowIndex,
            CsvConditionRule<TField>[] conditions,
            IFormatProvider formatProvider)
            where TField : struct, Enum
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                if (!Matches(table, rowIndex, conditions[i], formatProvider)) return false;
            }

            return true;
        }

        private static bool Matches<TField>(
            CsvTable<TField> table,
            int rowIndex,
            CsvConditionRule<TField> condition,
            IFormatProvider formatProvider)
            where TField : struct, Enum
        {
            CsvCell cell = table.Cell(rowIndex, condition.Field);
            switch (condition.Comparison)
            {
                case Compare.IsEmpty:
                    return cell.IsEmpty;
                case Compare.IsNotEmpty:
                    return !cell.IsEmpty;
                case Compare.In:
                    return MatchesAny(table, rowIndex, cell, condition, formatProvider);
                case Compare.NotIn:
                    return !MatchesAny(table, rowIndex, cell, condition, formatProvider);
                default:
                    return CompareCell(
                        table,
                        rowIndex,
                        cell,
                        condition.Values[0],
                        condition.Comparison,
                        condition.IgnoreCase,
                        formatProvider);
            }
        }

        private static bool MatchesAny<TField>(
            CsvTable<TField> table,
            int rowIndex,
            CsvCell cell,
            CsvConditionRule<TField> condition,
            IFormatProvider formatProvider)
            where TField : struct, Enum
        {
            for (int i = 0; i < condition.Values.Length; i++)
            {
                if (CompareCell(
                        table,
                        rowIndex,
                        cell,
                        condition.Values[i],
                        Compare.Equal,
                        condition.IgnoreCase,
                        formatProvider))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CompareCell<TField>(
            CsvTable<TField> table,
            int rowIndex,
            CsvCell left,
            object rightOperand,
            Compare comparison,
            bool ignoreCase,
            IFormatProvider formatProvider)
            where TField : struct, Enum
        {
            if (rightOperand is TField rightField)
            {
                return CompareCells(
                    left,
                    table.Cell(rowIndex, rightField),
                    comparison,
                    ignoreCase,
                    formatProvider);
            }

            if (rightOperand != null && IsNumericType(rightOperand.GetType()))
            {
                if (!left.TryGet(out double leftNumber, formatProvider))
                {
                    return comparison == Compare.NotEqual;
                }

                double rightNumber = Convert.ToDouble(rightOperand, formatProvider);
                return CompareNumbers(leftNumber, rightNumber, comparison);
            }

            if (rightOperand is bool rightBoolean)
            {
                if (!left.TryGet(out bool leftBoolean, formatProvider))
                {
                    return comparison == Compare.NotEqual;
                }

                return CompareNumbers(leftBoolean ? 1 : 0, rightBoolean ? 1 : 0, comparison);
            }

            string rightText = Convert.ToString(rightOperand, formatProvider) ?? string.Empty;
            return CompareCellText(left, rightText, comparison, ignoreCase);
        }

        private static bool CompareCells(
            CsvCell left,
            CsvCell right,
            Compare comparison,
            bool ignoreCase,
            IFormatProvider formatProvider)
        {
            if (comparison == Compare.Equal || comparison == Compare.NotEqual)
            {
                return CompareCellTexts(left, right, comparison, ignoreCase);
            }

            if (!left.TryGet(out double leftNumber, formatProvider) ||
                !right.TryGet(out double rightNumber, formatProvider))
            {
                return false;
            }

            return CompareNumbers(leftNumber, rightNumber, comparison);
        }

        private static bool CompareCellText(
            CsvCell left,
            string right,
            Compare comparison,
            bool ignoreCase)
        {
            StringComparison stringComparison = ignoreCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            if (!left.HasEscapedQuotes)
            {
                int spanResult = left.RawSpan.CompareTo(right.AsSpan(), stringComparison);
                return CompareResult(spanResult, comparison);
            }

            int stringResult = string.Compare(left.GetString(), right, stringComparison);
            return CompareResult(stringResult, comparison);
        }

        private static bool CompareCellTexts(
            CsvCell left,
            CsvCell right,
            Compare comparison,
            bool ignoreCase)
        {
            StringComparison stringComparison = ignoreCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            if (!left.HasEscapedQuotes && !right.HasEscapedQuotes)
            {
                int spanResult = left.RawSpan.CompareTo(right.RawSpan, stringComparison);
                return CompareResult(spanResult, comparison);
            }

            int stringResult = string.Compare(left.GetString(), right.GetString(), stringComparison);
            return CompareResult(stringResult, comparison);
        }

        private static bool CompareNumbers(double left, double right, Compare comparison)
        {
            return CompareResult(left.CompareTo(right), comparison);
        }

        private static bool CompareResult(int result, Compare comparison)
        {
            switch (comparison)
            {
                case Compare.Equal:
                    return result == 0;
                case Compare.NotEqual:
                    return result != 0;
                case Compare.GreaterThan:
                    return result > 0;
                case Compare.GreaterThanOrEqual:
                    return result >= 0;
                case Compare.LessThan:
                    return result < 0;
                case Compare.LessThanOrEqual:
                    return result <= 0;
                default:
                    return false;
            }
        }

        private static bool IsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }
    }
}
