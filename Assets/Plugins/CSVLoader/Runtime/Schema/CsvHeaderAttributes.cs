using System;
using System.Text.RegularExpressions;

namespace CSV4Unity
{
    /// <summary>
    /// Enumフィールドへ対応付けるCSVヘッダー名を指定します。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class CsvHeaderAttribute : Attribute
    {
        /// <summary>CSVヘッダー名を指定して属性を生成します。</summary>
        /// <param name="name">対応付けるCSVヘッダー名。</param>
        /// <exception cref="ArgumentException"><paramref name="name"/>が空です。</exception>
        public CsvHeaderAttribute(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Header name must not be empty.", nameof(name));
            Name = name;
        }

        /// <summary>対応付けるCSVヘッダー名を取得します。</summary>
        public string Name { get; }

        /// <summary>ヘッダー名の大文字小文字を区別しない場合は<see langword="true"/>を指定します。</summary>
        public bool IgnoreCase { get; set; }
    }

    /// <summary>
    /// Enumフィールドへ対応付けるCSVヘッダー名を正規表現で指定します。
    /// </summary>
    /// <remarks>正規表現は部分一致ではなく、ヘッダー名全体に対して評価されます。</remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class CsvHeaderPatternAttribute : Attribute
    {
        /// <summary>正規表現パターンを指定して属性を生成します。</summary>
        /// <param name="pattern">ヘッダー名全体へ適用する正規表現パターン。</param>
        /// <exception cref="ArgumentException"><paramref name="pattern"/>が空です。</exception>
        public CsvHeaderPatternAttribute(string pattern)
            : this(pattern, RegexOptions.None)
        {
        }

        /// <summary>正規表現パターンとオプションを指定して属性を生成します。</summary>
        /// <param name="pattern">ヘッダー名全体へ適用する正規表現パターン。</param>
        /// <param name="options">正規表現の評価に使用するオプション。</param>
        /// <exception cref="ArgumentException"><paramref name="pattern"/>が空です。</exception>
        public CsvHeaderPatternAttribute(string pattern, RegexOptions options)
        {
            if (string.IsNullOrEmpty(pattern)) throw new ArgumentException("Header pattern must not be empty.", nameof(pattern));
            Pattern = pattern;
            Options = options;
        }

        /// <summary>ヘッダー名全体へ適用する正規表現パターンを取得します。</summary>
        public string Pattern { get; }

        /// <summary>正規表現の評価に使用するオプションを取得します。</summary>
        public RegexOptions Options { get; }
    }
}
