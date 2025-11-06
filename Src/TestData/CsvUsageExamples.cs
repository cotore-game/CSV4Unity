using UnityEngine;
using CSV4Unity;
using CSV4Unity.Fields;
using System.Linq;

namespace CSV4Unity.Examples
{
    /// <summary>
    /// CSV4Unityライブラリの実践的な使用例
    /// このスクリプトをコピー＆ペーストして、自分のプロジェクトで活用できます
    /// </summary>
    public class CsvUsageExamples : MonoBehaviour
    {
        [Header("CSV Files")]
        [Tooltip("シナリオデータのCSVファイル")]
        public TextAsset scenarioCsv;

        [Tooltip("大量データのCSVファイル（パフォーマンステスト用）")]
        public TextAsset hugeDataCsv;

        [Tooltip("ヘッダーなしのCSVファイル（オプション）")]
        public TextAsset headerlessCsv;

        void Start()
        {
            Debug.Log("<color=cyan>========== CSV4Unity 使用例 ==========</color>");

            // 基本的な使い方
            Example1_BasicEnumBased();

            // 列単位アクセス（高速処理）
            Example2_ColumnAccess();

            // 検索とフィルタリング
            Example3_QueryAndFilter();

            // インデックスを使った高速検索
            Example4_IndexedSearch();

            // 非ジェネリック版（ヘッダー名でアクセス）
            Example5_NonGenericVersion();

            // ヘッダーなしCSV（インデックスベース）
            if (headerlessCsv != null)
            {
                Example6_HeaderlessCSV();
            }

            // パフォーマンス比較
            Example7_PerformanceComparison();

            Debug.Log("<color=cyan>========================================</color>");
        }

        /// <summary>
        /// 例1: 基本的な使い方（Enumベース）
        /// </summary>
        void Example1_BasicEnumBased()
        {
            Debug.Log("\n<color=yellow>=== 例1: 基本的な使い方 ===</color>");

            if (scenarioCsv == null)
            {
                Debug.LogWarning("scenarioCsv が設定されていません");
                return;
            }

            // CSVを読み込み（Enumを使った型安全なアクセス）
            var scenarioData = CSVLoader.LoadCSV<ScenarioFields>(scenarioCsv);

            Debug.Log($"読み込んだ行数: {scenarioData.RowCount}");
            Debug.Log($"列数: {scenarioData.ColumnCount}");

            // 最初の5行を表示
            int displayCount = Mathf.Min(5, scenarioData.RowCount);
            for (int i = 0; i < displayCount; i++)
            {
                var row = scenarioData.Rows[i];

                // 基本的な取得方法（nullや空文字列に注意）
                string command = row.GetOrDefault<string>(ScenarioFields.Command, "");
                string text = row.GetOrDefault<string>(ScenarioFields.Text, "");
                string arg1 = row.GetOrDefault<string>(ScenarioFields.Arg1, "");

                if (!string.IsNullOrEmpty(command))
                {
                    Debug.Log($"行{i + 1}: [{command}] Arg1={arg1}, Text={text}");
                }
            }

            // より安全な取得方法（TryGet）
            var firstRow = scenarioData.Rows[0];
            if (firstRow.TryGet<string>(ScenarioFields.Voice, out string voice) && !string.IsNullOrEmpty(voice))
            {
                Debug.Log($"Voice: {voice}");
            }

            // フィールドの存在確認
            if (firstRow.HasField(ScenarioFields.WindowType))
            {
                string windowType = firstRow.GetOrDefault<string>(ScenarioFields.WindowType, "Normal");
                Debug.Log($"WindowType: {windowType}");
            }
        }

