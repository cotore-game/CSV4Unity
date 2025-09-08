using System.Globalization;
using System;

namespace CSV4Unity
{
    public sealed class CsvLoaderOptions
    {
        public char Delimiter { get; set; } = ',';
        public bool HasHeader { get; set; } = true;
        public string CommentPrefix { get; set; } = "#";
        public bool TrimFields { get; set; } = true;
        public bool IgnoreEmptyLines { get; set; } = true;
        public MissingFieldPolicy MissingFieldPolicy { get; set; } = MissingFieldPolicy.Throw;
        public IFormatProvider FormatProvider { get; set; } = CultureInfo.InvariantCulture;
    }
}
