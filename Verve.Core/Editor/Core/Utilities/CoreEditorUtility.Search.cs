#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using UnityEditor;
    using UnityEditor.IMGUI.Controls;
    using UnityEngine;

    /// <summary>
    ///   <para>搜索条件；保存文本、长度和大小写选项。</para>
    /// </summary>
    public readonly struct EditorSearchFilter
    {
        /// <summary>
        ///   <para>搜索文本；空白表示不筛选。</para>
        /// </summary>
        public string Text { get; }
        /// <summary>
        ///   <para>是否要求匹配文本长度相同。</para>
        /// </summary>
        public bool RequiresExactLength { get; }
        /// <summary>
        ///   <para>是否区分大小写。</para>
        /// </summary>
        public bool IsCaseSensitive { get; }

        /// <summary>
        ///   <para>创建搜索条件。</para>
        /// </summary>
        /// <param name="text">搜索文本。</param>
        /// <param name="requiresExactLength">是否要求匹配文本长度相同。</param>
        /// <param name="isCaseSensitive">是否区分大小写。</param>
        public EditorSearchFilter(string text, bool requiresExactLength = false, bool isCaseSensitive = false)
        {
            Text = text;
            RequiresExactLength = requiresExactLength;
            IsCaseSensitive = isCaseSensitive;
        }

        /// <summary>
        ///   <para>判断文本是否匹配。</para>
        /// </summary>
        /// <param name="value">待匹配文本。</param>
        public bool Matches(string value) => string.IsNullOrWhiteSpace(Text) ||
            value != null && (!RequiresExactLength || value.Length == Text.Length) &&
            value.IndexOf(Text, IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    ///   <para>编辑器工具；搜索栏部分。</para>
    /// </summary>
    public static partial class CoreEditorUtility
    {
        /// <summary>
        ///   <para>长度按钮。</para>
        /// </summary>
        private static readonly GUIContent SearchExactLengthContent = new("#", "要求匹配文本长度相同，即完整匹配。空白搜索不筛选。");
        /// <summary>
        ///   <para>大小写按钮。</para>
        /// </summary>
        private static readonly GUIContent SearchCaseSensitiveContent = new("Aa", "区分大小写。");

        /// <summary>
        ///   <para>绘制搜索栏；返回文本或选项是否变化。</para>
        /// </summary>
        /// <param name="rect">绘制区域。</param>
        /// <param name="searchField">由调用方持有的搜索框。</param>
        /// <param name="filter">搜索条件。</param>
        public static bool DrawSearchToolbar(Rect rect, SearchField searchField, ref EditorSearchFilter filter)
        {
            const float buttonWidth = 24f;
            var textRect = new Rect(rect.x, rect.y, rect.width - buttonWidth * 2, rect.height);
            var lengthRect = new Rect(textRect.xMax, rect.y, buttonWidth, rect.height);
            var caseRect = new Rect(lengthRect.xMax, rect.y, buttonWidth, rect.height);
            var text = searchField.OnToolbarGUI(textRect, filter.Text ?? string.Empty);
            var exactLength = GUI.Toggle(lengthRect, filter.RequiresExactLength, SearchExactLengthContent, EditorStyles.toolbarButton);
            var caseSensitive = GUI.Toggle(caseRect, filter.IsCaseSensitive, SearchCaseSensitiveContent, EditorStyles.toolbarButton);
            if (text == filter.Text && exactLength == filter.RequiresExactLength && caseSensitive == filter.IsCaseSensitive)
                return false;

            filter = new EditorSearchFilter(text, exactLength, caseSensitive);
            return true;
        }
    }
}

#endif
