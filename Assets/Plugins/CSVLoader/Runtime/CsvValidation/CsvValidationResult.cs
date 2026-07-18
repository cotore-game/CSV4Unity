using System.Collections.Generic;
using System.Linq;

namespace CSV4Unity.Validation
{
    /// <summary>
    /// CSVテーブルのValidation結果を保持します。
    /// </summary>
    public class CsvValidationResult
    {
        /// <summary>エラーがなく、Validationに成功したかを取得します。</summary>
        /// <remarks>Warningの有無はこの値に影響しません。</remarks>
        public bool IsValid => Errors.Count == 0;

        /// <summary>検出されたエラーの変更可能なリストを取得します。</summary>
        public List<ValidationError> Errors { get; } = new List<ValidationError>();

        /// <summary>検出されたWarningの変更可能なリストを取得します。</summary>
        public List<ValidationWarning> Warnings { get; } = new List<ValidationWarning>();

        /// <summary>エラーを追加します。</summary>
        /// <param name="row">ヘッダーを除くゼロ始まり行番号。列全体のエラーには負数を指定します。</param>
        /// <param name="column">対象の列名。</param>
        /// <param name="message">エラー内容。</param>
        public void AddError(int row, string column, string message)
        {
            Errors.Add(new ValidationError
            {
                Row = row,
                Column = column,
                Message = message
            });
        }

        /// <summary>Warningを追加します。</summary>
        /// <param name="row">ヘッダーを除くゼロ始まり行番号。列全体のWarningには負数を指定します。</param>
        /// <param name="column">対象の列名。</param>
        /// <param name="message">Warning内容。</param>
        public void AddWarning(int row, string column, string message)
        {
            Warnings.Add(new ValidationWarning
            {
                Row = row,
                Column = column,
                Message = message
            });
        }

        /// <summary>エラー数とWarning数を含む短い英語の概要を生成します。</summary>
        /// <returns>Validation結果の概要。末尾に改行を含む場合があります。</returns>
        public string GetSummary()
        {
            if (IsValid && Warnings.Count == 0)
            {
                return "✓ All validations passed!";
            }

            var summary = "";
            if (Errors.Count > 0)
            {
                summary += $"✗ {Errors.Count} Error(s)\n";
            }
            if (Warnings.Count > 0)
            {
                summary += $"⚠ {Warnings.Count} Warning(s)\n";
            }
            return summary;
        }

        /// <summary>すべてのエラーを表示用文字列へ変換します。</summary>
        /// <returns>エラーリストを遅延列挙する文字列シーケンス。</returns>
        public IEnumerable<string> GetErrorMessages()
        {
            return Errors.Select(e => e.ToString());
        }

        /// <summary>すべてのWarningを表示用文字列へ変換します。</summary>
        /// <returns>Warningリストを遅延列挙する文字列シーケンス。</returns>
        public IEnumerable<string> GetWarningMessages()
        {
            return Warnings.Select(w => w.ToString());
        }
    }

    /// <summary>
    /// 1件のValidationエラーを表します。
    /// </summary>
    public class ValidationError
    {
        /// <summary>ヘッダーを除くゼロ始まり行番号を取得または設定します。</summary>
        /// <value>列全体のエラーの場合は負数。</value>
        public int Row { get; set; }

        /// <summary>対象の列名を取得または設定します。</summary>
        public string Column { get; set; }

        /// <summary>エラー内容を取得または設定します。</summary>
        public string Message { get; set; }

        /// <summary>行番号、列名、エラー内容を表示用文字列へ変換します。</summary>
        /// <returns>行番号を1始まりで表記したエラー文字列。行番号が負数の場合は列名だけを含みます。</returns>
        public override string ToString()
        {
            if (Row < 0) return $"[Column '{Column}'] {Message}";
            return $"[Row {Row + 1}, Column '{Column}'] {Message}";
        }
    }

    /// <summary>
    /// 1件のValidation Warningを表します。
    /// </summary>
    public class ValidationWarning
    {
        /// <summary>ヘッダーを除くゼロ始まり行番号を取得または設定します。</summary>
        /// <value>列全体のWarningの場合は負数。</value>
        public int Row { get; set; }

        /// <summary>対象の列名を取得または設定します。</summary>
        public string Column { get; set; }

        /// <summary>Warning内容を取得または設定します。</summary>
        public string Message { get; set; }

        /// <summary>行番号、列名、Warning内容を表示用文字列へ変換します。</summary>
        /// <returns>行番号を1始まりで表記したWarning文字列。行番号が負数の場合は列名だけを含みます。</returns>
        public override string ToString()
        {
            if (Row < 0) return $"[Column '{Column}'] {Message}";
            return $"[Row {Row + 1}, Column '{Column}'] {Message}";
        }
    }
}
