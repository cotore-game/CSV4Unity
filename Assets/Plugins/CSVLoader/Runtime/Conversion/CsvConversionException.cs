using System;

namespace CSV4Unity
{
    /// <summary>
    /// CSVセルの文字列を要求された型へ変換できない場合に送出される例外です。
    /// </summary>
    public sealed class CsvConversionException : FormatException
    {
        /// <summary>変換できなかった値と変換先型を指定して例外を生成します。</summary>
        /// <param name="value">変換できなかったCSVセルの文字列。</param>
        /// <param name="targetType">要求された変換先型。</param>
        /// <exception cref="NullReferenceException"><paramref name="targetType"/>が<see langword="null"/>です。</exception>
        public CsvConversionException(string value, Type targetType)
            : base($"CSV value '{value}' cannot be converted to {targetType.Name}.")
        {
            Value = value;
            TargetType = targetType;
        }

        /// <summary>変換できなかったCSVセルの文字列を取得します。</summary>
        public string Value { get; }

        /// <summary>要求された変換先型を取得します。</summary>
        public Type TargetType { get; }
    }
}
