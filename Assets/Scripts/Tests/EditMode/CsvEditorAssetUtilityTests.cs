using CSV4Unity.Editor;
using NUnit.Framework;

namespace CSV4Unity.Tests.EditMode
{
    public sealed class CsvEditorAssetUtilityTests
    {
        [TestCase("Assets/Data/Scenario.csv", true)]
        [TestCase("Assets/Data/Scenario.CSV", true)]
        [TestCase("Assets/Data/Notes.txt", false)]
        [TestCase("Assets/Data/Scenario.csv.meta", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void IsCsvPath_DetectsOnlyCsvExtension(string path, bool expected)
        {
            Assert.That(CsvEditorAssetUtility.IsCsvPath(path), Is.EqualTo(expected));
        }

        [Test]
        public void FindBuiltInTextAssetInspectorType_UnityEditorTypeIsAvailable()
        {
            Assert.That(CsvInspectorEditor.FindBuiltInTextAssetInspectorType(), Is.Not.Null);
        }
    }
}
