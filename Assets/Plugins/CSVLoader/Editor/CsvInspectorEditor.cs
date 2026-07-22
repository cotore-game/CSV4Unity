#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using CSV4Unity.Validation;
using UnityEditor;
using UnityEngine;

namespace CSV4Unity.Editor
{
    /// <summary>
    /// CSVのTextAssetに、文字コード変換、Viewer、ValidationのUIを追加します。
    /// </summary>
    [CustomEditor(typeof(TextAsset))]
    public sealed class CsvInspectorEditor : UnityEditor.Editor
    {
        private const string FieldsNamespace = "CSV4Unity.Fields";
        private static readonly CsvSourceEncoding[] SourceEncodingValues =
        {
            CsvSourceEncoding.Auto,
            CsvSourceEncoding.Utf8,
            CsvSourceEncoding.ShiftJis,
            CsvSourceEncoding.Utf16LittleEndian,
            CsvSourceEncoding.Utf16BigEndian,
            CsvSourceEncoding.Utf32LittleEndian,
            CsvSourceEncoding.Utf32BigEndian
        };
        private static readonly string[] SourceEncodingLabels =
        {
            "Auto Detect",
            "UTF-8",
            "Shift_JIS (CP932)",
            "UTF-16 LE",
            "UTF-16 BE",
            "UTF-32 LE",
            "UTF-32 BE"
        };
        private static readonly MethodInfo ValidateDocumentMethod = typeof(CsvInspectorEditor)
            .GetMethod(nameof(ValidateDocument), BindingFlags.NonPublic | BindingFlags.Static);

        private readonly List<Type> _availableEnums = new List<Type>();
        private TextAsset _csvFile;
        private Type _selectedEnumType;
        private int _selectedEnumIndex = -1;
        private CsvValidationResult _validationResult;
        private Vector2 _scrollPosition;
        private bool _showValidationResults;
        private bool _showEncodingPreview;
        private bool _overrideEncodingDetection;
        private bool _isCsv;
        private string _assetPath;
        private CsvSourceEncoding _sourceEncoding;
        private CsvEncodingInspection _automaticEncodingInspection;
        private CsvEncodingInspection _encodingInspection;

        private void OnEnable()
        {
            _csvFile = target as TextAsset;
            _assetPath = AssetDatabase.GetAssetPath(_csvFile);
            _isCsv = string.Equals(Path.GetExtension(_assetPath), ".csv", StringComparison.OrdinalIgnoreCase);
            if (!_isCsv) return;

            RefreshEncodingInspection();
            RefreshEnums();
            RestoreSelection(_assetPath);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!_isCsv || _csvFile == null) return;

            // TextAssetの標準Inspectorは読み取り専用なので、追加UIだけ操作可能にする。
            bool previousEnabled = GUI.enabled;
            GUI.enabled = true;
            try
            {
                DrawCsvControls();
            }
            finally
            {
                GUI.enabled = previousEnabled;
            }
        }

