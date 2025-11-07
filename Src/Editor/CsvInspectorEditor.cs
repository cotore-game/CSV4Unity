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
            if (_csvFile != null)
            {
                var savedEnumName = EditorPrefs.GetString($"CSV4Unity_SelectedEnum_{_csvFile.name}", "");
                if (!string.IsNullOrEmpty(savedEnumName) && _availableEnums != null)
                {
                    var savedEnum = _availableEnums.FirstOrDefault(e => e.FullName == savedEnumName);
                    if (savedEnum != null)
                    {
                        _selectedEnumIndex = _availableEnums.IndexOf(savedEnum);
                        _selectedEnumType = savedEnum;
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            // デフォルトインスペクターを表示
            DrawDefaultInspector();

            if (_csvFile == null)
            {
                return;
            }

            EditorGUILayout.Space(10);

            // セパレータ
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));

            EditorGUILayout.Space(5);
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
                    _selectedEnumIndex = -1;
                    _selectedEnumType = null;
                    Repaint();
                }
                return;
            }

            // Enum選択ドロップダウン（確実に有効化）
            EditorGUILayout.Space(5);

            // 選択肢を作成
            string[] popupOptions = new string[_availableEnums.Count + 1];
            popupOptions[0] = "None (No validation)";
            for (int i = 0; i < _availableEnums.Count; i++)
            {
                popupOptions[i + 1] = _availableEnums[i].Name;
            }

            // 現在の選択インデックス（+1はNoneのオフセット）
            int currentPopupIndex = _selectedEnumIndex + 1;

            // GUIの状態を確実に有効化
            bool wasEnabled = GUI.enabled;
            GUI.enabled = true;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Validation Schema:");

            // Popupを表示
            int newPopupIndex = EditorGUILayout.Popup(currentPopupIndex, popupOptions);

            EditorGUILayout.EndHorizontal();

            // GUIの状態を復元
            GUI.enabled = wasEnabled;

            // 選択が変更された場合
            if (newPopupIndex != currentPopupIndex)
            {
                int newEnumIndex = newPopupIndex - 1;
                _selectedEnumIndex = newEnumIndex;
                _selectedEnumType = newEnumIndex >= 0 ? _availableEnums[newEnumIndex] : null;
                _validationResult = null;
                _showValidationResults = false;

                // 選択を保存
                if (_selectedEnumType != null)
                {
                    EditorPrefs.SetString($"CSV4Unity_SelectedEnum_{_csvFile.name}", _selectedEnumType.FullName);
                    Debug.Log($"Selected validation schema: {_selectedEnumType.Name}");
                }
                else
                {
                    EditorPrefs.DeleteKey($"CSV4Unity_SelectedEnum_{_csvFile.name}");
                    Debug.Log("Validation schema cleared");
                }

                Repaint();
            }

            // 選択されたEnumの制約情報を表示
            if (_selectedEnumType != null)
            {
                EditorGUILayout.Space(5);
                DisplayEnumConstraints(_selectedEnumType);
            }
            else
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("バリデーションスキーマが選択されていません。", MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // バリデーション実行ボタン
            bool canValidate = _selectedEnumType != null;
            GUI.enabled = canValidate;

            if (GUILayout.Button("Validate CSV", GUILayout.Height(30)))
            {
                ExecuteValidation();
            }

            GUI.enabled = true;

            if (!canValidate)
            {
                EditorGUILayout.HelpBox("バリデーションを実行するには、Validation Schemaを選択してください。", MessageType.Warning);
            }

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

            try
            {
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
                    catch (ReflectionTypeLoadException ex)
                    {
                        // 一部の型のロードに失敗しても続行
                        foreach (var loadedType in ex.Types)
                        {
                            if (loadedType != null &&
                                loadedType.IsEnum &&
                                loadedType.Namespace != null &&
                                loadedType.Namespace.StartsWith(namespaceName))
                            {
                                enumTypes.Add(loadedType);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"列挙型の検出中にアセンブリエラーが発生しました: {ex.Message} \n {ex.StackTrace}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error finding enums: {ex.Message}");
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
                    IgnoreEmptyLines = true,
                    ValidationEnabled = false  // エディタでの手動バリデーション時は無効化
                };

                // リフレクションを使ってLoadCSVを呼び出し
                var loaderType = typeof(CSVLoader);

                // ジェネリック版のLoadCSVメソッドを明示的に取得
                var methods = loaderType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                MethodInfo loadMethod = null;

                foreach (var method in methods)
                {
                    if (method.Name == "LoadCSV" &&
                        method.IsGenericMethodDefinition &&
                        method.GetParameters().Length == 3)
                    {
                        loadMethod = method;
                        break;
                    }
                }

                if (loadMethod == null)
                {
                    throw new Exception("LoadCSV generic method not found");
                }

                var genericMethod = loadMethod.MakeGenericMethod(_selectedEnumType);
                var csvData = genericMethod.Invoke(null, new object[] { _csvFile, options, null });

                EditorUtility.DisplayProgressBar("CSV Validation", "Validating data...", 0.6f);

                // バリデーション実行
                var validatorType = typeof(CsvValidator);
                var validateMethods = validatorType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                MethodInfo validateMethod = null;

                foreach (var method in validateMethods)
                {
                    if (method.Name == "Validate" &&
                        method.IsGenericMethodDefinition &&
                        method.GetParameters().Length == 1)
                    {
                        validateMethod = method;
                        break;
                    }
                }

                if (validateMethod == null)
                {
                    throw new Exception("Validate generic method not found");
                }

                var genericValidateMethod = validateMethod.MakeGenericMethod(_selectedEnumType);
                _validationResult = (CsvValidationResult)genericValidateMethod.Invoke(null, new[] { csvData });
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

                // 内部例外を取得（リフレクション呼び出しの場合）
                var innerException = ex.InnerException ?? ex;
                _validationResult.AddError(0, "System", $"Validation error: {innerException.Message}");
                _showValidationResults = true;

                Debug.LogError($"Validation exception: {innerException}");
                Debug.LogError($"Stack trace: {innerException.StackTrace}");
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
                Repaint();
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
