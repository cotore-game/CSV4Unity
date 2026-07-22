#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace CSV4Unity.Editor
{
    /// <summary>
    /// CSVの内容を読み取り専用の表として表示します。
    /// </summary>
    public sealed class CsvViewerWindow : EditorWindow
    {
        private const float ToolbarHeight = 22f;
        private const float StatusHeight = 20f;
        private const float MinimumZoom = 0.75f;
        private const float MaximumZoom = 2f;

        [SerializeField] private TextAsset _csvAsset;
        [SerializeField] private bool _hasHeader = true;
        [SerializeField] private string _searchText = string.Empty;
        [SerializeField] private float _zoom = 1f;

        [NonSerialized] private CsvViewerTable _table;
        [NonSerialized] private string _errorMessage;
        [NonSerialized] private Hash128 _assetHash;

        /// <summary>指定したCSVをViewerで開きます。</summary>
        /// <param name="csvAsset">表示するCSVのTextAsset。</param>
        public static void Open(TextAsset csvAsset)
        {
            CsvViewerWindow window = GetWindow<CsvViewerWindow>();
            window.titleContent = new GUIContent("CSV Viewer");
            window.minSize = new Vector2(640f, 260f);
            window.SetAsset(csvAsset);
            window.Show();
        }

        [MenuItem("Window/CSV4Unity/CSV Viewer")]
        private static void OpenWindow()
        {
            CsvViewerWindow window = GetWindow<CsvViewerWindow>();
            window.titleContent = new GUIContent("CSV Viewer");
            window.minSize = new Vector2(640f, 260f);
            window.Show();
        }

        [MenuItem("Assets/Open in CSV Viewer", false, 2000)]
        private static void OpenSelectedAsset()
        {
            Open(Selection.activeObject as TextAsset);
        }

        [MenuItem("Assets/Open in CSV Viewer", true)]
        private static bool CanOpenSelectedAsset()
        {
            if (!(Selection.activeObject is TextAsset textAsset)) return false;
            string path = AssetDatabase.GetAssetPath(textAsset);
            return CsvEditorAssetUtility.IsCsvPath(path);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("CSV Viewer");
            minSize = new Vector2(640f, 260f);
            if (_zoom <= 0f) _zoom = 1f;
            _zoom = Mathf.Clamp(_zoom, MinimumZoom, MaximumZoom);
            EditorApplication.projectChanged += HandleProjectChanged;
            Reload();
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= HandleProjectChanged;
        }

        private void OnGUI()
        {
            DrawToolbar(new Rect(0f, 0f, position.width, ToolbarHeight));

            Rect contentRect = new Rect(
                0f,
                ToolbarHeight,
                position.width,
                Mathf.Max(0f, position.height - ToolbarHeight - StatusHeight));

            if (!string.IsNullOrEmpty(_errorMessage))
            {
                EditorGUI.HelpBox(
                    new Rect(contentRect.x + 8f, contentRect.y + 8f, contentRect.width - 16f, 44f),
                    _errorMessage,
                    MessageType.Error);
            }
            else if (_table != null)
            {
                _table.OnGUI(contentRect);
            }
            else
            {
                EditorGUI.HelpBox(
                    new Rect(contentRect.x + 8f, contentRect.y + 8f, contentRect.width - 16f, 38f),
                    "Select a CSV TextAsset to preview.",
                    MessageType.Info);
            }

            DrawStatusBar(new Rect(0f, position.height - StatusHeight, position.width, StatusHeight));
        }

        private void DrawToolbar(Rect rect)
        {
            GUILayout.BeginArea(rect, EditorStyles.toolbar);
            GUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            TextAsset selectedAsset = (TextAsset)EditorGUILayout.ObjectField(
                _csvAsset,
                typeof(TextAsset),
                false,
                GUILayout.MinWidth(140f));
            if (EditorGUI.EndChangeCheck()) SetAsset(selectedAsset);

            EditorGUI.BeginChangeCheck();
            bool hasHeader = GUILayout.Toggle(_hasHeader, "Header", EditorStyles.toolbarButton, GUILayout.Width(58f));
            if (EditorGUI.EndChangeCheck())
            {
                _hasHeader = hasHeader;
                Reload();
            }

            if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(54f))) Reload();

            GUILayout.Space(6f);
            GUILayout.Label("Zoom", EditorStyles.miniLabel, GUILayout.Width(34f));
            EditorGUI.BeginChangeCheck();
            float zoom = GUILayout.HorizontalSlider(
                _zoom,
                MinimumZoom,
                MaximumZoom,
                GUILayout.Width(82f));
            if (EditorGUI.EndChangeCheck())
            {
                _zoom = Mathf.Round(zoom * 20f) / 20f;
                _table?.SetZoom(_zoom);
                Repaint();
            }

            GUILayout.Label($"{_zoom * 100f:0}%", EditorStyles.miniLabel, GUILayout.Width(34f));

            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();
            string search = EditorGUILayout.TextField(
                _searchText ?? string.Empty,
                EditorStyles.toolbarSearchField,
                GUILayout.MinWidth(100f),
                GUILayout.MaxWidth(240f));
            if (EditorGUI.EndChangeCheck())
            {
                _searchText = search;
                _table?.SetSearch(_searchText);
                Repaint();
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_searchText)))
            {
                if (GUILayout.Button(new GUIContent("x", "Clear search"), EditorStyles.toolbarButton, GUILayout.Width(20f)))
                {
                    _searchText = string.Empty;
                    _table?.SetSearch(_searchText);
                    GUI.FocusControl(null);
                    Repaint();
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawStatusBar(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.12f));

            string status;
            if (_table == null)
            {
                status = _csvAsset == null ? "No CSV selected" : "CSV unavailable";
            }
            else
            {
                status = _table.FilteredRowCount == _table.RowCount
                    ? $"{_table.RowCount:N0} rows, {_table.ColumnCount:N0} columns"
                    : $"{_table.FilteredRowCount:N0} of {_table.RowCount:N0} rows, {_table.ColumnCount:N0} columns";
            }

            GUI.Label(new Rect(rect.x + 6f, rect.y + 2f, rect.width - 12f, rect.height - 2f), status, EditorStyles.miniLabel);
        }

        private void SetAsset(TextAsset csvAsset)
        {
            if (_csvAsset == csvAsset) return;
            _csvAsset = csvAsset;
            _searchText = string.Empty;
            Reload();
        }

        private void Reload()
        {
            _table = null;
            _errorMessage = null;
            _assetHash = default;

            if (_csvAsset == null)
            {
                Repaint();
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(_csvAsset);
            if (!CsvEditorAssetUtility.IsCsvPath(assetPath))
            {
                _errorMessage = "The selected TextAsset is not a .csv file.";
                Repaint();
                return;
            }

            try
            {
                var options = new CsvParseOptions
                {
                    HasHeader = _hasHeader,
                    IgnoreEmptyRecords = false,
                    TrimUnquotedFields = false
                };

                CsvDocument document = CSVLoader.LoadDocument(_csvAsset, options);
                _table = new CsvViewerTable(document);
                _table.SetZoom(_zoom);
                _table.SetSearch(_searchText);
                _assetHash = AssetDatabase.GetAssetDependencyHash(assetPath);
            }
            catch (Exception exception)
            {
                _errorMessage = exception.Message;
            }

            Repaint();
        }

        private void HandleProjectChanged()
        {
            if (_csvAsset == null) return;

            string assetPath = AssetDatabase.GetAssetPath(_csvAsset);
            if (string.IsNullOrEmpty(assetPath))
            {
                Reload();
                return;
            }

            Hash128 currentHash = AssetDatabase.GetAssetDependencyHash(assetPath);
            if (currentHash != _assetHash) Reload();
        }
    }
}
#endif
