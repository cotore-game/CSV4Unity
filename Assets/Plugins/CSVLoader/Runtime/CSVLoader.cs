using System;
using UnityEngine;

namespace CSV4Unity
{
    /// <summary>
    /// Unityの<see cref="TextAsset"/>とPure C#のCSVコアを接続します。
    /// </summary>
    public static class CSVLoader
    {
        /// <summary>
        /// TextAssetを解析し、ヘッダー名または列番号で参照できるドキュメントを返します。
        /// </summary>
        /// <param name="csvFile">解析するCSVを保持したTextAsset。</param>
        /// <param name="options">解析方法。<see langword="null"/>の場合は既定値を使用します。</param>
        /// <param name="dataName">ドキュメントの識別名。<see langword="null"/>の場合はTextAsset名を使用します。</param>
        /// <returns>解析された読み取り専用ドキュメント。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="csvFile"/>が<see langword="null"/>です。</exception>
        /// <exception cref="ArgumentException">区切り文字にダブルクォートまたは改行文字が指定されています。</exception>
        /// <exception cref="CsvParseException">CSVの構文またはレコードの列数が不正です。</exception>
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
        /// <param name="csvText">解析するCSV文字列。</param>
        /// <param name="options">解析方法。<see langword="null"/>の場合は既定値を使用します。</param>
        /// <param name="dataName">ドキュメントの識別名。<see langword="null"/>の場合は空文字列を使用します。</param>
        /// <returns>解析された読み取り専用ドキュメント。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="csvText"/>が<see langword="null"/>です。</exception>
        /// <exception cref="ArgumentException">区切り文字にダブルクォートまたは改行文字が指定されています。</exception>
        /// <exception cref="CsvParseException">CSVの構文またはレコードの列数が不正です。</exception>
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
        /// <typeparam name="TField">CSVヘッダーと同名のフィールドを持つEnum型。</typeparam>
        /// <param name="csvFile">解析するCSVを保持したTextAsset。</param>
        /// <param name="options">解析方法。<see langword="null"/>の場合は既定値を使用します。</param>
        /// <param name="dataName">ドキュメントの識別名。<see langword="null"/>の場合はTextAsset名を使用します。</param>
        /// <returns>Enumで列を指定できるテーブル。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="csvFile"/>が<see langword="null"/>です。</exception>
        /// <exception cref="ArgumentException">区切り文字にダブルクォートまたは改行文字が指定されています。</exception>
        /// <exception cref="CsvParseException">CSVの構文またはレコードの列数が不正です。</exception>
        /// <exception cref="CsvSchemaException">ヘッダーとEnumを一意に対応付けられません。</exception>
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
        /// <typeparam name="TField">CSVヘッダーと同名のフィールドを持つEnum型。</typeparam>
        /// <param name="csvText">解析するCSV文字列。</param>
        /// <param name="options">解析方法。<see langword="null"/>の場合は既定値を使用します。</param>
        /// <param name="dataName">ドキュメントの識別名。<see langword="null"/>の場合は空文字列を使用します。</param>
        /// <returns>Enumで列を指定できるテーブル。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="csvText"/>が<see langword="null"/>です。</exception>
        /// <exception cref="ArgumentException">区切り文字にダブルクォートまたは改行文字が指定されています。</exception>
        /// <exception cref="CsvParseException">CSVの構文またはレコードの列数が不正です。</exception>
        /// <exception cref="CsvSchemaException">ヘッダーとEnumを一意に対応付けられません。</exception>
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
