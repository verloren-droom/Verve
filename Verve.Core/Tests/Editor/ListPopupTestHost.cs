namespace Verve.Tests.Editor
{
    using System;
    using UnityEditor;

    /// <summary>
    ///   <para>列表弹窗测试宿主；提供真实的编辑器 GUI 上下文。</para>
    /// </summary>
    internal sealed class ListPopupTestHost : EditorWindow
    {
        /// <summary>
        ///   <para>待执行的 GUI 操作。</para>
        /// </summary>
        internal Action Draw;

        /// <summary>
        ///   <para>执行并清除 GUI 操作。</para>
        /// </summary>
        private void OnGUI()
        {
            var draw = Draw;
            Draw = null;
            draw?.Invoke();
        }
    }
}
