using System;
using System.Text;

namespace CSV4Unity
{
    /// <summary>
    /// CsvDocument内の1セルを参照し、文字列または指定型として値を取得します。
    /// </summary>
    public readonly struct CsvCell
    {
        private readonly string _source;
        private readonly CsvCellRange _range;

        internal CsvCell(string source, CsvCellRange range)
        {
            _source = source;
            _range = range;
        }

        public bool IsEmpty => _range.Length == 0;
        public bool IsQuoted => (_range.Flags & CsvCellFlags.Quoted) != 0;
        public bool HasEscapedQuotes => (_range.Flags & CsvCellFlags.EscapedQuotes) != 0;
        public ReadOnlySpan<char> RawSpan => _source.AsSpan(_range.Start, _range.Length);

        /// <summary>CSVのエスケープを解除した文字列を返します。</summary>
        public string GetString()
        {
            return Decode(_source, _range);
        }

        /// <summary>セルを指定型へ変換し、失敗時はCsvConversionExceptionを送出します。</summary>
        public T Get<T>(IFormatProvider formatProvider = null)
        {
            if (TryGet(out T value, formatProvider)) return value;
            throw new CsvConversionException(GetString(), typeof(T));
        }

        /// <summary>セルを指定型へ変換できる場合に値を返します。</summary>
        public bool TryGet<T>(out T value, IFormatProvider formatProvider = null)
        {
            if (typeof(T) == typeof(string))
            {
                value = (T)(object)GetString();
                return true;
            }

            if (!HasEscapedQuotes)
            {
                return CsvValueConverter.TryConvert(RawSpan, out value, formatProvider);
            }

            string decoded = GetString();
            return CsvValueConverter.TryConvert(decoded.AsSpan(), out value, formatProvider);
        }

        /// <summary>例外を送出せず、指定型へ変換可能かを確認します。</summary>
        public bool CanGet(Type targetType, IFormatProvider formatProvider = null)
        {
            if (!HasEscapedQuotes)
            {
                return CsvValueConverter.CanConvert(RawSpan, targetType, formatProvider);
            }

            string decoded = GetString();
            return CsvValueConverter.CanConvert(decoded.AsSpan(), targetType, formatProvider);
        }

        public bool TryGetInt32(out int value)
        {
            if (!HasEscapedQuotes)
            {
                return CsvValueConverter.TryConvertInt32(RawSpan, out value);
            }

            value = default;
            return false;
        }

        public int GetInt32()
        {
            if (TryGetInt32(out int value)) return value;
            throw new CsvConversionException(GetString(), typeof(int));
        }

        public bool TryGetInt64(out long value)
        {
            if (!HasEscapedQuotes)
            {
                return CsvValueConverter.TryConvertInt64(RawSpan, out value);
            }

            value = default;
            return false;
        }

        public long GetInt64()
        {
            if (TryGetInt64(out long value)) return value;
            throw new CsvConversionException(GetString(), typeof(long));
        }

        public bool TryGetSingle(out float value)
        {
            if (!HasEscapedQuotes)
            {
                return CsvValueConverter.TryConvertSingle(RawSpan, out value);
            }

            value = default;
            return false;
        }

        public float GetSingle()
        {
            if (TryGetSingle(out float value)) return value;
            throw new CsvConversionException(GetString(), typeof(float));
        }

        public bool TryGetDouble(out double value)
        {
            if (!HasEscapedQuotes)
            {
                return CsvValueConverter.TryConvertDouble(RawSpan, out value);
            }

            value = default;
            return false;
        }

        public double GetDouble()
        {
            if (TryGetDouble(out double value)) return value;
            throw new CsvConversionException(GetString(), typeof(double));
        }

        public bool TryGetBoolean(out bool value)
        {
            if (!HasEscapedQuotes)
            {
                return CsvValueConverter.TryConvertBoolean(RawSpan, out value);
            }

            value = default;
            return false;
        }

        public bool GetBoolean()
        {
            if (TryGetBoolean(out bool value)) return value;
            throw new CsvConversionException(GetString(), typeof(bool));
        }

        public override string ToString()
        {
            return GetString();
        }

        internal static string Decode(string source, CsvCellRange range)
        {
            ReadOnlySpan<char> raw = source.AsSpan(range.Start, range.Length);
            if ((range.Flags & CsvCellFlags.EscapedQuotes) == 0) return raw.ToString();

            var builder = new StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                if (raw[i] == '"' && i + 1 < raw.Length && raw[i + 1] == '"')
                {
                    builder.Append('"');
                    i++;
                }
                else
                {
                    builder.Append(raw[i]);
                }
            }

            return builder.ToString();
        }

    }
}
