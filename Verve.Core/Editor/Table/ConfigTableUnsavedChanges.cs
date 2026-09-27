#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using UnityEditor;

    /// <summary>
    ///   <para>配置表未保存更改确认工具。</para>
    /// </summary>
    internal static class ConfigTableUnsavedChanges
    {
        /// <summary>
        ///   <para>延迟显示未保存确认，避开 Unity 当前窗口销毁回调。</para>
        /// </summary>
        /// <param name="editState">待确认的编辑状态。</param>
        /// <param name="completed">确认流程结束后的回调。</param>
        internal static void Queue(ConfigTableEditState editState, Action completed) => EditorApplication.delayCall += () => Show(editState, completed);
    
        /// <summary>
        ///   <para>显示。</para>
        /// </summary>
        /// <param name="editState">编辑状态。</param>
        /// <param name="completed">确认流程结束后的回调。</param>
        private static void Show(ConfigTableEditState editState, Action completed)
        {
            try
            {
                if (editState == null || !editState.IsDirty || EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    return;
                }
    
                if (EditorUtility.DisplayDialog("未保存的配置表", "该配置表有未保存的更改。关闭前是否保存？", "保存", "丢弃"))
                {
                    if (!editState.Save(true))
                    {
                        Game.LogError("保存配置表失败：" + editState.AssetPath + "\n" + (editState.Error ?? string.Empty));
                    }
                }
                else
                {
                    editState.Reload();
                }
            }
            finally
            {
                completed?.Invoke();
            }
        }
    }
}

#endif