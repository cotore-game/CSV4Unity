using UnityEngine;
using CSV4Unity;
using System.Linq;

namespace CSV4Unity.Test
{
    /// <summary>
    /// 改善されたCSVライブラリの使用例
    /// </summary>
    public class CsvUsageExamples : MonoBehaviour
    {
        public TextAsset enemyCsv;
        public TextAsset scenarioCsv;
        public TextAsset headerlessCsv; // ヘッダーなしCSV

        public enum EnemyFields
        {
            ID,
            Name,
            HP,
            Attack,
            Defense,
            Speed,
            Type
        }

        void Start()
        {
            Example_BasicUsage();
            Example_ColumnAccess();
            Example_QueryAndFilter();
            Example_IndexedSearch();
            Example_NonGenericVersion();
            Example_HeaderlessCSV();
            Example_PerformanceComparison();
        }

        /// <summary>
        /// 基本的な使い方
        /// </summary>
        void Example_BasicUsage()
        {
            Debug.Log("=== 基本的な使い方 ===");

            // CSVを読み込み（optionsはオプショナルで省略可）
            var enemyData = CSVLoader.LoadCSV<EnemyFields>(enemyCsv);

            // 行ごとにアクセス（従来の方法）
            foreach (var row in enemyData.Rows)
            {
                int id = row.Get<int>(EnemyFields.ID);
                string name = row.Get<string>(EnemyFields.Name);
                int hp = row.Get<int>(EnemyFields.HP);

                Debug.Log($"Enemy: {name} (ID:{id}, HP:{hp})");
            }

            // より安全な取得方法
            var firstEnemy = enemyData.Rows[0];
            if (firstEnemy.TryGet<int>(EnemyFields.Attack, out int attack))
            {
                Debug.Log($"Attack: {attack}");
            }

            // デフォルト値付きで取得
            int defense = firstEnemy.GetOrDefault<int>(EnemyFields.Defense, 10);
            Debug.Log($"Defense: {defense}");
        }

        /// <summary>
        /// 列単位でのアクセス（新機能）
        /// </summary>
        void Example_ColumnAccess()
        {
            Debug.Log("=== 列単位アクセス ===");

            var enemyData = CSVLoader.LoadCSV<EnemyFields>(enemyCsv);

            // 列全体を一度に取得（高速）
            var allNames = enemyData.GetColumn<string>(EnemyFields.Name);
            var allHPs = enemyData.GetColumn<int>(EnemyFields.HP);
            var allAttacks = enemyData.GetColumn<int>(EnemyFields.Attack);

            Debug.Log($"Total enemies: {allNames.Count}");

            // 列データで統計処理が簡単に
            float avgHP = (float)allHPs.Average();
            int maxAttack = allAttacks.Max();
            int minAttack = allAttacks.Min();

            Debug.Log($"Average HP: {avgHP}");
            Debug.Log($"Max Attack: {maxAttack}, Min Attack: {minAttack}");

            // 列データをまとめて処理（メモリ効率的）
            for (int i = 0; i < allNames.Count; i++)
            {
                Debug.Log($"{allNames[i]}: HP={allHPs[i]}, ATK={allAttacks[i]}");
            }
        }

        /// <summary>
        /// クエリとフィルタリング（新機能）
        /// </summary>
        void Example_QueryAndFilter()
        {
            Debug.Log("=== クエリとフィルタリング ===");

            var enemyData = CSVLoader.LoadCSV<EnemyFields>(enemyCsv);

            // 条件に一致する敵を検索
            var strongEnemies = enemyData.Where(row =>
                row.Get<int>(EnemyFields.HP) > 100 &&
                row.Get<int>(EnemyFields.Attack) > 50
            );

            Debug.Log($"Strong enemies count: {strongEnemies.Count()}");

            // 特定の値で検索（最初の一致）
            var bossEnemy = enemyData.FindFirst(EnemyFields.Type, "Boss");
            if (bossEnemy != null)
            {
                Debug.Log($"Boss found: {bossEnemy.Get<string>(EnemyFields.Name)}");
            }

            // 特定の値で検索（全ての一致）
            var fireEnemies = enemyData.FindAll(EnemyFields.Type, "Fire");
            Debug.Log($"Fire type enemies: {fireEnemies.Count()}");

            // グループ化
            var groupedByType = enemyData.GroupBy(EnemyFields.Type);
            foreach (var group in groupedByType)
            {
                Debug.Log($"Type: {group.Key}, Count: {group.Count()}");
            }
        }

        /// <summary>
        /// インデックスを使った高速検索（新機能）
        /// </summary>
        void Example_IndexedSearch()
        {
            Debug.Log("=== インデックス検索 ===");

            var enemyData = CSVLoader.LoadCSV<EnemyFields>(enemyCsv);

            // IDでの検索用インデックスを作成（一度だけ実行）
            var idIndex = enemyData.CreateIndex(EnemyFields.ID);

            // インデックスを使って高速検索
            if (idIndex.TryGetValue(5, out var rowIndices))
            {
                foreach (var rowIndex in rowIndices)
                {
                    var enemy = enemyData.Rows[rowIndex];
                    Debug.Log($"Enemy ID=5: {enemy.Get<string>(EnemyFields.Name)}");
                }
            }

            // Typeでのグループ検索用インデックス
            var typeIndex = enemyData.CreateIndex(EnemyFields.Type);
            if (typeIndex.TryGetValue("Fire", out var fireIndices))
            {
                Debug.Log($"Fire type enemies: {fireIndices.Count}");
                foreach (var idx in fireIndices)
                {
                    Debug.Log($"  - {enemyData.Rows[idx].Get<string>(EnemyFields.Name)}");
                }
            }
        }