        /// <summary>
        /// 例2: 列単位でのアクセス（統計処理に便利）
        /// </summary>
        void Example2_ColumnAccess()
        {
            Debug.Log("\n<color=yellow>=== 例2: 列単位アクセス ===</color>");

            if (scenarioCsv == null) return;

            var scenarioData = CSVLoader.LoadCSV<ScenarioFields>(scenarioCsv);

            // 列全体を一度に取得（メモリ効率的）
            var allCommands = scenarioData.GetColumn<string>(ScenarioFields.Command);
            var allTexts = scenarioData.GetColumn<string>(ScenarioFields.Text);

            Debug.Log($"全コマンド数: {allCommands.Count}");

            // コマンドの種類をカウント（nullと空文字列を除外）
            var commandTypes = allCommands
                .Where(cmd => !string.IsNullOrEmpty(cmd))
                .Distinct()
                .ToList();

            Debug.Log($"コマンド種類: {commandTypes.Count}種類");
            Debug.Log($"コマンド一覧: {string.Join(", ", commandTypes)}");

            // テキストが空でない行をカウント
            int textCount = allTexts.Count(t => !string.IsNullOrEmpty(t));
            Debug.Log($"テキストあり: {textCount}行 / {allTexts.Count}行");

            // 列データを直接処理（高速）
            Debug.Log("\n最初の5つのコマンド:");
            for (int i = 0; i < Mathf.Min(5, allCommands.Count); i++)
            {
                string cmd = allCommands[i] as string ?? "";
                string txt = allTexts[i] as string ?? "";
                if (!string.IsNullOrEmpty(cmd))
                {
                    Debug.Log($"  [{i + 1}] {cmd}: {txt}");
                }
            }
        }

        /// <summary>
        /// 例3: クエリとフィルタリング
        /// </summary>
        void Example3_QueryAndFilter()
        {
            Debug.Log("\n<color=yellow>=== 例3: クエリとフィルタリング ===</color>");

            if (scenarioCsv == null) return;

            var scenarioData = CSVLoader.LoadCSV<ScenarioFields>(scenarioCsv);

            // 条件に一致する行を検索（nullチェック付き）
            var textCommands = scenarioData.Where(row =>
            {
                var cmd = row.GetOrDefault<string>(ScenarioFields.Command, "");
                return cmd == "Text";
            });

            Debug.Log($"Textコマンド: {textCommands.Count()}件");

            // 最初の一致を検索
            var firstText = scenarioData.FindFirst(ScenarioFields.Command, "Text");
            if (firstText != null)
            {
                string text = firstText.GetOrDefault<string>(ScenarioFields.Text, "");
                Debug.Log($"最初のText: {text}");
            }

            // すべての一致を検索
            var allTexts = scenarioData.FindAll(ScenarioFields.Command, "Text");
            Debug.Log($"全Textコマンド: {allTexts.Count()}件");

            // 最初の3件を表示
            Debug.Log("\n最初の3件のTextコマンド:");
            foreach (var txt in allTexts.Take(3))
            {
                string content = txt.GetOrDefault<string>(ScenarioFields.Text, "");
                Debug.Log($"  - {content}");
            }

            // グループ化（nullや空文字列も含まれる）
            var groupedByCommand = scenarioData.GroupBy(ScenarioFields.Command)
                .Where(g => g.Key != null && !string.IsNullOrEmpty(g.Key.ToString()));

            Debug.Log("\nコマンド別の件数:");
            foreach (var group in groupedByCommand)
            {
                Debug.Log($"  {group.Key}: {group.Count()}件");
            }
        }

        /// <summary>
        /// 例4: インデックスを使った高速検索
        /// </summary>
        void Example4_IndexedSearch()
        {
            Debug.Log("\n<color=yellow>=== 例4: インデックス検索 ===</color>");

            if (scenarioCsv == null) return;

            var scenarioData = CSVLoader.LoadCSV<ScenarioFields>(scenarioCsv);

            // Commandでの検索用インデックスを作成
            // 注意: CreateIndexは自動的にnullと空文字列をスキップします
            var commandIndex = scenarioData.CreateIndex(ScenarioFields.Command);

            Debug.Log($"インデックス作成完了: {commandIndex.Count}種類のコマンド");

            // インデックスを使って高速検索
            if (commandIndex.TryGetValue("Text", out var textIndices))
            {
                Debug.Log($"Textコマンド: {textIndices.Count}件（インデックス検索）");

                // 最初の3件を表示
                Debug.Log("\n最初の3件のTextコマンド:");
                foreach (var idx in textIndices.Take(3))
                {
                    var row = scenarioData.Rows[idx];
                    string text = row.GetOrDefault<string>(ScenarioFields.Text, "");
                    Debug.Log($"  行{idx + 1}: {text}");
                }
            }

            // Bgコマンドの検索
            if (commandIndex.TryGetValue("Bg", out var bgIndices))
            {
                Debug.Log($"\nBgコマンド: {bgIndices.Count}件");
                foreach (var idx in bgIndices.Take(3))
                {
                    var row = scenarioData.Rows[idx];
                    string arg1 = row.GetOrDefault<string>(ScenarioFields.Arg1, "");
                    Debug.Log($"  背景: {arg1}");
                }
            }

            // WaitTypeでのグループ検索（空文字列を除外）
            var waitTypeIndex = scenarioData.CreateIndex(ScenarioFields.WaitType);
            if (waitTypeIndex.Count > 0)
            {
                Debug.Log($"\nWaitType別の件数:");
                foreach (var kvp in waitTypeIndex)
                {
                    Debug.Log($"  '{kvp.Key}': {kvp.Value.Count}件");
                }
            }
            else
            {
                Debug.Log("\nWaitType: データなし（すべて空）");
            }
        }

