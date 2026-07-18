using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace CSV4Unity.Tests
{
    public sealed class CsvParserTests
    {
        private enum ScenarioField
        {
            Command,
            Arg1,
            Text
        }

        private enum ScenarioCommand
        {
            Wait,
            Text
        }

        private enum MissingField
        {
            Command,
            NotInCsv
        }

        [Test]
        public void Parse_Rfc4180QuotedFields_DecodesValues()
        {
            const string source =
                "Command,Arg1,Text\r\n" +
                "Text,1,\"Hello, world\"\r\n" +
                "Text,true,\"He said \"\"hello\"\".\"\r\n" +
                "Text,3.5,\"first line\r\nsecond line\"";

            CsvDocument document = CsvParser.Parse(source);

            Assert.That(document.RowCount, Is.EqualTo(3));
            Assert.That(document.ColumnCount, Is.EqualTo(3));
            Assert.That(document.Cell(0, "Text").GetString(), Is.EqualTo("Hello, world"));
            Assert.That(document.Cell(1, "Text").GetString(), Is.EqualTo("He said \"hello\"."));
            Assert.That(document.Cell(2, "Text").GetString(), Is.EqualTo("first line\r\nsecond line"));
        }

        [Test]
        public void Parse_RowAndColumnViews_ReferenceSameCell()
        {
            CsvDocument document = CsvParser.Parse("Command,Arg1,Text\nWait,120,hello\nText,001,world");

            CsvRow row = document.Row(1);
            CsvColumn column = document.Column("Arg1");

            Assert.That(row["Arg1"].GetString(), Is.EqualTo("001"));
            Assert.That(column[1].GetString(), Is.EqualTo("001"));
            Assert.That(column[1].GetInt32(), Is.EqualTo(1));
        }

        [Test]
        public void Parse_EnumView_MapsFieldsWithoutPerRowDictionaries()
        {
            CsvTable<ScenarioField> table = CsvParser
                .Parse("Command,Arg1,Text\nWait,true,hello\nText,42,world")
                .WithFields<ScenarioField>();

            Assert.That(table.Row(0)[ScenarioField.Command].GetString(), Is.EqualTo("Wait"));
            Assert.That(table.Column(ScenarioField.Text)[1].GetString(), Is.EqualTo("world"));
            Assert.That(table.Cell(1, ScenarioField.Arg1).GetInt32(), Is.EqualTo(42));
        }

        [Test]
        public void EnumSchema_BindsFieldsOnceAndExposesColumnIndices()
        {
            CsvDocument document = CsvParser.Parse("Command,Arg1,Text\nWait,1,hello");

            CsvEnumSchema<ScenarioField> schema = CsvEnumSchema<ScenarioField>.Bind(document);
            var table = new CsvTable<ScenarioField>(document, schema);

            Assert.That(schema.FieldCount, Is.EqualTo(3));
            Assert.That(schema.GetColumnIndex(ScenarioField.Arg1), Is.EqualTo(1));
            Assert.That(table.Schema, Is.SameAs(schema));
        }

        [Test]
        public void EnumSchema_MissingRequiredHeader_ThrowsSchemaException()
        {
            CsvDocument document = CsvParser.Parse("Command,Arg1\nWait,1");

            Assert.Throws<CsvSchemaException>(() => CsvEnumSchema<MissingField>.Bind(document));
        }

        [Test]
        public void Cell_GenericConversion_IsExplicitAndSupportsMixedArgumentTypes()
        {
            CsvDocument document = CsvParser.Parse(
                "Command,Arg1,Text\nWait,120,\nText,true,hello\nText,3.5,world");

            Assert.That(document.Cell(0, "Command").Get<ScenarioCommand>(), Is.EqualTo(ScenarioCommand.Wait));
            Assert.That(document.Cell(0, "Arg1").Get<int>(), Is.EqualTo(120));
            Assert.That(document.Cell(1, "Arg1").Get<bool>(), Is.True);
            Assert.That(document.Cell(2, "Arg1").Get<float>(), Is.EqualTo(3.5f));
            Assert.That(document.Cell(0, "Text").Get<int?>(), Is.Null);
        }

        [Test]
        public void Cell_PrimitiveGenericConversion_DoesNotAllocateAfterWarmup()
        {
            CsvCell cell = CsvParser.Parse("Value\n123").Cell(0, 0);
            cell.TryGet(out int _);

            long before = GC.GetAllocatedBytesForCurrentThread();
            int sum = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (cell.TryGet(out int value)) sum += value;
            }
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(sum, Is.EqualTo(123000));
            Assert.That(allocatedBytes, Is.Zero);
        }

        [Test]
        public void Cell_EnumConversion_DoesNotAllocateAfterWarmup()
        {
            CsvCell cell = CsvParser.Parse("Command\nText").Cell(0, 0);
            cell.TryGet(out ScenarioCommand _);

            long before = GC.GetAllocatedBytesForCurrentThread();
            int textCount = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (cell.TryGet(out ScenarioCommand command) && command == ScenarioCommand.Text) textCount++;
            }
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(textCount, Is.EqualTo(1000));
            Assert.That(allocatedBytes, Is.Zero);
        }

        [Test]
        public void ColumnIndex_StoresFirstRowsAndDuplicateRows()
        {
            CsvDocument document = CsvParser.Parse(
                "Command,Arg1,Text\nText,1,a\nWait,2,b\nText,3,c\nText,4,d");

            CsvIndex<string> index = CsvIndex<string>.Create(document.Column("Command"));
            CsvIndexMatches matches = index.FindAll("Text");

            Assert.That(index.KeyCount, Is.EqualTo(2));
            Assert.That(index.TryFindFirst("Wait", out int waitRow), Is.True);
            Assert.That(waitRow, Is.EqualTo(1));
            Assert.That(matches.Count, Is.EqualTo(3));
            Assert.That(matches[0], Is.EqualTo(0));
            Assert.That(matches[1], Is.EqualTo(2));
            Assert.That(matches[2], Is.EqualTo(3));
        }

        [Test]
        public void Parse_InvalidQuotedField_ThrowsWithLocation()
        {
            CsvParseException exception = Assert.Throws<CsvParseException>(
                () => CsvParser.Parse("A,B\r\nvalue,\"unterminated"));

            Assert.That(exception.RecordIndex, Is.EqualTo(1));
            Assert.That(exception.FieldIndex, Is.EqualTo(1));
        }

        [Test]
        public void Parse_DifferentFieldCount_Throws()
        {
            Assert.Throws<CsvParseException>(() => CsvParser.Parse("A,B\r\n1,2\r\n3"));
        }

        [Test]
        public void Parse_BomAndTrailingEmptyField_PreservesCell()
        {
            CsvDocument document = CsvParser.Parse("\uFEFFA,B,C\r\n1,2,");

            Assert.That(document.Headers[0], Is.EqualTo("A"));
            Assert.That(document.RowCount, Is.EqualTo(1));
            Assert.That(document.Cell(0, 2).IsEmpty, Is.True);
            Assert.That(document.Cell(0, 2).GetString(), Is.Empty);
        }

        [Test]
        public void Parse_HeaderOnly_CreatesEmptyTableWithSchema()
        {
            CsvDocument document = CsvParser.Parse("A,B\r\n");

            Assert.That(document.RowCount, Is.Zero);
            Assert.That(document.ColumnCount, Is.EqualTo(2));
            Assert.That(document.Headers, Is.EqualTo(new[] { "A", "B" }));
        }

        [Test]
        public void Parse_EmptyRecord_IsRejectedWhenItBreaksSchema()
        {
            Assert.Throws<CsvParseException>(() => CsvParser.Parse("A,B\r\n1,2\r\n\r\n3,4"));
        }

        [Test]
        public void Parse_EmptyRecord_CanBeIgnoredExplicitly()
        {
            var options = new CsvParseOptions { IgnoreEmptyRecords = true };

            CsvDocument document = CsvParser.Parse("A,B\r\n1,2\r\n\r\n3,4", options);

            Assert.That(document.RowCount, Is.EqualTo(2));
        }

        [Test]
        public void Parse_ScenarioFixture_LoadsCommandsIncludingHashPrefix()
        {
            string path = Path.Combine(Application.dataPath, "TestData", "CSV4Unity", "Scenario.csv");
            string source = File.ReadAllText(path);

            CsvDocument document = CsvParser.Parse(source, name: "Scenario");
            CsvIndex<string> commandIndex = CsvIndex<string>.Create(document.Column("Command"));

            Assert.That(document.RowCount, Is.EqualTo(21));
            Assert.That(document.ColumnCount, Is.EqualTo(13));
            Assert.That(document.Headers[0], Is.EqualTo("Command"));
            Assert.That(commandIndex.FindAll("#CameraShake").Count, Is.EqualTo(1));
            Assert.That(commandIndex.FindAll("#Fade").Count, Is.EqualTo(1));
        }

        [Test]
        public void Parse_ExistingHugeData_LoadsExpectedShape()
        {
            string path = Path.Combine(Application.dataPath, "TestData", "CSV4Unity", "HugeData.csv");
            string source = File.ReadAllText(path);

            CsvDocument document = CsvParser.Parse(source, name: "HugeData");

            Assert.That(document.RowCount, Is.EqualTo(1000));
            Assert.That(document.ColumnCount, Is.EqualTo(21));
            Assert.That(document.Column("u").Count, Is.EqualTo(1000));
        }

        [Test]
        public void LoadTable_TextAsset_UsesUnityAssetName()
        {
            var asset = new TextAsset("Command,Arg1,Text\nWait,30,hello") { name = "Scenario" };

            CsvTable<ScenarioField> table = CSVLoader.LoadTable<ScenarioField>(asset);

            Assert.That(table.Document.Name, Is.EqualTo("Scenario"));
            Assert.That(table.Row(0)[ScenarioField.Arg1].GetInt32(), Is.EqualTo(30));
        }
    }
}
