using System;
using UnityEngine;

namespace CSV4Unity
{
    /// <summary>
    /// UnityのTextAssetとPure C#のCSVコアを接続します。
    /// </summary>
    public static class CSVLoader
    {
        /// <summary>
        /// TextAssetを解析し、ヘッダー名または列番号で参照できるドキュメントを返します。
        /// </summary>
        public static CsvDocument LoadDocument(
            TextAsset csvFile,
            CsvParseOptions options = null,
            string dataName = null)
        {
            if (csvFile == null) throw new ArgumentNullException(nameof(csvFile));
            return CsvParser.Parse(csvFile.text, options, dataName ?? csvFile.name);
        }

        /// <summary>
        /// CSV文字列を解析し、ヘッダー名または列番号で参照できるドキュメントを返します。
        /// </summary>
        public static CsvDocument LoadDocument(
            string csvText,
            CsvParseOptions options = null,
            string dataName = null)
        {
            return CsvParser.Parse(csvText, options, dataName);
        }

        /// <summary>
        /// TextAssetを解析し、Enumで列を指定できるテーブルを返します。
        /// </summary>
        public static CsvTable<TField> LoadTable<TField>(
            TextAsset csvFile,
            CsvParseOptions options = null,
            string dataName = null)
            where TField : struct, Enum
        {
            return LoadDocument(csvFile, options, dataName).WithFields<TField>();
        }

        /// <summary>
        /// CSV文字列を解析し、Enumで列を指定できるテーブルを返します。
        /// </summary>
        public static CsvTable<TField> LoadTable<TField>(
            string csvText,
            CsvParseOptions options = null,
            string dataName = null)
            where TField : struct, Enum
        {
            return LoadDocument(csvText, options, dataName).WithFields<TField>();
        }
    }
}
