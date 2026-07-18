using System;
using System.Globalization;

namespace CSV4Unity
{
    /// <summary>
    /// CSVセルの文字列を、呼び出し側が指定した型へ変換します。
    /// </summary>
    public static class CsvValueConverter
    {
        /// <summary>文字列をBoolean値へ変換します。</summary>
        /// <param name="value">変換する文字列。</param>
        /// <param name="result">変換に成功した場合の値。</param>
        /// <returns>変換に成功した場合は<see langword="true"/>、それ以外は<see langword="false"/>。</returns>
        /// <remarks><see cref="bool.TryParse(string, out bool)"/>と同じ文字列表現を受け付けます。</remarks>
        public static bool TryConvertBoolean(ReadOnlySpan<char> value, out bool result)
        {
            return bool.TryParse(value, out result);
        }

        /// <summary>文字列を32ビット符号付き整数へ変換します。</summary>
        /// <param name="value">変換する文字列。</param>
        /// <param name="result">変換に成功した場合の値。</param>
        /// <param name="formatProvider">数値形式。<see langword="null"/>の場合は<see cref="CultureInfo.InvariantCulture"/>を使用します。</param>
        /// <returns>変換に成功した場合は<see langword="true"/>、それ以外は<see langword="false"/>。</returns>
        /// <remarks><see cref="NumberStyles.Integer"/>として解析します。</remarks>
        public static bool TryConvertInt32(
            ReadOnlySpan<char> value,
            out int result,
            IFormatProvider formatProvider = null)
        {
            return int.TryParse(value, NumberStyles.Integer, formatProvider ?? CultureInfo.InvariantCulture, out result);
        }

        /// <summary>文字列を64ビット符号付き整数へ変換します。</summary>
        /// <param name="value">変換する文字列。</param>
        /// <param name="result">変換に成功した場合の値。</param>
        /// <param name="formatProvider">数値形式。<see langword="null"/>の場合は<see cref="CultureInfo.InvariantCulture"/>を使用します。</param>
        /// <returns>変換に成功した場合は<see langword="true"/>、それ以外は<see langword="false"/>。</returns>
        /// <remarks><see cref="NumberStyles.Integer"/>として解析します。</remarks>
        public static bool TryConvertInt64(
            ReadOnlySpan<char> value,
            out long result,
            IFormatProvider formatProvider = null)
        {
            return long.TryParse(value, NumberStyles.Integer, formatProvider ?? CultureInfo.InvariantCulture, out result);
        }

        /// <summary>文字列を単精度浮動小数点数へ変換します。</summary>
        /// <param name="value">変換する文字列。</param>
        /// <param name="result">変換に成功した場合の値。</param>
        /// <param name="formatProvider">数値形式。<see langword="null"/>の場合は<see cref="CultureInfo.InvariantCulture"/>を使用します。</param>
        /// <returns>変換に成功した場合は<see langword="true"/>、それ以外は<see langword="false"/>。</returns>
        /// <remarks><see cref="NumberStyles.Float"/>として解析します。</remarks>
        public static bool TryConvertSingle(
            ReadOnlySpan<char> value,
            out float result,
            IFormatProvider formatProvider = null)
        {
            return float.TryParse(value, NumberStyles.Float, formatProvider ?? CultureInfo.InvariantCulture, out result);
        }

        /// <summary>文字列を倍精度浮動小数点数へ変換します。</summary>
        /// <param name="value">変換する文字列。</param>
        /// <param name="result">変換に成功した場合の値。</param>
        /// <param name="formatProvider">数値形式。<see langword="null"/>の場合は<see cref="CultureInfo.InvariantCulture"/>を使用します。</param>
        /// <returns>変換に成功した場合は<see langword="true"/>、それ以外は<see langword="false"/>。</returns>
        /// <remarks><see cref="NumberStyles.Float"/>として解析します。</remarks>
        public static bool TryConvertDouble(
            ReadOnlySpan<char> value,
            out double result,
            IFormatProvider formatProvider = null)
        {
            return double.TryParse(value, NumberStyles.Float, formatProvider ?? CultureInfo.InvariantCulture, out result);
        }

        /// <summary>文字列を指定型へ変換します。</summary>
        /// <typeparam name="T">変換先型。</typeparam>
        /// <param name="value">変換する文字列。</param>
        /// <param name="formatProvider">数値および日時形式。<see langword="null"/>の場合は<see cref="CultureInfo.InvariantCulture"/>を使用します。</param>
        /// <returns>変換された値。</returns>
        /// <exception cref="CsvConversionException"><typeparamref name="T"/>へ変換できません。</exception>
        /// <remarks>
        /// string、bool、short、int、uint、long、ulong、float、double、decimal、
        /// <see cref="DateTime"/>、<see cref="Guid"/>、Enum、およびこれらのNullable型をサポートします。
        /// Enum名は大文字小文字を区別します。空文字列はNullable型の<see langword="null"/>へ変換されます。
        /// </remarks>
        public static T Convert<T>(ReadOnlySpan<char> value, IFormatProvider formatProvider = null)
        {
            if (TryConvert(value, out T result, formatProvider)) return result;
            throw new CsvConversionException(value.ToString(), typeof(T));
        }

