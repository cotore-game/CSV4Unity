using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using CSV4Unity.Validation;
using System.Linq;

namespace CSV4Unity
{
    public static class CSVLoader
    {
        /// <summary>
        /// CSVファイルを読み込み、Enumをキーとした型安全なデータ構造に変換します
        /// </summary>
        /// <typeparam name="TEnum">CSVのヘッダーに対応するEnum型</typeparam>
        /// <param name="csvFile">読み込むTextAsset（CSVファイル）</param>
        /// <param name="options">読み込みオプション。nullの場合はデフォルト設定を使用</param>
        /// <param name="dataName">データの識別名。nullの場合はファイル名を使用</param>
        /// <returns>パースされたCSVデータ</returns>
        /// <exception cref="ArgumentNullException">csvFileがnullの場合</exception>
        /// <exception cref="Exception">CSVの形式が不正な場合、またはバリデーションエラーの場合</exception>
        public static CsvData<TEnum> LoadCSV<TEnum>(TextAsset csvFile, CsvLoaderOptions options = null, string dataName = null)
            where TEnum : struct, Enum
        {
            if (csvFile == null) throw new ArgumentNullException(nameof(csvFile), "CSV file cannot be null.");
            options ??= new CsvLoaderOptions();

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
                int lineEnd = IndexOfLineEnd(span, ref pos);
                if (lineEnd < 0) lineEnd = length;

                headerSpan = span.Slice(0, lineEnd);
                // CR/LFを除去
                headerSpan = TrimLineEnd(headerSpan);

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
            result.Initialize();

            var rowPositions = new List<(int Start, int Length)>();

            // データ行をループ
            while (pos < length)
            {
                int start = pos;
                int lineEnd = IndexOfLineEnd(span, ref pos);
                if (lineEnd < 0) lineEnd = length;

                var lineSpan = span.Slice(start, lineEnd - start);
                // CR/LFを除去
                lineSpan = TrimLineEnd(lineSpan);

                if (options.TrimFields) lineSpan = Trim(lineSpan);

                // 空行とコメント行をスキップ
                if (options.IgnoreEmptyLines && lineSpan.IsEmpty) continue;
                if (!string.IsNullOrEmpty(options.CommentPrefix) && lineSpan.StartsWith(options.CommentPrefix.AsSpan())) continue;

                ParseLine(lineSpan, rowPositions, options.Delimiter);

                if (options.HasHeader && rowPositions.Count != headerPositions.Count)
                {
                    Debug.LogWarning($"Column count mismatch at row {result.RowCount + 1}: header has {headerPositions.Count} columns, data row has {rowPositions.Count} columns.");
                    if (options.MissingFieldPolicy == MissingFieldPolicy.Throw)
                    {
                        throw new Exception($"Column count mismatch: header has {headerPositions.Count} columns, data row has {rowPositions.Count} columns.");
                    }
                }

                var row = new LineData<TEnum>();
                for (int i = 0; i < enumCount; i++)
                {
                    var colIndex = enumToIndex[i];
                    if (colIndex >= rowPositions.Count || colIndex < 0)
                    {
                        if (options.MissingFieldPolicy == MissingFieldPolicy.Throw)
                        {
                            throw new Exception($"Field for enum '{enumValues[i]}' not found in data row.");
                        }
                        else if (options.MissingFieldPolicy == MissingFieldPolicy.SetToDefault)
                        {
                            row[enumValues[i]] = null;
                        }
                        // Ignore の場合は何もしない
                        continue;
                    }

                    var posInfo = rowPositions[colIndex];
                    var spanVal = lineSpan.Slice(posInfo.Start, posInfo.Length);
                    if (options.TrimFields) spanVal = Trim(spanVal);

                    row[enumValues[i]] = ParseValue(spanVal, options);
                }

                result.Add(row);
            }

            Debug.Log($"Loaded CsvData<{typeof(TEnum).Name}> ({result.Rows.Count} rows) from '{csvFile.name}'.");

            // 自動バリデーション
            if (options.ValidationEnabled)
            {
                var validationResult = CsvValidator.Validate(result);

                if (!validationResult.IsValid)
                {
                    var errorSummary = $"CSV Validation failed for '{csvFile.name}':\n";
                    foreach (var error in validationResult.Errors.Take(5))  // 最初の5件のみ表示
                    {
                        errorSummary += $"  - {error}\n";
                    }

                    if (validationResult.Errors.Count > 5)
                    {
                        errorSummary += $"  ... and {validationResult.Errors.Count - 5} more errors\n";
                    }

                    if (options.ThrowOnValidationError)
                    {
                        throw new CsvValidationException($"CSV validation failed with {validationResult.Errors.Count} error(s). See console for details.", validationResult);
                    }
                    else
                    {
                        Debug.LogError(errorSummary);
                    }
                }
                else
                {
                    Debug.Log($"<color=green>✓</color> CSV validation passed for '{csvFile.name}'");
                }
            }

            return result;
        }