        /// <summary>
        /// 例5: 非ジェネリック版（Enumなしでヘッダー名で直接アクセス）
        /// </summary>
        void Example5_NonGenericVersion()
        {
            Debug.Log("\n<color=yellow>=== 例5: 非ジェネリック版 ===</color>");

            if (scenarioCsv == null) return;

            // Enumを定義せずに使える
            var data = CSVLoader.LoadCSV(scenarioCsv);

            Debug.Log($"ヘッダー: {string.Join(", ", data.Headers)}");
            Debug.Log($"行数: {data.RowCount}, 列数: {data.ColumnCount}");

            // ヘッダー名で直接アクセス（文字列ベース、大文字小文字を区別しない）
            Debug.Log("\n最初の5行:");
            for (int i = 0; i < Mathf.Min(5, data.RowCount); i++)
            {
                var row = data.Rows[i];

                string command = row.GetOrDefault<string>("Command", "");
                string text = row.GetOrDefault<string>("Text", "");

                if (!string.IsNullOrEmpty(command))
                {
                    Debug.Log($"行{i + 1}: {command} - {text}");
                }
            }

            // 列アクセスも可能
            var allCommands = data.GetColumn<string>("Command");
            var validCommands = allCommands.Count(c => !string.IsNullOrEmpty(c as string));
            Debug.Log($"\nコマンド総数: {validCommands}");

            // グループ化
            var commandGroups = data.GroupBy("Command")
                .Where(g => g.Key != null && !string.IsNullOrEmpty(g.Key.ToString()));

            Debug.Log("\nコマンド別件数:");
            foreach (var group in commandGroups)
            {
                Debug.Log($"  {group.Key}: {group.Count()}件");
            }

            // 検索
            var firstText = data.FindFirst("Command", "Text");
            if (firstText != null)
            {
                string text = firstText.GetOrDefault<string>("Text", "");
                Debug.Log($"\n最初のText: {text}");
            }
        }

