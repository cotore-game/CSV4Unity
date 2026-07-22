#if UNITY_EDITOR
using System;
using System.IO;

namespace CSV4Unity.Editor
{
    /// <summary>
    /// CSV用Editor拡張が対象にするアセットを判定します。
    /// </summary>
    internal static class CsvEditorAssetUtility
    {
        internal static bool IsCsvPath(string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath) &&
                   string.Equals(Path.GetExtension(assetPath), ".csv", StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif
