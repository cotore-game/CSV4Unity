using CSV4Unity.Validation;

namespace CSV4Unity.Examples
{
    /// <summary>
    /// Scenario.csvの列をEnumで参照するためのスキーマです。
    /// </summary>
    [CsvSchema]
    public enum ScenarioFields
    {
        [NotNull]
        Command,
        Arg1,
        Arg2,
        Arg3,
        Arg4,
        Arg5,
        Arg6,
        WaitType,
        Text,
        PageCtrl,
        Voice,
        WindowType,
        English
    }
}
