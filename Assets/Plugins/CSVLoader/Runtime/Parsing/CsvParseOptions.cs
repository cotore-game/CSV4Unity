namespace CSV4Unity
{
    /// <summary>
    /// CSV解析時の構文設定を指定します。
    /// </summary>
    public sealed class CsvParseOptions
    {
        /// <summary>先頭レコードをヘッダーとして扱うかを指定します。</summary>
        public bool HasHeader { get; set; } = true;

        /// <summary>フィールドの区切り文字を指定します。</summary>
        public char Delimiter { get; set; } = ',';

        /// <summary>空レコードを読み飛ばすかを指定します。</summary>
        public bool IgnoreEmptyRecords { get; set; }

        /// <summary>クォートされていないフィールドの前後空白を除去するかを指定します。</summary>
        public bool TrimUnquotedFields { get; set; }
    }
}