        /// <summary>
        /// CSVファイルを読み込み、ヘッダー名またはインデックスでアクセス可能なデータ構造に変換します
        /// </summary>
        /// <param name="csvFile">読み込むTextAsset（CSVファイル）</param>
        /// <param name="options">読み込みオプション。nullの場合はデフォルト設定を使用</param>
        /// <param name="dataName">データの識別名。nullの場合はファイル名を使用</param>
        /// <returns>パースされたCSVデータ</returns>
        /// <exception cref="ArgumentNullException">csvFileがnullの場合</exception>
        /// <exception cref="Exception">CSVの形式が不正な場合</exception>
        public static CsvData LoadCSV(TextAsset csvFile, CsvLoaderOptions options = null, string dataName = null)
        {
            if (csvFile == null) throw new ArgumentNullException(nameof(csvFile), "CSV file cannot be null.");
            options ??= new CsvLoaderOptions();

            var buffer = csvFile.text.AsMemory();
            var span = buffer.Span;
            int pos = 0;
            int length = span.Length;

            var result = new CsvData { DataName = string.IsNullOrEmpty(dataName) ? csvFile.name : dataName };
            var headerNames = new List<string>();
            int columnCount = 0;

            // ヘッダーの読み込みと保存
            if (options.HasHeader)
            {
                int lineEnd = IndexOfLineEnd(span, ref pos);
                if (lineEnd < 0) lineEnd = length;

                var headerSpan = span.Slice(0, lineEnd);
                headerSpan = TrimLineEnd(headerSpan);

                if (options.TrimFields) headerSpan = Trim(headerSpan);

                if (headerSpan.IsEmpty) throw new Exception("Header not found or is empty. Please check CSV format.");

                var headerPositions = new List<(int Start, int Length)>();
                ParseLine(headerSpan, headerPositions, options.Delimiter);

                for (int i = 0; i < headerPositions.Count; i++)
                {
                    var posInfo = headerPositions[i];
                    headerNames.Add(Trim(headerSpan.Slice(posInfo.Start, posInfo.Length)).ToString());
                }

                result.SetHeaders(headerNames);
                columnCount = headerNames.Count;
            }
            else
            {
                // ヘッダーなしの場合、最初の行から列数を判定
                int lineEnd = IndexOfLineEnd(span, ref pos);
                if (lineEnd < 0) lineEnd = length;

                var firstLineSpan = span.Slice(0, lineEnd);
                firstLineSpan = TrimLineEnd(firstLineSpan);
                if (options.TrimFields) firstLineSpan = Trim(firstLineSpan);

                var tempPositions = new List<(int Start, int Length)>();
                ParseLine(firstLineSpan, tempPositions, options.Delimiter);
                columnCount = tempPositions.Count;

                result.SetColumnCount(columnCount);

                // posを最初に戻す（最初の行もデータとして読み込む）
                pos = 0;
            }

            // データ行をループ
            var rowPositions = new List<(int Start, int Length)>();
            while (pos < length)
            {
                int start = pos;
                int lineEnd = IndexOfLineEnd(span, ref pos);
                if (lineEnd < 0) lineEnd = length;

                var lineSpan = span.Slice(start, lineEnd - start);
                lineSpan = TrimLineEnd(lineSpan);

                if (options.TrimFields) lineSpan = Trim(lineSpan);

                if (options.IgnoreEmptyLines && lineSpan.IsEmpty) continue;
                if (!string.IsNullOrEmpty(options.CommentPrefix) && lineSpan.StartsWith(options.CommentPrefix.AsSpan())) continue;

                ParseLine(lineSpan, rowPositions, options.Delimiter);

                // 列数チェック
                if (rowPositions.Count != columnCount)
                {
                    Debug.LogWarning($"Column count mismatch at row {result.RowCount + 1}: expected {columnCount} columns, got {rowPositions.Count} columns.");
                    if (options.MissingFieldPolicy == MissingFieldPolicy.Throw)
                    {
                        throw new Exception($"Column count mismatch: expected {columnCount} columns, data row has {rowPositions.Count} columns.");
                    }
                }

                var row = new LineData();

                if (options.HasHeader)
                {
                    // ヘッダーありの場合
                    for (int i = 0; i < rowPositions.Count && i < headerNames.Count; i++)
                    {
                        var posInfo = rowPositions[i];
                        var spanVal = lineSpan.Slice(posInfo.Start, posInfo.Length);
                        if (options.TrimFields) spanVal = Trim(spanVal);
                        row.Add(headerNames[i], ParseValue(spanVal, options), i);
                    }
                }
                else
                {
                    // ヘッダーなしの場合（インデックスベース）
                    for (int i = 0; i < rowPositions.Count; i++)
                    {
                        var posInfo = rowPositions[i];
                        var spanVal = lineSpan.Slice(posInfo.Start, posInfo.Length);
                        if (options.TrimFields) spanVal = Trim(spanVal);
                        row.AddByIndex(i, ParseValue(spanVal, options));
                    }
                }

                result.Add(row);
            }

            Debug.Log($"Loaded CsvData ({result.Rows.Count} rows, {columnCount} columns) from '{csvFile.name}'.");

            // 非ジェネリック版では自動バリデーションは行わない（Enumが必要なため）

            return result;
        }

