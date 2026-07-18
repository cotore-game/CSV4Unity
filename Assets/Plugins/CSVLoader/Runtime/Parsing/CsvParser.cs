using System;
using System.Collections.Generic;

namespace CSV4Unity
{
    /// <summary>
    /// CSV文字列を解析して、読み取り専用のドキュメントを生成します。
    /// </summary>
    public static class CsvParser
    {
        /// <summary>
        /// CSV文字列を解析します。セル値の型推測やValidationは行いません。
        /// </summary>
        public static CsvDocument Parse(string source, CsvParseOptions options = null, string name = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            options ??= new CsvParseOptions();
            ValidateOptions(options);

            var cells = new List<CsvCellRange>();
            string[] headers = null;
            int columnCount = -1;
            int rowCount = 0;
            int recordIndex = 0;
            int position = source.Length > 0 && source[0] == '\uFEFF' ? 1 : 0;

            while (position < source.Length)
            {
                int recordStart = cells.Count;
                int fieldIndex = 0;
                bool reachedEnd = false;

                while (!reachedEnd)
                {
                    CsvCellRange cell = ParseCell(source, options, recordIndex, fieldIndex, ref position);
                    cells.Add(cell);
                    fieldIndex++;

                    if (position >= source.Length)
                    {
                        reachedEnd = true;
                    }
                    else if (source[position] == options.Delimiter)
                    {
                        position++;
                    }
                    else if (IsLineEnding(source[position]))
                    {
                        ConsumeLineEnding(source, ref position);
                        reachedEnd = true;
                    }
                    else
                    {
                        throw Error("Expected a delimiter or record terminator", recordIndex, fieldIndex - 1, position);
                    }
                }

                int recordFieldCount = cells.Count - recordStart;
                if (options.IgnoreEmptyRecords && IsEmptyRecord(cells, recordStart, recordFieldCount))
                {
                    cells.RemoveRange(recordStart, recordFieldCount);
                    recordIndex++;
                    continue;
                }

                if (headers == null && options.HasHeader)
                {
                    headers = CreateHeaders(source, cells, recordStart, recordFieldCount);
                    columnCount = recordFieldCount;
                    cells.RemoveRange(recordStart, recordFieldCount);
                }
                else
                {
                    if (columnCount < 0)
                    {
                        columnCount = recordFieldCount;
                    }
                    else if (recordFieldCount != columnCount)
                    {
                        throw Error(
                            $"Expected {columnCount} fields but found {recordFieldCount}",
                            recordIndex,
                            recordFieldCount,
                            position);
                    }

                    rowCount++;
                }

                recordIndex++;
            }

            if (columnCount < 0) columnCount = 0;
            if (headers == null) headers = Array.Empty<string>();

            return new CsvDocument(name ?? string.Empty, source, headers, cells.ToArray(), rowCount, columnCount);
        }

        private static CsvCellRange ParseCell(
            string source,
            CsvParseOptions options,
            int recordIndex,
            int fieldIndex,
            ref int position)
        {
            if (position >= source.Length || source[position] == options.Delimiter || IsLineEnding(source[position]))
            {
                return new CsvCellRange(position, 0, CsvCellFlags.None);
            }

            if (source[position] == '"')
            {
                return ParseQuotedCell(source, recordIndex, fieldIndex, ref position);
            }

            int start = position;
            while (position < source.Length && source[position] != options.Delimiter && !IsLineEnding(source[position]))
            {
                if (source[position] == '"')
                {
                    throw Error("A quote may only appear at the start of a quoted field", recordIndex, fieldIndex, position);
                }

                position++;
            }

            int end = position;
            if (options.TrimUnquotedFields)
            {
                while (start < end && char.IsWhiteSpace(source[start])) start++;
                while (end > start && char.IsWhiteSpace(source[end - 1])) end--;
            }

            return new CsvCellRange(start, end - start, CsvCellFlags.None);
        }

        private static CsvCellRange ParseQuotedCell(
            string source,
            int recordIndex,
            int fieldIndex,
            ref int position)
        {
            int start = ++position;
            bool hasEscapedQuotes = false;

            while (position < source.Length)
            {
                if (source[position] != '"')
                {
                    position++;
                    continue;
                }

                if (position + 1 < source.Length && source[position + 1] == '"')
                {
                    hasEscapedQuotes = true;
                    position += 2;
                    continue;
                }

                int length = position - start;
                position++;

                CsvCellFlags flags = CsvCellFlags.Quoted;
                if (hasEscapedQuotes) flags |= CsvCellFlags.EscapedQuotes;
                return new CsvCellRange(start, length, flags);
            }

            throw Error("Unterminated quoted field", recordIndex, fieldIndex, source.Length);
        }

        private static string[] CreateHeaders(
            string source,
            List<CsvCellRange> cells,
            int start,
            int count)
        {
            var headers = new string[count];
            var names = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < count; i++)
            {
                string header = CsvCell.Decode(source, cells[start + i]);
                if (!names.Add(header))
                {
                    throw Error($"Duplicate header '{header}'", 0, i, cells[start + i].Start);
                }

                headers[i] = header;
            }

            return headers;
        }

        private static bool IsEmptyRecord(List<CsvCellRange> cells, int start, int count)
        {
            return count == 1 && cells[start].Length == 0 && cells[start].Flags == CsvCellFlags.None;
        }

        private static void ValidateOptions(CsvParseOptions options)
        {
            if (options.Delimiter == '"' || IsLineEnding(options.Delimiter))
            {
                throw new ArgumentException("Delimiter cannot be a quote or line ending.", nameof(options));
            }
        }

        private static bool IsLineEnding(char value)
        {
            return value == '\r' || value == '\n';
        }

        private static void ConsumeLineEnding(string source, ref int position)
        {
            if (source[position] == '\r' && position + 1 < source.Length && source[position + 1] == '\n')
            {
                position += 2;
            }
            else
            {
                position++;
            }
        }

        private static CsvParseException Error(string message, int recordIndex, int fieldIndex, int characterIndex)
        {
            return new CsvParseException(message, recordIndex, fieldIndex, characterIndex);
        }
    }
}
