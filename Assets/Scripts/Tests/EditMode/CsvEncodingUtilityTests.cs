using System;
using System.IO;
using System.Text;
using CSV4Unity.Editor;
using NUnit.Framework;

namespace CSV4Unity.Tests
{
    public sealed class CsvEncodingUtilityTests
    {
        private const string JapaneseCsv = "Id,名前\r\n1,太郎";

        [Test]
        public void Inspect_Utf8WithoutBom_ReturnsUtf8()
        {
            byte[] bytes = new UTF8Encoding(false).GetBytes(JapaneseCsv);

            CsvEncodingInspection result = CsvEncodingUtility.Inspect(bytes);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Encoding, Is.EqualTo(CsvSourceEncoding.Utf8));
            Assert.That(result.HasBom, Is.False);
            Assert.That(result.RequiresConversion, Is.False);
            Assert.That(result.Text, Is.EqualTo(JapaneseCsv));
        }

        [Test]
        public void Inspect_Utf8WithBom_RemovesBom()
        {
            var encoding = new UTF8Encoding(true);
            byte[] bytes = Combine(encoding.GetPreamble(), encoding.GetBytes(JapaneseCsv));

            CsvEncodingInspection result = CsvEncodingUtility.Inspect(bytes);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Encoding, Is.EqualTo(CsvSourceEncoding.Utf8));
            Assert.That(result.HasBom, Is.True);
            Assert.That(result.Text, Is.EqualTo(JapaneseCsv));
        }

        [Test]
        public void Inspect_ShiftJis_ReturnsDecodedText()
        {
            Encoding encoding = Encoding.GetEncoding(932);
            byte[] bytes = encoding.GetBytes(JapaneseCsv);

            CsvEncodingInspection result = CsvEncodingUtility.Inspect(bytes);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Encoding, Is.EqualTo(CsvSourceEncoding.ShiftJis));
            Assert.That(result.RequiresConversion, Is.True);
            Assert.That(result.Text, Is.EqualTo(JapaneseCsv));
        }

        [Test]
        public void Inspect_Utf16Bom_ReturnsDecodedText()
        {
            var encoding = new UnicodeEncoding(false, true);
            byte[] bytes = Combine(encoding.GetPreamble(), encoding.GetBytes(JapaneseCsv));

            CsvEncodingInspection result = CsvEncodingUtility.Inspect(bytes);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Encoding, Is.EqualTo(CsvSourceEncoding.Utf16LittleEndian));
            Assert.That(result.HasBom, Is.True);
            Assert.That(result.Text, Is.EqualTo(JapaneseCsv));
        }

        [Test]
        public void Decode_ExplicitUtf16WithoutBom_UsesSelectedEncoding()
        {
            byte[] bytes = new UnicodeEncoding(false, false).GetBytes(JapaneseCsv);

            CsvEncodingInspection result = CsvEncodingUtility.Decode(
                bytes,
                CsvSourceEncoding.Utf16LittleEndian);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Encoding, Is.EqualTo(CsvSourceEncoding.Utf16LittleEndian));
            Assert.That(result.HasBom, Is.False);
            Assert.That(result.Text, Is.EqualTo(JapaneseCsv));
        }

        [Test]
        public void ConvertToUtf8_ShiftJis_PreservesTextAndLineEndings()
        {
            byte[] source = Encoding.GetEncoding(932).GetBytes(JapaneseCsv);

            byte[] converted = CsvEncodingUtility.ConvertToUtf8(
                source,
                CsvSourceEncoding.ShiftJis);

            Assert.That(HasUtf8Bom(converted), Is.False);
            Assert.That(new UTF8Encoding(false, true).GetString(converted), Is.EqualTo(JapaneseCsv));
        }

        [Test]
        public void Inspect_InvalidByteSequence_ReturnsInvalidResult()
        {
            byte[] bytes = { 0x81 };

            CsvEncodingInspection result = CsvEncodingUtility.Inspect(bytes);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Encoding, Is.EqualTo(CsvSourceEncoding.Auto));
            Assert.That(result.ErrorMessage, Is.Not.Empty);
        }

        [Test]
        public void CreateBackup_ExistingBackupIsNotOverwritten()
        {
            string directory = CreateTemporaryDirectory();
            string backupPath = Path.Combine(directory, "source.bytes");
            byte[] original = { 1, 2, 3 };

            try
            {
                bool created = CsvEncodingBackupUtility.CreateIfMissing(backupPath, original);
                bool createdAgain = CsvEncodingBackupUtility.CreateIfMissing(
                    backupPath,
                    new byte[] { 9, 9, 9 });

                Assert.That(created, Is.True);
                Assert.That(createdAgain, Is.False);
                Assert.That(File.ReadAllBytes(backupPath), Is.EqualTo(original));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void RestoreBackup_ReplacesTargetAndConsumesBackup()
        {
            string directory = CreateTemporaryDirectory();
            string backupPath = Path.Combine(directory, "source.bytes");
            string targetPath = Path.Combine(directory, "target.csv");
            byte[] original = { 1, 2, 3 };

            try
            {
                CsvEncodingBackupUtility.CreateIfMissing(backupPath, original);
                File.WriteAllBytes(targetPath, new byte[] { 9, 9, 9 });

                CsvEncodingBackupUtility.Restore(backupPath, targetPath);

                Assert.That(File.ReadAllBytes(targetPath), Is.EqualTo(original));
                Assert.That(File.Exists(backupPath), Is.False);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        private static byte[] Combine(byte[] first, byte[] second)
        {
            var result = new byte[first.Length + second.Length];
            first.CopyTo(result, 0);
            second.CopyTo(result, first.Length);
            return result;
        }

        private static bool HasUtf8Bom(byte[] bytes)
        {
            return bytes.Length >= 3 &&
                   bytes[0] == 0xEF &&
                   bytes[1] == 0xBB &&
                   bytes[2] == 0xBF;
        }

        private static string CreateTemporaryDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), "CSV4Unity-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
