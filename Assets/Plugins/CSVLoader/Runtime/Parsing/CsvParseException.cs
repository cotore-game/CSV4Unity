using System;

namespace CSV4Unity
{
    /// <summary>
    /// CSVの構文またはレコード構造を解析できない場合に送出される例外です。
    /// </summary>
    public sealed class CsvParseException : FormatException
    {
        /// <summary>エラーが検出されたレコードのゼロ始まり番号を取得します。</summary>
        /// <value>ヘッダーを含む入力上のレコード番号。</value>
        public int RecordIndex { get; }

        /// <summary>エラーが検出されたフィールドのゼロ始まり番号を取得します。</summary>
        public int FieldIndex { get; }

        /// <summary>入力文字列内でエラーが検出されたゼロ始まり位置を取得します。</summary>
        public int CharacterIndex { get; }

        internal CsvParseException(string message, int recordIndex, int fieldIndex, int characterIndex)
            : base($"{message} (record: {recordIndex}, field: {fieldIndex}, character: {characterIndex})")
        {
            RecordIndex = recordIndex;
            FieldIndex = fieldIndex;
            CharacterIndex = characterIndex;
        }
    }
}