        private void DrawCsvControls()
        {
            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(5);

            DrawEncodingControls();
            if (!IsUtf8Ready())
            {
                EditorGUILayout.HelpBox(
                    "CSV ViewerとValidationを使用する前に、CSVをUTF-8へ変換してください。",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("CSV Viewer", EditorStyles.boldLabel);
            if (GUILayout.Button("Open CSV Viewer", GUILayout.Height(26)))
            {
                CsvViewerWindow.Open(_csvFile);
            }

            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("CSV Validation", EditorStyles.boldLabel);

            if (_availableEnums.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    $"{FieldsNamespace} 名前空間にEnumが見つかりません。",
                    MessageType.Info);

                if (GUILayout.Button("Refresh Enums")) RefreshEnums();
                return;
            }

            DrawSchemaSelector();
            if (_selectedEnumType != null) DrawConstraints(_selectedEnumType);

            EditorGUILayout.Space(10);
            using (new EditorGUI.DisabledScope(_selectedEnumType == null))
            {
                if (GUILayout.Button("Validate CSV", GUILayout.Height(30))) ExecuteValidation();
            }

            if (_selectedEnumType == null)
            {
                EditorGUILayout.HelpBox("検証に使用するEnumスキーマを選択してください。", MessageType.Info);
            }

            if (_showValidationResults && _validationResult != null)
            {
                EditorGUILayout.Space(10);
                DrawValidationResults();
            }
        }

        private void DrawEncodingControls()
        {
            EditorGUILayout.LabelField("CSV Encoding", EditorStyles.boldLabel);

            if (!_automaticEncodingInspection.IsValid)
            {
                EditorGUILayout.HelpBox(
                    $"Encoding could not be detected. {_automaticEncodingInspection.ErrorMessage}",
                    MessageType.Error);
            }
            else if (_automaticEncodingInspection.RequiresConversion)
            {
                EditorGUILayout.HelpBox(
                    $"Detected {_automaticEncodingInspection.DisplayName}. Convert this file to UTF-8 before using it.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Encoding: {_automaticEncodingInspection.DisplayName}",
                    MessageType.Info);
            }

            bool overrideDetection = EditorGUILayout.ToggleLeft(
                "Override automatic detection",
                _overrideEncodingDetection);
            if (overrideDetection != _overrideEncodingDetection)
            {
                _overrideEncodingDetection = overrideDetection;
                if (_overrideEncodingDetection)
                {
                    InspectUsingSelectedEncoding();
                    _showEncodingPreview = true;
                }
                else
                {
                    _encodingInspection = _automaticEncodingInspection;
                }
            }

            if (_overrideEncodingDetection)
            {
                int selectedIndex = Array.IndexOf(SourceEncodingValues, _sourceEncoding);
                int nextIndex = EditorGUILayout.Popup(
                    "Source Encoding",
                    Math.Max(selectedIndex, 0),
                    SourceEncodingLabels);
                CsvSourceEncoding selectedEncoding = SourceEncodingValues[nextIndex];
                if (selectedEncoding != _sourceEncoding)
                {
                    _sourceEncoding = selectedEncoding;
                    InspectUsingSelectedEncoding();
                    _showEncodingPreview = true;
                }

                if (IsOverrideDifferentFromDetection())
                {
                    EditorGUILayout.HelpBox(
                        $"Selected {_encodingInspection.DisplayName}, but automatic detection found " +
                        $"{_automaticEncodingInspection.DisplayName}. Check the preview carefully before converting.",
                        MessageType.Error);
                }

                if (!_encodingInspection.IsValid)
                {
                    EditorGUILayout.HelpBox(_encodingInspection.ErrorMessage, MessageType.Error);
                }
            }

            if (_encodingInspection.IsValid)
            {
                _showEncodingPreview = EditorGUILayout.Foldout(
                    _showEncodingPreview,
                    "Decoded Preview",
                    true);
                if (_showEncodingPreview)
                {
                    EditorGUILayout.SelectableLabel(
                        CreatePreview(_encodingInspection.Text),
                        EditorStyles.textArea,
                        GUILayout.MinHeight(80),
                        GUILayout.MaxHeight(160));
                }
            }

            using (new EditorGUI.DisabledScope(!_encodingInspection.RequiresConversion))
            {
                if (GUILayout.Button(
                        $"Convert {_encodingInspection.DisplayName} to UTF-8",
                        GUILayout.Height(26)))
                {
                    ConvertAssetToUtf8();
                }
            }

            DrawEncodingBackupControls();
        }

        private void RefreshEncodingInspection()
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(GetAbsoluteAssetPath());
                _automaticEncodingInspection = CsvEncodingUtility.Inspect(bytes);
                _encodingInspection = _automaticEncodingInspection;
                _sourceEncoding = _automaticEncodingInspection.IsValid
                    ? _automaticEncodingInspection.Encoding
                    : CsvSourceEncoding.Auto;
                _overrideEncodingDetection = !_automaticEncodingInspection.IsValid;
                _showEncodingPreview = _encodingInspection.RequiresConversion;
            }
            catch (Exception exception)
            {
                _automaticEncodingInspection = new CsvEncodingInspection(
                    CsvSourceEncoding.Auto,
                    false,
                    false,
                    null,
                    exception.Message);
                _encodingInspection = _automaticEncodingInspection;
                _sourceEncoding = CsvSourceEncoding.Auto;
                _overrideEncodingDetection = true;
            }
        }

        private void InspectUsingSelectedEncoding()
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(GetAbsoluteAssetPath());
                _encodingInspection = CsvEncodingUtility.Decode(bytes, _sourceEncoding);
            }
            catch (Exception exception)
            {
                _encodingInspection = new CsvEncodingInspection(
                    _sourceEncoding,
                    false,
                    false,
                    null,
                    exception.Message);
            }
        }

        private void ConvertAssetToUtf8()
        {
            if (!_encodingInspection.RequiresConversion) return;

            bool confirmed = EditorUtility.DisplayDialog(
                "Convert CSV to UTF-8",
                $"{_csvFile.name}.csv を {_encodingInspection.DisplayName} からUTF-8へ変換します。\n" +
                "ファイル内容が更新され、Gitの変更対象になります。" +
                (IsOverrideDifferentFromDetection()
                    ? $"\n\n警告: 自動判定は {_automaticEncodingInspection.DisplayName} です。"
                    : string.Empty),
                "Convert",
                "Cancel");
            if (!confirmed) return;

            try
            {
                string absolutePath = GetAbsoluteAssetPath();
                byte[] source = File.ReadAllBytes(absolutePath);
                CsvSourceEncoding sourceEncoding = _overrideEncodingDetection
                    ? _sourceEncoding
                    : CsvSourceEncoding.Auto;
                byte[] utf8 = CsvEncodingUtility.ConvertToUtf8(source, sourceEncoding);
                CsvEncodingBackupUtility.CreateIfMissing(GetBackupPath(), source);
                CsvEncodingBackupUtility.WriteAtomically(absolutePath, utf8);
                AssetDatabase.ImportAsset(_assetPath, ImportAssetOptions.ForceUpdate);
                _csvFile = AssetDatabase.LoadAssetAtPath<TextAsset>(_assetPath);
                RefreshEncodingInspection();
                _validationResult = null;
                _showValidationResults = false;
                Debug.Log($"CSV4Unity: Converted '{_assetPath}' to UTF-8.", _csvFile);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CSV4Unity: Failed to convert '{_assetPath}' to UTF-8. {exception}", _csvFile);
                EditorUtility.DisplayDialog("CSV Conversion Failed", exception.Message, "OK");
                RefreshEncodingInspection();
            }
        }

        private void DrawEncodingBackupControls()
        {
            if (!File.Exists(GetBackupPath())) return;

            EditorGUILayout.HelpBox(
                "The original bytes from before the first conversion are available as a backup.",
                MessageType.Info);
            if (GUILayout.Button("Restore Pre-conversion File")) RestoreEncodingBackup();
        }

        private void RestoreEncodingBackup()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Restore CSV Before Conversion",
                $"{_csvFile.name}.csv を最初の文字コード変換前の状態へ戻します。",
                "Restore",
                "Cancel");
            if (!confirmed) return;

            try
            {
                CsvEncodingBackupUtility.Restore(GetBackupPath(), GetAbsoluteAssetPath());
                AssetDatabase.ImportAsset(_assetPath, ImportAssetOptions.ForceUpdate);
                _csvFile = AssetDatabase.LoadAssetAtPath<TextAsset>(_assetPath);
                RefreshEncodingInspection();
                _validationResult = null;
                _showValidationResults = false;
                Debug.Log($"CSV4Unity: Restored the pre-conversion file for '{_assetPath}'.", _csvFile);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CSV4Unity: Failed to restore '{_assetPath}'. {exception}", _csvFile);
                EditorUtility.DisplayDialog("CSV Restore Failed", exception.Message, "OK");
            }
        }

        private bool IsOverrideDifferentFromDetection()
        {
            return _overrideEncodingDetection &&
                   _automaticEncodingInspection.IsValid &&
                   _sourceEncoding != CsvSourceEncoding.Auto &&
                   _sourceEncoding != _automaticEncodingInspection.Encoding;
        }

        private bool IsUtf8Ready()
        {
            return _encodingInspection.IsValid &&
                   _encodingInspection.Encoding == CsvSourceEncoding.Utf8;
        }

        private string GetAbsoluteAssetPath()
        {
            return Path.GetFullPath(_assetPath);
        }

        private string GetBackupPath()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            string assetGuid = AssetDatabase.AssetPathToGUID(_assetPath);
            return Path.Combine(
                projectRoot ?? Path.GetFullPath("."),
                "Library",
                "CSV4Unity",
                "EncodingBackups",
                assetGuid + ".bytes");
        }

        private static string CreatePreview(string text)
        {
            const int maxLength = 2000;
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text ?? string.Empty;
            return text.Substring(0, maxLength) + "\n...";
        }

        private void DrawSchemaSelector()
        {
            string[] options = new string[_availableEnums.Count + 1];
            options[0] = "None";
            for (int i = 0; i < _availableEnums.Count; i++)
            {
                Type enumType = _availableEnums[i];
                options[i + 1] = enumType.FullName ?? enumType.Name;
            }

            int popupIndex = EditorGUILayout.Popup("Validation Schema", _selectedEnumIndex + 1, options);
            int enumIndex = popupIndex - 1;
            if (enumIndex == _selectedEnumIndex) return;

            _selectedEnumIndex = enumIndex;
            _selectedEnumType = enumIndex >= 0 ? _availableEnums[enumIndex] : null;
            _validationResult = null;
            _showValidationResults = false;
            SaveSelection();
        }

        private void DrawConstraints(Type enumType)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Schema Preview: {enumType.Name}", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            bool hasConstraints = false;
            FieldInfo[] fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < fields.Length; i++)
            {
                object[] attributes = fields[i].GetCustomAttributes(false);
                ConditionAttribute[] conditions = attributes.OfType<ConditionAttribute>().ToArray();
                CsvValidationAttribute[] validations = attributes.OfType<CsvValidationAttribute>().ToArray();

                if (conditions.Length == 0 && validations.Length == 0) continue;
                if (hasConstraints)
                {
                    EditorGUILayout.Space(5);
                    DrawSeparator();
                    EditorGUILayout.Space(5);
                }

                hasConstraints = true;
                DrawFieldConstraints(fields[i].Name, conditions, validations);
            }

            if (!hasConstraints)
            {
                EditorGUILayout.LabelField("No validation constraints are defined.", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawFieldConstraints(
            string fieldName,
            IReadOnlyList<ConditionAttribute> conditions,
            IReadOnlyList<CsvValidationAttribute> validations)
        {
            EditorGUILayout.LabelField(fieldName, EditorStyles.boldLabel);

            int[] groups = conditions
                .Select(condition => condition.Group)
                .Concat(validations.Select(validation => validation.ConditionGroup))
                .Distinct()
                .OrderBy(group => group)
                .ToArray();

            GUIStyle expressionStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                padding = new RectOffset(8, 4, 1, 1)
            };

            for (int i = 0; i < groups.Length; i++)
            {
                int group = groups[i];
                ConditionAttribute[] groupConditions = conditions
                    .Where(condition => condition.Group == group)
                    .ToArray();
                CsvValidationAttribute[] groupValidations = validations
                    .Where(validation => validation.ConditionGroup == group)
                    .ToArray();

                if (i > 0) EditorGUILayout.Space(4);

                string conditionExpression = groupConditions.Length == 0
                    ? group == 0 ? "ALWAYS" : $"IF (GROUP {group} HAS NO CONDITION)"
                    : $"IF ({string.Join(" && ", groupConditions.Select(FormatCondition))})";
                EditorGUILayout.LabelField(conditionExpression, expressionStyle);

                string validationExpression = groupValidations.Length == 0
                    ? "NO CONSTRAINT"
                    : string.Join(" && ", groupValidations.Select(GetValidationDisplayText));
                EditorGUILayout.LabelField(
                    $"=> {fieldName}: {validationExpression}",
                    expressionStyle);
            }
        }

        private static string FormatCondition(ConditionAttribute condition)
        {
            string field = condition.Field?.ToString() ?? "null";
            string suffix = condition.IgnoreCase ? " [IGNORE CASE]" : string.Empty;

            switch (condition.Comparison)
            {
                case Compare.Equal:
                    return $"{field} == {FormatSingleValue(condition)}{suffix}";
                case Compare.NotEqual:
                    return $"{field} != {FormatSingleValue(condition)}{suffix}";
                case Compare.GreaterThan:
                    return $"{field} > {FormatSingleValue(condition)}";
                case Compare.GreaterThanOrEqual:
                    return $"{field} >= {FormatSingleValue(condition)}";
                case Compare.LessThan:
                    return $"{field} < {FormatSingleValue(condition)}";
                case Compare.LessThanOrEqual:
                    return $"{field} <= {FormatSingleValue(condition)}";
                case Compare.IsEmpty:
                    return $"{field} IS EMPTY";
                case Compare.IsNotEmpty:
                    return $"{field} IS NOT EMPTY";
                case Compare.In:
                    return $"{field} IN ({string.Join(", ", condition.Values.Select(FormatValue))}){suffix}";
                case Compare.NotIn:
                    return $"{field} NOT IN ({string.Join(", ", condition.Values.Select(FormatValue))}){suffix}";
                default:
                    return $"{field} {condition.Comparison}";
            }
        }

        private static string FormatSingleValue(ConditionAttribute condition)
        {
            return condition.Values.Length == 0 ? "<?>" : FormatValue(condition.Values[0]);
        }

        private static string FormatValue(object value)
        {
            switch (value)
            {
                case null:
                    return "null";
                case string text:
                    return $"\"{EscapeValue(text)}\"";
                case char character:
                    return $"'{EscapeValue(character.ToString())}'";
                case bool boolean:
                    return boolean ? "true" : "false";
                case Enum enumValue:
                    return enumValue.ToString();
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return value.ToString();
            }
        }

        private static string EscapeValue(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private void ExecuteValidation()
        {
            if (_selectedEnumType == null || _csvFile == null) return;

            try
            {
                var options = new CsvParseOptions
                {
                    HasHeader = true,
                    IgnoreEmptyRecords = true,
                    TrimUnquotedFields = true
                };

                CsvDocument document = CSVLoader.LoadDocument(_csvFile, options);
                MethodInfo method = ValidateDocumentMethod.MakeGenericMethod(_selectedEnumType);
                _validationResult = (CsvValidationResult)method.Invoke(null, new object[] { document });
            }
            catch (Exception exception)
            {
                Exception cause = exception is TargetInvocationException && exception.InnerException != null
                    ? exception.InnerException
                    : exception;

                _validationResult = new CsvValidationResult();
                _validationResult.AddError(-1, "CSV", cause.Message);
                Debug.LogError($"CSV validation failed: {cause}", _csvFile);
            }

            _showValidationResults = true;
        }

        private void DrawValidationResults()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            MessageType summaryType = _validationResult.IsValid ? MessageType.Info : MessageType.Error;
            string summary = _validationResult.IsValid
                ? $"Validation passed. Warnings: {_validationResult.Warnings.Count}"
                : $"Validation failed. Errors: {_validationResult.Errors.Count}, Warnings: {_validationResult.Warnings.Count}";
            EditorGUILayout.HelpBox(summary, summaryType);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(300));
            DrawIssues("Errors", _validationResult.Errors);
            DrawIssues("Warnings", _validationResult.Warnings);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy Report")) CopyReportToClipboard();
            if (GUILayout.Button("Clear"))
            {
                _validationResult = null;
                _showValidationResults = false;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private static void DrawIssues<TIssue>(string heading, IReadOnlyList<TIssue> issues)
        {
            if (issues.Count == 0) return;

            EditorGUILayout.LabelField(heading, EditorStyles.boldLabel);
            for (int i = 0; i < issues.Count; i++)
            {
                EditorGUILayout.LabelField(issues[i].ToString(), EditorStyles.wordWrappedLabel);
            }
        }

        private void CopyReportToClipboard()
        {
            var lines = new List<string>
            {
                $"CSV Validation Report: {_csvFile.name}",
                $"Schema: {_selectedEnumType?.FullName}",
                $"Errors: {_validationResult.Errors.Count}",
                $"Warnings: {_validationResult.Warnings.Count}"
            };

            lines.AddRange(_validationResult.Errors.Select(error => $"[ERROR] {error}"));
            lines.AddRange(_validationResult.Warnings.Select(warning => $"[WARNING] {warning}"));
            GUIUtility.systemCopyBuffer = string.Join("\n", lines);
        }

        private void RefreshEnums()
        {
            _availableEnums.Clear();

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                foreach (Type type in GetLoadableTypes(assemblies[i]))
                {
                    if (type.IsEnum && type.Namespace != null &&
                        type.Namespace.StartsWith(FieldsNamespace, StringComparison.Ordinal))
                    {
                        _availableEnums.Add(type);
                    }
                }
            }

            _availableEnums.Sort((left, right) =>
                string.Compare(left.FullName, right.FullName, StringComparison.Ordinal));
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        private void RestoreSelection(string assetPath)
        {
            string selectedTypeName = EditorPrefs.GetString(GetSelectionKey(assetPath), string.Empty);
            if (string.IsNullOrEmpty(selectedTypeName)) return;

            _selectedEnumIndex = _availableEnums.FindIndex(type => type.AssemblyQualifiedName == selectedTypeName);
            _selectedEnumType = _selectedEnumIndex >= 0 ? _availableEnums[_selectedEnumIndex] : null;
        }

        private void SaveSelection()
        {
            string key = GetSelectionKey(AssetDatabase.GetAssetPath(_csvFile));
            if (_selectedEnumType == null)
            {
                EditorPrefs.DeleteKey(key);
                return;
            }

            EditorPrefs.SetString(key, _selectedEnumType.AssemblyQualifiedName);
        }

        private static string GetSelectionKey(string assetPath)
        {
            return $"CSV4Unity.SelectedEnum.{AssetDatabase.AssetPathToGUID(assetPath)}";
        }

        private static string GetValidationDisplayText(CsvValidationAttribute attribute)
        {
            switch (attribute)
            {
                case PrimaryKeyAttribute:
                    return "PRIMARY KEY";
                case NotNullAttribute:
                    return "VALUE IS NOT EMPTY";
                case UniqueAttribute:
                    return "UNIQUE";
                case TypeConstraintAttribute typeConstraint:
                    return $"TYPE = {typeConstraint.ExpectedType.Name}";
                case Validation.RangeAttribute range:
                    return $"{FormatValue(range.Min)} <= VALUE <= {FormatValue(range.Max)}";
                case RegexAttribute regex:
                    return $"MATCHES {FormatValue(regex.Pattern)}";
                case AllowedValuesAttribute allowed:
                    return $"VALUE IN ({string.Join(", ", allowed.AllowedValues.Select(FormatValue))})";
                case MinLengthAttribute minLength:
                    return $"LENGTH >= {minLength.MinLength}";
                case MaxLengthAttribute maxLength:
                    return $"LENGTH <= {maxLength.MaxLength}";
                default:
                    return attribute.GetType().Name;
            }
        }

        private static CsvValidationResult ValidateDocument<TField>(CsvDocument document)
            where TField : struct, Enum
        {
            return CsvValidator.Validate(document.WithFields<TField>());
        }

        private static void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f));
        }
    }
}
#endif
