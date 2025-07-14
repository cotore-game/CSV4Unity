using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.CompilerServices;

/// <summary>
/// CSVデータをListに格納するためのユーティリティ
/// </summary>
namespace CSV4Unity
{
    public static class CSVLoader
    {
        /// <summary>
        /// Enumを利用してCSVを汎用的に読み込むメソッド。
        /// このメソッドは文字列操作に基づいており、パフォーマンスは 'LoadCSV' に劣ります。
        /// </summary>
        /// <typeparam name="TEnum">Enum型</typeparam>
        /// <param name="csvFile">読み込むCSVファイルを表すTextAsset</param>
        /// <param name="dataName">データ名 (省略時はファイル名)</param>
        /// <returns>読み込まれたデータを格納するオブジェクト</returns>
        /// <exception cref="Exception">CSVファイルの形式が不正な場合にスローされます</exception>
        [Obsolete("Please use CSVLoader.LoadCSVSpan<TEnum>(TextAsset csvFile, string dataName = null) for better performance and future compatibility.", false)]
        public static CsvData<TEnum> LoadCSV<TEnum>(TextAsset csvFile, string dataName = default)
            where TEnum : struct, Enum
        {
            if (csvFile == null)
            {
                throw new ArgumentNullException(nameof(csvFile), "CSV file cannot be null.");
            }

            var result = new CsvData<TEnum>
            {
                DataName = string.IsNullOrEmpty(dataName) ? csvFile.name : dataName
            };

            // Read all lines (including empty lines)
            string[] lines = csvFile.text.Split('\n');

            if (lines.Length < 2)
            {
                throw new Exception("CSV file is empty or missing a header.");
            }

            // Get header
            string headerLine = lines[0];
            string[] headers = headerLine.Split(',');

            // Create a dictionary from header name to column index
            var headerToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                var name = headers[i].Trim();
                if (headerToIndex.ContainsKey(name))
                {
                    throw new Exception($"Duplicate header name found: '{name}'.");
                }
                headerToIndex[name] = i;
            }

            // Create an enum to index map, ensuring all enum members have a corresponding header
            var enumToIndex = new Dictionary<TEnum, int>();
            foreach (TEnum enumVal in Enum.GetValues(typeof(TEnum)))
            {
                string enumName = enumVal.ToString();
                if (!headerToIndex.TryGetValue(enumName, out int idx))
                {
                    throw new Exception($"Enum '{enumName}' not found in CSV header.");
                }
                enumToIndex[enumVal] = idx;
            }

            // Read data rows
            for (int lineNo = 1; lineNo < lines.Length; lineNo++)
            {
                var raw = lines[lineNo];
                if (string.IsNullOrWhiteSpace(raw)) continue; // Skip empty lines

                var values = raw.Split(',');
                if (values.Length != headers.Length)
                {
                    throw new Exception($"Column count mismatch on line {lineNo + 1}: expected {headers.Length}, got {values.Length}.");
                }

                var row = new LineData<TEnum>();

                // Set values using the enum column index map
                foreach (var kv in enumToIndex)
                {
                    TEnum field = kv.Key;
                    int idx = kv.Value;
                    string str = values[idx].Trim();
                    row[field] = ParseValue(str);
                }

                result.Add(row);
            }

            Debug.Log($"Loaded CsvData<{typeof(TEnum).Name}> ({result.Rows.Count} rows) for '{result.DataName}' using LoadCSVLegacy.");
            return result;
        }

        /// <summary>
        /// Enumを利用してCSVを汎用的に読み込む、パフォーマンス重視のメソッド
        /// ReadOnlySpanを使用してメモリ割り当てを最小限に抑えます
        /// </summary>
        /// <typeparam name="TEnum">Enum型</typeparam>
        /// <param name="csvFile">読み込むCSVファイルを表すTextAsset</param>
        /// <param name="dataName">データ名 (省略時はファイル名)</param>
        /// <returns>読み込まれたデータを格納するオブジェクト</returns>
        /// <exception cref="ArgumentNullException">csvFileがnullの場合にスローされます</exception>
        /// <exception cref="Exception">CSVファイルの形式が不正な場合にスローされます</exception>
        public static CsvData<TEnum> LoadCSVSpan<TEnum>(TextAsset csvFile, string dataName = null)
            where TEnum : struct, Enum
        {
            if (csvFile == null) throw new ArgumentNullException(nameof(csvFile), "CSV file cannot be null.");

            var buffer = csvFile.text.AsMemory();
            var span = buffer.Span;
            int pos = 0;
            int length = span.Length;

            // Read header
            int lineEnd = IndexOf(span, '\n', ref pos);
            if (lineEnd < 0)
            {
                // If no newline, assume the whole content is the header (single line CSV)
                lineEnd = length;
            }
            else if (lineEnd == 0) // Handle cases where the file starts with an empty line before header
            {
                // Advance pos to skip the empty line and find the next newline for the actual header
                while (pos < length && span[pos] == '\n') pos++;
                lineEnd = IndexOf(span, '\n', ref pos);
                if (lineEnd < 0) lineEnd = length;
            }

            var headerSpan = Trim(span.Slice(0, lineEnd));
            if (headerSpan.IsEmpty) throw new Exception("Header not found or is empty. Please check CSV format.");

            int enumCount = Enum.GetValues(typeof(TEnum)).Length;
            // Allocate enough space, assuming max possible columns
            var headerPositions = new (int Start, int Length)[enumCount * 2];
            ParseLine(headerSpan, headerPositions, out int headerCount);

            var enumValues = (TEnum[])Enum.GetValues(typeof(TEnum));
            var enumToIndex = new int[enumCount];
            for (int i = 0; i < enumCount; i++)
            {
                var nameSpan = enumValues[i].ToString().AsSpan();
                enumToIndex[i] = FindColumnIndex(nameSpan, headerPositions, headerSpan, headerCount);
            }

            var result = new CsvData<TEnum> { DataName = string.IsNullOrEmpty(dataName) ? csvFile.name : dataName };

            // Loop through data rows
            while (pos < length)
            {
                int start = pos;
                int end = IndexOf(span, '\n', ref pos);
                if (end < 0) end = length; // Last line might not end with a newline

                var lineSpan = Trim(span.Slice(start, end - start));

                // Advance pos beyond the current line's newline character for the next iteration
                if (pos < length && span[pos - 1] == '\n')
                {
                    // Already advanced by IndexOf
                }
                else if (end == length && start < length) // If it's the very last line without a newline
                {
                    pos = length;
                }
                else
                {
                    // Ensure pos advances even if no newline was found by IndexOf, to prevent infinite loop for malformed last line
                    pos++;
                }

                if (lineSpan.IsEmpty) continue;

                var cellPositions = new (int Start, int Length)[headerCount]; // Max headerCount cells per line
                ParseLine(lineSpan, cellPositions, out int cellCount);
                if (cellCount != headerCount)
                {
                    throw new Exception($"Column count mismatch: header has {headerCount} columns, data row has {cellCount} columns.");
                }

                var row = new LineData<TEnum>();
                for (int i = 0; i < enumCount; i++)
                {
                    var posInfo = cellPositions[enumToIndex[i]];
                    var spanVal = lineSpan.Slice(posInfo.Start, posInfo.Length);
                    var trimmed = Trim(spanVal);
                    row[enumValues[i]] = ParseValue(trimmed);
                }

                result.Add(row);
            }

            Debug.Log($"Loaded CsvData<{typeof(TEnum).Name}> ({result.Rows.Count} rows) from '{csvFile.name}'.");
            return result;
        }

