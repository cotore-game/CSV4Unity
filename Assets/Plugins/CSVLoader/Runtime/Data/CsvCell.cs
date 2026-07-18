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

        /// <summary>セルの内容が空かを取得します。</summary>
        /// <value>空フィールドまたは空のクォートフィールドの場合は<see langword="true"/>。</value>
        public bool IsEmpty => _range.Length == 0;

        /// <summary>入力CSVでセルがダブルクォートに囲まれていたかを取得します。</summary>
        public bool IsQuoted => (_range.Flags & CsvCellFlags.Quoted) != 0;

        /// <summary>セル内に二重化されたダブルクォートが含まれているかを取得します。</summary>
        public bool HasEscapedQuotes => (_range.Flags & CsvCellFlags.EscapedQuotes) != 0;

        /// <summary>元CSV文字列内のセル内容を割り当てなしで参照します。</summary>
        /// <value>外側のダブルクォートを除き、二重化されたダブルクォートを解除していない文字列範囲。</value>
        public ReadOnlySpan<char> RawSpan => _source.AsSpan(_range.Start, _range.Length);

        /// <summary>CSVのダブルクォートエスケープを解除した文字列を返します。</summary>
        /// <returns>セルのデコード済み文字列。</returns>
        /// <remarks>戻り値として新しい文字列を生成します。</remarks>
        public string GetString()
        {
            return Decode(_source, _range);
        }

        /// <summary>セルを指定型へ変換します。</summary>
        /// <typeparam name="T">変換先型。</typeparam>
        /// <param name="formatProvider">数値および日時形式。<see langword="null"/>の場合はInvariantCultureを使用します。</param>
        /// <returns>変換された値。</returns>
        /// <exception cref="CsvConversionException"><typeparamref name="T"/>へ変換できません。</exception>
        public T Get<T>(IFormatProvider formatProvider = null)
        {
            if (TryGet(out T value, formatProvider)) return value;
            throw new CsvConversionException(GetString(), typeof(T));
        }

        /// <summary>セルを指定型へ変換します。</summary>
        /// <typeparam name="T">変換先型。</typeparam>
        /// <param name="value">変換に成功した場合の値。失敗した場合は<see langword="default"/>。</param>
        /// <param name="formatProvider">数値および日時形式。<see langword="null"/>の場合はInvariantCultureを使用します。</param>
        /// <returns>変換に成功した場合は<see langword="true"/>、それ以外は<see langword="false"/>。</returns>
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
        /// <param name="targetType">変換先型。</param>
        /// <param name="formatProvider">数値および日時形式。<see langword="null"/>の場合はInvariantCultureを使用します。</param>
        /// <returns>変換可能な場合は<see langword="true"/>、それ以外は<see langword="false"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="targetType"/>が<see langword="null"/>です。</exception>
        public bool CanGet(Type targetType, IFormatProvider formatProvider = null)
        {
            if (!HasEscapedQuotes)
            {
                return CsvValueConverter.CanConvert(RawSpan, targetType, formatProvider);
            }

            string decoded = GetString();
            return CsvValueConverter.CanConvert(decoded.AsSpan(), targetType, formatProvider);
        }

        /// <summary>セルを32ビット符号付き整数へ変換します。</summary>
        /// <param name="value">変換に成功した場合の値。</param>
        /// <returns>変換に成功した場合は<see langword="true"/>、それ以外は<see langword="false"/>。</returns>
        /// <remarks>InvariantCultureを使用します。</remarks>
        public bool TryGetInt32(out int value)
        {
            if (!HasEscapedQuotes)
            {
                return CsvValueConverter.TryConvertInt32(RawSpan, out value);
            }

            value = default;
            return false;
        }

        /// <summary>セルを32ビット符号付き整数として取得します。</summary>
        /// <returns>変換された値。</returns>
        /// <exception cref="CsvConversionException">32ビット符号付き整数へ変換できません。</exception>
        public int GetInt32()
        {
            if (TryGetInt32(out int value)) return value;
            throw new CsvConversionException(GetString(), typeof(int));
        }

        /// <summary>セルを64ビット符号付き整数へ変換します。</summary>
        /// <param name="value">変換に成功した場合の値。</param>
        /// <returns>変換に成功した場合は<see langword="true"/>、それ以外は<see langword="false"/>。</returns>
        /// <remarks>InvariantCultureを使用します。</remarks>
        public bool TryGetInt64(out long value)
        {
            if (!HasEscapedQuotes)
            {
                return CsvValueConverter.TryConvertInt64(RawSpan, out value);
            }

            value = default;
            return false;
        }

        /// <summary>セルを64ビット符号付き整数として取得します。</summary>
        /// <returns>変換された値。</returns>
        /// <exception cref="CsvConversionException">64ビット符号付き整数へ変換できません。</exception>
        public long GetInt64()
        {
            if (TryGetInt64(out long value)) return value;
            throw new CsvConversionException(GetString(), typeof(long));
        }

        /// <summary>セルを単精度浮動小数点数へ変換します。</summary>
        /// <param name="value">変換に成功した場合の値。</param>
        /// <returns>変換に成功した場合は<see langword="true"/>、それ以外は<see langword="false"/>。</returns>
        /// <remarks>InvariantCultureを使用します。</remarks>
        public bool TryGetSingle(out float value)
        {
            if (!HasEscapedQuotes)
            {
                return CsvValueConverter.TryConvertSingle(RawSpan, out value);
            }

            value = default;
            return false;
        }

        /// <summary>セルを単精度浮動小数点数として取得します。</summary>
        /// <returns>変換された値。</returns>
        /// <exception cref="CsvConversionException">単精度浮動小数点数へ変換できません。</exception>
        public float GetSingle()
        {
            if (TryGetSingle(out float value)) return value;
            throw new CsvConversionException(GetString(), typeof(float));
        }

        /// <summary>セルを倍精度浮動小数点数へ変換します。</summary>
        /// <param name="value">変換に成功した場合の値。</param>
        /// <returns>変換に成功した場合は<see langword="true"/>、それ以外は<see langword="false"/>。</returns>
        /// <remarks>InvariantCultureを使用します。</remarks>
        public bool TryGetDouble(out double value)
        {
            if (!HasEscapedQuotes)
            {
                return CsvValueConverter.TryConvertDouble(RawSpan, out value);
            }

            value = default;
            return false;
        }

        /// <summary>セルを倍精度浮動小数点数として取得します。</summary>
        /// <returns>変換された値。</returns>
        /// <exception cref="CsvConversionException">倍精度浮動小数点数へ変換できません。</exception>
        public double GetDouble()
        {
            if (TryGetDouble(out double value)) return value;
            throw new CsvConversionException(GetString(), typeof(double));
        }

        /// <summary>セルをBoolean値へ変換します。</summary>
        /// <param name="value">変換に成功した場合の値。</param>
        /// <returns>変換に成功した場合は<see langword="true"/>、それ以外は<see langword="false"/>。</returns>
        public bool TryGetBoolean(out bool value)
        {
            if (!HasEscapedQuotes)
            {
                return CsvValueConverter.TryConvertBoolean(RawSpan, out value);
            }

            value = default;
            return false;
        }

        /// <summary>セルをBoolean値として取得します。</summary>
        /// <returns>変換された値。</returns>
        /// <exception cref="CsvConversionException">Boolean値へ変換できません。</exception>
        public bool GetBoolean()
        {
            if (TryGetBoolean(out bool value)) return value;
            throw new CsvConversionException(GetString(), typeof(bool));
        }

        /// <summary>CSVのエスケープを解除したセル文字列を返します。</summary>
        /// <returns><see cref="GetString"/>と同じ文字列。</returns>
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
