using CSV4Unity.Editor;
using NUnit.Framework;

namespace CSV4Unity.Tests.EditMode
{
    public sealed class CsvViewerTableTests
    {
        private const string Csv =
            "Name,Text\n" +
            "Alice,Hello\n" +
            "Bob,\"First line\nSecond line\"\n" +
            "Carol,\"He said \"\"Hi\"\"\"";

        [Test]
        public void Constructor_ReportsDocumentDimensions()
        {
            CsvDocument document = CSVLoader.LoadDocument(Csv);
            var table = new CsvViewerTable(document);

            Assert.That(table.RowCount, Is.EqualTo(3));
            Assert.That(table.ColumnCount, Is.EqualTo(2));
            Assert.That(table.FilteredRowCount, Is.EqualTo(3));
        }

        [TestCase("alice", 1)]
        [TestCase("HELLO", 1)]
        [TestCase("second line", 1)]
        [TestCase("hi", 1)]
        [TestCase("missing", 0)]
        public void SetSearch_FiltersRowsWithoutCaseSensitivity(string search, int expectedRows)
        {
            CsvDocument document = CSVLoader.LoadDocument(Csv);
            var table = new CsvViewerTable(document);

            table.SetSearch(search);

            Assert.That(table.FilteredRowCount, Is.EqualTo(expectedRows));
        }

        [Test]
        public void SetSearch_EmptyTextRestoresAllRows()
        {
            CsvDocument document = CSVLoader.LoadDocument(Csv);
            var table = new CsvViewerTable(document);
            table.SetSearch("Alice");

            table.SetSearch(string.Empty);

            Assert.That(table.FilteredRowCount, Is.EqualTo(3));
        }
    }
}
