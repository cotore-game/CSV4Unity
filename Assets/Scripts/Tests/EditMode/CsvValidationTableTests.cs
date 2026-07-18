using System.IO;
using System.Linq;
using CSV4Unity.Validation;
using NUnit.Framework;
using UnityEngine;

namespace CSV4Unity.Tests
{
    public sealed class CsvValidationTableTests
    {
        private enum CharacterField
        {
            [PrimaryKey]
            [TypeConstraint(typeof(int))]
            Id,

            [NotNull]
            [MinLength(2)]
            [MaxLength(5)]
            Name,

            [CSV4Unity.Validation.Range(1, 10)]
            Level,

            [Unique]
            Code,

            [Regex(@"^TAG-\d+$")]
            Tag,

            [AllowedValues("A", "B")]
            Kind
        }

        private enum ItemField
        {
            [PrimaryKey]
            Id
        }

        private enum ScenarioReferenceField
        {
            [ForeignKey(typeof(ItemField), nameof(ItemField.Id))]
            ItemId
        }

        private enum FixtureValidationField
        {
            [PrimaryKey]
            [TypeConstraint(typeof(int))]
            Id,

            [NotNull]
            [MinLength(2)]
            Name,

            [CSV4Unity.Validation.Range(1, 10)]
            Level
        }

        [Test]
        public void Validate_ValidTable_ReturnsNoIssues()
        {
            CsvTable<CharacterField> table = CsvParser.Parse(
                    "Id,Name,Level,Code,Tag,Kind\n1,Alice,5,X,TAG-1,A\n2,Bob,10,Y,TAG-2,B")
                .WithFields<CharacterField>();

            CsvValidationResult result = CsvValidator.Validate(table);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
        }

        [Test]
        public void Validate_InvalidTable_ReportsEachConstraintOnce()
        {
            const string csv =
                "Id,Name,Level,Code,Tag,Kind\n" +
                "1,Alice,5,X,TAG-1,A\n" +
                "1,,20,X,bad,C\n" +
                "abc,A,not-number,,TAG-3,B";
            CsvTable<CharacterField> table = CsvParser.Parse(csv).WithFields<CharacterField>();

            CsvValidationResult result = CsvValidator.Validate(table);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(9));
            Assert.That(result.Errors.Count(error => error.Column == nameof(CharacterField.Id)), Is.EqualTo(2));
            Assert.That(result.Errors.Count(error => error.Column == nameof(CharacterField.Code)), Is.EqualTo(1));
            Assert.That(result.Errors.Count(error => error.Column == nameof(CharacterField.Level)), Is.EqualTo(2));
        }

        [Test]
        public void Validate_InvalidFixture_ReportsExpectedErrors()
        {
            string path = Path.Combine(Application.dataPath, "TestData", "CSV4Unity", "ValidationInvalid.csv");
            string csv = File.ReadAllText(path);
            CsvTable<FixtureValidationField> table = CsvParser.Parse(csv).WithFields<FixtureValidationField>();

            CsvValidationResult result = CsvValidator.Validate(table);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(6));
        }

        [Test]
        public void Validate_ForeignKey_UsesRegisteredReferenceTable()
        {
            CsvTable<ItemField> items = CsvParser.Parse("Id\nA\nB").WithFields<ItemField>();
            CsvTable<ScenarioReferenceField> scenario = CsvParser.Parse("ItemId\nA\nC")
                .WithFields<ScenarioReferenceField>();
            var context = new CsvValidationContext().Register(items);

            CsvValidationResult result = CsvValidator.Validate(scenario, context: context);

            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0].Row, Is.EqualTo(1));
            Assert.That(result.Errors[0].Column, Is.EqualTo(nameof(ScenarioReferenceField.ItemId)));
        }

        [Test]
        public void Validate_ForeignKeyWithoutContext_ReportsOneWarning()
        {
            CsvTable<ScenarioReferenceField> scenario = CsvParser.Parse("ItemId\nA")
                .WithFields<ScenarioReferenceField>();

            CsvValidationResult result = CsvValidator.Validate(scenario);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Warnings.Count, Is.EqualTo(1));
            Assert.That(result.Warnings[0].Row, Is.EqualTo(-1));
        }
    }
}