        /// <summary>
        /// 非ジェネリック版の使用例（ヘッダーあり）
        /// </summary>
        void Example_NonGenericVersion()
        {
            Debug.Log("=== 非ジェネリック版（ヘッダーあり） ===");

            // Enumを定義せずに使える
            var data = CSVLoader.LoadCSV(scenarioCsv);

            Debug.Log($"Headers: {string.Join(", ", data.Headers)}");

            // ヘッダー名で直接アクセス
            foreach (var row in data.Rows)
            {
                string command = row.Get<string>("Command");
                string text = row.GetOrDefault<string>("Text", "");

                Debug.Log($"Command: {command}, Text: {text}");
            }

            // 列アクセスも可能
            var allCommands = data.GetColumn<string>("Command");
            var commandGroups = data.GroupBy("Command");

            foreach (var group in commandGroups)
            {
                Debug.Log($"Command {group.Key}: {group.Count()} times");
            }
        }

        /// <summary>
        /// ヘッダーなしCSVの使用例（インデックスベースの高速アクセス）
        /// </summary>
        void Example_HeaderlessCSV()
        {
            Debug.Log("=== ヘッダーなしCSV（インデックスベース） ===");

            var options = new CsvLoaderOptions
            {
                HasHeader = false,  // ヘッダーなし
                Delimiter = ',',
                TrimFields = true
            };

            var data = CSVLoader.LoadCSV(headerlessCsv, options);

            Debug.Log($"Loaded {data.RowCount} rows with {data.ColumnCount} columns");

            // インデックスで行データにアクセス（Enumと同等のパフォーマンス）
            foreach (var row in data.Rows)
            {
                // 0列目: ID, 1列目: Name, 2列目: Score など
                int id = row.Get<int>(0);
                string name = row.Get<string>(1);
                float score = row.Get<float>(2);

                Debug.Log($"ID:{id}, Name:{name}, Score:{score}");
            }

            // 列全体をインデックスで取得（高速）
            var allIDs = data.GetColumnByIndex<int>(0);
            var allNames = data.GetColumnByIndex<string>(1);
            var allScores = data.GetColumnByIndex<float>(2);

            Debug.Log($"Average Score: {allScores.Average()}");

            // インデックスで検索
            var topScorer = data.FindFirstByIndex(2, allScores.Max());
            if (topScorer != null)
            {
                Debug.Log($"Top scorer: {topScorer.Get<string>(1)} with {topScorer.Get<float>(2)} points");
            }

            // インデックスでグループ化
            var groupsByCategory = data.GroupByIndex(3); // 3列目でグループ化
            foreach (var group in groupsByCategory)
            {
                Debug.Log($"Category {group.Key}: {group.Count()} items");
            }

            // インデックスベースの検索インデックス作成
            var categoryIndex = data.CreateIndexByColumnIndex(3);
            if (categoryIndex.TryGetValue("A", out var categoryARows))
            {
                Debug.Log($"Category A has {categoryARows.Count} rows");
            }
        }

        /// <summary>
        /// パフォーマンス比較例
        /// </summary>
        void Example_PerformanceComparison()
        {
            Debug.Log("=== パフォーマンス比較 ===");

            var enemyData = CSVLoader.LoadCSV<EnemyFields>(enemyCsv);

            // 方法1: 行ごとにアクセス（従来）
            var sw1 = System.Diagnostics.Stopwatch.StartNew();
            int sum1 = 0;
            foreach (var row in enemyData.Rows)
            {
                sum1 += row.Get<int>(EnemyFields.HP);
            }
            sw1.Stop();
            Debug.Log($"Row-based access: {sw1.ElapsedTicks} ticks, Sum: {sum1}");

            // 方法2: 列アクセス（新機能、高速）
            var sw2 = System.Diagnostics.Stopwatch.StartNew();
            var hpColumn = enemyData.GetColumn<int>(EnemyFields.HP);
            int sum2 = 0;
            for (int i = 0; i < hpColumn.Count; i++)
            {
                sum2 += hpColumn[i];
            }
            sw2.Stop();
            Debug.Log($"Column-based access: {sw2.ElapsedTicks} ticks, Sum: {sum2}");

            float speedup = (float)sw1.ElapsedTicks / sw2.ElapsedTicks;
            Debug.Log($"Column access is {speedup:F2}x faster");

            // ヘッダーなしCSVのインデックスアクセス性能テスト
            var options = new CsvLoaderOptions { HasHeader = false };
            var headerlessData = CSVLoader.LoadCSV(headerlessCsv, options);

            var sw3 = System.Diagnostics.Stopwatch.StartNew();
            var col0 = headerlessData.GetColumnByIndex<int>(0);
            int sum3 = 0;
            for (int i = 0; i < col0.Count; i++)
            {
                sum3 += col0[i];
            }
            sw3.Stop();
            Debug.Log($"Headerless index-based column access: {sw3.ElapsedTicks} ticks, Sum: {sum3}");
            Debug.Log("(Index-based access has similar performance to Enum-based access)");
        }
    }
}
