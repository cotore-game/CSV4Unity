using CSV4Unity.Validation;

namespace CSV4Unity.Fields
{
    /// <summary>
    /// Scenario.csvの列をEnumで参照するためのスキーマです。
    /// </summary>
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
