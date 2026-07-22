#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CSV4Unity.Validation;
using UnityEditor;
using UnityEngine;

namespace CSV4Unity.Editor
{
    /// <summary>
    /// CSVのTextAssetに、Enumスキーマを使った検証UIを追加します。
    /// </summary>
    [CustomEditor(typeof(TextAsset))]
    public sealed class CsvInspectorEditor : UnityEditor.Editor
    {
        private const string FieldsNamespace = "CSV4Unity.Fields";
        private static readonly MethodInfo ValidateDocumentMethod = typeof(CsvInspectorEditor)
            .GetMethod(nameof(ValidateDocument), BindingFlags.NonPublic | BindingFlags.Static);

        private readonly List<Type> _availableEnums = new List<Type>();
        private TextAsset _csvFile;
        private Type _selectedEnumType;
        private int _selectedEnumIndex = -1;
        private CsvValidationResult _validationResult;
        private Vector2 _scrollPosition;
        private bool _showValidationResults;
        private bool _isCsv;

        private void OnEnable()
        {
            _csvFile = target as TextAsset;
            string assetPath = AssetDatabase.GetAssetPath(_csvFile);
            _isCsv = string.Equals(Path.GetExtension(assetPath), ".csv", StringComparison.OrdinalIgnoreCase);
            if (!_isCsv) return;

            RefreshEnums();
            RestoreSelection(assetPath);
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
                DrawCsvValidationControls();
            }
            finally
            {
                GUI.enabled = previousEnabled;
            }
        }

        private void DrawCsvValidationControls()
        {
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
            EditorGUILayout.LabelField($"Constraints: {enumType.Name}", EditorStyles.boldLabel);

            bool hasConstraints = false;
            FieldInfo[] fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < fields.Length; i++)
            {
                object[] attributes = fields[i].GetCustomAttributes(false);
                List<string> labels = attributes
                    .OfType<Attribute>()
                    .Select(GetAttributeDisplayText)
                    .Where(label => label != null)
                    .ToList();

                if (labels.Count == 0) continue;
                hasConstraints = true;
                EditorGUILayout.LabelField(fields[i].Name, string.Join(", ", labels));
            }

            if (!hasConstraints)
            {
                EditorGUILayout.LabelField("制約属性は定義されていません。", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
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

        private static string GetAttributeDisplayText(Attribute attribute)
        {
            switch (attribute)
            {
                case PrimaryKeyAttribute:
                    return "[PrimaryKey]";
                case NotNullAttribute:
                    return "[NotNull]";
                case UniqueAttribute:
                    return "[Unique]";
                case TypeConstraintAttribute typeConstraint:
                    return $"[Type: {typeConstraint.ExpectedType.Name}]";
                case Validation.RangeAttribute range:
                    return $"[Range: {range.Min}-{range.Max}]";
                case RegexAttribute regex:
                    return $"[Regex: {regex.Pattern}]";
                case AllowedValuesAttribute allowed:
                    return $"[Allowed: {string.Join("|", allowed.AllowedValues)}]";
                case MinLengthAttribute minLength:
                    return $"[MinLength: {minLength.MinLength}]";
                case MaxLengthAttribute maxLength:
                    return $"[MaxLength: {maxLength.MaxLength}]";
                default:
                    return null;
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
