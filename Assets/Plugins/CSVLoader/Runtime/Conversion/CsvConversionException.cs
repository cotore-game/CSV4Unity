using System;

namespace CSV4Unity
{
    public sealed class CsvConversionException : FormatException
    {
        public CsvConversionException(string value, Type targetType)
            : base($"CSV value '{value}' cannot be converted to {targetType.Name}.")
        {
            Value = value;
            TargetType = targetType;
        }

        public string Value { get; }
        public Type TargetType { get; }
    }
}
