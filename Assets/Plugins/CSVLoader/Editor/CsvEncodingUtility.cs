#if UNITY_EDITOR
using System;
using System.Text;

namespace CSV4Unity.Editor
{
    /// <summary>
    /// CSVファイルの変換元文字コードを表します。
    /// </summary>
    internal enum CsvSourceEncoding
    {
        Auto,
        Utf8,
        ShiftJis,
        Utf16LittleEndian,
        Utf16BigEndian,
        Utf32LittleEndian,
        Utf32BigEndian
    }

    /// <summary>
    /// CSVファイルの文字コード検査結果を保持します。
    /// </summary>
    internal readonly struct CsvEncodingInspection
    {
        public CsvEncodingInspection(
            CsvSourceEncoding encoding,
            bool hasBom,
            bool isValid,
            string text,
            string errorMessage)
        {
            Encoding = encoding;
            HasBom = hasBom;
            IsValid = isValid;
            Text = text;
            ErrorMessage = errorMessage;
        }

        public CsvSourceEncoding Encoding { get; }
        public bool HasBom { get; }
        public bool IsValid { get; }
        public string Text { get; }
        public string ErrorMessage { get; }

        public bool RequiresConversion => IsValid && Encoding != CsvSourceEncoding.Utf8;

        public string DisplayName
        {
            get
            {
                switch (Encoding)
                {
                    case CsvSourceEncoding.Utf8:
                        return HasBom ? "UTF-8 (BOM)" : "UTF-8";
                    case CsvSourceEncoding.ShiftJis:
                        return "Shift_JIS (CP932)";
                    case CsvSourceEncoding.Utf16LittleEndian:
                        return "UTF-16 LE";
                    case CsvSourceEncoding.Utf16BigEndian:
                        return "UTF-16 BE";
                    case CsvSourceEncoding.Utf32LittleEndian:
                        return "UTF-32 LE";
                    case CsvSourceEncoding.Utf32BigEndian:
                        return "UTF-32 BE";
                    default:
                        return "Unknown";
                }
            }
        }
    }

    /// <summary>
    /// CSVの元バイト列を検査し、UTF-8へ変換します。
    /// </summary>
    internal static class CsvEncodingUtility
    {
        private static readonly UTF8Encoding Utf8Strict = new UTF8Encoding(false, true);
        private static readonly UnicodeEncoding Utf16LittleEndianStrict =
            new UnicodeEncoding(false, true, true);
        private static readonly UnicodeEncoding Utf16BigEndianStrict =
            new UnicodeEncoding(true, true, true);
        private static readonly UTF32Encoding Utf32LittleEndianStrict =
            new UTF32Encoding(false, true, true);
        private static readonly UTF32Encoding Utf32BigEndianStrict =
            new UTF32Encoding(true, true, true);

        public static CsvEncodingInspection Inspect(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            if (HasPrefix(bytes, 0x00, 0x00, 0xFE, 0xFF))
            {
                return Decode(bytes, CsvSourceEncoding.Utf32BigEndian, 4, true);
            }

            if (HasPrefix(bytes, 0xFF, 0xFE, 0x00, 0x00))
            {
                return Decode(bytes, CsvSourceEncoding.Utf32LittleEndian, 4, true);
            }

            if (HasPrefix(bytes, 0xEF, 0xBB, 0xBF))
            {
                return Decode(bytes, CsvSourceEncoding.Utf8, 3, true);
            }

            if (HasPrefix(bytes, 0xFE, 0xFF))
            {
                return Decode(bytes, CsvSourceEncoding.Utf16BigEndian, 2, true);
            }

            if (HasPrefix(bytes, 0xFF, 0xFE))
            {
                return Decode(bytes, CsvSourceEncoding.Utf16LittleEndian, 2, true);
            }

            CsvEncodingInspection utf8 = Decode(bytes, CsvSourceEncoding.Utf8, 0, false);
            if (utf8.IsValid) return utf8;

            CsvEncodingInspection shiftJis = Decode(bytes, CsvSourceEncoding.ShiftJis, 0, false);
            if (shiftJis.IsValid) return shiftJis;

            return Invalid(CsvSourceEncoding.Auto, "UTF-8またはShift_JISとして解釈できません。");
        }

        public static CsvEncodingInspection Decode(byte[] bytes, CsvSourceEncoding encoding)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (encoding == CsvSourceEncoding.Auto) return Inspect(bytes);

            int preambleLength = GetMatchingPreambleLength(bytes, encoding);
            return Decode(bytes, encoding, preambleLength, preambleLength > 0);
        }

