#if UNITY_EDITOR
using System;
using System.IO;

namespace CSV4Unity.Editor
{
    /// <summary>
    /// 文字コード変換前のCSVをLibrary以下へ退避し、必要に応じて復元します。
    /// </summary>
    internal static class CsvEncodingBackupUtility
    {
        public static bool CreateIfMissing(string backupPath, byte[] source)
        {
            if (string.IsNullOrEmpty(backupPath)) throw new ArgumentException("Backup path is required.", nameof(backupPath));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (File.Exists(backupPath)) return false;

            string directory = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            WriteAtomically(backupPath, source);
            return true;
        }

        public static void Restore(string backupPath, string targetPath)
        {
            if (string.IsNullOrEmpty(backupPath)) throw new ArgumentException("Backup path is required.", nameof(backupPath));
            if (string.IsNullOrEmpty(targetPath)) throw new ArgumentException("Target path is required.", nameof(targetPath));
            if (!File.Exists(backupPath)) throw new FileNotFoundException("CSV encoding backup was not found.", backupPath);

            byte[] original = File.ReadAllBytes(backupPath);
            WriteAtomically(targetPath, original);
            File.Delete(backupPath);
        }

        public static void WriteAtomically(string targetPath, byte[] bytes)
        {
            if (string.IsNullOrEmpty(targetPath)) throw new ArgumentException("Target path is required.", nameof(targetPath));
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            string directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            string temporaryPath = targetPath + ".csv4unity-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporaryPath, bytes);
                if (File.Exists(targetPath))
                {
                    File.Replace(temporaryPath, targetPath, null);
                }
                else
                {
                    File.Move(temporaryPath, targetPath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }
    }
}
#endif
