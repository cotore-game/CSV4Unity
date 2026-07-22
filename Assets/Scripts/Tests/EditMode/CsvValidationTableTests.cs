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

        private enum ConditionalField
        {
            Command,
            Enabled,

            [Condition(ConditionalField.Command, Compare.Equal, "A")]
            [Condition(ConditionalField.Enabled, Compare.Equal, true)]
            [NotNull]
            [TypeConstraint(typeof(int))]
            Arg
        }

        private enum BranchField
        {
            Command,

            [Condition(1, BranchField.Command, Compare.Equal, "A")]
            [NotNull(ConditionGroup = 1)]
            [TypeConstraint(typeof(int), ConditionGroup = 1)]

            [Condition(2, BranchField.Command, Compare.Equal, "B")]
            [NotNull(ConditionGroup = 2)]
            [TypeConstraint(typeof(bool), ConditionGroup = 2)]

            [Condition(3, BranchField.Command, Compare.NotIn, "A", "B")]
            [AllowedValues("fallback", ConditionGroup = 3)]
            Arg
        }

        private enum CompareField
        {
            Numeric,
            OtherNumeric,
            Text,
            EmptySource,

            [Condition(CompareField.Numeric, Compare.Equal, 10)]
            [NotNull]
            Equal,

            [Condition(CompareField.Numeric, Compare.NotEqual, 11)]
            [NotNull]
            NotEqual,

            [Condition(CompareField.Numeric, Compare.GreaterThan, 9)]
            [NotNull]
            GreaterThan,

            [Condition(CompareField.Numeric, Compare.GreaterThanOrEqual, 10)]
            [NotNull]
            GreaterThanOrEqual,

            [Condition(CompareField.Numeric, Compare.LessThan, 11)]
            [NotNull]
            LessThan,

            [Condition(CompareField.Numeric, Compare.LessThanOrEqual, 10)]
            [NotNull]
            LessThanOrEqual,

            [Condition(CompareField.EmptySource, Compare.IsEmpty)]
            [NotNull]
            IsEmpty,

            [Condition(CompareField.Text, Compare.IsNotEmpty)]
            [NotNull]
            IsNotEmpty,

            [Condition(CompareField.Text, Compare.In, "Beta", "Alpha")]
            [NotNull]
            In,

            [Condition(CompareField.Text, Compare.NotIn, "Beta", "Gamma")]
            [NotNull]
            NotIn,

            [Condition(CompareField.Text, Compare.Equal, "alpha", IgnoreCase = true)]
            [NotNull]
            IgnoreCase,

            [Condition(CompareField.Numeric, Compare.GreaterThan, CompareField.OtherNumeric)]
            [NotNull]
            ColumnComparison
        }

        private enum OtherField
        {
            Value
        }

        private enum InvalidConditionField
        {
            Source,

            [Condition(OtherField.Value, Compare.Equal, "A")]
            [NotNull]
            Target
        }

        private enum UndefinedGroupField
        {
            Source,

            [Condition(1, UndefinedGroupField.Source, Compare.Equal, "A")]
            [NotNull(ConditionGroup = 2)]
            Target
        }

        private enum InvalidConditionValueCountField
        {
            Source,

            [Condition(InvalidConditionValueCountField.Source, Compare.Equal)]
            [NotNull]
            Target
        }

        private enum InvalidCompareField
        {
            Source,

            [Condition(InvalidCompareField.Source, (Compare)999, "A")]
            [NotNull]
            Target
        }

        private enum ConditionalConstraintField
        {
            Mode,

            [Condition(ConditionalConstraintField.Mode, Compare.Equal, "A")]
            [CSV4Unity.Validation.Range(1, 3)]
            Number,

            [Condition(ConditionalConstraintField.Mode, Compare.Equal, "A")]
            [Regex(@"^TAG-\d+$")]
            Code,

            [Condition(ConditionalConstraintField.Mode, Compare.Equal, "A")]
            [MinLength(2)]
            [MaxLength(5)]
            Name,

            [Condition(ConditionalConstraintField.Mode, Compare.Equal, "A")]
            [Unique]
            Key
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
        public void Validate_MultipleConditions_AppliesRulesOnlyWhenAllConditionsMatch()
        {
            const string csv =
                "Command,Enabled,Arg\n" +
                "A,true,\n" +
                "A,false,invalid\n" +
                "B,true,invalid\n" +
                "A,true,invalid\n" +
                "A,true,10";
            CsvTable<ConditionalField> table = CsvParser.Parse(csv).WithFields<ConditionalField>();

            CsvValidationResult result = CsvValidator.Validate(table);

            Assert.That(result.Errors.Count, Is.EqualTo(2));
            Assert.That(result.Errors[0].Row, Is.EqualTo(0));
            Assert.That(result.Errors[1].Row, Is.EqualTo(3));
            Assert.That(result.Errors.All(error => error.Column == nameof(ConditionalField.Arg)), Is.True);
        }

        [Test]
        public void Validate_ConditionGroups_SupportsCommandSpecificTypesAndFallback()
        {
            const string csv =
                "Command,Arg\n" +
                "A,\n" +
                "A,invalid\n" +
                "A,10\n" +
                "B,invalid\n" +
                "B,true\n" +
                "C,invalid\n" +
                "C,fallback";
            CsvTable<BranchField> table = CsvParser.Parse(csv).WithFields<BranchField>();

            CsvValidationResult result = CsvValidator.Validate(table);

            Assert.That(result.Errors.Count, Is.EqualTo(4));
            Assert.That(
                result.Errors.Select(error => error.Row).OrderBy(row => row),
                Is.EqualTo(new[] { 0, 1, 3, 5 }));
        }

        [Test]
        public void Validate_CompareOperators_EvaluatesAllSupportedConditions()
        {
            const string header =
                "Numeric,OtherNumeric,Text,EmptySource," +
                "Equal,NotEqual,GreaterThan,GreaterThanOrEqual,LessThan,LessThanOrEqual," +
                "IsEmpty,IsNotEmpty,In,NotIn,IgnoreCase,ColumnComparison";
            const string row = "10,5,Alpha,,,,,,,,,,,,,";
            CsvTable<CompareField> table = CsvParser.Parse(header + "\n" + row).WithFields<CompareField>();

            CsvValidationResult result = CsvValidator.Validate(table);

            Assert.That(result.Errors.Count, Is.EqualTo(12));
            Assert.That(result.Errors.All(error => error.Row == 0), Is.True);
        }

        [Test]
        public void Validate_Conditions_ApplyToRangeTextAndUniqueConstraints()
        {
            const string csv =
                "Mode,Number,Code,Name,Key\n" +
                "A,5,bad,x,K1\n" +
                "B,5,bad,x,K1\n" +
                "A,2,TAG-1,Okay,K2\n" +
                "A,2,TAG-2,VeryLong,K3\n" +
                "A,2,TAG-3,Valid,K2";
            CsvTable<ConditionalConstraintField> table = CsvParser.Parse(csv)
                .WithFields<ConditionalConstraintField>();

            CsvValidationResult result = CsvValidator.Validate(table);

            Assert.That(result.Errors.Count, Is.EqualTo(5));
            Assert.That(result.Errors.Count(error => error.Row == 0), Is.EqualTo(3));
            Assert.That(result.Errors.Count(error => error.Row == 1), Is.EqualTo(0));
            Assert.That(result.Errors.Count(error => error.Column == nameof(ConditionalConstraintField.Key)), Is.EqualTo(1));
        }

        [Test]
        public void CreateSchema_ConditionFromDifferentEnum_ThrowsSchemaException()
        {
            Assert.Throws<CsvSchemaException>(() => CsvValidationSchema<InvalidConditionField>.Create());
        }

        [Test]
        public void CreateSchema_UndefinedConditionGroup_ThrowsSchemaException()
        {
            Assert.Throws<CsvSchemaException>(() => CsvValidationSchema<UndefinedGroupField>.Create());
        }

        [Test]
        public void CreateSchema_InvalidConditionValueCount_ThrowsSchemaException()
        {
            Assert.Throws<CsvSchemaException>(
                () => CsvValidationSchema<InvalidConditionValueCountField>.Create());
        }

        [Test]
        public void CreateSchema_UnknownComparison_ThrowsSchemaException()
        {
            Assert.Throws<CsvSchemaException>(() => CsvValidationSchema<InvalidCompareField>.Create());
        }

        [Test]
        public void DefaultSchema_UnknownComparison_ThrowsSchemaException()
        {
            Assert.Throws<CsvSchemaException>(() =>
                Assert.That(CsvValidationSchema<InvalidCompareField>.Default, Is.Not.Null));
        }
    }
}
