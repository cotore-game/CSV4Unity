using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CSV4Unity.Validation
{
    /// <summary>
    /// Enumフィールドの制約属性を、再利用可能な検証規則へ変換します。
    /// </summary>
    public sealed class CsvValidationSchema<TField> where TField : struct, Enum
    {
        private readonly CsvFieldValidationRule<TField>[] _rules;

        private CsvValidationSchema(CsvFieldValidationRule<TField>[] rules)
        {
            _rules = rules;
        }

        public static CsvValidationSchema<TField> Default { get; } = Create();
        public int RuleCount => _rules.Length;

        /// <summary>Enumに定義された制約属性からスキーマを作成します。</summary>
        public static CsvValidationSchema<TField> Create()
        {
            FieldInfo[] fields = typeof(TField).GetFields(BindingFlags.Public | BindingFlags.Static);
            var rules = new List<CsvFieldValidationRule<TField>>(fields.Length);

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo fieldInfo = fields[i];
                object[] attributes = fieldInfo.GetCustomAttributes(typeof(Attribute), false);
                CsvFieldValidationRule<TField> rule = CreateRule(fieldInfo, attributes);
                if (rule != null) rules.Add(rule);
            }

            return new CsvValidationSchema<TField>(rules.ToArray());
        }

        internal IReadOnlyList<CsvFieldValidationRule<TField>> Rules => _rules;

        private static CsvFieldValidationRule<TField> CreateRule(FieldInfo fieldInfo, object[] attributes)
        {
            var rule = new CsvFieldValidationRule<TField>
            {
                Field = (TField)fieldInfo.GetValue(null),
                FieldName = fieldInfo.Name
            };

            bool hasConstraint = false;
            for (int i = 0; i < attributes.Length; i++)
            {
                switch (attributes[i])
                {
                    case PrimaryKeyAttribute:
                        rule.IsPrimaryKey = true;
                        hasConstraint = true;
                        break;
                    case NotNullAttribute:
                        rule.IsRequired = true;
                        hasConstraint = true;
                        break;
                    case UniqueAttribute:
                        rule.IsUnique = true;
                        hasConstraint = true;
                        break;
                    case TypeConstraintAttribute typeConstraint:
                        rule.ExpectedType = typeConstraint.ExpectedType;
                        hasConstraint = true;
                        break;
                    case RangeAttribute range:
                        rule.RangeMin = range.Min;
                        rule.RangeMax = range.Max;
                        hasConstraint = true;
                        break;
                    case RegexAttribute regex:
                        rule.Pattern = new Regex(regex.Pattern, RegexOptions.CultureInvariant);
                        hasConstraint = true;
                        break;
                    case AllowedValuesAttribute allowedValues:
                        rule.AllowedValues = CreateAllowedValues(allowedValues.AllowedValues);
                        hasConstraint = true;
                        break;
                    case MinLengthAttribute minLength:
                        rule.MinLength = minLength.MinLength;
                        hasConstraint = true;
                        break;
                    case MaxLengthAttribute maxLength:
                        rule.MaxLength = maxLength.MaxLength;
                        hasConstraint = true;
                        break;
                    case ForeignKeyAttribute foreignKey:
                        rule.ForeignKey = foreignKey;
                        hasConstraint = true;
                        break;
                }
            }

            return hasConstraint ? rule : null;
        }

        private static HashSet<string> CreateAllowedValues(object[] values)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < values.Length; i++)
            {
                result.Add(Convert.ToString(values[i], CultureInfo.InvariantCulture) ?? string.Empty);
            }

            return result;
        }
    }

    internal sealed class CsvFieldValidationRule<TField> where TField : struct, Enum
    {
        public TField Field { get; set; }
        public string FieldName { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsRequired { get; set; }
        public bool IsUnique { get; set; }
        public Type ExpectedType { get; set; }
        public double? RangeMin { get; set; }
        public double? RangeMax { get; set; }
        public Regex Pattern { get; set; }
        public HashSet<string> AllowedValues { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public ForeignKeyAttribute ForeignKey { get; set; }
    }
}