        /// <summary>
        /// 例6: ヘッダーなしCSV（インデックスベースの高速アクセス）
        /// </summary>
        void Example6_HeaderlessCSV()
        {
            Debug.Log("\n<color=yellow>=== 例6: ヘッダーなしCSV ===</color>");

            if (headerlessCsv == null)
            {
                Debug.LogWarning("headerlessCsv が設定されていません");
                return;
            }

            // ヘッダーなしでロード
            var options = new CsvLoaderOptions
            {
                HasHeader = false,
                Delimiter = ',',
                TrimFields = true,
                ValidationEnabled = false  // ヘッダーなしの場合はバリデーション無効
            };

            var data = CSVLoader.LoadCSV(headerlessCsv, options);

            Debug.Log($"読み込み完了: {data.RowCount}行 × {data.ColumnCount}列");

            // インデックスで行データにアクセス
            Debug.Log("\n最初の3行:");
            for (int i = 0; i < Mathf.Min(3, data.RowCount); i++)
            {
                var row = data.Rows[i];

                // 列インデックスで取得（0始まり）
                var col0 = row.GetOrDefault<string>(0, "");
                var col1 = row.GetOrDefault<string>(1, "");
                var col2 = row.GetOrDefault<string>(2, "");

                Debug.Log($"行{i + 1}: [{col0}] [{col1}] [{col2}]");
            }

            // 列全体をインデックスで取得
            if (data.ColumnCount > 0)
            {
                var firstColumn = data.GetColumnByIndex<string>(0);
                var validCount = firstColumn.Count(v => !string.IsNullOrEmpty(v as string));
                Debug.Log($"\n0列目の有効データ: {validCount}件");
            }

            // インデックスで検索
            if (data.RowCount > 0 && data.ColumnCount > 0)
            {
                var firstValue = data.Rows[0].GetOrDefault<string>(0, "");
                if (!string.IsNullOrEmpty(firstValue))
                {
                    var found = data.FindFirstByIndex(0, firstValue);
                    if (found != null)
                    {
                        Debug.Log($"検索成功: 0列目='{firstValue}' の行を発見");
                    }
                }
            }

            // インデックスでグループ化
            if (data.ColumnCount > 0)
            {
                var groups = data.GroupByIndex(0)
                    .Where(g => g.Key != null && !string.IsNullOrEmpty(g.Key.ToString()));
                Debug.Log($"\n0列目でグループ化: {groups.Count()}グループ");
            }
        }

        /// <summary>
        /// 例7: パフォーマンス比較
        /// </summary>
        void Example7_PerformanceComparison()
        {
            Debug.Log("\n<color=yellow>=== 例7: パフォーマンス比較 ===</color>");

            if (hugeDataCsv == null)
            {
                Debug.LogWarning("hugeDataCsv が設定されていません（パフォーマンステストをスキップ）");
                return;
            }

            var hugeData = CSVLoader.LoadCSV<HugeDataFields>(hugeDataCsv);

            Debug.Log($"データサイズ: {hugeData.RowCount}行 × {hugeData.ColumnCount}列");

            // 方法1: 行ごとにアクセス（従来の方法）
            var sw1 = System.Diagnostics.Stopwatch.StartNew();
            int count1 = 0;
            foreach (var row in hugeData.Rows)
            {
                var value = row.GetOrDefault<string>(HugeDataFields.a, "");
                if (!string.IsNullOrEmpty(value))
                {
                    count1++;
                }
            }
            sw1.Stop();

            // 方法2: 列アクセス（推奨、高速）
            var sw2 = System.Diagnostics.Stopwatch.StartNew();
            var columnA = hugeData.GetColumn<string>(HugeDataFields.a);
            int count2 = 0;
            for (int i = 0; i < columnA.Count; i++)
            {
                var value = columnA[i] as string;
                if (!string.IsNullOrEmpty(value))
                {
                    count2++;
                }
            }
            sw2.Stop();

            Debug.Log($"<color=lime>行ベースアクセス: {sw1.ElapsedMilliseconds}ms (結果: {count1})</color>");
            Debug.Log($"<color=lime>列ベースアクセス: {sw2.ElapsedMilliseconds}ms (結果: {count2})</color>");

            if (sw1.ElapsedTicks > 0 && sw2.ElapsedTicks > 0)
            {
                float speedup = (float)sw1.ElapsedTicks / sw2.ElapsedTicks;
                Debug.Log($"<color=cyan>列アクセスは約 {speedup:F2}倍 高速です！</color>");
            }

            // 方法3: LINQ集計（簡潔だが遅い）
            var sw3 = System.Diagnostics.Stopwatch.StartNew();
            int count3 = hugeData.Where(row =>
                !string.IsNullOrEmpty(row.GetOrDefault<string>(HugeDataFields.a, ""))
            ).Count();
            sw3.Stop();

            Debug.Log($"LINQ集計: {sw3.ElapsedMilliseconds}ms (結果: {count3})");

            // ヘッダーなしCSVのパフォーマンステスト
            if (headerlessCsv != null)
            {
                var options = new CsvLoaderOptions { HasHeader = false, ValidationEnabled = false };
                var headerlessData = CSVLoader.LoadCSV(headerlessCsv, options);

                if (headerlessData.ColumnCount > 0)
                {
                    var sw4 = System.Diagnostics.Stopwatch.StartNew();
                    var col0 = headerlessData.GetColumnByIndex<string>(0);
                    int count4 = 0;
                    for (int i = 0; i < col0.Count; i++)
                    {
                        var value = col0[i] as string;
                        if (!string.IsNullOrEmpty(value))
                        {
                            count4++;
                        }
                    }
                    sw4.Stop();

                    Debug.Log($"ヘッダーなしインデックスアクセス: {sw4.ElapsedMilliseconds}ms (結果: {count4})");
                    Debug.Log("（インデックスアクセスはEnumアクセスと同等の性能を持ちます）");
                }
            }
        }

