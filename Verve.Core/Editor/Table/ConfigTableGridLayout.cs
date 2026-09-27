#if UNITY_EDITOR

namespace Verve.Editor
{
    using UnityEngine;
    using System.Collections.Generic;
    
    /// <summary>
    ///   <para>字符串字段的资源预览方式。</para>
    /// </summary>
    internal enum ConfigTableResourcePreviewMode
    {
        /// <summary>
        ///   <para>不使用资源预览。</para>
        /// </summary>
        None,
        /// <summary>
        ///   <para>使用 <see cref="UnityEngine.Resources"/> 资源预览。</para>
        /// </summary>
        Resources
    }
    
    /// <summary>
    ///   <para>配置表网格布局状态。</para>
    /// </summary>
    internal sealed class ConfigTableGridLayout
    {
        /// <summary>
        ///   <para>列宽列表。</para>
        /// </summary>
        internal float[] columnWidths;
        /// <summary>
        ///   <para>资源模式。</para>
        /// </summary>
        internal ConfigTableResourcePreviewMode[] resourceModes;
        /// <summary>
        ///   <para>正在调整宽度的列索引。</para>
        /// </summary>
        internal int resizingColumn = -1;
        /// <summary>
        ///   <para>开始调整列宽时的横坐标。</para>
        /// </summary>
        internal float resizeStartX;
        /// <summary>
        ///   <para>开始调整时的列宽。</para>
        /// </summary>
        internal float resizeStartWidth;
        /// <summary>
        ///   <para>冻结列。</para>
        /// </summary>
        internal int frozenColumn = -1;
        /// <summary>
        ///   <para>冻结行。</para>
        /// </summary>
        internal int frozenRow = -1;
        /// <summary>
        ///   <para>开始平移时的鼠标位置。</para>
        /// </summary>
        internal Vector2 panStartMousePosition;
        /// <summary>
        ///   <para>开始平移时的滚动位置。</para>
        /// </summary>
        internal Vector2 panStartScrollPosition;
        /// <summary>
        ///   <para>滚动区域。</para>
        /// </summary>
        internal Rect scrollViewRect;
        /// <summary>
        ///   <para>是否平移中。</para>
        /// </summary>
        internal bool isPanning;
        /// <summary>
        ///   <para>冻结列输入控件。</para>
        /// </summary>
        internal int frozenColumnInputControl;
        /// <summary>
        ///   <para>取消冻结列。</para>
        /// </summary>
        internal bool cancelFrozenColumn;
        /// <summary>
        ///   <para>冻结行输入控件。</para>
        /// </summary>
        internal int frozenRowInputControl;
        /// <summary>
        ///   <para>取消冻结行。</para>
        /// </summary>
        internal bool cancelFrozenRow;
        /// <summary>
        ///   <para>可见起始行索引。</para>
        /// </summary>
        internal int visibleFirstRowIndex;
        /// <summary>
        ///   <para>可见结束行索引；不包含该行。</para>
        /// </summary>
        internal int visibleLastRowIndexExclusive;
        /// <summary>
        ///   <para>可见行数。</para>
        /// </summary>
        internal int visibleRowCount = -1;
        /// <summary>
        ///   <para>行高度。</para>
        /// </summary>
        internal float[] rowHeights;
        /// <summary>
        ///   <para>行偏移。</para>
        /// </summary>
        internal float[] rowOffsets;
        /// <summary>
        ///   <para>行高版本。</para>
        /// </summary>
        internal int rowHeightVersion = -1;
        /// <summary>
        ///   <para>行高计算使用的列宽哈希。</para>
        /// </summary>
        internal int rowHeightWidthHash;
        /// <summary>
        ///   <para>重复标识。</para>
        /// </summary>
        internal HashSet<string> duplicateIds;
        /// <summary>
        ///   <para>重复标识版本。</para>
        /// </summary>
        internal int duplicateIdVersion = -1;
    
        /// <summary>
        ///   <para>重置网格布局缓存和交互状态。</para>
        /// </summary>
        internal void Clear()
        {
            columnWidths = null;
            resourceModes = null;
            resizingColumn = -1;
            resizeStartX = 0f;
            resizeStartWidth = 0f;
            frozenColumn = -1;
            frozenRow = -1;
            panStartMousePosition = Vector2.zero;
            panStartScrollPosition = Vector2.zero;
            scrollViewRect = Rect.zero;
            isPanning = false;
            frozenColumnInputControl = 0;
            cancelFrozenColumn = false;
            frozenRowInputControl = 0;
            cancelFrozenRow = false;
            visibleFirstRowIndex = 0;
            visibleLastRowIndexExclusive = 0;
            visibleRowCount = -1;
            rowHeights = null;
            rowOffsets = null;
            rowHeightVersion = -1;
            rowHeightWidthHash = 0;
            duplicateIds = null;
            duplicateIdVersion = -1;
        }
    }
}

#endif