        public static byte[] ConvertToUtf8(byte[] bytes, CsvSourceEncoding sourceEncoding)
        {
            CsvEncodingInspection inspection = Decode(bytes, sourceEncoding);
            if (!inspection.IsValid)
            {
                throw new InvalidOperationException(inspection.ErrorMessage);
            }

            return new UTF8Encoding(false).GetBytes(inspection.Text);
        }

        private static CsvEncodingInspection Decode(
            byte[] bytes,
            CsvSourceEncoding encoding,
            int offset,
            bool hasBom)
        {
            // Unity/MonoのCP932デコーダーは、不完全な先行バイトを例外にしない場合がある。
            if (encoding == CsvSourceEncoding.ShiftJis && !IsValidShiftJis(bytes, offset))
            {
                return Invalid(encoding, "Shift_JISのバイト列が不正です。");
            }

            try
            {
                Encoding decoder = GetEncoding(encoding);
                string text = decoder.GetString(bytes, offset, bytes.Length - offset);
                return new CsvEncodingInspection(encoding, hasBom, true, text, string.Empty);
            }
            catch (Exception exception) when (
                exception is DecoderFallbackException ||
                exception is ArgumentException ||
                exception is NotSupportedException)
            {
                return Invalid(encoding, exception.Message);
            }
        }

        private static bool IsValidShiftJis(byte[] bytes, int offset)
        {
            for (int i = offset; i < bytes.Length; i++)
            {
                byte current = bytes[i];
                bool isSingleByte = current <= 0x80 ||
                                    current == 0xA0 ||
                                    current >= 0xA1 && current <= 0xDF ||
                                    current >= 0xFD;
                if (isSingleByte)
                {
                    continue;
                }

                bool isLeadByte = current >= 0x81 && current <= 0x9F ||
                                  current >= 0xE0 && current <= 0xFC;
                if (!isLeadByte || i + 1 >= bytes.Length) return false;

                byte trail = bytes[++i];
                bool isTrailByte = trail >= 0x40 && trail <= 0x7E ||
                                   trail >= 0x80 && trail <= 0xFC;
                if (!isTrailByte) return false;
            }

            return true;
        }

        private static Encoding GetEncoding(CsvSourceEncoding encoding)
        {
            switch (encoding)
            {
                case CsvSourceEncoding.Utf8:
                    return Utf8Strict;
                case CsvSourceEncoding.ShiftJis:
                    return Encoding.GetEncoding(
                        932,
                        EncoderFallback.ExceptionFallback,
                        DecoderFallback.ExceptionFallback);
                case CsvSourceEncoding.Utf16LittleEndian:
                    return Utf16LittleEndianStrict;
                case CsvSourceEncoding.Utf16BigEndian:
                    return Utf16BigEndianStrict;
                case CsvSourceEncoding.Utf32LittleEndian:
                    return Utf32LittleEndianStrict;
                case CsvSourceEncoding.Utf32BigEndian:
                    return Utf32BigEndianStrict;
                default:
                    throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null);
            }
        }

        private static int GetMatchingPreambleLength(byte[] bytes, CsvSourceEncoding encoding)
        {
            switch (encoding)
            {
                case CsvSourceEncoding.Utf8:
                    return HasPrefix(bytes, 0xEF, 0xBB, 0xBF) ? 3 : 0;
                case CsvSourceEncoding.Utf16LittleEndian:
                    return HasPrefix(bytes, 0xFF, 0xFE) ? 2 : 0;
                case CsvSourceEncoding.Utf16BigEndian:
                    return HasPrefix(bytes, 0xFE, 0xFF) ? 2 : 0;
                case CsvSourceEncoding.Utf32LittleEndian:
                    return HasPrefix(bytes, 0xFF, 0xFE, 0x00, 0x00) ? 4 : 0;
                case CsvSourceEncoding.Utf32BigEndian:
                    return HasPrefix(bytes, 0x00, 0x00, 0xFE, 0xFF) ? 4 : 0;
                default:
                    return 0;
            }
        }

        private static bool HasPrefix(byte[] bytes, params byte[] prefix)
        {
            if (bytes.Length < prefix.Length) return false;

            for (int i = 0; i < prefix.Length; i++)
            {
                if (bytes[i] != prefix[i]) return false;
            }

            return true;
        }

        private static CsvEncodingInspection Invalid(CsvSourceEncoding encoding, string message)
        {
            return new CsvEncodingInspection(encoding, false, false, null, message);
        }
    }
}
#endif
