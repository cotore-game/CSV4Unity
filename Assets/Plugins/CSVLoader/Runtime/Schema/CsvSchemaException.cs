using System;

namespace CSV4Unity
{
    public sealed class CsvSchemaException : InvalidOperationException
    {
        public CsvSchemaException(string message)
            : base(message)
        {
        }

        public CsvSchemaException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
