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
        [SerializeField] private TextAsset hugeDataCsv;

        private void Start()
        {
            // ────────────────
            // 1. ScenarioFields を使ってシナリオデータを読み込み
            //    第2引数は表示用のデータ名（省略可）
            // 1. Load scenario data using ScenarioFields
            //    Second argument sets data name for display (optional)
            // ────────────────

            // ZeroAlloc版
            var sw = Stopwatch.StartNew();
            long memBefore = GC.GetTotalMemory(false);

            var scenarioData = CSVLoader.LoadCSVSpan<ScenarioFields>(scenarioCsv, "MainScenario");

            sw.Stop();
            long memAfter = GC.GetTotalMemory(false);
            UnityEngine.Debug.Log(
                $"[ZeroAlloc/Scenario] Loaded {scenarioData.Rows.Count} rows in {sw.ElapsedMilliseconds} ms, " +
                $"Heap Δ = {memAfter - memBefore:N0} bytes"
            );

            // Split版（legacy）
            // GCを一度クリアして同条件に
            GC.Collect();
            GC.WaitForPendingFinalizers();

            sw.Restart();
            memBefore = GC.GetTotalMemory(false);

            var scenarioDataLegacy = CSVLoader.LoadCSV<ScenarioFields>(scenarioCsv, "MainScenario_Legacy");

            sw.Stop();
            memAfter = GC.GetTotalMemory(false);
            UnityEngine.Debug.Log(
                $"[Legacy/Scenario]    Loaded {scenarioDataLegacy.Rows.Count} rows in {sw.ElapsedMilliseconds} ms, " +
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

            // ────────────────
            // 2. EnemyFields を使って敵データを読み込み
            // 2.Load enemy data using EnemyFields
            // ────────────────

            // ZeroAlloc版
            sw.Restart();
            memBefore = GC.GetTotalMemory(false);

            var enemyData = CSVLoader.LoadCSVSpan<HugeDataFields>(hugeDataCsv, "HugeData");

            sw.Stop();
            memAfter = GC.GetTotalMemory(false);
            UnityEngine.Debug.Log(
                $"[ZeroAlloc/Enemy]    Loaded {enemyData.Rows.Count} rows in {sw.ElapsedMilliseconds} ms, " +
                $"Heap Δ = {memAfter - memBefore:N0} bytes"
            );

            // Split版（legacy）
            GC.Collect();
            GC.WaitForPendingFinalizers();

            sw.Restart();
            memBefore = GC.GetTotalMemory(false);

            var enemyDataLegacy = CSVLoader.LoadCSV<HugeDataFields>(hugeDataCsv, "HugeData_Legacy");

            sw.Stop();
            memAfter = GC.GetTotalMemory(false);
            UnityEngine.Debug.Log(
                $"[Legacy/Enemy]       Loaded {enemyDataLegacy.Rows.Count} rows in {sw.ElapsedMilliseconds} ms, " +
                $"Heap Δ = {memAfter - memBefore:N0} bytes"
            );

            // 各行をループしてフィールド値を取得
            // Loop through each row and get field values
            foreach (var line in enemyData.Rows)
            {
                string a = line.Get<string>(HugeDataFields.a);
                string b = line.Get<string>(HugeDataFields.b);
                string c = line.Get<string>(HugeDataFields.c);
                string d = line.Get<string>(HugeDataFields.d);
                string e = line.Get<string>(HugeDataFields.e);
                string f = line.Get<string>(HugeDataFields.f);
                string g = line.Get<string>(HugeDataFields.g);
                string h = line.Get<string>(HugeDataFields.h);
                string i = line.Get<string>(HugeDataFields.i);
                string j = line.Get<string>(HugeDataFields.j);
                string k = line.Get<string>(HugeDataFields.k);
                string l = line.Get<string>(HugeDataFields.l);
                string m = line.Get<string>(HugeDataFields.m);
                string n = line.Get<string>(HugeDataFields.n);
                string o = line.Get<string>(HugeDataFields.o);
                string p = line.Get<string>(HugeDataFields.p);
                string q = line.Get<string>(HugeDataFields.q);
                string r = line.Get<string>(HugeDataFields.r);
                string s = line.Get<string>(HugeDataFields.s);
                string t = line.Get<string>(HugeDataFields.t);
                string u = line.Get<string>(HugeDataFields.u);
            }
        }
    }
}