        /// <summary>
        /// 値を適切な型にパースします（精度を保つように改善）
        /// </summary>
        private static object ParseValue(ReadOnlySpan<char> span, CsvLoaderOptions options)
        {
            if (span.IsEmpty) return null;

            // bool解析を最初に試行（true/falseを優先）
            if (bool.TryParse(span, out var b)) return b;

            // 小数点が含まれているかチェック
            bool hasDecimalPoint = false;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == '.')
                {
                    hasDecimalPoint = true;
                    break;
                }
            }

            // 小数点がない場合のみint解析を試行（精度損失を防ぐ）
            if (!hasDecimalPoint)
            {
                if (int.TryParse(span, System.Globalization.NumberStyles.Integer, options.FormatProvider, out var i))
                {
                    return i;
                }
                // intでオーバーフローする場合はlong解析も試行
                if (long.TryParse(span, System.Globalization.NumberStyles.Integer, options.FormatProvider, out var l))
                {
                    return l;
                }
            }

            // 小数点がある、またはint/long解析に失敗した場合はfloat解析
            if (float.TryParse(span, System.Globalization.NumberStyles.Float, options.FormatProvider, out var f))
            {
                return f;
            }

            // すべて失敗した場合は文字列として返す
            return span.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int IndexOfLineEnd(ReadOnlySpan<char> span, ref int pos)
        {
            for (int i = pos; i < span.Length; i++)
            {
                if (span[i] == '\n' || span[i] == '\r')
                {
                    // \r\n または \n の処理
                    if (span[i] == '\r' && i + 1 < span.Length && span[i + 1] == '\n')
                    {
                        pos = i + 2;
                    }
                    else
                    {
                        pos = i + 1;
                    }
                    return i;
                }
            }
            pos = span.Length;
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<char> TrimLineEnd(ReadOnlySpan<char> span)
        {
            int end = span.Length - 1;
            while (end >= 0 && (span[end] == '\r' || span[end] == '\n'))
            {
                end--;
            }
            return span.Slice(0, end + 1);
        }

        /// <summary>
        /// 1行を解析してフィールド位置のリストを作成します（クォート処理を改善）
        /// </summary>
        private static void ParseLine(ReadOnlySpan<char> line, List<(int Start, int Length)> positions, char delimiter)
        {
            positions.Clear();

            if (line.IsEmpty)
            {
                return;
            }

            int start = 0;
            bool inQuote = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    // フィールドの先頭でクォートが始まる
                    if (!inQuote && i == start)
                    {
                        inQuote = true;
                        start = i + 1; // クォート自体をスキップ
                    }
                    // クォート内で二重クォート（エスケープ）
                    else if (inQuote && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        i++; // 次のクォートもスキップ
                    }
                    // クォートが閉じる
                    else if (inQuote)
                    {
                        // フィールド値を追加（クォート内の内容）
                        positions.Add((start, i - start));
                        inQuote = false;

                        // 【修正】クォート閉じ後の処理を簡潔化
                        // 次の文字がデリミタか行末かチェック
                        i++; // 閉じクォートの次へ

                        // デリミタまたは行末まで進む
                        while (i < line.Length && line[i] != delimiter)
                        {
                            i++;
                        }

                        // デリミタが見つかった場合、次のフィールドの開始位置を設定
                        if (i < line.Length && line[i] == delimiter)
                        {
                            start = i + 1;
                        }
                        else
                        {
                            // 行末に到達
                            start = line.Length;
                            i--; // forループのi++で調整されるため
                        }
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

            // 最後のフィールドを追加
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
            return start <= end ? s.Slice(start, end - start + 1) : ReadOnlySpan<char>.Empty;
        }
    }

    /// <summary>
    /// CSVバリデーションエラー専用例外
    /// </summary>
    public class CsvValidationException : Exception
    {
        public CsvValidationResult ValidationResult { get; }

        public CsvValidationException(string message, CsvValidationResult validationResult)
            : base(message)
        {
            ValidationResult = validationResult;
        }
    }
}
