#if UNITY_EDITOR

namespace Verve.Editor
{
    using UnityEditor;
    using UnityEngine;
#if UNITY_6000_3_OR_NEWER
    using UnityEditor.Toolbars;
#endif

    /// <summary>
    ///   <para>配置表编辑入口。</para>
    /// </summary>
    internal static class ConfigTableToolbar
    {
#if UNITY_6000_3_OR_NEWER
        /// <summary>
        ///   <para>工具栏路径。</para>
        /// </summary>
        private const string ToolbarPath = "Verve/ConfigTable";

        /// <summary>
        ///   <para>注册 Unity 6.3 及以上版本的主工具栏入口。</para>
        /// </summary>
        [MainToolbarElement(ToolbarPath, defaultDockPosition = MainToolbarDockPosition.Right)]
        private static MainToolbarDropdown CreateToolbarElement()
        {
            return new MainToolbarDropdown(
                new MainToolbarContent("Table", "配置表操作"),
                ShowToolbarMenu);
        }

        /// <summary>
        ///   <para>显示工具栏菜单。</para>
        /// </summary>
        /// <param name="activatorRect">弹窗锚定区域。</param>
        private static void ShowToolbarMenu(Rect activatorRect)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("从 CSV 构建所有配置表"), false, BuildAllTablesFromCsv);
            menu.AddItem(new GUIContent("从 ." + ConfigTableModule.FileExtension + " 导出所有 CSV"), false,
                ExportAllTablesToCsv);
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("配置表列表..."), false,
                () => ConfigTableListWindow.Open(activatorRect));
            menu.DropDown(activatorRect);
        }
#else
        /// <summary>
        ///   <para>菜单根节点。</para>
        /// </summary>
        private const string MenuRoot = "Verve/配置表/";

        /// <summary>
        ///   <para>构建全部表菜单。</para>
        /// </summary>
        [MenuItem(MenuRoot + "从 CSV 构建所有配置表")]
        private static void BuildAllTablesMenu() => BuildAllTablesFromCsv();

        /// <summary>
        ///   <para>导出全部表菜单。</para>
        /// </summary>
        [MenuItem(MenuRoot + "从 ." + ConfigTableModule.FileExtension + " 导出所有 CSV")]
        private static void ExportAllTablesMenu() => ExportAllTablesToCsv();

        /// <summary>
        ///   <para>打开表列表菜单。</para>
        /// </summary>
        [MenuItem(MenuRoot + "配置表列表...")]
        private static void OpenTableListMenu() => ConfigTableListWindow.Open(new Rect(0f, 0f, 1f, 1f));
#endif

        /// <summary>
        ///   <para>从 CSV 构建全部表。</para>
        /// </summary>
        private static void BuildAllTablesFromCsv()
        {
            if (EditorUtility.DisplayDialog(
                    "从 CSV 构建所有配置表",
                    "这将使用 CSV 源文件替换所有配置表及生成的访问代码。",
                    "全部构建",
                    "取消"))
            {
                ConfigTableImporter.BuildAllTables();
            }
        }

        /// <summary>
        ///   <para>将全部表导出为 CSV。</para>
        /// </summary>
        private static void ExportAllTablesToCsv()
        {
            if (EditorUtility.DisplayDialog(
                    "从 ." + ConfigTableModule.FileExtension + " 导出所有 CSV",
                    "这将合并所有分表并覆盖 CSV 源文件。",
                    "全部导出",
                    "取消"))
            {
                ConfigTableImporter.ExportAllTablesToCsv();
            }
        }
    }
}

#endif