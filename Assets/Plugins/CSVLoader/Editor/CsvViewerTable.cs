#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CSV4Unity.Editor
{
    /// <summary>
    /// CsvDocumentを仮想スクロールする読み取り専用テーブルです。
    /// </summary>
    internal sealed class CsvViewerTable
    {
        private const float BaseHeaderHeight = 24f;
        private const float BaseRowHeight = 21f;
        private const float BaseRowNumberWidth = 56f;
        private const float MinimumColumnWidth = 64f;
        private const float MaximumInitialColumnWidth = 280f;
        private const float MinimumZoom = 0.75f;
        private const float MaximumZoom = 2f;
        private const int CellCacheRowLimit = 256;

        private readonly CsvDocument _document;
        private readonly float[] _columnWidths;
        private readonly List<int> _filteredRows = new List<int>();
        private readonly Dictionary<int, string[]> _cellTextCache = new Dictionary<int, string[]>();

        private Vector2 _scrollPosition;
        private string _searchText = string.Empty;
        private int _selectedDisplayRow = -1;
        private int _selectedColumn = -1;
        private int _resizingColumn = -1;
        private float _resizeStartMouseX;
        private float _resizeStartWidth;
        private float _zoom = 1f;
        private GUIStyle _headerStyle;
        private GUIStyle _cellStyle;
        private GUIStyle _rowNumberStyle;

        public CsvViewerTable(CsvDocument document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _columnWidths = CreateInitialColumnWidths(document);
        }

        public int RowCount => _document.RowCount;

        public int ColumnCount => _document.ColumnCount;

        public int FilteredRowCount => string.IsNullOrEmpty(_searchText) ? RowCount : _filteredRows.Count;

        internal float Zoom => _zoom;

        public void SetZoom(float zoom)
        {
            float clamped = Mathf.Clamp(zoom, MinimumZoom, MaximumZoom);
            if (Mathf.Approximately(_zoom, clamped)) return;

            _zoom = clamped;
            _headerStyle = null;
            _cellStyle = null;
            _rowNumberStyle = null;
        }

        public void SetSearch(string searchText)
        {
            string normalized = searchText ?? string.Empty;
            if (string.Equals(_searchText, normalized, StringComparison.Ordinal)) return;

            _searchText = normalized;
            _filteredRows.Clear();
            _selectedDisplayRow = -1;
            _selectedColumn = -1;
            _scrollPosition.y = 0f;

            if (string.IsNullOrEmpty(_searchText)) return;

            ReadOnlySpan<char> query = _searchText.AsSpan();
            for (int rowIndex = 0; rowIndex < RowCount; rowIndex++)
            {
                if (RowContains(rowIndex, query)) _filteredRows.Add(rowIndex);
            }
        }

        public void OnGUI(Rect rect)
        {
            float headerHeight = BaseHeaderHeight * _zoom;
            float rowHeight = BaseRowHeight * _zoom;
            if (rect.width <= 0f || rect.height <= headerHeight) return;

            Rect headerRect = new Rect(rect.x, rect.y, rect.width, headerHeight);
            Rect bodyRect = new Rect(rect.x, rect.y + headerHeight, rect.width, rect.height - headerHeight);
            float contentWidth = CalculateContentWidth();
            float contentHeight = Mathf.Max(bodyRect.height, FilteredRowCount * rowHeight);

            _scrollPosition = GUI.BeginScrollView(
                bodyRect,
                _scrollPosition,
                new Rect(0f, 0f, contentWidth, contentHeight),
                true,
                true);
            DrawVisibleRows(bodyRect.height, contentWidth);
            HandleCopyShortcut();
            GUI.EndScrollView();

            DrawHeader(headerRect, contentWidth);
        }

        private void DrawHeader(Rect rect, float contentWidth)
        {
            EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.16f, 1f));
            GUI.BeginGroup(rect);

            float headerHeight = BaseHeaderHeight * _zoom;
            float rowNumberWidth = BaseRowNumberWidth * _zoom;
            float x = -_scrollPosition.x;
            DrawHeaderCell(new Rect(x, 0f, rowNumberWidth, headerHeight), "#");
            x += rowNumberWidth;

            for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
            {
                float width = _columnWidths[columnIndex] * _zoom;
                string name = _document.HasHeader
                    ? _document.Headers[columnIndex]
                    : $"Column {columnIndex + 1}";
                DrawHeaderCell(new Rect(x, 0f, width, headerHeight), name);
                HandleColumnResize(columnIndex, new Rect(x + width - 3f, 0f, 6f, headerHeight));
                x += width;
            }

            if (x < contentWidth - _scrollPosition.x)
            {
                EditorGUI.DrawRect(
                    new Rect(x, 0f, contentWidth - _scrollPosition.x - x, headerHeight),
                    new Color(0.16f, 0.16f, 0.16f, 1f));
            }

            GUI.EndGroup();
        }

        private void DrawHeaderCell(Rect rect, string text)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.toolbarButton);
            GUI.Label(
                new Rect(
                    rect.x + (6f * _zoom),
                    rect.y + (2f * _zoom),
                    Mathf.Max(0f, rect.width - (12f * _zoom)),
                    rect.height - (4f * _zoom)),
                new GUIContent(text, text),
                HeaderStyle);
        }

        private void DrawVisibleRows(float viewportHeight, float contentWidth)
        {
            float rowHeight = BaseRowHeight * _zoom;
            float rowNumberWidth = BaseRowNumberWidth * _zoom;
            int firstRow = Mathf.Max(0, Mathf.FloorToInt(_scrollPosition.y / rowHeight));
            int visibleCount = Mathf.CeilToInt(viewportHeight / rowHeight) + 2;
            int lastRow = Mathf.Min(FilteredRowCount, firstRow + visibleCount);

            for (int displayRow = firstRow; displayRow < lastRow; displayRow++)
            {
                int sourceRow = GetSourceRow(displayRow);
                float y = displayRow * rowHeight;
                Rect rowRect = new Rect(0f, y, contentWidth, rowHeight);

                if ((displayRow & 1) != 0)
                {
                    EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.025f));
                }

                DrawCellBackgrounds(displayRow, rowRect);

                float x = 0f;
                GUI.Label(
                    new Rect(
                        x + (5f * _zoom),
                        y + _zoom,
                        rowNumberWidth - (10f * _zoom),
                        rowHeight - (2f * _zoom)),
                    (sourceRow + 1).ToString(),
                    RowNumberStyle);
                x += rowNumberWidth;

                string[] cellTexts = GetRowTexts(sourceRow);
                for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
                {
                    float width = _columnWidths[columnIndex] * _zoom;
                    Rect cellRect = new Rect(x, y, width, rowHeight);
                    DrawCell(displayRow, columnIndex, cellRect, cellTexts[columnIndex]);
                    x += width;
                }

                EditorGUI.DrawRect(new Rect(0f, y + rowHeight - 1f, contentWidth, 1f), new Color(0f, 0f, 0f, 0.16f));
            }
        }

        private void DrawCellBackgrounds(int displayRow, Rect rowRect)
        {
            if (displayRow != _selectedDisplayRow) return;
            EditorGUI.DrawRect(rowRect, new Color(0.24f, 0.49f, 0.90f, 0.12f));
        }

        private void DrawCell(int displayRow, int columnIndex, Rect rect, string text)
        {
            if (displayRow == _selectedDisplayRow && columnIndex == _selectedColumn)
            {
                EditorGUI.DrawRect(rect, new Color(0.24f, 0.49f, 0.90f, 0.28f));
            }

            GUI.Label(
                new Rect(
                    rect.x + (5f * _zoom),
                    rect.y + _zoom,
                    Mathf.Max(0f, rect.width - (10f * _zoom)),
                    rect.height - (2f * _zoom)),
                new GUIContent(text, text),
                CellStyle);
            EditorGUI.DrawRect(new Rect(rect.x + rect.width - 1f, rect.y, 1f, rect.height), new Color(0f, 0f, 0f, 0.14f));

            Event current = Event.current;
            if (!rect.Contains(current.mousePosition)) return;

            if (current.type == EventType.MouseDown && current.button == 0)
            {
                _selectedDisplayRow = displayRow;
                _selectedColumn = columnIndex;
                current.Use();
            }
            else if (current.type == EventType.ContextClick)
            {
                _selectedDisplayRow = displayRow;
                _selectedColumn = columnIndex;
                ShowContextMenu();
                current.Use();
            }
        }

        private void ShowContextMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy Cell"), false, CopySelectedCell);
            menu.AddItem(new GUIContent("Copy Row"), false, CopySelectedRow);
            menu.ShowAsContext();
        }

        private void HandleCopyShortcut()
        {
            Event current = Event.current;
            if (current.type != EventType.KeyDown || current.keyCode != KeyCode.C) return;
            if (!current.control && !current.command) return;

            CopySelectedCell();
            current.Use();
        }

        private void CopySelectedCell()
        {
            if (!TryGetSelectedSourceRow(out int sourceRow) || _selectedColumn < 0) return;
            GUIUtility.systemCopyBuffer = _document.Cell(sourceRow, _selectedColumn).GetString();
        }

        private void CopySelectedRow()
        {
            if (!TryGetSelectedSourceRow(out int sourceRow)) return;

            string[] values = GetRowTexts(sourceRow);
            var builder = new StringBuilder();
            for (int columnIndex = 0; columnIndex < values.Length; columnIndex++)
            {
                if (columnIndex > 0) builder.Append('\t');
                builder.Append(_document.Cell(sourceRow, columnIndex).GetString());
            }

            GUIUtility.systemCopyBuffer = builder.ToString();
        }

        private bool TryGetSelectedSourceRow(out int sourceRow)
        {
            if ((uint)_selectedDisplayRow >= (uint)FilteredRowCount)
            {
                sourceRow = -1;
                return false;
            }

            sourceRow = GetSourceRow(_selectedDisplayRow);
            return true;
        }

        private void HandleColumnResize(int columnIndex, Rect handleRect)
        {
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal);
            Event current = Event.current;

            if (current.type == EventType.MouseDown && current.button == 0 && handleRect.Contains(current.mousePosition))
            {
                _resizingColumn = columnIndex;
                _resizeStartMouseX = current.mousePosition.x;
                _resizeStartWidth = _columnWidths[columnIndex];
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && _resizingColumn == columnIndex)
            {
                float delta = (current.mousePosition.x - _resizeStartMouseX) / _zoom;
                _columnWidths[columnIndex] = Mathf.Max(MinimumColumnWidth, _resizeStartWidth + delta);
                current.Use();
            }
            else if (current.type == EventType.MouseUp && _resizingColumn == columnIndex)
            {
                _resizingColumn = -1;
                current.Use();
            }
        }

        private bool RowContains(int rowIndex, ReadOnlySpan<char> query)
        {
            for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
            {
                CsvCell cell = _document.Cell(rowIndex, columnIndex);
                if (cell.RawSpan.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }

            return false;
        }

        private int GetSourceRow(int displayRow)
        {
            return string.IsNullOrEmpty(_searchText) ? displayRow : _filteredRows[displayRow];
        }

        private string[] GetRowTexts(int sourceRow)
        {
            if (_cellTextCache.TryGetValue(sourceRow, out string[] values)) return values;

            if (_cellTextCache.Count >= CellCacheRowLimit) _cellTextCache.Clear();

            values = new string[ColumnCount];
            for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
            {
                values[columnIndex] = ToSingleLine(_document.Cell(sourceRow, columnIndex).GetString());
            }

            _cellTextCache.Add(sourceRow, values);
            return values;
        }

        private static string ToSingleLine(string value)
        {
            return value
                .Replace("\r\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private float CalculateContentWidth()
        {
            float width = BaseRowNumberWidth;
            for (int i = 0; i < _columnWidths.Length; i++) width += _columnWidths[i];
            return width * _zoom;
        }

        private GUIStyle HeaderStyle => _headerStyle ??= CreateScaledStyle(EditorStyles.boldLabel, 12);

        private GUIStyle CellStyle => _cellStyle ??= CreateScaledStyle(EditorStyles.label, 12);

        private GUIStyle RowNumberStyle => _rowNumberStyle ??= CreateScaledStyle(EditorStyles.miniLabel, 10);

        private GUIStyle CreateScaledStyle(GUIStyle source, int baseFontSize)
        {
            if (Mathf.Approximately(_zoom, 1f)) return source;

            return new GUIStyle(source)
            {
                fontSize = Mathf.Max(1, Mathf.RoundToInt(baseFontSize * _zoom))
            };
        }

        private static float[] CreateInitialColumnWidths(CsvDocument document)
        {
            var widths = new float[document.ColumnCount];
            int sampleRows = Math.Min(document.RowCount, 100);

            for (int columnIndex = 0; columnIndex < document.ColumnCount; columnIndex++)
            {
                int longest = document.HasHeader ? document.Headers[columnIndex].Length : 8;
                for (int rowIndex = 0; rowIndex < sampleRows; rowIndex++)
                {
                    longest = Math.Max(longest, Math.Min(document.Cell(rowIndex, columnIndex).RawSpan.Length, 40));
                }

                widths[columnIndex] = Mathf.Clamp((longest * 7f) + 24f, 110f, MaximumInitialColumnWidth);
            }

            return widths;
        }
    }
}
#endif
