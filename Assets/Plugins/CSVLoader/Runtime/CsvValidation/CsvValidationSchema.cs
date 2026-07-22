using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CSV4Unity.Validation
{
    /// <summary>
    /// Enumフィールドの制約属性と行条件を、再利用可能な検証規則へ変換します。
    /// </summary>
    /// <typeparam name="TField">制約属性を定義したEnum型。</typeparam>
    /// <remarks>生成後の規則は変更されず、複数の<see cref="CsvTable{TField}"/>のValidationに再利用できます。</remarks>
    public sealed class CsvValidationSchema<TField> where TField : struct, Enum
    {
        private static readonly Lazy<CsvValidationSchema<TField>> DefaultSchema =
            new Lazy<CsvValidationSchema<TField>>(Create);

        private readonly CsvFieldValidationRule<TField>[] _rules;

        private CsvValidationSchema(CsvFieldValidationRule<TField>[] rules)
        {
            _rules = rules;
        }

        /// <summary>Enum型ごとに一度生成される既定スキーマを取得します。</summary>
        public static CsvValidationSchema<TField> Default => DefaultSchema.Value;

        /// <summary>Enumに定義されたValidation属性の総数を取得します。</summary>
        public int RuleCount => _rules.Length;

        /// <summary>Enumに定義された制約属性と行条件からスキーマを作成します。</summary>
        /// <returns>属性をコンパイルした新しいValidationスキーマ。</returns>
        /// <exception cref="ArgumentException">定義された正規表現パターンが不正です。</exception>
        /// <exception cref="CsvSchemaException">Conditionのフィールドまたはグループ定義が不正です。</exception>
        /// <remarks>
        /// Reflectionによる属性読み取りと正規表現の生成を行います。繰り返し検証する場合は<see cref="Default"/>を再利用してください。
        /// 制約属性を持たないEnumフィールドは規則に含まれません。
        /// </remarks>
        public static CsvValidationSchema<TField> Create()
        {
            FieldInfo[] fields = typeof(TField).GetFields(BindingFlags.Public | BindingFlags.Static);
            var rules = new List<CsvFieldValidationRule<TField>>(fields.Length);

            for (int i = 0; i < fields.Length; i++)
            {
                CreateRules(fields[i], rules);
            }

            return new CsvValidationSchema<TField>(rules.ToArray());
        }

        internal IReadOnlyList<CsvFieldValidationRule<TField>> Rules => _rules;

        private static void CreateRules(
            FieldInfo fieldInfo,
            List<CsvFieldValidationRule<TField>> destination)
        {
            object[] attributes = fieldInfo.GetCustomAttributes(typeof(Attribute), false);
            var conditionsByGroup = new Dictionary<int, List<CsvConditionRule<TField>>>();
            var validations = new List<CsvValidationAttribute>();

            for (int i = 0; i < attributes.Length; i++)
            {
                if (attributes[i] is ConditionAttribute condition)
                {
                    CsvConditionRule<TField> compiledCondition = CreateCondition(fieldInfo, condition);
                    if (!conditionsByGroup.TryGetValue(condition.Group, out List<CsvConditionRule<TField>> group))
                    {
                        group = new List<CsvConditionRule<TField>>();
                        conditionsByGroup.Add(condition.Group, group);
                    }

                    group.Add(compiledCondition);
                }
                else if (attributes[i] is CsvValidationAttribute validation)
                {
                    if (validation.ConditionGroup < 0)
                    {
                        throw new CsvSchemaException(
                            $"Validation attribute on '{typeof(TField).Name}.{fieldInfo.Name}' has a negative condition group.");
                    }

                    validations.Add(validation);
                }
            }

            var usedGroups = new HashSet<int>();
            var primaryKeyGroups = new HashSet<int>();
            var typeConstraintGroups = new HashSet<int>();
            for (int i = 0; i < validations.Count; i++)
            {
                if (validations[i] is PrimaryKeyAttribute)
                {
                    primaryKeyGroups.Add(validations[i].ConditionGroup);
                }
                else if (validations[i] is TypeConstraintAttribute)
                {
                    typeConstraintGroups.Add(validations[i].ConditionGroup);
                }
            }

            for (int i = 0; i < validations.Count; i++)
            {
                CsvValidationAttribute validation = validations[i];
                CsvConditionRule<TField>[] conditions = Array.Empty<CsvConditionRule<TField>>();
                if (conditionsByGroup.TryGetValue(
                        validation.ConditionGroup,
                        out List<CsvConditionRule<TField>> conditionList))
                {
                    conditions = conditionList.ToArray();
                    usedGroups.Add(validation.ConditionGroup);
                }
                else if (validation.ConditionGroup != 0)
                {
                    throw new CsvSchemaException(
                        $"Validation attribute on '{typeof(TField).Name}.{fieldInfo.Name}' references undefined condition group {validation.ConditionGroup}.");
                }

                destination.Add(CreateRule(
                    fieldInfo,
                    validation,
                    conditions,
                    primaryKeyGroups.Contains(validation.ConditionGroup),
                    typeConstraintGroups.Contains(validation.ConditionGroup)));
            }

            foreach (int group in conditionsByGroup.Keys)
            {
                if (!usedGroups.Contains(group))
                {
                    throw new CsvSchemaException(
                        $"Condition group {group} on '{typeof(TField).Name}.{fieldInfo.Name}' is not used by a validation attribute.");
                }
            }
        }

        private static CsvConditionRule<TField> CreateCondition(
            FieldInfo targetField,
            ConditionAttribute condition)
        {
            if (condition.Group < 0)
            {
                throw new CsvSchemaException(
                    $"Condition on '{typeof(TField).Name}.{targetField.Name}' has a negative group number.");
            }

            if (!Enum.IsDefined(typeof(Compare), condition.Comparison))
            {
                throw new CsvSchemaException(
                    $"Condition on '{typeof(TField).Name}.{targetField.Name}' uses an unsupported comparison value.");
            }

            ValidateConditionValueCount(targetField, condition);

            if (!(condition.Field is TField conditionField) || !Enum.IsDefined(typeof(TField), conditionField))
            {
                throw new CsvSchemaException(
                    $"Condition on '{typeof(TField).Name}.{targetField.Name}' must reference a field from enum '{typeof(TField).Name}'.");
            }

            object[] values = new object[condition.Values.Length];
            for (int i = 0; i < condition.Values.Length; i++)
            {
                object value = condition.Values[i];
                if (value is TField referencedField && !Enum.IsDefined(typeof(TField), referencedField))
                {
                    throw new CsvSchemaException(
                        $"Condition on '{typeof(TField).Name}.{targetField.Name}' references an undefined enum value.");
                }

                values[i] = value;
            }

            return new CsvConditionRule<TField>
            {
                Field = conditionField,
                Comparison = condition.Comparison,
                Values = values,
                IgnoreCase = condition.IgnoreCase
            };
        }

        private static void ValidateConditionValueCount(
            FieldInfo targetField,
            ConditionAttribute condition)
        {
            int count = condition.Values.Length;
            bool valid;
            switch (condition.Comparison)
            {
                case Compare.IsEmpty:
                case Compare.IsNotEmpty:
                    valid = count == 0;
                    break;
                case Compare.In:
                case Compare.NotIn:
                    valid = count > 0;
                    break;
                default:
                    valid = count == 1;
                    break;
            }

            if (!valid)
            {
                throw new CsvSchemaException(
                    $"Condition '{condition.Comparison}' on '{typeof(TField).Name}.{targetField.Name}' has an invalid number of comparison values.");
            }
        }

        private static CsvFieldValidationRule<TField> CreateRule(
            FieldInfo fieldInfo,
            CsvValidationAttribute validation,
            CsvConditionRule<TField>[] conditions,
            bool hasPrimaryKey,
            bool hasTypeConstraint)
        {
            var rule = new CsvFieldValidationRule<TField>
            {
                Field = (TField)fieldInfo.GetValue(null),
                FieldName = fieldInfo.Name,
                ConditionGroup = validation.ConditionGroup,
                Conditions = conditions,
                SuppressRequiredError = validation is NotNullAttribute && hasPrimaryKey,
                SuppressNumericError = validation is RangeAttribute && hasTypeConstraint
            };

            switch (validation)
            {
                case PrimaryKeyAttribute:
                    rule.Kind = CsvValidationRuleKind.PrimaryKey;
                    break;
                case NotNullAttribute:
                    rule.Kind = CsvValidationRuleKind.NotNull;
                    break;
                case UniqueAttribute:
                    rule.Kind = CsvValidationRuleKind.Unique;
                    break;
                case TypeConstraintAttribute typeConstraint:
                    rule.Kind = CsvValidationRuleKind.TypeConstraint;
                    rule.ExpectedType = typeConstraint.ExpectedType;
                    break;
                case RangeAttribute range:
                    rule.Kind = CsvValidationRuleKind.Range;
                    rule.RangeMin = range.Min;
                    rule.RangeMax = range.Max;
                    break;
                case RegexAttribute regex:
                    rule.Kind = CsvValidationRuleKind.Regex;
                    rule.Pattern = new Regex(regex.Pattern, RegexOptions.CultureInvariant);
                    break;
                case AllowedValuesAttribute allowedValues:
                    rule.Kind = CsvValidationRuleKind.AllowedValues;
                    rule.AllowedValues = CreateAllowedValues(allowedValues.AllowedValues);
                    break;
                case MinLengthAttribute minLength:
                    rule.Kind = CsvValidationRuleKind.MinLength;
                    rule.MinLength = minLength.MinLength;
                    break;
                case MaxLengthAttribute maxLength:
                    rule.Kind = CsvValidationRuleKind.MaxLength;
                    rule.MaxLength = maxLength.MaxLength;
                    break;
                case ForeignKeyAttribute foreignKey:
                    rule.Kind = CsvValidationRuleKind.ForeignKey;
                    rule.ForeignKey = foreignKey;
                    break;
                default:
                    throw new CsvSchemaException(
                        $"Unsupported validation attribute '{validation.GetType().FullName}'.");
            }

            return rule;
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

    internal enum CsvValidationRuleKind
    {
        PrimaryKey,
        NotNull,
        TypeConstraint,
        Range,
        Unique,
        Regex,
        AllowedValues,
        MinLength,
        MaxLength,
        ForeignKey
    }

    internal sealed class CsvFieldValidationRule<TField> where TField : struct, Enum
    {
        public TField Field { get; set; }
        public string FieldName { get; set; }
        public int ConditionGroup { get; set; }
        public CsvConditionRule<TField>[] Conditions { get; set; }
        public CsvValidationRuleKind Kind { get; set; }
        public Type ExpectedType { get; set; }
        public double RangeMin { get; set; }
        public double RangeMax { get; set; }
        public Regex Pattern { get; set; }
        public HashSet<string> AllowedValues { get; set; }
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public ForeignKeyAttribute ForeignKey { get; set; }
        public bool SuppressRequiredError { get; set; }
        public bool SuppressNumericError { get; set; }
    }

    internal sealed class CsvConditionRule<TField> where TField : struct, Enum
    {
        public TField Field { get; set; }
        public Compare Comparison { get; set; }
        public object[] Values { get; set; }
        public bool IgnoreCase { get; set; }
    }
}
