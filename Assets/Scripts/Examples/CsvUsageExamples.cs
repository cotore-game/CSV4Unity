using CSV4Unity.Fields;
using UnityEngine;

namespace CSV4Unity.Examples
{
    /// <summary>
    /// 新しいCSVコアの代表的なアクセス方法を確認するサンプルです。
    /// </summary>
    public sealed class CsvUsageExamples : MonoBehaviour
    {
        [Header("CSV Files")]
        [SerializeField]
        [Tooltip("Assets/TestData/CSV4Unity/Scenario.csv を指定してください")]
        private TextAsset scenarioCsv;

        [SerializeField]
        [Tooltip("Assets/TestData/CSV4Unity/HugeData.csv を指定してください。未設定でも他の例は動作します")]
        private TextAsset hugeDataCsv;

        private void Start()
        {
            RunAllExamples();
        }

        [ContextMenu("Run CSV4Unity Examples")]
        public void RunAllExamples()
        {
            if (scenarioCsv == null)
            {
                Debug.LogWarning("scenarioCsv に Scenario.csv を指定してください。", this);
                return;
            }

            CsvTable<ScenarioFields> table = CSVLoader.LoadTable<ScenarioFields>(scenarioCsv);

            LogEnumRowAccess(table);
            LogMixedValueAccess(table);
            LogColumnAndIndexAccess(table);
            LogHeaderNameAccess();
            LogHeaderlessAccess();
            LogRfc4180Access();
            LogLargeCsvAccess();
        }

        private static void LogEnumRowAccess(CsvTable<ScenarioFields> table)
        {
            CsvRow<ScenarioFields> firstRow = table.Row(0);
            string command = firstRow[ScenarioFields.Command].GetString();
            string background = firstRow[ScenarioFields.Arg1].GetString();
            float duration = firstRow[ScenarioFields.Arg2].Get<float>();

            Debug.Log(
                $"[Enum row] Command={command}, Background={background}, Duration={duration}, Rows={table.RowCount}");
        }

        private static void LogMixedValueAccess(CsvTable<ScenarioFields> table)
        {
            CsvIndex<string> commandIndex = CsvIndex<string>.Create(table.Column(ScenarioFields.Command));

            if (commandIndex.TryFindFirst("Wait", out int waitRowIndex))
            {
                int milliseconds = table.Row(waitRowIndex)[ScenarioFields.Arg1].Get<int>();
                Debug.Log($"[Mixed value] Wait.Arg1 as int = {milliseconds}");
            }

            if (commandIndex.TryFindFirst("PlayBGM", out int bgmRowIndex))
            {
                string bgmName = table.Row(bgmRowIndex)[ScenarioFields.Arg1].GetString();
                Debug.Log($"[Mixed value] PlayBGM.Arg1 as string = {bgmName}");
            }
        }

        private static void LogColumnAndIndexAccess(CsvTable<ScenarioFields> table)
        {
            CsvColumn<ScenarioFields> commandColumn = table.Column(ScenarioFields.Command);
            int textCommandCount = 0;

            for (int rowIndex = 0; rowIndex < commandColumn.Count; rowIndex++)
            {
                if (commandColumn[rowIndex].GetString() == "Text") textCommandCount++;
            }

            CsvIndex<string> commandIndex = CsvIndex<string>.Create(commandColumn);
            CsvIndexMatches textRows = commandIndex.FindAll("Text");

            Debug.Log(
                $"[Column / index] Text rows by scan={textCommandCount}, by index={textRows.Count}");
        }

        private static void LogHeaderNameAccess()
        {
            CsvDocument document = CsvParser.Parse("Name,Level\nAlice,12\nBob,8");
            string name = document.Row(0)["Name"].GetString();
            int level = document.Column("Level")[0].Get<int>();

            Debug.Log($"[Header name] Name={name}, Level={level}");
        }

        private static void LogHeaderlessAccess()
        {
            var options = new CsvParseOptions { HasHeader = false };
            CsvDocument document = CsvParser.Parse("Wait,500\nText,Hello", options);

            string command = document.Row(0)[0].GetString();
            int argument = document.Row(0)[1].Get<int>();
            Debug.Log($"[Headerless] Command={command}, Arg={argument}");
        }

        private static void LogRfc4180Access()
        {
            const string csv =
                "Id,Text,Note\r\n" +
                "1,\"first line\r\nsecond line\",\"comma, inside\"\r\n" +
                "2,\"escaped \"\"quote\"\"\",tail";

            CsvDocument document = CsvParser.Parse(csv);
            string multiline = document.Cell(0, "Text").GetString()
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");

            Debug.Log($"[RFC 4180] Text={multiline}, Note={document.Cell(0, "Note").GetString()}");
        }

        private void LogLargeCsvAccess()
        {
            if (hugeDataCsv == null) return;

            CsvTable<HugeDataFields> table = CSVLoader.LoadTable<HugeDataFields>(hugeDataCsv);
            CsvColumn<HugeDataFields> firstColumn = table.Column(HugeDataFields.a);
            Debug.Log($"[Large CSV] Rows={table.RowCount}, Columns={table.ColumnCount}, Column a={firstColumn.Count}");
        }
    }
}