        /// <summary>
        /// 実践例: シナリオシステムでの使用
        /// </summary>
        [ContextMenu("実践例: シナリオデータの処理")]
        void PracticalExample_ScenarioProcessing()
        {
            Debug.Log("\n<color=cyan>=== 実践例: シナリオデータの処理 ===</color>");

            if (scenarioCsv == null) return;

            var scenarioData = CSVLoader.LoadCSV<ScenarioFields>(scenarioCsv);

            // シナリオの統計情報を取得（nullと空文字列を除外）
            var commands = scenarioData.GetColumn<string>(ScenarioFields.Command);
            var texts = scenarioData.GetColumn<string>(ScenarioFields.Text);
            var voices = scenarioData.GetColumn<string>(ScenarioFields.Voice);

            int textCommandCount = commands.Count(c => c as string == "Text");
            int voiceCount = voices.Count(v => !string.IsNullOrEmpty(v as string));
            int totalTextLength = texts
                .Where(t => !string.IsNullOrEmpty(t as string))
                .Sum(t => (t as string)?.Length ?? 0);

            Debug.Log($"Textコマンド数: {textCommandCount}");
            Debug.Log($"ボイス付き: {voiceCount}");
            Debug.Log($"総文字数: {totalTextLength}");

            // 特定のコマンドでフィルタリング
            var bgCommands = scenarioData.FindAll(ScenarioFields.Command, "Bg");
            Debug.Log($"背景変更: {bgCommands.Count()}回");

            // インデックスを使った高速アクセス（頻繁に検索する場合）
            var commandIndex = scenarioData.CreateIndex(ScenarioFields.Command);

            Debug.Log("\nコマンド別の統計:");
            foreach (var kvp in commandIndex)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value.Count}回");
            }
        }

        /// <summary>
        /// トラブルシューティング: よくあるエラーと対処法
        /// </summary>
        [ContextMenu("トラブルシューティング表示")]
        void ShowTroubleshooting()
        {
            Debug.Log("\n<color=orange>=== トラブルシューティング ===</color>");
            Debug.Log("1. 「Header not found」エラー");
            Debug.Log("   → CSVの1行目にヘッダーがあることを確認");
            Debug.Log("   → Enumの名前とヘッダー名が一致しているか確認");
            Debug.Log("");
            Debug.Log("2. 「Column count mismatch」警告");
            Debug.Log("   → 一部の行で列数が異なる場合に表示");
            Debug.Log("   → オプションで MissingFieldPolicy を調整");
            Debug.Log("");
            Debug.Log("3. 「ArgumentNullException: key」エラー");
            Debug.Log("   → CreateIndex()使用時にnull値がある場合");
            Debug.Log("   → 修正済み: nullと空文字列は自動的にスキップされます");
            Debug.Log("");
            Debug.Log("4. 型変換エラー");
            Debug.Log("   → Get<T>()の代わりに GetOrDefault<T>() を使用");
            Debug.Log("   → または TryGet<T>() で安全に取得");
            Debug.Log("");
            Debug.Log("5. パフォーマンスが遅い");
            Debug.Log("   → 行ベースではなく列ベースアクセスを使用");
            Debug.Log("   → 頻繁に検索する場合は CreateIndex() を使用");
            Debug.Log("");
            Debug.Log("6. 空のセルやnull値の扱い");
            Debug.Log("   → 常に GetOrDefault() または TryGet() を使用");
            Debug.Log("   → string.IsNullOrEmpty() でチェック");
        }
    }
}
