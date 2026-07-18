using System;

namespace CSV4Unity
{
    public sealed class CsvParseException : FormatException
    {
        public int RecordIndex { get; }
        public int FieldIndex { get; }
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
