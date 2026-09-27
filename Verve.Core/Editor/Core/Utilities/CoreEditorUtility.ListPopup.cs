#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.IMGUI.Controls;
    using UnityEngine;

    /// <summary>
    ///   <para>编辑器工具；列表弹窗部分。</para>
    /// </summary>
    public static partial class CoreEditorUtility
    {
        /// <summary>
        ///   <para>显示列表弹窗；选择后自动关闭，条目和资源仅借用。</para>
        /// </summary>
        /// <typeparam name="T">条目类型。</typeparam>
        /// <param name="activatorRect">弹窗锚定区域。</param>
        /// <param name="items">条目；打开时保存列表快照。</param>
        /// <param name="getContent">显示内容；每个条目仅在打开时调用一次。</param>
        /// <param name="onSelected">选择回调。</param>
        /// <param name="matches">搜索规则；默认按搜索条件匹配标签和提示。</param>
        /// <param name="onSearchChanged">搜索变化回调；首次筛选及条件变化时，在匹配条目前调用。</param>
        public static void ShowListPopup<T>(Rect activatorRect, IReadOnlyList<T> items,
            Func<T, GUIContent> getContent, Action<T> onSelected,
            Func<T, EditorSearchFilter, bool> matches = null, Action<EditorSearchFilter> onSearchChanged = null) =>
            PopupWindow.Show(activatorRect, new EditorListPopup<T>(items, getContent, onSelected, matches, onSearchChanged));
    }

    /// <summary>
    ///   <para>列表弹窗；缓存搜索结果并只绘制可见行。</para>
    /// </summary>
    /// <typeparam name="T">条目类型。</typeparam>
    internal sealed class EditorListPopup<T> : PopupWindowContent
    {
        /// <summary>
        ///   <para>条目高度。</para>
        /// </summary>
        private const float RowHeight = 28f;
        /// <summary>
        ///   <para>搜索栏高度。</para>
        /// </summary>
        private const float SearchHeight = 20f;
        /// <summary>
        ///   <para>外边距。</para>
        /// </summary>
        private const float Padding = 4f;
        /// <summary>
        ///   <para>图标大小。</para>
        /// </summary>
        private const float IconSize = 18f;
        /// <summary>
        ///   <para>条目快照；显示内容复制，图标和条目借用。</para>
        /// </summary>
        private (T value, GUIContent content)[] m_Items;
        /// <summary>
        ///   <para>匹配索引。</para>
        /// </summary>
        private readonly List<int> m_Visible = new();
        /// <summary>
        ///   <para>选择回调。</para>
        /// </summary>
        private Action<T> m_OnSelected;
        /// <summary>
        ///   <para>搜索规则。</para>
        /// </summary>
        private Func<T, EditorSearchFilter, bool> m_Matches;
        /// <summary>
        ///   <para>搜索变化回调。</para>
        /// </summary>
        private Action<EditorSearchFilter> m_OnSearchChanged;
        /// <summary>
        ///   <para>搜索框；在 GUI 绘制阶段创建。</para>
        /// </summary>
        private SearchField m_SearchField;
        /// <summary>
        ///   <para>标签样式。</para>
        /// </summary>
        private GUIStyle m_LabelStyle;
        /// <summary>
        ///   <para>搜索条件。</para>
        /// </summary>
        private EditorSearchFilter m_Search = new(string.Empty);
        /// <summary>
        ///   <para>搜索框已聚焦。</para>
        /// </summary>
        private bool m_SearchFocused;
        /// <summary>
        ///   <para>首次搜索标记。</para>
        /// </summary>
        private bool m_FilterPending = true;
        /// <summary>
        ///   <para>滚动位置。</para>
        /// </summary>
        private Vector2 m_ScrollPosition;

        /// <summary>
        ///   <para>创建列表弹窗。</para>
        /// </summary>
        /// <param name="items">条目。</param>
        /// <param name="getContent">显示内容。</param>
        /// <param name="onSelected">选择回调。</param>
        /// <param name="matches">搜索规则。</param>
        /// <param name="onSearchChanged">搜索变化回调。</param>
        internal EditorListPopup(IReadOnlyList<T> items, Func<T, GUIContent> getContent,
            Action<T> onSelected, Func<T, EditorSearchFilter, bool> matches = null, Action<EditorSearchFilter> onSearchChanged = null)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (getContent == null) throw new ArgumentNullException(nameof(getContent));
            m_OnSelected = onSelected ?? throw new ArgumentNullException(nameof(onSelected));
            m_Matches = matches;
            m_OnSearchChanged = onSearchChanged;
            m_Items = items.Select(item => (item, new GUIContent(getContent(item)))).ToArray();
        }

        /// <inheritdoc />
        public override Vector2 GetWindowSize()
        {
            var width = 360f;
            foreach (var item in m_Items)
                width = Mathf.Max(width, EditorStyles.label.CalcSize(item.content).x + 56f);
            return new Vector2(Mathf.Min(width, 640f),
                Mathf.Clamp(SearchHeight + Padding * 2 + m_Items.Length * RowHeight, 84f, 480f));
        }

        /// <inheritdoc />
        public override void OnOpen() => editorWindow.wantsMouseMove = true;

        /// <inheritdoc />
        public override void OnClose()
        {
            m_Items = Array.Empty<(T, GUIContent)>();
            m_Visible.Clear();
            m_OnSelected = null;
            m_Matches = null;
            m_OnSearchChanged = null;
        }

        /// <inheritdoc />
        public override void OnGUI(Rect rect)
        {
            if (Event.current.type == EventType.MouseMove) editorWindow.Repaint();
            m_LabelStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                imagePosition = ImagePosition.TextOnly
            };

            var searchRect = new Rect(Padding, Padding, rect.width - Padding * 2, SearchHeight);
            var changed = DrawSearch(searchRect);
            if (m_FilterPending || changed)
            {
                m_OnSearchChanged?.Invoke(m_Search);
                m_ScrollPosition = Vector2.zero;
                FilterItems();
                m_FilterPending = false;
            }

            var listRect = new Rect(0f, searchRect.yMax + Padding, rect.width, rect.height - searchRect.yMax - Padding);
            if (m_Visible.Count == 0)
            {
                GUI.Label(listRect, m_Items.Length == 0 ? "列表为空。" : "没有匹配的条目。", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            var height = m_Visible.Count * RowHeight;
            var scrollbar = GUI.skin.verticalScrollbar;
            var width = listRect.width - (height > listRect.height ? scrollbar.fixedWidth + scrollbar.margin.left : 0f);
            var mouseInList = listRect.Contains(Event.current.mousePosition);
            using (var scroll = new GUI.ScrollViewScope(listRect, m_ScrollPosition, new Rect(0f, 0f, width, height)))
            {
                m_ScrollPosition = scroll.scrollPosition;
                var first = Mathf.Max(0, Mathf.FloorToInt(m_ScrollPosition.y / RowHeight));
                var end = Mathf.Min(m_Visible.Count, Mathf.CeilToInt((m_ScrollPosition.y + listRect.height) / RowHeight));
                for (var i = first; i < end; i++)
                    DrawRow(new Rect(0f, i * RowHeight, width, RowHeight), i, mouseInList);
            }
        }

        /// <summary>
        ///   <para>绘制搜索栏。</para>
        /// </summary>
        /// <param name="rect">绘制区域。</param>
        private bool DrawSearch(Rect rect)
        {
            m_SearchField ??= new SearchField();
            var changed = CoreEditorUtility.DrawSearchToolbar(rect, m_SearchField, ref m_Search);
            if (!m_SearchFocused && Event.current.type == EventType.Repaint)
            {
                m_SearchField.SetFocus();
                m_SearchFocused = true;
            }
            return changed;
        }

        /// <summary>
        ///   <para>更新搜索结果；仅在搜索条件变化时执行。</para>
        /// </summary>
        private void FilterItems()
        {
            m_Visible.Clear();
            for (var i = 0; i < m_Items.Length; i++)
            {
                var item = m_Items[i];
                var matches = m_Matches != null ? m_Matches(item.value, m_Search)
                    : m_Search.Matches(item.content.text) || m_Search.Matches(item.content.tooltip);
                if (matches) m_Visible.Add(i);
            }
        }

        /// <summary>
        ///   <para>绘制条目并处理选择。</para>
        /// </summary>
        /// <param name="rect">行区域。</param>
        /// <param name="index">搜索结果索引。</param>
        /// <param name="mouseInList">指针是否位于列表视口。</param>
        private void DrawRow(Rect rect, int index, bool mouseInList)
        {
            var item = m_Items[m_Visible[index]];
            var currentEvent = Event.current;
            var hovered = mouseInList && rect.Contains(currentEvent.mousePosition);
            CoreEditorUtility.DrawRowBackground(rect, hovered, index);
            var labelRect = new Rect(rect.x + Padding * 2, rect.y, rect.width - Padding * 4, rect.height);
            if (item.content.image != null)
            {
                GUI.DrawTexture(new Rect(labelRect.x, rect.y + (RowHeight - IconSize) / 2, IconSize, IconSize),
                    item.content.image, ScaleMode.ScaleToFit);
                labelRect.xMin += IconSize + Padding * 2;
            }
            GUI.Label(labelRect, item.content, m_LabelStyle);
            if (!hovered || currentEvent.type != EventType.MouseDown || currentEvent.button != 0) return;

            var onSelected = m_OnSelected;
            currentEvent.Use();
            editorWindow.Close();
            onSelected(item.value);
            GUIUtility.ExitGUI();
        }
    }
}

#endif
