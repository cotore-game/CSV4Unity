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
