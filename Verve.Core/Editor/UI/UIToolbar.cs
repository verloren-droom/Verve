#if UNITY_EDITOR

namespace Verve.Editor
{
    using UnityEditor;
    using UnityEngine;
    using UnityEditor.Build;
#if UNITY_6000_3_OR_NEWER
    using UnityEditor.Toolbars;
#endif

    /// <summary>
    ///   <para>UI 编辑工具入口。</para>
    /// </summary>
    internal static class UIToolbar
    {
#if UNITY_6000_3_OR_NEWER
        /// <summary>
        ///   <para>工具栏路径。</para>
        /// </summary>
        private const string ToolbarPath = "Verve/UI";

        /// <summary>
        ///   <para>注册 Unity 6.3 及以上版本的主工具栏入口。</para>
        /// </summary>
        [MainToolbarElement(ToolbarPath, defaultDockPosition = MainToolbarDockPosition.Right)]
        private static MainToolbarDropdown CreateToolbarElement()
        {
            return new MainToolbarDropdown(
                new MainToolbarContent("UI", "UI工具"),
                ShowToolbarMenu);
        }
        
        /// <summary>
        ///   <para>显示工具栏菜单。</para>
        /// </summary>
        /// <param name="activatorRect">弹窗锚定区域。</param>
        private static void ShowToolbarMenu(Rect activatorRect)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("构建前校验 UI"), false, ValidateUI);
            menu.AddItem(new GUIContent("UI 预制体列表..."), false,
                () => UIPrefabListWindow.Open(activatorRect));
            menu.DropDown(activatorRect);
        }
#else
        /// <summary>
        ///   <para>菜单路径。</para>
        /// </summary>
        private const string MenuPath = "Verve/UI/构建前校验 UI";
        /// <summary>
        ///   <para>预制体列表菜单路径。</para>
        /// </summary>
        private const string PrefabListMenuPath = "Verve/UI/UI 预制体列表...";

        /// <summary>
        ///   <para>校验菜单。</para>
        /// </summary>
        [MenuItem(MenuPath, priority = 100)]
        private static void ValidateMenu() => ValidateUI();

        /// <summary>
        ///   <para>打开预制体列表菜单。</para>
        /// </summary>
        [MenuItem(PrefabListMenuPath, priority = 101)]
        private static void OpenPrefabListMenu() => UIPrefabListWindow.Open(new Rect(0f, 0f, 1f, 1f));
#endif

        /// <summary>
        ///   <para>校验 UI。</para>
        /// </summary>
        private static void ValidateUI()
        {
            try
            {
                UIBuildValidator.Validate();
                EditorUtility.DisplayDialog("Verve UI", "UI View 校验通过。", "确定");
            }
            catch (BuildFailedException exception)
            {
                var result = EditorUtility.DisplayDialogComplex(
                    "Verve UI 构建前校验失败",
                    exception.Message,
                    "确定",
                    "打印详细输出日志",
                    string.Empty);
                if (result == 1)
                {
                    Debug.LogError(exception.Message);
                }
            }
        }
    }
}

#endif
