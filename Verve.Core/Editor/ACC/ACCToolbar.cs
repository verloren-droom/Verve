#if UNITY_EDITOR

namespace Verve.Editor
{
    using UnityEditor;
    using UnityEngine;
#if UNITY_6000_3_OR_NEWER
    using UnityEditor.Toolbars;
#endif

    /// <summary>
    ///   <para>ACC 编辑入口；提供时间记录器、资源列表和资产校验。</para>
    /// </summary>
    internal static class ACCToolbar
    {
#if UNITY_6000_3_OR_NEWER
        /// <summary>
        ///   <para>工具栏路径。</para>
        /// </summary>
        private const string ToolbarPath = "Verve/ACC";

        /// <summary>
        ///   <para>注册 Unity 6.3 及以上版本的主工具栏入口。</para>
        /// </summary>
        [MainToolbarElement(ToolbarPath, defaultDockPosition = MainToolbarDockPosition.Right)]
        private static MainToolbarDropdown CreateToolbarElement()
        {
            return new MainToolbarDropdown(
                new MainToolbarContent("ACC", "ACC 时间记录器"),
                ShowToolbarMenu);
        }

        /// <summary>
        ///   <para>显示工具栏菜单。</para>
        /// </summary>
        /// <param name="activatorRect">弹窗锚定区域。</param>
        private static void ShowToolbarMenu(Rect activatorRect)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("时间记录器..."), false, ACCTemporalLoggerWindow.Open);
            menu.AddItem(new GUIContent("能力表单资源..."), false, () => CapabilitySheetListWindow.Open(activatorRect));
            menu.AddItem(new GUIContent("校验全部能力表单"), false, ValidateAssets);
            menu.DropDown(activatorRect);
        }
#else
        /// <summary>
        ///   <para>菜单根路径。</para>
        /// </summary>
        private const string MenuRoot = "Verve/ACC/";

        /// <summary>
        ///   <para>打开 ACC 时间记录器。</para>
        /// </summary>
        [MenuItem(MenuRoot + "时间记录器...", priority = 100)]
        private static void OpenTemporalLogger() => ACCTemporalLoggerWindow.Open();

        /// <summary>
        ///   <para>校验全部能力表单。</para>
        /// </summary>
        [MenuItem(MenuRoot + "校验全部能力表单", priority = 102)]
        private static void ValidateAssetsMenu() => ValidateAssets();
#endif

        /// <summary>
        ///   <para>显示能力表单资源列表弹窗。</para>
        /// </summary>
#if !UNITY_6000_3_OR_NEWER
        [MenuItem(MenuRoot + "能力表单资源...", priority = 101)]
#endif
        private static void OpenCapabilitySheets()
        {
            CapabilitySheetListWindow.Open(new Rect(80f, 80f, 1f, 1f));
        }

        /// <summary>
        ///   <para>校验资产并将结果显示在控制台。</para>
        /// </summary>
        private static void ValidateAssets()
        {
            var report = ACCAssetValidator.ValidateAll();
            if (report.IsValid)
            {
                EditorUtility.DisplayDialog("Verve ACC", $"已校验 {report.AssetCount} 个能力表单，结果通过。", "确定");
                return;
            }

            for (int i = 0; i < report.Issues.Count; i++)
                Debug.LogError($"[Verve ACC] {report.Issues[i].path}: {report.Issues[i].message}");
            EditorUtility.DisplayDialog("Verve ACC", $"发现 {report.ErrorCount} 个错误，详细信息已输出到 Console。", "确定");
        }
    }
}

#endif
