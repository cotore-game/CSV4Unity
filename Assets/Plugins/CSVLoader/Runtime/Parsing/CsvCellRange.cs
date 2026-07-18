using System;

namespace CSV4Unity
{
    [Flags]
    internal enum CsvCellFlags : byte
    {
        None = 0,
        Quoted = 1,
        EscapedQuotes = 2
    }

    internal readonly struct CsvCellRange
    {
        public CsvCellRange(int start, int length, CsvCellFlags flags)
        {
            Start = start;
            Length = length;
            Flags = flags;
        }

        public int Start { get; }
        public int Length { get; }
        public CsvCellFlags Flags { get; }
    }
}
