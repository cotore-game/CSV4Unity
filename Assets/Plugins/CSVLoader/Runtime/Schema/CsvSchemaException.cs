using System;

namespace CSV4Unity
{
    /// <summary>
    /// CSVヘッダー、Enumフィールド、またはValidation属性から有効なスキーマを構築できない場合に送出される例外です。
    /// </summary>
    public sealed class CsvSchemaException : InvalidOperationException
    {
        /// <summary>エラー内容を指定して例外を生成します。</summary>
        /// <param name="message">エラー内容。</param>
        public CsvSchemaException(string message)
            : base(message)
        {
        }

        /// <summary>エラー内容と原因となった例外を指定して例外を生成します。</summary>
        /// <param name="message">エラー内容。</param>
        /// <param name="innerException">この例外の原因となった例外。</param>
        public CsvSchemaException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
