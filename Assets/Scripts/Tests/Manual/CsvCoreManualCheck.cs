using System;
using CSV4Unity.Examples;
using CSV4Unity.Validation;
using UnityEngine;

namespace CSV4Unity.Tests.Manual
{
    /// <summary>
    /// Inspectorで指定した実CSVを使い、新しいCSVコアをUnity上で手動確認します。
    /// </summary>
    public sealed class CsvCoreManualCheck : MonoBehaviour
    {
        [Header("CSV Fixtures")]
        [SerializeField]
        [Tooltip("Assets/TestData/CSV4Unity/Scenario.csv を指定してください")]
        private TextAsset scenarioCsv;

        [SerializeField]
        [Tooltip("Assets/TestData/CSV4Unity/Rfc4180.csv を指定してください")]
        private TextAsset rfc4180Csv;

        [SerializeField]
        [Tooltip("Assets/TestData/CSV4Unity/ValidationInvalid.csv を指定してください")]
        private TextAsset invalidValidationCsv;

        [SerializeField]
        [Tooltip("Assets/TestData/CSV4Unity/ConditionalValidation.csv を指定してください")]
        private TextAsset conditionalValidationCsv;

        [SerializeField]
        [Tooltip("Assets/TestData/CSV4Unity/HugeData.csv を指定してください")]
        private TextAsset hugeDataCsv;

        [SerializeField]
        [Tooltip("Assets/TestData/CSV4Unity/HeaderMapping.csv を指定してください")]
        private TextAsset headerMappingCsv;

        [SerializeField]
        private bool runOnStart = true;

        private void Start()
        {
            if (runOnStart) RunChecks();
        }

        [ContextMenu("Run CSV4Unity Core Checks")]
        public void RunChecks()
        {
            int passed = 0;

            try
            {
                EnsureFixturesAssigned();
                CheckScenarioAccess(ref passed);
                CheckRfc4180(ref passed);
                CheckValidation(ref passed);
                CheckConditionalValidation(ref passed);
                CheckLargeCsv(ref passed);
                CheckHeaderMapping(ref passed);

                Debug.Log($"[CSV4Unity Check] PASS: {passed} checks completed.", this);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[CSV4Unity Check] FAIL after {passed} checks.\n{exception}", this);
            }
        }

        private void CheckScenarioAccess(ref int passed)
        {
            CsvTable<ScenarioFields> table = CSVLoader.LoadTable<ScenarioFields>(scenarioCsv);
            CsvIndex<string> commandIndex = CsvIndex<string>.Create(table.Column(ScenarioFields.Command));

            Require(table.RowCount == 21, $"Scenario.csvの行数が不正です: {table.RowCount}");
            Require(table.Row(0)[ScenarioFields.Command].GetString() == "Bg", "先頭Commandを取得できません。");
            Require(commandIndex.FindAll("Text").Count == 10, "Textコマンドの件数が不正です。");
            Require(commandIndex.FindAll("#CameraShake").Count == 1, "#CameraShakeがデータとして保持されていません。");

            passed += 4;
        }

        private void CheckRfc4180(ref int passed)
        {
            CsvTable<Rfc4180Fields> table = CSVLoader.LoadTable<Rfc4180Fields>(rfc4180Csv);

            Require(table.RowCount == 2, "クォート内改行を含む行数が不正です。");
            Require(table.Row(0)[Rfc4180Fields.Text].GetString() == "first line\nsecond line", "クォート内改行を復元できません。");
            Require(table.Row(0)[Rfc4180Fields.Note].GetString() == "comma, inside", "クォート内カンマを保持できません。");
            Require(table.Row(1)[Rfc4180Fields.Text].GetString() == "escaped \"quote\"", "二重引用符を復元できません。");

            passed += 4;
        }

        private void CheckValidation(ref int passed)
        {
            CsvTable<ManualValidationFields> table = CSVLoader
                .LoadTable<ManualValidationFields>(invalidValidationCsv);
            CsvValidationResult result = CsvValidator.Validate(table);

            Require(!result.IsValid, "ValidationInvalid.csvがValidとして扱われました。");
            Require(result.Errors.Count == 6, $"想定エラー数は6件ですが、実際は{result.Errors.Count}件です。");

            passed += 2;
        }

        private void CheckLargeCsv(ref int passed)
        {
            CsvTable<HugeDataFields> table = CSVLoader.LoadTable<HugeDataFields>(hugeDataCsv);

            Require(table.RowCount == 1000, $"HugeData.csvの行数が不正です: {table.RowCount}");
            Require(table.ColumnCount == 21, $"HugeData.csvの列数が不正です: {table.ColumnCount}");

            passed += 2;
        }

        private void CheckConditionalValidation(ref int passed)
        {
            CsvTable<ConditionalValidationFields> table = CSVLoader
                .LoadTable<ConditionalValidationFields>(conditionalValidationCsv);
            CsvValidationResult result = CsvValidator.Validate(table);

            Require(!result.IsValid, "ConditionalValidation.csvがValidとして扱われました。");
            Require(result.Errors.Count == 4, $"条件付きValidationの想定エラー数は4件ですが、実際は{result.Errors.Count}件です。");

            passed += 2;
        }

        private void CheckHeaderMapping(ref int passed)
        {
            CsvTable<HeaderMappingFields> table = CSVLoader.LoadTable<HeaderMappingFields>(headerMappingCsv);

            Require(table.Row(0)[HeaderMappingFields.Id].GetInt32() == 10, "CsvHeaderでItem IDをIdへ対応付けられません。");
            Require(table.Row(0)[HeaderMappingFields.DisplayName].GetString() == "Potion", "CsvHeaderPatternでdisplay-nameを対応付けられません。");
            Require(table.Row(0)[HeaderMappingFields.Enabled].GetBoolean(), "CsvHeaderのIgnoreCaseが機能していません。");

            passed += 3;
        }

        private void EnsureFixturesAssigned()
        {
            Require(scenarioCsv != null, "scenarioCsv に Scenario.csv を指定してください。");
            Require(rfc4180Csv != null, "rfc4180Csv に Rfc4180.csv を指定してください。");
            Require(invalidValidationCsv != null, "invalidValidationCsv に ValidationInvalid.csv を指定してください。");
            Require(conditionalValidationCsv != null, "conditionalValidationCsv に ConditionalValidation.csv を指定してください。");
            Require(hugeDataCsv != null, "hugeDataCsv に HugeData.csv を指定してください。");
            Require(headerMappingCsv != null, "headerMappingCsv に HeaderMapping.csv を指定してください。");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
