#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;
    using UnityEditor.IMGUI.Controls;

    /// <summary>
    ///   <para>选项弹窗；支持搜索、层级导航和单选、多选。</para>
    /// </summary>
    public sealed class SearchableOptionsPopup : PopupWindowContent
    {
        /// <summary>
        ///   <para>宽度。</para>
        /// </summary>
        private const float Width = 200f;
        /// <summary>
        ///   <para>最小高度。</para>
        /// </summary>
        private const float MinHeight = 250f;
        /// <summary>
        ///   <para>最大高度。</para>
        /// </summary>
        private const float MaxHeight = 260f;
        /// <summary>
        ///   <para>外部内边距。</para>
        /// </summary>
        private const float OuterPadding = 3f;
        /// <summary>
        ///   <para>搜索高度。</para>
        /// </summary>
        private const float SearchHeight = 18f;
        /// <summary>
        ///   <para>表头高度。</para>
        /// </summary>
        private const float HeaderHeight = 24f;
        /// <summary>
        ///   <para>行高。</para>
        /// </summary>
        private const float RowHeight = 18f;
        /// <summary>
        ///   <para>滚动栏宽度。</para>
        /// </summary>
        private const float ScrollBarWidth = 15f;
        /// <summary>
        ///   <para>最大可见行数。</para>
        /// </summary>
        private const int MaxVisibleRows = 12;

        /// <summary>
        ///   <para>当前值。</para>
        /// </summary>
        private readonly string m_CurrentValue;
        /// <summary>
        ///   <para>选项。</para>
        /// </summary>
        private readonly string[] m_Options;
        /// <summary>
        ///   <para>选项路径片段。</para>
        /// </summary>
        private readonly string[][] m_OptionParts;
        /// <summary>
        ///   <para>选中回调。</para>
        /// </summary>
        private readonly Action<string> m_OnSelected;
        /// <summary>
        ///   <para>选择变化回调。</para>
        /// </summary>
        private readonly Action<IReadOnlyList<string>> m_OnSelectionChanged;
        /// <summary>
        ///   <para>选中值。</para>
        /// </summary>
        private readonly HashSet<string> m_SelectedValues;
        /// <summary>
        ///   <para>控件名称。</para>
        /// </summary>
        private readonly string m_ControlName;
        /// <summary>
        ///   <para>标题。</para>
        /// </summary>
        private readonly string m_Title;
        /// <summary>
        ///   <para>分隔符。</para>
        /// </summary>
        private readonly string m_Separator;
        /// <summary>
        ///   <para>路径。</para>
        /// </summary>
        private readonly List<string> m_Path = new();
        
        /// <summary>
        ///   <para>搜索框。</para>
        /// </summary>
        private SearchField m_SearchField;
        /// <summary>
        ///   <para>滚动位置。</para>
        /// </summary>
        private Vector2 m_ScrollPosition;
        /// <summary>
        ///   <para>搜索文本。</para>
        /// </summary>
        private string m_SearchText = string.Empty;
        /// <summary>
        ///   <para>搜索框焦点状态。</para>
        /// </summary>
        private bool m_SearchFocused;

        /// <summary>
        ///   <para>选项样式。</para>
        /// </summary>
        private static GUIStyle s_OptionStyle;
        /// <summary>
        ///   <para>表头样式。</para>
        /// </summary>
        private static GUIStyle s_HeaderStyle;
        /// <summary>
        ///   <para>表头文本样式。</para>
        /// </summary>
        private static GUIStyle s_HeaderTextStyle;
        /// <summary>
        ///   <para>右箭头样式。</para>
        /// </summary>
        private static GUIStyle s_RightArrowStyle;
        /// <summary>
        ///   <para>左箭头样式。</para>
        /// </summary>
        private static GUIStyle s_LeftArrowStyle;
        /// <summary>
        ///   <para>样式使用专业版皮肤。</para>
        /// </summary>
        private static bool s_StylesUseProSkin;
        /// <summary>
        ///   <para>悬停颜色。</para>
        /// </summary>
        private static Color s_HoverColor;

        /// <summary>
        ///   <para>创建可搜索选项弹窗。</para>
        /// </summary>
        /// <param name="currentValue">当前值。</param>
        /// <param name="options">选项。</param>
        /// <param name="onSelected">选中回调。</param>
        /// <param name="controlName">控件名称。</param>
        /// <param name="title">标题。</param>
        /// <param name="separator">分隔符。</param>
        public SearchableOptionsPopup(
            string currentValue,
            string[] options,
            Action<string> onSelected,
            string controlName = null,
            string title = null,
            string separator = null)
        {
            m_CurrentValue = currentValue ?? string.Empty;
            m_Options = options == null || options.Length == 0
                ? Array.Empty<string>()
                : (string[])options.Clone();
            m_OptionParts = new string[m_Options.Length][];
            m_OnSelected = onSelected;
            m_OnSelectionChanged = null;
            m_SelectedValues = new HashSet<string>(StringComparer.Ordinal);
            m_ControlName = string.IsNullOrEmpty(controlName)
                ? nameof(SearchableOptionsPopup)
                : controlName;
            m_Title = title;
            m_Separator = separator;

            for (var i = 0; i < m_Options.Length; i++)
            {
                m_OptionParts[i] = SplitOption(m_Options[i]);
            }
        }

        /// <summary>
        ///   <para>创建一个选择多个选项且不会自动关闭的弹窗。</para>
        /// </summary>
        /// <param name="selectedValues">选中值。</param>
        /// <param name="options">选项。</param>
        /// <param name="onSelectionChanged">选择变化回调。</param>
        /// <param name="controlName">控件名称。</param>
        /// <param name="title">标题。</param>
        /// <param name="separator">分隔符。</param>
        public SearchableOptionsPopup(
            IReadOnlyList<string> selectedValues,
            string[] options,
            Action<IReadOnlyList<string>> onSelectionChanged,
            string controlName = null,
            string title = null,
            string separator = null)
        {
            m_CurrentValue = string.Empty;
            m_Options = options == null || options.Length == 0
                ? Array.Empty<string>()
                : (string[])options.Clone();
            m_OptionParts = new string[m_Options.Length][];
            m_OnSelected = null;
            m_OnSelectionChanged = onSelectionChanged;
            m_SelectedValues = new HashSet<string>(StringComparer.Ordinal);
            if (selectedValues != null)
            {
                for (var i = 0; i < selectedValues.Count; i++)
                {
                    if (!string.IsNullOrEmpty(selectedValues[i]))
                    {
                        m_SelectedValues.Add(selectedValues[i]);
                    }
                }
            }

            m_ControlName = string.IsNullOrEmpty(controlName)
                ? nameof(SearchableOptionsPopup)
                : controlName;
            m_Title = title;
            m_Separator = separator;

            for (var i = 0; i < m_Options.Length; i++)
            {
                m_OptionParts[i] = SplitOption(m_Options[i]);
            }
        }

        /// <inheritdoc />
        public override Vector2 GetWindowSize()
        {
            var visibleRows = Mathf.Min(GetVisibleItemCount(), MaxVisibleRows);
            var hasHeader = !string.IsNullOrEmpty(m_Title) || m_Path.Count > 0;
            var height = OuterPadding * 2f + SearchHeight +
                         (hasHeader ? HeaderHeight : 0f) +
                         Mathf.Max(visibleRows, 1) * RowHeight + 2f;
            return new Vector2(Width, Mathf.Clamp(height, MinHeight, MaxHeight));
        }

        /// <inheritdoc />
        public override void OnOpen()
        {
            EnsureStyles();
            m_SearchField ??= new SearchField();
            m_SearchFocused = false;
            if (editorWindow != null)
            {
                editorWindow.wantsMouseMove = true;
            }
        }

        /// <inheritdoc />
        public override void OnClose()
        {
            EditorGUIUtility.editingTextField = false;
            GUIUtility.keyboardControl = 0;
        }

        /// <inheritdoc />
        public override void OnGUI(Rect rect)
        {
            EnsureStyles();
            if (Event.current.type == EventType.MouseMove && editorWindow != null)
            {
                editorWindow.Repaint();
            }

            GUI.Box(new Rect(0f, 0f, rect.width, rect.height), GUIContent.none, "grey_border");

            var y = OuterPadding;
            var searchRect = new Rect(
                OuterPadding,
                y,
                Mathf.Max(0f, rect.width - OuterPadding * 2f),
                SearchHeight);
            DrawSearchField(searchRect);
            y += SearchHeight;

            if (!string.IsNullOrEmpty(m_Title) || m_Path.Count > 0)
            {
                y += 1f;
                DrawHeader(new Rect(0f, y, rect.width, HeaderHeight));
                y += HeaderHeight;
            }

            var listRect = new Rect(
                0f,
                y,
                rect.width,
                Mathf.Max(0f, rect.height - y - OuterPadding));

            var rowCount = GetVisibleItemCount();
            var contentHeight = Mathf.Max(RowHeight, rowCount * RowHeight);
            var contentWidth = Mathf.Max(
                0f,
                listRect.width - (contentHeight > listRect.height ? ScrollBarWidth : 1f));
            m_ScrollPosition = GUI.BeginScrollView(
                listRect,
                m_ScrollPosition,
                new Rect(0f, 0f, contentWidth, contentHeight),
                false,
                false);

            var contentRect = new Rect(0f, 0f, contentWidth, contentHeight);
            var hasMatch = string.IsNullOrEmpty(m_SearchText)
                ? DrawCurrentLevel(contentRect)
                : DrawSearchResults(contentRect);
            if (!hasMatch)
            {
                GUI.Label(
                    new Rect(0f, 0f, contentRect.width, RowHeight),
                    "没有匹配的选项。",
                    EditorStyles.centeredGreyMiniLabel);
            }

            GUI.EndScrollView();
        }

        /// <summary>
        ///   <para>绘制搜索框。</para>
        /// </summary>
        /// <param name="searchRect">搜索区域。</param>
        private void DrawSearchField(Rect searchRect)
        {
            m_SearchField ??= new SearchField();
            GUI.SetNextControlName(m_ControlName);
            if (!m_SearchFocused && Event.current.type == EventType.Repaint)
            {
                m_SearchField.SetFocus();
                m_SearchFocused = true;
            }

            var nextSearch = m_SearchField.OnToolbarGUI(searchRect, m_SearchText);
            if (!string.Equals(nextSearch, m_SearchText, StringComparison.Ordinal))
            {
                m_SearchText = nextSearch ?? string.Empty;
                m_ScrollPosition = Vector2.zero;
            }
        }

        /// <summary>
        ///   <para>绘制表头。</para>
        /// </summary>
        /// <param name="headerRect">表头区域。</param>
        private void DrawHeader(Rect headerRect)
        {
            var title = m_Path.Count > 0 ? m_Path[m_Path.Count - 1] : m_Title;
            if (Event.current.type == EventType.Repaint)
            {
                s_HeaderStyle.Draw(headerRect, GUIContent.none, false, false, false, false);
            }
            
            GUI.Label(headerRect, title, s_HeaderTextStyle);

            if (m_Path.Count > 0)
            {
                const float arrowSize = 13f;
                var backRect = new Rect(
                    headerRect.x + 4f,
                    headerRect.y + (headerRect.height - arrowSize) * 0.5f,
                    arrowSize,
                    arrowSize);
                if (Event.current.type == EventType.Repaint)
                {
                    s_LeftArrowStyle.Draw(backRect, GUIContent.none, false, false, false, false);
                }

                if (GUI.Button(backRect, GUIContent.none, GUIStyle.none))
                {
                    m_Path.RemoveAt(m_Path.Count - 1);
                    m_ScrollPosition = Vector2.zero;
                    GUIUtility.ExitGUI();
                }
            }
        }

        /// <summary>
        ///   <para>绘制当前级别。</para>
        /// </summary>
        /// <param name="contentRect">内容区域。</param>
        private bool DrawCurrentLevel(Rect contentRect)
        {
            var hasMatch = false;
            var labels = new HashSet<string>(StringComparer.Ordinal);
            var rowIndex = 0;
            for (var i = 0; i < m_Options.Length; i++)
            {
                var parts = m_OptionParts[i];
                if (!IsUnderPath(parts) || parts.Length <= m_Path.Count)
                {
                    continue;
                }

                var label = parts[m_Path.Count];
                if (!labels.Add(label))
                {
                    continue;
                }

                var isCategory = parts.Length > m_Path.Count + 1 && !string.IsNullOrEmpty(m_Separator);
                DrawOptionRow(
                    new Rect(0f, rowIndex++ * RowHeight, contentRect.width, RowHeight),
                    label,
                    isCategory,
                    isCategory ? null : m_Options[i]);
                hasMatch = true;
            }

            return hasMatch;
        }

        /// <summary>
        ///   <para>绘制搜索结果。</para>
        /// </summary>
        /// <param name="contentRect">内容区域。</param>
        private bool DrawSearchResults(Rect contentRect)
        {
            var hasMatch = false;
            var rowIndex = 0;
            for (var i = 0; i < m_Options.Length; i++)
            {
                var option = m_Options[i] ?? string.Empty;
                if (!Matches(option, m_SearchText))
                {
                    continue;
                }

                DrawOptionRow(
                    new Rect(0f, rowIndex++ * RowHeight, contentRect.width, RowHeight),
                    option,
                    false,
                    option);
                hasMatch = true;
            }

            return hasMatch;
        }

        /// <summary>
        ///   <para>绘制选项行。</para>
        /// </summary>
        /// <param name="rowRect">行区域。</param>
        /// <param name="label">标签。</param>
        /// <param name="isCategory">是否分类。</param>
        /// <param name="option">选项。</param>
        private void DrawOptionRow(Rect rowRect, string label, bool isCategory, string option)
        {
            var selected = !isCategory &&
                           (m_OnSelectionChanged != null
                               ? m_SelectedValues.Contains(option)
                               : string.Equals(option, m_CurrentValue, StringComparison.Ordinal));
            var hovered = rowRect.Contains(Event.current.mousePosition);
            if (Event.current.type == EventType.Repaint)
            {
                if (hovered && !selected)
                {
                    EditorGUI.DrawRect(rowRect, s_HoverColor);
                }
                
                s_OptionStyle.Draw(
                    rowRect,
                    new GUIContent(label),
                    false,
                    false,
                    selected,
                    selected);
            }
            if (isCategory)
            {
                const float arrowSize = 13f;
                var arrowRect = new Rect(
                    rowRect.xMax - arrowSize - 3f,
                    rowRect.y + (rowRect.height - arrowSize) * 0.5f,
                    arrowSize,
                    arrowSize);
                if (Event.current.type == EventType.Repaint)
                {
                    s_RightArrowStyle.Draw(arrowRect, GUIContent.none, false, false, false, false);
                }
            }

            if (!GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
            {
                return;
            }

            if (isCategory)
            {
                m_Path.Add(label);
                m_ScrollPosition = Vector2.zero;
                GUIUtility.ExitGUI();
                return;
            }

            if (m_OnSelectionChanged != null)
            {
                if (!m_SelectedValues.Add(option))
                {
                    m_SelectedValues.Remove(option);
                }

                m_OnSelectionChanged(GetSelectedValues());
                editorWindow?.Repaint();
                return;
            }

            // 先结束选择弹窗，避免回调打开模态窗口后再次关闭弹窗而改变焦点。
            editorWindow.Close();
            m_OnSelected?.Invoke(option);
            GUIUtility.ExitGUI();
        }

        /// <summary>
        ///   <para>获取选中值。</para>
        /// </summary>
        private IReadOnlyList<string> GetSelectedValues()
        {
            var values = new List<string>(m_SelectedValues.Count);
            for (var i = 0; i < m_Options.Length; i++)
            {
                if (m_SelectedValues.Contains(m_Options[i]))
                {
                    values.Add(m_Options[i]);
                }
            }

            return values;
        }

        /// <summary>
        ///   <para>获取可见项数量。</para>
        /// </summary>
        private int GetVisibleItemCount()
        {
            if (!string.IsNullOrEmpty(m_SearchText))
            {
                var matchCount = 0;
                for (var i = 0; i < m_Options.Length; i++)
                {
                    if (Matches(m_Options[i] ?? string.Empty, m_SearchText))
                    {
                        matchCount++;
                    }
                }

                return matchCount;
            }

            var labels = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < m_Options.Length; i++)
            {
                var parts = m_OptionParts[i];
                if (IsUnderPath(parts) && parts.Length > m_Path.Count)
                {
                    labels.Add(parts[m_Path.Count]);
                }
            }

            return labels.Count;
        }

        /// <summary>
        ///   <para>判断选项是否位于指定路径下。</para>
        /// </summary>
        /// <param name="parts">路径片段。</param>
        private bool IsUnderPath(string[] parts)
        {
            if (string.IsNullOrEmpty(m_Separator))
            {
                return m_Path.Count == 0;
            }

            if (parts.Length <= m_Path.Count)
            {
                return false;
            }

            for (var i = 0; i < m_Path.Count; i++)
            {
                if (!string.Equals(parts[i], m_Path[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///   <para>拆分选项。</para>
        /// </summary>
        /// <param name="option">选项。</param>
        private string[] SplitOption(string option)
        {
            if (string.IsNullOrEmpty(m_Separator))
            {
                return new[] { option ?? string.Empty };
            }

            return (option ?? string.Empty).Split(
                new[] { m_Separator },
                StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        ///   <para>初始化尚未创建的样式。</para>
        /// </summary>
        private static void EnsureStyles()
        {
            var isProSkin = EditorGUIUtility.isProSkin;
            if (s_OptionStyle != null && s_StylesUseProSkin == isProSkin)
            {
                return;
            }

            s_StylesUseProSkin = isProSkin;
            s_OptionStyle = new GUIStyle("PR Label")
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = RowHeight,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(6, 4, 0, 0),
                fontSize = 11,
            };
            s_OptionStyle.normal.background = null;
            s_OptionStyle.hover.background = null;
            s_OptionStyle.active.background = null;
            s_HeaderStyle = new GUIStyle("In BigTitle")
            {
                fixedHeight = HeaderHeight,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
            };
            s_HeaderTextStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = HeaderHeight,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                fontSize = 11,
            };
            s_RightArrowStyle = new GUIStyle("AC RightArrow");
            s_LeftArrowStyle = new GUIStyle("AC LeftArrow");
            s_HoverColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.12f)
                : new Color(0f, 0f, 0f, 0.12f);
        }

        /// <summary>
        ///   <para>匹配。</para>
        /// </summary>
        /// <param name="value">值。</param>
        /// <param name="search">搜索。</param>
        private static bool Matches(string value, string search)
        {
            return string.IsNullOrEmpty(search) ||
                   value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

#endif
