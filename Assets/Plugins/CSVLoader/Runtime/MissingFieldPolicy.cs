namespace CSV4Unity
{
    /// <summary>
    /// CSV のフィールドが存在しない場合のポリシー
    /// </summary>
    public enum MissingFieldPolicy
    {
        Throw,
        SetToDefault,
        Ignore
    }
}
