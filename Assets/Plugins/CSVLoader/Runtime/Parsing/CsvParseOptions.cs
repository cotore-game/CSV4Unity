namespace CSV4Unity
{
    /// <summary>
    /// CSV解析時の構文設定を指定します。
    /// </summary>
    public sealed class CsvParseOptions
    {
        /// <summary>先頭レコードをヘッダーとして扱うかを指定します。</summary>
        /// <value>ヘッダーとして扱う場合は<see langword="true"/>。既定値は<see langword="true"/>です。</value>
        public bool HasHeader { get; set; } = true;

        /// <summary>フィールドの区切り文字を指定します。</summary>
        /// <value>1文字の区切り文字。既定値はカンマです。</value>
        /// <remarks>ダブルクォート、CR、LFは指定できません。</remarks>
        public char Delimiter { get; set; } = ',';

        /// <summary>空レコードを読み飛ばすかを指定します。</summary>
        /// <value>空レコードを結果に含めない場合は<see langword="true"/>。既定値は<see langword="false"/>です。</value>
        /// <remarks>区切り文字を含まず、単一の空フィールドだけで構成されるレコードを空レコードとして扱います。</remarks>
        public bool IgnoreEmptyRecords { get; set; }

        /// <summary>クォートされていないフィールドの前後空白を除去するかを指定します。</summary>
        /// <value>前後空白を除去する場合は<see langword="true"/>。既定値は<see langword="false"/>です。</value>
        /// <remarks>クォートされたフィールド内の空白は除去しません。</remarks>
        public bool TrimUnquotedFields { get; set; }
    }
}
