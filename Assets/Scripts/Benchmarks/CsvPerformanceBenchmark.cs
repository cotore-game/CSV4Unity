using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CSV4Unity.Benchmarks
{
    /// <summary>
    /// 生成したCSVを使い、CSVコアの主要操作をUnity上で測定します。
    /// </summary>
    public sealed class CsvPerformanceBenchmark : MonoBehaviour
    {
        private enum BenchmarkField
        {
            Id,
            Group,
            Enabled,
            Score,
            Name,
            Text
        }

        [SerializeField, Min(1)] private int rowCount = 10000;
        [SerializeField, Min(1)] private int measurementIterations = 10;
        [SerializeField, Min(1)] private int indexedLookupCount = 100000;
        [SerializeField] private bool runOnStart;

        private void Start()
        {
            if (runOnStart) RunBenchmark();
        }

        [ContextMenu("Run CSV4Unity Benchmark")]
        public void RunBenchmark()
        {
            int rows = Math.Max(1, rowCount);
            int iterations = Math.Max(1, measurementIterations);
            int lookups = Math.Max(1, indexedLookupCount);
            string source = CreateCsv(rows);
            string[] lookupKeys = CreateLookupKeys();
            bool supportsThreadAllocationCounter = SupportsThreadAllocationCounter();

            CsvDocument document = null;
            CsvTable<BenchmarkField> table = null;
            CsvIndex<string> index = null;
            long checksum = 0;

            BenchmarkResult parse = Measure(
                "Parse",
                iterations,
                rows,
                supportsThreadAllocationCounter,
                () => document = CsvParser.Parse(source, name: "GeneratedBenchmark"));

            document = CsvParser.Parse(source, name: "GeneratedBenchmark");
            BenchmarkResult bind = Measure(
                "Enum schema bind",
                iterations,
                1,
                supportsThreadAllocationCounter,
                () => table = document.WithFields<BenchmarkField>());

            table = document.WithFields<BenchmarkField>();
            BenchmarkResult scan = Measure(
                "Sequential typed scan",
                iterations,
                rows,
                supportsThreadAllocationCounter,
                () => checksum = ScanRows(table));

            BenchmarkResult buildIndex = Measure(
                "String index build",
                iterations,
                rows,
                supportsThreadAllocationCounter,
                () => index = CsvIndex<string>.Create(table.Column(BenchmarkField.Group)));

            index = CsvIndex<string>.Create(table.Column(BenchmarkField.Group));
            BenchmarkResult indexedLookup = Measure(
                "Indexed lookup",
                iterations,
                lookups,
                supportsThreadAllocationCounter,
                () => checksum = LookupRows(index, lookupKeys, lookups));

            GC.KeepAlive(document);
            GC.KeepAlive(table);
            GC.KeepAlive(index);
            GC.KeepAlive(checksum);

            Debug.Log(BuildReport(
                source,
                rows,
                iterations,
                supportsThreadAllocationCounter,
                parse,
                bind,
                scan,
                buildIndex,
                indexedLookup), this);
        }

        private static BenchmarkResult Measure(
            string name,
            int iterations,
            int operationsPerIteration,
            bool supportsThreadAllocationCounter,
            Action action)
        {
            action();

            var elapsedTicks = new long[iterations];
            var allocatedBytes = new long[iterations];
            var managedHeapDeltas = new long[iterations];

            for (int i = 0; i < iterations; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                long managedHeapBefore = GC.GetTotalMemory(false);
                long startedAt = Stopwatch.GetTimestamp();
                action();
                long finishedAt = Stopwatch.GetTimestamp();
                long managedHeapAfter = GC.GetTotalMemory(false);
                long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();

                elapsedTicks[i] = finishedAt - startedAt;
                allocatedBytes[i] = supportsThreadAllocationCounter
                    ? allocatedAfter - allocatedBefore
                    : -1;
                managedHeapDeltas[i] = Math.Max(0, managedHeapAfter - managedHeapBefore);
            }

            Array.Sort(elapsedTicks);
            Array.Sort(allocatedBytes);
            Array.Sort(managedHeapDeltas);

            return new BenchmarkResult(
                name,
                operationsPerIteration,
                ToMilliseconds(Median(elapsedTicks)),
                Median(allocatedBytes),
                Median(managedHeapDeltas));
        }

        private static bool SupportsThreadAllocationCounter()
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            var probe = new byte[4096];
            long after = GC.GetAllocatedBytesForCurrentThread();
            GC.KeepAlive(probe);
            return after > before;
        }

        private static long ScanRows(CsvTable<BenchmarkField> table)
        {
            long checksum = 0;
            for (int rowIndex = 0; rowIndex < table.RowCount; rowIndex++)
            {
                CsvRow<BenchmarkField> row = table.Row(rowIndex);
                checksum += row[BenchmarkField.Id].GetInt32();
                checksum += row[BenchmarkField.Score].GetInt32();
                if (row[BenchmarkField.Enabled].GetBoolean()) checksum++;
            }

            return checksum;
        }

        private static long LookupRows(CsvIndex<string> index, IReadOnlyList<string> keys, int lookupCount)
        {
            long checksum = 0;
            for (int i = 0; i < lookupCount; i++)
            {
                if (index.TryFindFirst(keys[i % keys.Count], out int rowIndex)) checksum += rowIndex;
            }

            return checksum;
        }

        private static string CreateCsv(int rows)
        {
            // 巨大な行数でも、初期確保だけでメモリを使い切らないよう推定容量に上限を設けます。
            const int estimatedCharactersPerRow = 64;
            const int maximumInitialCapacity = 16 * 1024 * 1024;
            int initialCapacity = (int)Math.Min((long)rows * estimatedCharactersPerRow, maximumInitialCapacity);
            var builder = new StringBuilder(initialCapacity);
            builder.AppendLine("Id,Group,Enabled,Score,Name,Text");

            for (int rowIndex = 0; rowIndex < rows; rowIndex++)
            {
                builder.Append(rowIndex).Append(',');
                builder.Append('G').Append(rowIndex % 64).Append(',');
                builder.Append((rowIndex & 1) == 0 ? "true" : "false").Append(',');
                builder.Append((rowIndex * 17) % 10000).Append(',');
                builder.Append("Name_").Append(rowIndex).Append(',');
                builder.Append('"').Append("Row ").Append(rowIndex).Append(", says \"\"hello\"\".").Append('"');
                if (rowIndex + 1 < rows) builder.Append('\n');
            }

            return builder.ToString();
        }

        private static string[] CreateLookupKeys()
        {
            return Enumerable.Range(0, 64).Select(index => $"G{index}").ToArray();
        }

        private static string BuildReport(
            string source,
            int rows,
            int iterations,
            bool supportsThreadAllocationCounter,
            params BenchmarkResult[] results)
        {
            var builder = new StringBuilder();
            builder.AppendLine("[CSV4Unity Benchmark]");
            builder.Append("Rows: ").Append(rows.ToString("N0"));
            builder.Append(", Columns: 6");
            builder.Append(", UTF-16 source: ").Append(FormatBytes((long)source.Length * sizeof(char)));
            builder.Append(", Iterations: ").AppendLine(iterations.ToString());
            builder.AppendLine("Storage I/O and CSV generation are excluded. Values are medians.");
            builder.Append("Thread allocation counter: ")
                .AppendLine(supportsThreadAllocationCounter ? "available" : "unavailable");

            for (int i = 0; i < results.Length; i++)
            {
                BenchmarkResult result = results[i];
                builder.Append("- ").Append(result.Name).Append(": ");
                builder.Append(result.Milliseconds.ToString("F3")).Append(" ms/sample, ");
                if (result.AllocatedBytes >= 0)
                {
                    builder.Append(FormatBytes(result.AllocatedBytes)).Append(" thread-allocated/sample, ");
                }
                else
                {
                    builder.Append("thread allocation unavailable, ");
                }

                builder.Append(FormatBytes(result.ManagedHeapDelta)).Append(" managed-heap delta/sample");

                if (result.OperationsPerIteration > 1)
                {
                    double nanoseconds = result.Milliseconds * 1_000_000d / result.OperationsPerIteration;
                    builder.Append(", ").Append(nanoseconds.ToString("F1")).Append(" ns/operation");
                }

                builder.AppendLine();
            }

            builder.Append("Managed-heap delta is approximate and is not retained-memory size.");
            return builder.ToString();
        }

        private static long Median(IReadOnlyList<long> sortedValues)
        {
            int middle = sortedValues.Count / 2;
            if ((sortedValues.Count & 1) != 0) return sortedValues[middle];
            return (sortedValues[middle - 1] + sortedValues[middle]) / 2;
        }

        private static double ToMilliseconds(long stopwatchTicks)
        {
            return stopwatchTicks * 1000d / Stopwatch.Frequency;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024d:F1} KiB";
            return $"{bytes / (1024d * 1024d):F2} MiB";
        }

        private readonly struct BenchmarkResult
        {
            public BenchmarkResult(
                string name,
                int operationsPerIteration,
                double milliseconds,
                long allocatedBytes,
                long managedHeapDelta)
            {
                Name = name;
                OperationsPerIteration = operationsPerIteration;
                Milliseconds = milliseconds;
                AllocatedBytes = allocatedBytes;
                ManagedHeapDelta = managedHeapDelta;
            }

            public string Name { get; }
            public int OperationsPerIteration { get; }
            public double Milliseconds { get; }
            public long AllocatedBytes { get; }
            public long ManagedHeapDelta { get; }
        }
    }
}