        /// <summary>
        /// 文字列スパンを適切な型にパースするヘルパーメソッド
        /// </summary>
        /// <param name="span">パースする文字列スパン</param>
        /// <returns>パースされた値</returns>
        private static object ParseValue(ReadOnlySpan<char> span)
        {
            if (int.TryParse(span, out var i)) return i;
            if (float.TryParse(span, out var f)) return f;
            if (bool.TryParse(span, out var b)) return b;
            return span.ToString(); // Default to string
        }

        /// <summary>
        /// 文字列スパン内で指定された文字を検索し、そのインデックスを返す。
        /// 検索開始位置を 'pos' で管理し、次回の検索のために更新します。
        /// </summary>
        /// <param name="span">検索対象の文字列スパン</param>
        /// <param name="target">検索する文字</param>
        /// <param name="pos">現在の検索位置</param>
        /// <returns>見つかった場合は文字のインデックス、見つからなかった場合は -1</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int IndexOf(ReadOnlySpan<char> span, char target, ref int pos)
        {
            for (int i = pos; i < span.Length; i++)
            {
                if (span[i] == target)
                {
                    pos = i + 1; // Move past the found character for the next search
                    return i;
                }
            }
            pos = span.Length; // If not found, advance pos to the end
            return -1;
        }

        /// <summary>
        /// CSVの1行をカンマで区切り、各セルの開始位置と長さを計算して配列に格納します
        /// </summary>
        /// <param name="line">パースするCSVの行</param>
        /// <param name="positions">各セルの開始位置と長さを格納する配列</param>
        /// <param name="count">パースされたセルの数</param>
        private static void ParseLine(ReadOnlySpan<char> line, (int Start, int Length)[] positions, out int count)
        {
            count = 0;
            int start = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == ',')
                {
                    if (count >= positions.Length)
                    {
                        // This indicates that the line has more columns than anticipated
                        // It's a design choice whether to throw an error or handle it.
                        // For now, let's just break, as the array isn't large enough.
                        // In a real scenario, you might want to resize the array or throw.
                        Debug.LogWarning("Too many columns in line, some columns might be ignored.");
                        break;
                    }
                    positions[count++] = (start, i - start);
                    start = i + 1;
                }
            }
            if (count < positions.Length) // Ensure the last segment is also added
            {
                positions[count++] = (start, line.Length - start);
            }
        }

        /// <summary>
        /// ヘッダーのキーに対応する列のインデックスを検索します
        /// </summary>
        /// <param name="key">検索するヘッダー名</param>
        /// <param name="positions">ヘッダー各セルの位置情報配列</param>
        /// <param name="header">元のヘッダー文字列スパン</param>
        /// <param name="count">ヘッダー内のセルの数</param>
        /// <returns>見つかった列のインデックス</returns>
        /// <exception cref="Exception">指定されたキーがヘッダーに見つからなかった場合にスローされます</exception>
        private static int FindColumnIndex(ReadOnlySpan<char> key, (int Start, int Length)[] positions, ReadOnlySpan<char> header, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var span = header.Slice(positions[i].Start, positions[i].Length);
                if (Trim(span).SequenceEqual(key)) return i; // Trim also for comparison
            }
            throw new Exception($"Header '{key.ToString()}' not found.");
        }

        /// <summary>
        /// 文字列スパンの先頭と末尾の空白文字を削除します
        /// </summary>
        /// <param name="s">トリムする文字列スパン</param>
        /// <returns>トリムされた文字列スパン</returns>
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
