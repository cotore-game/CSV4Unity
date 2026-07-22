using System.Text.RegularExpressions;
using CSV4Unity.Validation;

namespace CSV4Unity.Tests.Manual
{
    /// <summary>
    /// Rfc4180.csvをEnumで読み込むための手動確認用スキーマです。
    /// </summary>
    [CsvSchema]
    public enum Rfc4180Fields
    {
        Id,
        Text,
        Note
    }

    /// <summary>
    /// HeaderMapping.csvのヘッダー補正を確認する手動確認用スキーマです。
    /// </summary>
    [CsvSchema]
    public enum HeaderMappingFields
    {
        [CsvHeader("Item ID")]
        Id,

        [CsvHeaderPattern(@"display[_\s-]?name", RegexOptions.IgnoreCase)]
        DisplayName,

        [CsvHeader("enabled", IgnoreCase = true)]
        Enabled
    }

    /// <summary>
    /// ValidationInvalid.csvの制約検出に使用する手動確認用スキーマです。
    /// </summary>
    [CsvSchema]
    public enum ManualValidationFields
    {
        [PrimaryKey]
        [TypeConstraint(typeof(int))]
        Id,

        [NotNull]
        [MinLength(2)]
        Name,

        [CSV4Unity.Validation.Range(1, 10)]
        Level
    }

    /// <summary>
    /// ConditionalValidation.csvのCommand別Validationに使用する手動確認用スキーマです。
    /// </summary>
    [CsvSchema]
    public enum ConditionalValidationFields
    {
        Command,
        Enabled,

        [Condition(1, ConditionalValidationFields.Command, Compare.Equal, "A")]
        [Condition(1, ConditionalValidationFields.Enabled, Compare.Equal, true)]
        [NotNull(ConditionGroup = 1)]
        [TypeConstraint(typeof(int), ConditionGroup = 1)]

        [Condition(2, ConditionalValidationFields.Command, Compare.Equal, "B")]
        [NotNull(ConditionGroup = 2)]
        [TypeConstraint(typeof(bool), ConditionGroup = 2)]

        [Condition(3, ConditionalValidationFields.Command, Compare.NotIn, "A", "B")]
        [AllowedValues("fallback", ConditionGroup = 3)]
        Arg
    }
}