        /// <summary>文字列を指定型へ変換できるか確認します。</summary>
        /// <param name="value">確認する文字列。</param>
        /// <param name="targetType">変換先型。</param>
        /// <param name="formatProvider">数値および日時形式。<see langword="null"/>の場合は<see cref="CultureInfo.InvariantCulture"/>を使用します。</param>
        /// <returns>変換可能な場合は<see langword="true"/>、それ以外は<see langword="false"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="targetType"/>が<see langword="null"/>です。</exception>
        public static bool CanConvert(
            ReadOnlySpan<char> value,
            Type targetType,
            IFormatProvider formatProvider = null)
        {
            if (targetType == null) throw new ArgumentNullException(nameof(targetType));
            if (targetType == typeof(string)) return true;

            Type nullableType = Nullable.GetUnderlyingType(targetType);
            if (nullableType != null)
            {
                if (value.IsEmpty) return true;
                targetType = nullableType;
            }

            return TryConvertKnownType(
                value,
                targetType,
                formatProvider ?? CultureInfo.InvariantCulture,
                out _);
        }

        /// <summary>文字列を指定型へ変換します。</summary>
        /// <typeparam name="T">変換先型。</typeparam>
        /// <param name="value">変換する文字列。</param>
        /// <param name="result">変換に成功した場合の値。失敗した場合は<see langword="default"/>。</param>
        /// <param name="formatProvider">数値および日時形式。<see langword="null"/>の場合は<see cref="CultureInfo.InvariantCulture"/>を使用します。</param>
        /// <returns>変換に成功した場合は<see langword="true"/>、それ以外は<see langword="false"/>。</returns>
        /// <remarks>
        /// 対応型とEnumの比較規則は<see cref="Convert{T}(ReadOnlySpan{char}, IFormatProvider)"/>と同じです。
        /// 変換先型ごとの変換処理はジェネリックキャッシュへ保存されます。
        /// </remarks>
        public static bool TryConvert<T>(
            ReadOnlySpan<char> value,
            out T result,
            IFormatProvider formatProvider = null)
        {
            return ConverterCache<T>.Converter(value, formatProvider ?? CultureInfo.InvariantCulture, out result);
        }

        private static bool TryConvertFallback<T>(
            ReadOnlySpan<char> value,
            IFormatProvider provider,
            out T result)
        {
            Type targetType = typeof(T);

            Type nullableType = Nullable.GetUnderlyingType(targetType);
            if (nullableType != null)
            {
                if (value.IsEmpty)
                {
                    result = default;
                    return true;
                }

                if (TryConvertKnownType(value, nullableType, provider, out object nullableValue))
                {
                    result = (T)nullableValue;
                    return true;
                }

                result = default;
                return false;
            }

            if (TryConvertKnownType(value, targetType, provider, out object convertedValue))
            {
                result = (T)convertedValue;
                return true;
            }

            result = default;
            return false;
        }

        private delegate bool TryConvertDelegate<T>(
            ReadOnlySpan<char> value,
            IFormatProvider formatProvider,
            out T result);

        private static class ConverterCache<T>
        {
            public static readonly TryConvertDelegate<T> Converter = CreateConverter();

            private static TryConvertDelegate<T> CreateConverter()
            {
                Type type = typeof(T);

                if (type == typeof(string)) return Cast<string>(TryConvertStringCore);
                if (type == typeof(bool)) return Cast<bool>(TryConvertBooleanCore);
                if (type == typeof(int)) return Cast<int>(TryConvertInt32Core);
                if (type == typeof(long)) return Cast<long>(TryConvertInt64Core);
                if (type == typeof(float)) return Cast<float>(TryConvertSingleCore);
                if (type == typeof(double)) return Cast<double>(TryConvertDoubleCore);
                if (type == typeof(decimal)) return Cast<decimal>(TryConvertDecimalCore);
                if (type == typeof(short)) return Cast<short>(TryConvertInt16Core);
                if (type == typeof(uint)) return Cast<uint>(TryConvertUInt32Core);
                if (type == typeof(ulong)) return Cast<ulong>(TryConvertUInt64Core);
                if (type == typeof(DateTime)) return Cast<DateTime>(TryConvertDateTimeCore);
                if (type == typeof(Guid)) return Cast<Guid>(TryConvertGuidCore);
                if (type.IsEnum) return TryConvertEnumCore;

                return TryConvertFallback;
            }

            private static bool TryConvertEnumCore(
                ReadOnlySpan<char> value,
                IFormatProvider formatProvider,
                out T result)
            {
                string[] names = EnumMetadata<T>.Names;
                T[] values = EnumMetadata<T>.Values;

                for (int i = 0; i < names.Length; i++)
                {
                    if (value.SequenceEqual(names[i].AsSpan()))
                    {
                        result = values[i];
                        return true;
                    }
                }

                result = default;
                return false;
            }

