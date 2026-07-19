using System.Text.RegularExpressions;
using CSV4Unity.Validation;

namespace CSV4Unity.Fields
{
    /// <summary>
    /// Rfc4180.csvをEnumで読み込むための手動確認用スキーマです。
    /// </summary>
    public enum Rfc4180Fields
    {
        Id,
        Text,
        Note
    }

    /// <summary>
    /// HeaderMapping.csvのヘッダー補正を確認する手動確認用スキーマです。
    /// </summary>
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
}
