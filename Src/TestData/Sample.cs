using System;
using System.Diagnostics;
using UnityEngine;
using CSV4Unity.Fields;

namespace CSV4Unity.Test
{
    /// <summary>
    /// サンプル：CSV ローダーの使い方 / Sample: How to use CSV Loader
    /// </summary>
    public class Sample : MonoBehaviour
    {
        [Header("読み込む CSV ファイル / CSV Files to Load")]
        [SerializeField] private TextAsset scenarioCsv;

        private void Start()
        {
            // ────────────────
            // 1. ScenarioFields を使ってシナリオデータを読み込み
            //    第2引数は表示用のデータ名（省略可）
            // 1. Load scenario data using ScenarioFields
            //    Second argument sets data name for display (optional)
            // ────────────────

            var sw = Stopwatch.StartNew();
            long memBefore = GC.GetTotalMemory(false);
            var options = new CsvLoaderOptions
            {
                Delimiter = ',',
                HasHeader = true,
                CommentPrefix = "#",
                TrimFields = true,
                IgnoreEmptyLines = true,
                MissingFieldPolicy = MissingFieldPolicy.Throw,
                FormatProvider = System.Globalization.CultureInfo.InvariantCulture
            };

            var scenarioData = CSVLoader.LoadCSV<ScenarioFields>(scenarioCsv, options, "MainScenario");

            sw.Stop();
            long memAfter = GC.GetTotalMemory(false);
            UnityEngine.Debug.Log(
                $"[ZeroAlloc/Scenario] Loaded {scenarioData.Rows.Count} rows in {sw.ElapsedMilliseconds} ms, " +
                $"Heap Δ = {memAfter - memBefore:N0} bytes"
            );

            // 取得例
            if (scenarioData.Rows.Count > 0)
            {
                // Rows は各行を表す LineDataリスト を含むクラスです。
                // LineData.Get<T> で同名のカラム値を取得します。
                // Rows is a Class contains list of LineData. Use LineData.Get<T> to retrieve column values.
                var firstLine = scenarioData.Rows[0];
                var command = firstLine.Get<string>(ScenarioFields.Command);
                UnityEngine.Debug.Log($"First scenario command: {command}");
            }
        }
    }
}
