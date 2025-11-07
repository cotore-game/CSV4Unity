using System.Globalization;
using System;

namespace CSV4Unity
{
    /// <summary>
    /// CSV ローダーのオプション設定
    /// </summary>
    public sealed class CsvLoaderOptions
    {
        /// <summary>
        /// フィールド区切り文字
        /// </summary>
        public char Delimiter { get; set; } = ',';

        /// <summary>
        /// 1行目がヘッダーかどうか
        /// </summary>
        public bool HasHeader { get; set; } = true;

        /// <summary>
        /// コメント行のプレフィックス
        /// </summary>
        public string CommentPrefix { get; set; } = "#";

        /// <summary>
        /// フィールドの前後の空白を削除するか
        /// </summary>
        public bool TrimFields { get; set; } = true;

        /// <summary>
        /// 空行を無視するか
        /// </summary>
        public bool IgnoreEmptyLines { get; set; } = true;

        /// <summary>
        /// フィールド欠損時のポリシー
        /// </summary>
        public MissingFieldPolicy MissingFieldPolicy { get; set; } = MissingFieldPolicy.Throw;

        /// <summary>
        /// 数値解析に使用するカルチャー
        /// </summary>
        public IFormatProvider FormatProvider { get; set; } = CultureInfo.InvariantCulture;

        /// <summary>
        /// ロード時に自動バリデーションを実行するか（デフォルト: true）
        /// </summary>
        public bool ValidationEnabled { get; set; } = true;

        /// <summary>
        /// バリデーション失敗時に例外をスローするか（デフォルト: true）
        /// ValidationEnabledがtrueの場合のみ有効
        /// </summary>
        public bool ThrowOnValidationError { get; set; } = true;
    }
}
