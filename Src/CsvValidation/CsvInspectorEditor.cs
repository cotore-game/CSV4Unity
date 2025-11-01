#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CSV4Unity.Validation;

namespace CSV4Unity.Editor
{
    /// <summary>
    /// TextAsset（CSV）のカスタムインスペクター
    /// CSV4Unity.Fields名前空間のEnumを自動検出してバリデーション機能を提供
    /// </summary>
    [CustomEditor(typeof(TextAsset))]
    public class CsvInspectorEditor : UnityEditor.Editor
    {
        private const string FIELDS_NAMESPACE = "CSV4Unity.Fields";

        private List<Type> _availableEnums;
        private int _selectedEnumIndex = -1;
        private Type _selectedEnumType;
        private CsvValidationResult _validationResult;
        private Vector2 _scrollPosition;
        private bool _showValidationResults = false;

        private TextAsset _csvFile;
        private string _lastCsvContent;

        private void OnEnable()
        {
            _csvFile = target as TextAsset;
            _lastCsvContent = _csvFile?.text;

            // CSV4Unity.Fields名前空間からEnum型を取得
            _availableEnums = FindEnumsInNamespace(FIELDS_NAMESPACE);

            // 前回選択されたEnumを復元（EditorPrefs使用）
            var savedEnumName = EditorPrefs.GetString($"CSV4Unity_SelectedEnum_{_csvFile?.name}", "");
            if (!string.IsNullOrEmpty(savedEnumName))
            {
                var savedEnum = _availableEnums.FirstOrDefault(e => e.FullName == savedEnumName);
                if (savedEnum != null)
                {
                    _selectedEnumIndex = _availableEnums.IndexOf(savedEnum);
                    _selectedEnumType = savedEnum;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            // デフォルトインスペクターを表示
            DrawDefaultInspector();

            /*
            // CSVファイルでない場合は何もしない
            if (_csvFile == null || !_csvFile.name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            */

            if (_csvFile == null)
            {
                return;
            }
            GUI.enabled = true;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("CSV Validation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "CSV4Unity.Fields名前空間にEnum型を定義することで、\n" +
                "そのEnumを使ってバリデーションを実行できます。",
                MessageType.Info
            );

            // Enum選択がない場合
            if (_availableEnums == null || _availableEnums.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "CSV4Unity.Fields名前空間にEnum型が見つかりません。\n" +
                    "例: namespace CSV4Unity.Fields { public enum MyFields { ... } }",
                    MessageType.Warning
                );

                if (GUILayout.Button("Refresh Enums"))
                {
                    _availableEnums = FindEnumsInNamespace(FIELDS_NAMESPACE);
                }
                return;
            }

            // Enum選択ドロップダウン
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Validation Schema:", GUILayout.Width(120));

            var enumNames = _availableEnums.Select(e => e.Name).ToArray();
            var newIndex = EditorGUILayout.Popup(_selectedEnumIndex, enumNames);

            if (newIndex != _selectedEnumIndex)
            {
                _selectedEnumIndex = newIndex;
                _selectedEnumType = newIndex >= 0 ? _availableEnums[newIndex] : null;
                _validationResult = null;
                _showValidationResults = false;

                // 選択を保存
                if (_selectedEnumType != null)
                {
                    EditorPrefs.SetString($"CSV4Unity_SelectedEnum_{_csvFile.name}", _selectedEnumType.FullName);
                }
            }
            EditorGUILayout.EndHorizontal();

            // 選択されたEnumの制約情報を表示
            if (_selectedEnumType != null)
            {
                EditorGUILayout.Space(5);
                DisplayEnumConstraints(_selectedEnumType);
            }

            EditorGUILayout.Space(10);

            // バリデーション実行ボタン
            GUI.enabled = _selectedEnumType != null;
            if (GUILayout.Button("Validate CSV", GUILayout.Height(30)))
            {
                ExecuteValidation();
            }
            GUI.enabled = true;

            // バリデーション結果の表示
            if (_showValidationResults && _validationResult != null)
            {
                EditorGUILayout.Space(10);
                DisplayValidationResults();
            }
        }

        /// <summary>
        /// 指定された名前空間からEnum型を検索
        /// </summary>
        private List<Type> FindEnumsInNamespace(string namespaceName)
        {
            var enumTypes = new List<Type>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.IsEnum &&
                            type.Namespace != null &&
                            type.Namespace.StartsWith(namespaceName))
                        {
                            enumTypes.Add(type);
                        }
                    }
                }
                catch
                {
                    // アセンブリの読み込みエラーは無視
                }
            }

            return enumTypes.OrderBy(t => t.Name).ToList();
        }

        /// <summary>
        /// Enumに定義された制約情報を表示
        /// </summary>
        private void DisplayEnumConstraints(Type enumType)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Constraints for {enumType.Name}:", EditorStyles.boldLabel);

            var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
            bool hasConstraints = false;

            foreach (var field in fields)
            {
                var attributes = field.GetCustomAttributes().ToList();
                if (attributes.Count > 0)
                {
                    hasConstraints = true;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"• {field.Name}:", GUILayout.Width(150));

                    var constraintTexts = new List<string>();
                    foreach (var attr in attributes)
                    {
                        constraintTexts.Add(GetAttributeDisplayText(attr));
                    }
                    EditorGUILayout.LabelField(string.Join(", ", constraintTexts));
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (!hasConstraints)
            {
                EditorGUILayout.LabelField("No constraints defined", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 属性の表示テキストを取得
        /// </summary>
        private string GetAttributeDisplayText(Attribute attr)
        {
            return attr switch
            {
                PrimaryKeyAttribute => "[PrimaryKey]",
                NotNullAttribute => "[NotNull]",
                UniqueAttribute => "[Unique]",
                TypeConstraintAttribute typeAttr => $"[Type: {typeAttr.ExpectedType.Name}]",
                Validation.RangeAttribute rangeAttr => $"[Range: {rangeAttr.Min}-{rangeAttr.Max}]",
                RegexAttribute regexAttr => $"[Regex: {regexAttr.Pattern}]",
                AllowedValuesAttribute allowedAttr => $"[Allowed: {string.Join("|", allowedAttr.AllowedValues)}]",
                MinLengthAttribute minAttr => $"[MinLen: {minAttr.MinLength}]",
                MaxLengthAttribute maxAttr => $"[MaxLen: {maxAttr.MaxLength}]",
                _ => attr.GetType().Name
            };
        }

        /// <summary>
        /// バリデーションを実行
        /// </summary>
        private void ExecuteValidation()
        {
            if (_selectedEnumType == null || _csvFile == null)
            {
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("CSV Validation", "Loading CSV...", 0.3f);

                // CSVを読み込み
                var options = new CsvLoaderOptions
                {
                    HasHeader = true,
                    TrimFields = true,
                    IgnoreEmptyLines = true
                };

                // リフレクションを使ってLoadCSVを呼び出し
                var loaderType = typeof(CSVLoader);
                var loadMethod = loaderType.GetMethod("LoadCSV", new[] { typeof(TextAsset), typeof(CsvLoaderOptions), typeof(string) });
                var genericMethod = loadMethod.MakeGenericMethod(_selectedEnumType);

                var csvData = genericMethod.Invoke(null, new object[] { _csvFile, options, null });

                EditorUtility.DisplayProgressBar("CSV Validation", "Validating data...", 0.6f);

                // バリデーション実行
                var validatorType = typeof(CsvValidator);
                var validateMethod = validatorType.GetMethod("Validate", new[] { csvData.GetType() });

                _validationResult = (CsvValidationResult)validateMethod.Invoke(null, new[] { csvData });
                _showValidationResults = true;

                EditorUtility.ClearProgressBar();

                // 結果サマリーをログ出力
                if (_validationResult.IsValid)
                {
                    Debug.Log($"<color=green>✓</color> CSV Validation passed for '{_csvFile.name}'");
                }
                else
                {
                    Debug.LogWarning($"<color=red>✗</color> CSV Validation failed for '{_csvFile.name}': {_validationResult.Errors.Count} error(s)");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                _validationResult = new CsvValidationResult();
                _validationResult.AddError(0, "System", $"Validation error: {ex.Message}");
                _showValidationResults = true;
                Debug.LogError($"Validation exception: {ex}");
            }
        }

        /// <summary>
        /// バリデーション結果を表示
        /// </summary>
        private void DisplayValidationResults()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // サマリー
            var summaryStyle = new GUIStyle(EditorStyles.boldLabel);
            if (_validationResult.IsValid)
            {
                summaryStyle.normal.textColor = new Color(0f, 0.6f, 0f);
                EditorGUILayout.LabelField("✓ Validation Passed", summaryStyle);
            }
            else
            {
                summaryStyle.normal.textColor = new Color(0.8f, 0f, 0f);
                EditorGUILayout.LabelField($"✗ Validation Failed", summaryStyle);
                EditorGUILayout.LabelField($"{_validationResult.Errors.Count} Error(s), {_validationResult.Warnings.Count} Warning(s)");
            }

            EditorGUILayout.Space(5);

            // エラーと警告をスクロール表示
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(300));

            // エラー表示
            if (_validationResult.Errors.Count > 0)
            {
                EditorGUILayout.LabelField("Errors:", EditorStyles.boldLabel);

                foreach (var error in _validationResult.Errors)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    // エラーアイコン
                    var iconContent = EditorGUIUtility.IconContent("console.erroricon.sml");
                    GUILayout.Label(iconContent, GUILayout.Width(20), GUILayout.Height(20));

                    // エラーメッセージ
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField($"Row {error.Row + 1}, Column '{error.Column}'", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(error.Message, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndHorizontal();
                }
            }

            // 警告表示
            if (_validationResult.Warnings.Count > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Warnings:", EditorStyles.boldLabel);

                foreach (var warning in _validationResult.Warnings)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    // 警告アイコン
                    var iconContent = EditorGUIUtility.IconContent("console.warnicon.sml");
                    GUILayout.Label(iconContent, GUILayout.Width(20), GUILayout.Height(20));

                    // 警告メッセージ
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField($"Row {warning.Row + 1}, Column '{warning.Column}'", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(warning.Message, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();

            // エクスポートボタン
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy Error Report to Clipboard"))
            {
                CopyErrorReportToClipboard();
            }
            if (GUILayout.Button("Clear Results"))
            {
                _validationResult = null;
                _showValidationResults = false;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// エラーレポートをクリップボードにコピー
        /// </summary>
        private void CopyErrorReportToClipboard()
        {
            if (_validationResult == null) return;

            var report = $"CSV Validation Report: {_csvFile.name}\n";
            report += $"Schema: {_selectedEnumType?.Name}\n";
            report += $"Date: {DateTime.Now}\n";
            report += "=" + new string('=', 50) + "\n\n";

            if (_validationResult.IsValid)
            {
                report += "✓ All validations passed!\n";
            }
            else
            {
                report += $"✗ {_validationResult.Errors.Count} Error(s)\n\n";

                foreach (var error in _validationResult.Errors)
                {
                    report += $"[ERROR] {error}\n";
                }
            }

            if (_validationResult.Warnings.Count > 0)
            {
                report += $"\n⚠ {_validationResult.Warnings.Count} Warning(s)\n\n";
                foreach (var warning in _validationResult.Warnings)
                {
                    report += $"[WARNING] {warning}\n";
                }
            }

            GUIUtility.systemCopyBuffer = report;
            Debug.Log("Validation report copied to clipboard");
        }
    }
}
#endif
