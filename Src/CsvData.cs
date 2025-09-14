using System.Collections.Generic;
using System;

namespace CSV4Unity
{
    /// <summary>
    /// 汎用的な CSV 全体を保持するクラス
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    public sealed class CsvData<TEnum> where TEnum : struct, Enum
    {
        public string DataName { get; set; }
        public IReadOnlyList<LineData<TEnum>> Rows => _rows;

        private readonly List<LineData<TEnum>> _rows = new();
        internal void Add(LineData<TEnum> row) => _rows.Add(row);
    }

    /// <summary>
    /// 汎用的な CSV 全体を保持する非ジェネリック版クラス
    /// </summary>
    public sealed class CsvData
    {
        public string DataName { get; set; }
        public IReadOnlyList<LineData> Rows => _rows;

        private readonly List<LineData> _rows = new();
        internal void Add(LineData row) => _rows.Add(row);
    }
}
