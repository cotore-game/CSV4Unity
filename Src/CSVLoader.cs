using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CSV4Unity
{
    public static class CSVLoader
    {
        /// <summary>
        /// ジェネリック版のLoadCSV
        /// </summary>
        public static CsvData<TEnum> LoadCSV<TEnum>(TextAsset csvFile, CsvLoaderOptions options, string dataName = null)
            where TEnum : struct, Enum
        {
            if (csvFile == null) throw new ArgumentNullException(nameof(csvFile), "CSV file cannot be null.");
            if (options == null) throw new ArgumentNullException(nameof(options), "Options cannot be null.");

            var buffer = csvFile.text.AsMemory();
            var span = buffer.Span;
            int pos = 0;
            int length = span.Length;

            List<(int Start, int Length)> headerPositions = new();
            int enumCount = Enum.GetValues(typeof(TEnum)).Length;
            var enumValues = (TEnum[])Enum.GetValues(typeof(TEnum));
            var enumToIndex = new int[enumCount];
            var headerSpan = ReadOnlySpan<char>.Empty;

            if (options.HasHeader)
            {
                // ヘッダー行を読み込む
                int lineEnd = IndexOf(span, '\n', ref pos);
                if (lineEnd < 0) lineEnd = length;

                headerSpan = span.Slice(0, lineEnd);
                if (options.TrimFields) headerSpan = Trim(headerSpan);

                if (headerSpan.IsEmpty) throw new Exception("Header not found or is empty. Please check CSV format.");

                ParseLine(headerSpan, headerPositions, options.Delimiter);

                for (int i = 0; i < enumCount; i++)
                {
                    var nameSpan = enumValues[i].ToString().AsSpan();
                    enumToIndex[i] = FindColumnIndex(nameSpan, headerPositions, headerSpan);
                }
            }
            else
            {
                // ヘッダーがない場合は、Enumのインデックスを直接使用する
                for (int i = 0; i < enumCount; i++)
                {
                    enumToIndex[i] = i;
                }
            }

            var result = new CsvData<TEnum> { DataName = string.IsNullOrEmpty(dataName) ? csvFile.name : dataName };
            var rowPositions = new List<(int Start, int Length)>();

            // データ行をループ
            while (pos < length)
            {
                int start = pos;
                int lineEnd = IndexOf(span, '\n', ref pos);
                if (lineEnd < 0) lineEnd = length;

                var lineSpan = span.Slice(start, lineEnd - start);
                if (options.TrimFields) lineSpan = Trim(lineSpan);

                // 空行とコメント行をスキップ
                if (options.IgnoreEmptyLines && lineSpan.IsEmpty) continue;
                if (!string.IsNullOrEmpty(options.CommentPrefix) && lineSpan.StartsWith(options.CommentPrefix.AsSpan())) continue;

                ParseLine(lineSpan, rowPositions, options.Delimiter);

                if (options.HasHeader && rowPositions.Count != headerPositions.Count)
                {
                    throw new Exception($"Column count mismatch: header has {headerPositions.Count} columns, data row has {rowPositions.Count} columns.");
                }

                var row = new LineData<TEnum>();
                for (int i = 0; i < enumCount; i++)
                {
                    var colIndex = enumToIndex[i];
                    if (colIndex >= rowPositions.Count)
                    {
                        if (options.MissingFieldPolicy == MissingFieldPolicy.Throw)
                        {
                            throw new Exception($"Field for enum '{enumValues[i]}' not found in data row.");
                        }
                        // MissingFieldPolicy.SetToDefault, Ignore は今後の拡張
                        continue;
                    }

                    var posInfo = rowPositions[colIndex];
                    var spanVal = lineSpan.Slice(posInfo.Start, posInfo.Length);
                    if (options.TrimFields) spanVal = Trim(spanVal);

                    row[enumValues[i]] = ParseValue(spanVal);
                }

                result.Add(row);
            }

            Debug.Log($"Loaded CsvData<{typeof(TEnum).Name}> ({result.Rows.Count} rows) from '{csvFile.name}'.");
            return result;
        }

        /// <summary>
        /// 非ジェネリック版のLoadCSV
        /// </summary>
        public static CsvData LoadCSV(TextAsset csvFile, CsvLoaderOptions options, string dataName = null)
        {
            if (csvFile == null) throw new ArgumentNullException(nameof(csvFile), "CSV file cannot be null.");
            if (options == null) throw new ArgumentNullException(nameof(options), "Options cannot be null.");

            var buffer = csvFile.text.AsMemory();
            var span = buffer.Span;
            int pos = 0;
            int length = span.Length;

            var result = new CsvData { DataName = string.IsNullOrEmpty(dataName) ? csvFile.name : dataName };
            var headerNames = new List<string>();

            // ヘッダーの読み込みと保存
            if (options.HasHeader)
            {
                int lineEnd = IndexOf(span, '\n', ref pos);
                if (lineEnd < 0) lineEnd = length;

                var headerSpan = span.Slice(0, lineEnd);
                if (options.TrimFields) headerSpan = Trim(headerSpan);

                if (headerSpan.IsEmpty) throw new Exception("Header not found or is empty. Please check CSV format.");

                var headerPositions = new List<(int Start, int Length)>();
                ParseLine(headerSpan, headerPositions, options.Delimiter);

                for (int i = 0; i < headerPositions.Count; i++)
                {
                    var posInfo = headerPositions[i];
                    headerNames.Add(Trim(headerSpan.Slice(posInfo.Start, posInfo.Length)).ToString());
                }
            }

            // データ行をループ
            var rowPositions = new List<(int Start, int Length)>();
            while (pos < length)
            {
                int start = pos;
                int lineEnd = IndexOf(span, '\n', ref pos);
                if (lineEnd < 0) lineEnd = length;

                var lineSpan = span.Slice(start, lineEnd - start);
                if (options.TrimFields) lineSpan = Trim(lineSpan);

                if (options.IgnoreEmptyLines && lineSpan.IsEmpty) continue;
                if (!string.IsNullOrEmpty(options.CommentPrefix) && lineSpan.StartsWith(options.CommentPrefix.AsSpan())) continue;

                ParseLine(lineSpan, rowPositions, options.Delimiter);

                // ヘッダーがなければ、行の列数チェックは行わない
                if (options.HasHeader && rowPositions.Count != headerNames.Count)
                {
                    throw new Exception($"Column count mismatch: header has {headerNames.Count} columns, data row has {rowPositions.Count} columns.");
                }

                var row = new LineData();
                for (int i = 0; i < rowPositions.Count; i++)
                {
                    var posInfo = rowPositions[i];
                    var spanVal = lineSpan.Slice(posInfo.Start, posInfo.Length);
                    if (options.TrimFields) spanVal = Trim(spanVal);

                    if (options.HasHeader && i < headerNames.Count)
                    {
                        row.Add(headerNames[i], ParseValue(spanVal));
                    }
                    else
                    {
                        // ヘッダーがない場合はインデックスをキーとして扱う (例: "0", "1", "2"...)
                        row.Add(i.ToString(), ParseValue(spanVal));
                    }
                }

                result.Add(row);
            }

            Debug.Log($"Loaded CsvData ({result.Rows.Count} rows) from '{csvFile.name}'.");
            return result;
        }

        private static object ParseValue(ReadOnlySpan<char> span)
        {
            if (int.TryParse(span, out var i)) return i;
            if (float.TryParse(span, out var f)) return f;
            if (bool.TryParse(span, out var b)) return b;
            return span.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int IndexOf(ReadOnlySpan<char> span, char target, ref int pos)
        {
            for (int i = pos; i < span.Length; i++)
            {
                if (span[i] == target)
                {
                    pos = i + 1;
                    return i;
                }
            }
            pos = span.Length;
            return -1;
        }

        private static void ParseLine(ReadOnlySpan<char> line, List<(int Start, int Length)> positions, char delimiter)
        {
            positions.Clear();
            int start = 0;
            bool inQuote = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    // クォート外でクォートが始まった場合
                    if (!inQuote && i == start)
                    {
                        inQuote = true;
                        start = i + 1;
                    }
                    // クォート内で二重クォートの場合
                    else if (inQuote && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        i++; // 次の文字もスキップ
                    }
                    // クォート内でクォートが終わる場合
                    else if (inQuote)
                    {
                        inQuote = false;
                        positions.Add((start, i - start));

                        // クォートの後のスペースと区切り文字をスキップ
                        while (i + 1 < line.Length && char.IsWhiteSpace(line[i + 1])) i++;
                        if (i + 1 < line.Length && line[i + 1] == delimiter) i++;
                        start = i + 1;
                    }
                }
                else if (c == delimiter && !inQuote)
                {
                    positions.Add((start, i - start));
                    start = i + 1;
                }
            }

            if (inQuote)
            {
                throw new Exception("Unclosed quote in line.");
            }

            if (start <= line.Length)
            {
                positions.Add((start, line.Length - start));
            }
        }

        private static int FindColumnIndex(ReadOnlySpan<char> key, List<(int Start, int Length)> positions, ReadOnlySpan<char> header)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                var span = header.Slice(positions[i].Start, positions[i].Length);
                if (Trim(span).SequenceEqual(key)) return i;
            }
            throw new Exception($"Header '{key.ToString()}' not found.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<char> Trim(ReadOnlySpan<char> s)
        {
            int start = 0, end = s.Length - 1;
            while (start <= end && char.IsWhiteSpace(s[start])) start++;
            while (end >= start && char.IsWhiteSpace(s[end])) end--;
            return s.Slice(start, end - start + 1);
        }
    }
}
