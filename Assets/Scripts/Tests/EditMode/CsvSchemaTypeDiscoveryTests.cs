using System.Linq;
using CSV4Unity.Editor;
using NUnit.Framework;

namespace CSV4Unity.Tests
{
    public sealed class CsvSchemaTypeDiscoveryTests
    {
        [CsvSchema]
        private enum AttributedSchema
        {
            Id
        }

        private enum UnmarkedSchema
        {
            Id
        }

        [Test]
        public void FindAll_AttributedEnumOutsideLegacyNamespace_IsDiscovered()
        {
            Assert.That(CsvSchemaTypeDiscovery.FindAll().Contains(typeof(AttributedSchema)), Is.True);
        }

        [Test]
        public void FindAll_UnmarkedEnumOutsideLegacyNamespace_IsNotDiscovered()
        {
            Assert.That(CsvSchemaTypeDiscovery.FindAll().Contains(typeof(UnmarkedSchema)), Is.False);
        }

        [Test]
        public void FindAll_ReturnsUniqueTypesInFullNameOrder()
        {
            var schemas = CsvSchemaTypeDiscovery.FindAll();
            string[] names = schemas
                .Select(type => type.FullName ?? type.Name)
                .ToArray();
            string[] sortedNames = names
                .OrderBy(name => name, System.StringComparer.Ordinal)
                .ToArray();

            Assert.That(schemas.Distinct().Count(), Is.EqualTo(schemas.Count));
            CollectionAssert.AreEqual(sortedNames, names);
        }

        [Test]
        public void IsLegacySchema_UnmarkedEnumInLegacyNamespace_ReturnsTrue()
        {
            Assert.That(
                CsvSchemaTypeDiscovery.IsLegacySchema(typeof(CSV4Unity.Fields.Tests.LegacySchema)),
                Is.True);
        }

        [Test]
        public void IsLegacySchema_AttributedEnumInLegacyNamespace_ReturnsFalse()
        {
            Assert.That(
                CsvSchemaTypeDiscovery.IsLegacySchema(typeof(CSV4Unity.Fields.Tests.AttributedLegacySchema)),
                Is.False);
        }
    }
}

namespace CSV4Unity.Fields.Tests
{
    internal enum LegacySchema
    {
        Id
    }

    [CsvSchema]
    internal enum AttributedLegacySchema
    {
        Id
    }
}
