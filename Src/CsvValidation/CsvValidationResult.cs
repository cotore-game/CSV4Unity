using System.Collections.Generic;
using System.Linq;

namespace CSV4Unity.Validation
{
    /// <summary>
    /// バリデーション結果
    /// </summary>
    public class CsvValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<ValidationError> Errors { get; } = new List<ValidationError>();
        public List<ValidationWarning> Warnings { get; } = new List<ValidationWarning>();

        public void AddError(int row, string column, string message)
        {
            Errors.Add(new ValidationError
            {
                Row = row,
                Column = column,
                Message = message
            });
        }

        public void AddWarning(int row, string column, string message)
        {
            Warnings.Add(new ValidationWarning
            {
                Row = row,
                Column = column,
                Message = message
            });
        }

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

        public IEnumerable<string> GetErrorMessages()
        {
            return Errors.Select(e => e.ToString());
        }

        public IEnumerable<string> GetWarningMessages()
        {
            return Warnings.Select(w => w.ToString());
        }
    }

    /// <summary>
    /// バリデーションエラー情報
    /// </summary>
    public class ValidationError
    {
        public int Row { get; set; }
        public string Column { get; set; }
        public string Message { get; set; }

        public override string ToString()
        {
            return $"[Row {Row + 1}, Column '{Column}'] {Message}";
        }
    }

    /// <summary>
    /// バリデーション警告情報
    /// </summary>
    public class ValidationWarning
    {
        public int Row { get; set; }
        public string Column { get; set; }
        public string Message { get; set; }

        public override string ToString()
        {
            return $"[Row {Row + 1}, Column '{Column}'] {Message}";
        }
    }
}
