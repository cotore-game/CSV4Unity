using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace CSV4Unity.Tests
{
    public class CsvLoaderRfc4180Tests
    {
        private enum Rfc4180Fields
        {
            Id,
            Text,
            Note
        }

        [Test]
        public void LoadTable_PreservesQuotedLineBreaksCommasAndEscapedQuotes()
        {
            string path = Path.Combine(Application.dataPath, "TestData", "CSV4Unity", "Rfc4180.csv");
            string csv = File.ReadAllText(path);

            CsvTable<Rfc4180Fields> table = CSVLoader.LoadTable<Rfc4180Fields>(
                csv,
                dataName: "rfc4180");

            Assert.That(table.RowCount, Is.EqualTo(2));
            Assert.That(table.Row(0)[Rfc4180Fields.Id].Get<int>(), Is.EqualTo(1));
            Assert.That(table.Row(0)[Rfc4180Fields.Text].GetString(), Is.EqualTo("first line\nsecond line"));
            Assert.That(table.Row(0)[Rfc4180Fields.Note].GetString(), Is.EqualTo("comma, inside"));
            Assert.That(table.Row(1)[Rfc4180Fields.Text].GetString(), Is.EqualTo("escaped \"quote\""));
        }

        [Test]
        public void LoadDocument_ThrowsForUnclosedQuotedField()
        {
            const string csv = "Id,Text,Note\r\n1,\"line1\r\nline2,tail";

            Assert.Throws<CsvParseException>(() => CSVLoader.LoadDocument(csv, dataName: "invalid"));
        }
    }
}
