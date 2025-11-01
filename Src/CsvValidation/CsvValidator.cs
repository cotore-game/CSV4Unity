using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CSV4Unity.Validation
{
    /// <summary>
    /// CSVデータのバリデーションを実行するクラス
    /// </summary>
    public static class CsvValidator
    {
        /// <summary>
        /// Enum型に定義された制約属性に基づいてCSVデータを検証します
        /// </summary>
        /// <typeparam name="TEnum">検証するEnum型</typeparam>
        /// <param name="data">検証対象のCSVデータ</param>
        /// <returns>検証結果</returns>
        public static CsvValidationResult Validate<TEnum>(CsvData<TEnum> data) where TEnum : struct, Enum
        {
            var result = new CsvValidationResult();
            var enumType = typeof(TEnum);
            var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);

            // 各フィールドの制約を収集
            var constraints = new Dictionary<TEnum, List<Attribute>>();
            foreach (var field in fields)
            {
                var enumValue = (TEnum)field.GetValue(null);
                var attributes = field.GetCustomAttributes().ToList();
                if (attributes.Count > 0)
                {
                    constraints[enumValue] = attributes;
                }
            }

            // PrimaryKey制約のチェック（先に実行）
            foreach (var kvp in constraints)
            {
                var field = kvp.Key;
                var attrs = kvp.Value;

                if (attrs.Any(a => a is PrimaryKeyAttribute))
                {
                    ValidatePrimaryKey(data, field, result);
                }
            }

            // 各行を検証
            for (int rowIndex = 0; rowIndex < data.RowCount; rowIndex++)
            {
                var row = data.Rows[rowIndex];

                foreach (var kvp in constraints)
                {
                    var field = kvp.Key;
                    var attrs = kvp.Value;
                    var value = row[field];

                    foreach (var attr in attrs)
                    {
                        ValidateAttribute(attr, field, value, rowIndex, result, data);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 非ジェネリック版のバリデーション
        /// </summary>
        public static CsvValidationResult Validate(CsvData data, Type enumType)
        {
            var result = new CsvValidationResult();

            if (enumType == null || !enumType.IsEnum)
            {
                result.AddError(0, "N/A", "Invalid enum type provided for validation");
                return result;
            }

            var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);

            // 各フィールドの制約を収集
            var constraints = new Dictionary<string, List<Attribute>>();
            foreach (var field in fields)
            {
                var fieldName = field.Name;
                var attributes = field.GetCustomAttributes().ToList();
                if (attributes.Count > 0)
                {
                    constraints[fieldName] = attributes;
                }
            }

            // PrimaryKey制約のチェック
            foreach (var kvp in constraints)
            {
                var fieldName = kvp.Key;
                var attrs = kvp.Value;

                if (attrs.Any(a => a is PrimaryKeyAttribute))
                {
                    ValidatePrimaryKeyNonGeneric(data, fieldName, result);
                }
            }

            // 各行を検証
            for (int rowIndex = 0; rowIndex < data.RowCount; rowIndex++)
            {
                var row = data.Rows[rowIndex];

                foreach (var kvp in constraints)
                {
                    var fieldName = kvp.Key;
                    var attrs = kvp.Value;
                    var value = row[fieldName];

                    foreach (var attr in attrs)
                    {
                        ValidateAttributeNonGeneric(attr, fieldName, value, rowIndex, result, data);
                    }
                }
            }

            return result;
        }

        private static void ValidateAttribute<TEnum>(Attribute attr, TEnum field, object value, int rowIndex, CsvValidationResult result, CsvData<TEnum> data) where TEnum : struct, Enum
        {
            var fieldName = field.ToString();

            switch (attr)
            {
                case NotNullAttribute:
                    if (value == null || (value is string str && string.IsNullOrEmpty(str)))
                    {
                        result.AddError(rowIndex, fieldName, "Value cannot be null or empty");
                    }
                    break;

                case TypeConstraintAttribute typeAttr:
                    if (value != null && !IsConvertibleToType(value, typeAttr.ExpectedType))
                    {
                        result.AddError(rowIndex, fieldName, $"Value '{value}' cannot be converted to type {typeAttr.ExpectedType.Name}");
                    }
                    break;

                case RangeAttribute rangeAttr:
                    if (value != null && TryConvertToDouble(value, out double numValue))
                    {
                        if (numValue < rangeAttr.Min || numValue > rangeAttr.Max)
                        {
                            result.AddError(rowIndex, fieldName, $"Value {numValue} is out of range [{rangeAttr.Min}, {rangeAttr.Max}]");
                        }
                    }
                    break;

                case UniqueAttribute:
                    // Unique制約は列全体でチェック
                    ValidateUnique(data, field, result);
                    break;

                case RegexAttribute regexAttr:
                    if (value != null)
                    {
                        var strValue = value.ToString();
                        if (!Regex.IsMatch(strValue, regexAttr.Pattern))
                        {
                            result.AddError(rowIndex, fieldName, $"Value '{strValue}' does not match pattern '{regexAttr.Pattern}'");
                        }
                    }
                    break;

                case AllowedValuesAttribute allowedAttr:
                    if (value != null && !allowedAttr.AllowedValues.Contains(value))
                    {
                        var allowedStr = string.Join(", ", allowedAttr.AllowedValues);
                        result.AddError(rowIndex, fieldName, $"Value '{value}' is not in allowed values: [{allowedStr}]");
                    }
                    break;

                case MinLengthAttribute minLenAttr:
                    if (value != null)
                    {
                        var strValue = value.ToString();
                        if (strValue.Length < minLenAttr.MinLength)
                        {
                            result.AddError(rowIndex, fieldName, $"Length {strValue.Length} is less than minimum {minLenAttr.MinLength}");
                        }
                    }
                    break;

                case MaxLengthAttribute maxLenAttr:
                    if (value != null)
                    {
                        var strValue = value.ToString();
                        if (strValue.Length > maxLenAttr.MaxLength)
                        {
                            result.AddError(rowIndex, fieldName, $"Length {strValue.Length} exceeds maximum {maxLenAttr.MaxLength}");
                        }
                    }
                    break;
            }
        }

        private static void ValidateAttributeNonGeneric(Attribute attr, string fieldName, object value, int rowIndex, CsvValidationResult result, CsvData data)
        {
            // 非ジェネリック版も同様の処理
            switch (attr)
            {
                case NotNullAttribute:
                    if (value == null || (value is string str && string.IsNullOrEmpty(str)))
                    {
                        result.AddError(rowIndex, fieldName, "Value cannot be null or empty");
                    }
                    break;

                case TypeConstraintAttribute typeAttr:
                    if (value != null && !IsConvertibleToType(value, typeAttr.ExpectedType))
                    {
                        result.AddError(rowIndex, fieldName, $"Value '{value}' cannot be converted to type {typeAttr.ExpectedType.Name}");
                    }
                    break;

                case RangeAttribute rangeAttr:
                    if (value != null && TryConvertToDouble(value, out double numValue))
                    {
                        if (numValue < rangeAttr.Min || numValue > rangeAttr.Max)
                        {
                            result.AddError(rowIndex, fieldName, $"Value {numValue} is out of range [{rangeAttr.Min}, {rangeAttr.Max}]");
                        }
                    }
                    break;

                case UniqueAttribute:
                    ValidateUniqueNonGeneric(data, fieldName, result);
                    break;

                case RegexAttribute regexAttr:
                    if (value != null)
                    {
                        var strValue = value.ToString();
                        if (!Regex.IsMatch(strValue, regexAttr.Pattern))
                        {
                            result.AddError(rowIndex, fieldName, $"Value '{strValue}' does not match pattern '{regexAttr.Pattern}'");
                        }
                    }
                    break;

                case AllowedValuesAttribute allowedAttr:
                    if (value != null && !allowedAttr.AllowedValues.Contains(value))
                    {
                        var allowedStr = string.Join(", ", allowedAttr.AllowedValues);
                        result.AddError(rowIndex, fieldName, $"Value '{value}' is not in allowed values: [{allowedStr}]");
                    }
                    break;

                case MinLengthAttribute minLenAttr:
                    if (value != null)
                    {
                        var strValue = value.ToString();
                        if (strValue.Length < minLenAttr.MinLength)
                        {
                            result.AddError(rowIndex, fieldName, $"Length {strValue.Length} is less than minimum {minLenAttr.MinLength}");
                        }
                    }
                    break;

                case MaxLengthAttribute maxLenAttr:
                    if (value != null)
                    {
                        var strValue = value.ToString();
                        if (strValue.Length > maxLenAttr.MaxLength)
                        {
                            result.AddError(rowIndex, fieldName, $"Length {strValue.Length} exceeds maximum {maxLenAttr.MaxLength}");
                        }
                    }
                    break;
            }
        }

        private static void ValidatePrimaryKey<TEnum>(CsvData<TEnum> data, TEnum field, CsvValidationResult result) where TEnum : struct, Enum
        {
            var fieldName = field.ToString();
            var column = data.GetColumn(field);
            var seen = new HashSet<object>();
            var duplicates = new List<int>();

            for (int i = 0; i < column.Count; i++)
            {
                var value = column[i];

                if (value == null || (value is string str && string.IsNullOrEmpty(str)))
                {
                    result.AddError(i, fieldName, "Primary key cannot be null or empty");
                    continue;
                }

                if (!seen.Add(value))
                {
                    duplicates.Add(i);
                }
            }

            if (duplicates.Count > 0)
            {
                foreach (var rowIndex in duplicates)
                {
                    result.AddError(rowIndex, fieldName, $"Duplicate primary key value: '{column[rowIndex]}'");
                }
            }
        }

        private static void ValidatePrimaryKeyNonGeneric(CsvData data, string fieldName, CsvValidationResult result)
        {
            var column = data.GetColumn(fieldName);
            var seen = new HashSet<object>();
            var duplicates = new List<int>();

            for (int i = 0; i < column.Count; i++)
            {
                var value = column[i];

                if (value == null || (value is string str && string.IsNullOrEmpty(str)))
                {
                    result.AddError(i, fieldName, "Primary key cannot be null or empty");
                    continue;
                }

                if (!seen.Add(value))
                {
                    duplicates.Add(i);
                }
            }

            if (duplicates.Count > 0)
            {
                foreach (var rowIndex in duplicates)
                {
                    result.AddError(rowIndex, fieldName, $"Duplicate primary key value: '{column[rowIndex]}'");
                }
            }
        }

        private static void ValidateUnique<TEnum>(CsvData<TEnum> data, TEnum field, CsvValidationResult result) where TEnum : struct, Enum
        {
            var fieldName = field.ToString();
            var column = data.GetColumn(field);
            var seen = new HashSet<object>();
            var duplicates = new List<int>();

            for (int i = 0; i < column.Count; i++)
            {
                var value = column[i];

                // Uniqueはnullを許可
                if (value == null || (value is string str && string.IsNullOrEmpty(str)))
                {
                    continue;
                }

                if (!seen.Add(value))
                {
                    duplicates.Add(i);
                }
            }

            if (duplicates.Count > 0)
            {
                foreach (var rowIndex in duplicates)
                {
                    result.AddError(rowIndex, fieldName, $"Duplicate value (Unique constraint): '{column[rowIndex]}'");
                }
            }
        }

        private static void ValidateUniqueNonGeneric(CsvData data, string fieldName, CsvValidationResult result)
        {
            var column = data.GetColumn(fieldName);
            var seen = new HashSet<object>();
            var duplicates = new List<int>();

            for (int i = 0; i < column.Count; i++)
            {
                var value = column[i];

                if (value == null || (value is string str && string.IsNullOrEmpty(str)))
                {
                    continue;
                }

                if (!seen.Add(value))
                {
                    duplicates.Add(i);
                }
            }

            if (duplicates.Count > 0)
            {
                foreach (var rowIndex in duplicates)
                {
                    result.AddError(rowIndex, fieldName, $"Duplicate value (Unique constraint): '{column[rowIndex]}'");
                }
            }
        }

        private static bool IsConvertibleToType(object value, Type targetType)
        {
            try
            {
                Convert.ChangeType(value, targetType);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryConvertToDouble(object value, out double result)
        {
            try
            {
                result = Convert.ToDouble(value);
                return true;
            }
            catch
            {
                result = 0;
                return false;
            }
        }
    }
}