            private static TryConvertDelegate<T> Cast<TValue>(TryConvertDelegate<TValue> converter)
            {
                return (TryConvertDelegate<T>)(object)converter;
            }
        }

        private static class EnumMetadata<T>
        {
            public static readonly string[] Names = Enum.GetNames(typeof(T));
            public static readonly T[] Values = (T[])Enum.GetValues(typeof(T));
        }

        private static bool TryConvertStringCore(
            ReadOnlySpan<char> value,
            IFormatProvider formatProvider,
            out string result)
        {
            result = value.ToString();
            return true;
        }

        private static bool TryConvertBooleanCore(
            ReadOnlySpan<char> value,
            IFormatProvider formatProvider,
            out bool result)
        {
            return TryConvertBoolean(value, out result);
        }

        private static bool TryConvertInt32Core(
            ReadOnlySpan<char> value,
            IFormatProvider formatProvider,
            out int result)
        {
            return TryConvertInt32(value, out result, formatProvider);
        }

        private static bool TryConvertInt64Core(
            ReadOnlySpan<char> value,
            IFormatProvider formatProvider,
            out long result)
        {
            return TryConvertInt64(value, out result, formatProvider);
        }

        private static bool TryConvertSingleCore(
            ReadOnlySpan<char> value,
            IFormatProvider formatProvider,
            out float result)
        {
            return TryConvertSingle(value, out result, formatProvider);
        }

        private static bool TryConvertDoubleCore(
            ReadOnlySpan<char> value,
            IFormatProvider formatProvider,
            out double result)
        {
            return TryConvertDouble(value, out result, formatProvider);
        }

        private static bool TryConvertDecimalCore(
            ReadOnlySpan<char> value,
            IFormatProvider formatProvider,
            out decimal result)
        {
            return decimal.TryParse(value, NumberStyles.Number, formatProvider, out result);
        }

        private static bool TryConvertInt16Core(
            ReadOnlySpan<char> value,
            IFormatProvider formatProvider,
            out short result)
        {
            return short.TryParse(value, NumberStyles.Integer, formatProvider, out result);
        }

        private static bool TryConvertUInt32Core(
            ReadOnlySpan<char> value,
            IFormatProvider formatProvider,
            out uint result)
        {
            return uint.TryParse(value, NumberStyles.Integer, formatProvider, out result);
        }

        private static bool TryConvertUInt64Core(
            ReadOnlySpan<char> value,
            IFormatProvider formatProvider,
            out ulong result)
        {
            return ulong.TryParse(value, NumberStyles.Integer, formatProvider, out result);
        }

        private static bool TryConvertDateTimeCore(
            ReadOnlySpan<char> value,
            IFormatProvider formatProvider,
            out DateTime result)
        {
            return DateTime.TryParse(value, formatProvider, DateTimeStyles.None, out result);
        }

        private static bool TryConvertGuidCore(
            ReadOnlySpan<char> value,
            IFormatProvider formatProvider,
            out Guid result)
        {
            return Guid.TryParse(value, out result);
        }

        private static bool TryConvertKnownType(
            ReadOnlySpan<char> value,
            Type targetType,
            IFormatProvider provider,
            out object result)
        {
            if (targetType == typeof(bool) && TryConvertBoolean(value, out bool boolValue))
            {
                result = boolValue;
                return true;
            }

            if (targetType == typeof(int) && TryConvertInt32(value, out int intValue, provider))
            {
                result = intValue;
                return true;
            }

            if (targetType == typeof(long) && TryConvertInt64(value, out long longValue, provider))
            {
                result = longValue;
                return true;
            }

            if (targetType == typeof(float) && TryConvertSingle(value, out float floatValue, provider))
            {
                result = floatValue;
                return true;
            }

            if (targetType == typeof(double) && TryConvertDouble(value, out double doubleValue, provider))
            {
                result = doubleValue;
                return true;
            }

            if (targetType == typeof(decimal) && decimal.TryParse(value, NumberStyles.Number, provider, out decimal decimalValue))
            {
                result = decimalValue;
                return true;
            }

            if (targetType == typeof(short) && short.TryParse(value, NumberStyles.Integer, provider, out short shortValue))
            {
                result = shortValue;
                return true;
            }

            if (targetType == typeof(uint) && uint.TryParse(value, NumberStyles.Integer, provider, out uint uintValue))
            {
                result = uintValue;
                return true;
            }

            if (targetType == typeof(ulong) && ulong.TryParse(value, NumberStyles.Integer, provider, out ulong ulongValue))
            {
                result = ulongValue;
                return true;
            }

            if (targetType == typeof(DateTime) && DateTime.TryParse(value, provider, DateTimeStyles.None, out DateTime dateTimeValue))
            {
                result = dateTimeValue;
                return true;
            }

            if (targetType == typeof(Guid) && Guid.TryParse(value, out Guid guidValue))
            {
                result = guidValue;
                return true;
            }

            if (targetType.IsEnum && Enum.TryParse(targetType, value.ToString(), false, out object enumValue))
            {
                result = enumValue;
                return true;
            }

            result = null;
            return false;
        }
    }
}
