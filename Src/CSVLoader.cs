using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using CSV4Unity.Validation;
using System.Linq;
using System.Text;

namespace CSV4Unity
{
    /// <summary>
    /// CSVファイルの読み込みとパース機能を提供する静的クラス
    /// </summary>
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

            List<FieldInfo> headerFields = new();
            int enumCount = Enum.GetValues(typeof(TEnum)).Length;
            var enumValues = (TEnum[])Enum.GetValues(typeof(TEnum));
            var enumToIndex = new int[enumCount];
            ReadOnlySpan<char> headerSpan = ReadOnlySpan<char>.Empty;

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

                ParseLine(headerSpan, headerFields, options.Delimiter);

                for (int i = 0; i < enumCount; i++)
                {
                    var nameSpan = enumValues[i].ToString().AsSpan();
                    enumToIndex[i] = FindColumnIndex(nameSpan, headerFields, headerSpan);
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

            var rowFields = new List<FieldInfo>();

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

                ParseLine(lineSpan, rowFields, options.Delimiter);

                if (options.HasHeader && rowFields.Count != headerFields.Count)
                {
                    Debug.LogWarning($"Column count mismatch at row {result.RowCount + 1}: header has {headerFields.Count} columns, data row has {rowFields.Count} columns.");
                    if (options.MissingFieldPolicy == MissingFieldPolicy.Throw)
                    {
                        throw new Exception($"Column count mismatch: header has {headerFields.Count} columns, data row has {rowFields.Count} columns.");
                    }
                }

                var row = new LineData<TEnum>();
                for (int i = 0; i < enumCount; i++)
                {
                    var colIndex = enumToIndex[i];
                    if (colIndex >= rowFields.Count || colIndex < 0)
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

                    var fieldInfo = rowFields[colIndex];

                    // エスケープ済み文字列がある場合はそれを使用、なければSpanから取得
                    ReadOnlySpan<char> spanVal;
                    if (fieldInfo.UnescapedString != null)
                    {
                        spanVal = fieldInfo.UnescapedString.AsSpan();
                    }
                    else
                    {
                        spanVal = lineSpan.Slice(fieldInfo.Start, fieldInfo.Length);
                    }

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
                    // 最大表示数までエラーを列挙
                    foreach (var error in validationResult.Errors.Take(options.MAX_DISPLAYED_ERRORS))
                    {
                        errorSummary += $"  - {error}\n";
                    }

                    if (validationResult.Errors.Count > options.MAX_DISPLAYED_ERRORS)
                    {
                        errorSummary += $"  ... and {validationResult.Errors.Count - options.MAX_DISPLAYED_ERRORS} more errors\n";
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

                var headerFields = new List<FieldInfo>();
                ParseLine(headerSpan, headerFields, options.Delimiter);

                for (int i = 0; i < headerFields.Count; i++)
                {
                    var fieldInfo = headerFields[i];

                    // エスケープ済み文字列がある場合はそれを使用、なければSpanから取得
                    ReadOnlySpan<char> fieldValue;
                    if (fieldInfo.UnescapedString != null)
                    {
                        fieldValue = fieldInfo.UnescapedString.AsSpan();
                    }
                    else
                    {
                        fieldValue = headerSpan.Slice(fieldInfo.Start, fieldInfo.Length);
                    }

                    headerNames.Add(Trim(fieldValue).ToString());
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

                var tempFields = new List<FieldInfo>();
                ParseLine(firstLineSpan, tempFields, options.Delimiter);
                columnCount = tempFields.Count;

                result.SetColumnCount(columnCount);

                // posを最初に戻す（最初の行もデータとして読み込む）
                pos = 0;
            }

            // データ行をループ
            var rowFields = new List<FieldInfo>();
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

                ParseLine(lineSpan, rowFields, options.Delimiter);

                // 列数チェック
                if (rowFields.Count != columnCount)
                {
                    Debug.LogWarning($"Column count mismatch at row {result.RowCount + 1}: expected {columnCount} columns, got {rowFields.Count} columns.");
                    if (options.MissingFieldPolicy == MissingFieldPolicy.Throw)
                    {
                        throw new Exception($"Column count mismatch: expected {columnCount} columns, data row has {rowFields.Count} columns.");
                    }
                }

                var row = new LineData();

                if (options.HasHeader)
                {
                    // ヘッダーありの場合
                    for (int i = 0; i < rowFields.Count && i < headerNames.Count; i++)
                    {
                        var fieldInfo = rowFields[i];

                        // エスケープ済み文字列がある場合はそれを使用、なければSpanから取得
                        ReadOnlySpan<char> spanVal;
                        if (fieldInfo.UnescapedString != null)
                        {
                            spanVal = fieldInfo.UnescapedString.AsSpan();
                        }
                        else
                        {
                            spanVal = lineSpan.Slice(fieldInfo.Start, fieldInfo.Length);
                        }

                        if (options.TrimFields) spanVal = Trim(spanVal);
                        row.Add(headerNames[i], ParseValue(spanVal, options), i);
                    }
                }
                else
                {
                    // ヘッダーなしの場合（インデックスベース）
                    for (int i = 0; i < rowFields.Count; i++)
                    {
                        var fieldInfo = rowFields[i];

                        // エスケープ済み文字列がある場合はそれを使用、なければSpanから取得
                        ReadOnlySpan<char> spanVal;
                        if (fieldInfo.UnescapedString != null)
                        {
                            spanVal = fieldInfo.UnescapedString.AsSpan();
                        }
                        else
                        {
                            spanVal = lineSpan.Slice(fieldInfo.Start, fieldInfo.Length);
                        }

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
        /// 値を適切な型にパースします
        /// 型の優先順位: bool → int → long → float → string
        /// </summary>
        /// <param name="span">パース対象の文字列スパン</param>
        /// <param name="options">ローダーオプション（数値解析用のFormatProviderを含む）</param>
        /// <returns>パースされた値。空の場合はnull</returns>
        private static object ParseValue(ReadOnlySpan<char> span, CsvLoaderOptions options)
        {
            if (span.IsEmpty) return null;

            // bool解析を最初に試行（true/falseを優先）
            if (bool.TryParse(span, out var b)) return b;

            // 小数点が含まれているかチェック（精度損失を防ぐため）
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

        /// <summary>
        /// 行末の位置を検索し、位置参照を更新します
        /// </summary>
        /// <param name="span">検索対象のスパン</param>
        /// <param name="pos">現在位置（参照渡し）。行末の次の位置に更新されます</param>
        /// <returns>行末の位置。見つからない場合は-1</returns>
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

        /// <summary>
        /// スパンの末尾から改行文字を除去します
        /// </summary>
        /// <param name="span">トリム対象のスパン</param>
        /// <returns>改行文字を除去したスパン</returns>
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
        /// フィールド情報を保持する構造体
        /// エスケープ処理済みの値を効率的に管理します
        /// </summary>
        private struct FieldInfo
        {
            /// <summary>
            /// フィールドの開始位置
            /// </summary>
            public readonly int Start;

            /// <summary>
            /// フィールドの長さ
            /// </summary>
            public readonly int Length;

            /// <summary>
            /// エスケープ処理済みの文字列（エスケープが不要な場合はnull）
            /// </summary>
            public readonly string UnescapedString;

            public FieldInfo(int start, int length, string unescaped = null)
            {
                Start = start;
                Length = length;
                UnescapedString = unescaped;
            }
        }

        /// <summary>
        /// 1行を解析してフィールド情報のリストを作成します
        /// RFC 4180準拠: クォートで囲まれたフィールド内の二重クォート（""）を単一クォート（"）にエスケープ処理します
        /// </summary>
        /// <param name="line">パース対象の行</param>
        /// <param name="fields">パース結果を格納するフィールド情報リスト</param>
        /// <param name="delimiter">フィールド区切り文字</param>
        /// <exception cref="Exception">クォートが閉じられていない場合</exception>
        private static void ParseLine(ReadOnlySpan<char> line, List<FieldInfo> fields, char delimiter)
        {
            fields.Clear();

            if (line.IsEmpty)
            {
                return;
            }

            int start = 0;
            bool inQuote = false;
            StringBuilder unescapeBuilder = null;

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
                        // 初めてエスケープに遭遇したらStringBuilderを作成
                        if (unescapeBuilder == null)
                        {
                            unescapeBuilder = new StringBuilder();
                            // これまでの内容を追加
                            unescapeBuilder.Append(line.Slice(start, i - start));
                        }

                        // エスケープされた"を1つの"として追加
                        unescapeBuilder.Append('"');
                        i++; // 2つ目の"をスキップ
                    }
                    // クォートが閉じる
                    else if (inQuote)
                    {
                        string unescapedString = null;

                        if (unescapeBuilder != null && unescapeBuilder.Length > 0)
                        {
                            // 閉じクォートまでの残りを追加
                            if (i > start)
                            {
                                unescapeBuilder.Append(line.Slice(start, i - start));
                            }
                            unescapedString = unescapeBuilder.ToString();
                            unescapeBuilder.Clear();
                        }

                        fields.Add(new FieldInfo(start, i - start, unescapedString));
                        inQuote = false;

                        // クォート閉じ後のデリミタまでスキップ
                        i++;
                        while (i < line.Length && line[i] != delimiter)
                        {
                            i++;
                        }

                        if (i < line.Length && line[i] == delimiter)
                        {
                            start = i + 1;
                        }
                        else
                        {
                            start = line.Length;
                            i--;
                        }
                    }
                }
                else if (c == delimiter && !inQuote)
                {
                    fields.Add(new FieldInfo(start, i - start));
                    start = i + 1;
                }
                else if (inQuote && unescapeBuilder != null)
                {
                    // エスケープ処理中は1文字ずつ追加
                    unescapeBuilder.Append(c);
                }
            }

            if (inQuote)
            {
                throw new Exception("Unclosed quote in line.");
            }

            // 最後のフィールドを追加
            if (start <= line.Length)
            {
                fields.Add(new FieldInfo(start, line.Length - start));
            }
        }

        /// <summary>
        /// フィールド情報リストから指定されたキーに一致する列インデックスを検索します
        /// </summary>
        /// <param name="key">検索するキー</param>
        /// <param name="fields">検索対象のフィールド情報リスト</param>
        /// <param name="line">元の行データ（Spanを取得するため）</param>
        /// <returns>一致した列のインデックス</returns>
        /// <exception cref="Exception">キーが見つからない場合</exception>
        private static int FindColumnIndex(ReadOnlySpan<char> key, List<FieldInfo> fields, ReadOnlySpan<char> line)
        {
            for (int i = 0; i < fields.Count; i++)
            {
                var fieldInfo = fields[i];

                // エスケープ済み文字列がある場合はそれを使用、なければSpanから取得
                ReadOnlySpan<char> span;
                if (fieldInfo.UnescapedString != null)
                {
                    span = fieldInfo.UnescapedString.AsSpan();
                }
                else
                {
                    span = line.Slice(fieldInfo.Start, fieldInfo.Length);
                }

                if (Trim(span).SequenceEqual(key)) return i;
            }
            throw new Exception($"Header '{key.ToString()}' not found.");
        }

        /// <summary>
        /// スパンの前後の空白文字を除去します
        /// </summary>
        /// <param name="s">トリム対象のスパン</param>
        /// <returns>空白文字を除去したスパン</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<char> Trim(ReadOnlySpan<char> s)
        {
            // 空のスパンは早期リターン
            if (s.IsEmpty) return ReadOnlySpan<char>.Empty;

            int start = 0, end = s.Length - 1;
            while (start <= end && char.IsWhiteSpace(s[start])) start++;
            while (end >= start && char.IsWhiteSpace(s[end])) end--;
            return start <= end ? s.Slice(start, end - start + 1) : ReadOnlySpan<char>.Empty;
        }
    }

    /// <summary>
    /// CSVバリデーションエラー専用例外
    /// バリデーション結果の詳細情報を保持します
    /// </summary>
    public class CsvValidationException : Exception
    {
        /// <summary>
        /// バリデーション結果の詳細情報
        /// </summary>
        public CsvValidationResult ValidationResult { get; }

        /// <summary>
        /// CSVバリデーション例外を初期化します
        /// </summary>
        /// <param name="message">エラーメッセージ</param>
        /// <param name="validationResult">バリデーション結果</param>
        public CsvValidationException(string message, CsvValidationResult validationResult)
            : base(message)
        {
            ValidationResult = validationResult;
        }
    }
}
