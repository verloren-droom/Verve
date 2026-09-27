#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using VisualElement = UnityEngine.UIElements.VisualElement;

    /// <summary>
    ///   <para>项目工具设置；自动发现能力接口与项目实现。</para>
    /// </summary>
    internal sealed class GameToolSettingsProvider : SettingsProvider
    {
        /// <summary>
        ///   <para>项目设置路径。</para>
        /// </summary>
        private const string SettingsPath = "Project/Verve/Tools";
        /// <summary>
        ///   <para>项目配置。</para>
        /// </summary>
        private GameToolSettings m_Settings;
        /// <summary>
        ///   <para>工具条目；接口信息与候选实现仅在页面激活时读取。</para>
        /// </summary>
        private (Type contract, GUIContent label, Type defaultType, Type[] implementations)[] m_Tools;
        /// <summary>
        ///   <para>配置文件冲突。</para>
        /// </summary>
        private string m_ConfigurationError;

        /// <summary>
        ///   <para>创建项目工具设置。</para>
        /// </summary>
        private GameToolSettingsProvider() : base(SettingsPath, SettingsScope.Project)
        {
            keywords = GameToolEditorUtility.FindToolContracts().SelectMany(contract => new[]
            {
                contract.GetCustomAttribute<GameToolAttribute>(inherit: false)?.DisplayName ?? contract.Name,
                contract.FullName
            }).Concat(new[] { "Verve", "Tools", "工具" });
        }

        /// <summary>
        ///   <para>注册设置页面。</para>
        /// </summary>
        [SettingsProvider]
        private static SettingsProvider CreateProvider() => new GameToolSettingsProvider();

        /// <inheritdoc />
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_Tools = GameToolEditorUtility.FindToolContracts().Select(contract =>
            {
                var declaration = contract.GetCustomAttribute<GameToolAttribute>(inherit: false);
                var label = new GUIContent(declaration?.DisplayName ?? contract.Name, GetTypeLabel(contract));
                return (contract, label, declaration?.DefaultImplementationType, GameToolEditorUtility.FindRuntimeImplementations(contract));
            }).ToArray();
            RefreshSettings();
            EditorApplication.projectChanged += RefreshSettings;
        }

        /// <inheritdoc />
        public override void OnDeactivate() => EditorApplication.projectChanged -= RefreshSettings;

        /// <summary>
        ///   <para>检查配置资源；文件冲突时禁用设置编辑。</para>
        /// </summary>
        private void RefreshSettings()
        {
            m_Settings = null;
            m_ConfigurationError = null;
            try { m_Settings = GameToolSettings.LoadAsset(); }
            catch (InvalidOperationException error) { m_ConfigurationError = error.Message; }
            Repaint();
        }

        /// <inheritdoc />
        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.Space();
            if (m_ConfigurationError != null)
            {
                EditorGUILayout.HelpBox(m_ConfigurationError, MessageType.Error);
                return;
            }
            EditorGUILayout.HelpBox("编辑态与运行态共用无状态工具。留空使用接口默认声明；没有默认实现时调用报错。修改在脚本重载后生效，关闭 Domain Reload 时切换运行模式不会替换实现。", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("应用配置并重载脚本")) EditorUtility.RequestScriptReload();
            foreach (var tool in m_Tools)
            {
                var contract = tool.contract;
                var name = contract.AssemblyQualifiedName;
                var selected = m_Settings?.configuration.selections.FirstOrDefault(selection => selection.contract == name);
                var types = tool.implementations;
                var selectedIndex = Array.FindIndex(types, type => type.AssemblyQualifiedName == selected?.implementation);
                var defaultType = tool.defaultType;
                var implementation = selected == null ? defaultType : selectedIndex >= 0 ? types[selectedIndex] : null;
                var defaultLabel = defaultType == null ? "未配置（调用时报错）" : "默认（" + defaultType.Name + "）";
                var label = selected == null ? defaultLabel : selectedIndex >= 0 ? GetTypeLabel(types[selectedIndex])
                    : "失效类型：" + selected.implementation;
                var rect = EditorGUILayout.GetControlRect();
                rect = EditorGUI.PrefixLabel(rect, tool.label);
                var locateRect = new Rect(rect.xMax - 44f, rect.y, 44f, rect.height);
                rect.width -= locateRect.width + 4f;
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (EditorGUI.DropdownButton(rect, new GUIContent(label), FocusType.Keyboard))
                    {
                        var labels = new[] { defaultLabel }.Concat(types.Select(GetTypeLabel)).ToArray();
                        PopupWindow.Show(rect, new SearchableOptionsPopup(label, labels, value =>
                        {
                            var choice = Array.IndexOf(labels, value);
                            var next = choice == 0 ? null : types[choice - 1];
                            if (selected?.implementation == next?.AssemblyQualifiedName) return;
                            var currentTypeName = selected?.implementation ?? defaultType?.AssemblyQualifiedName;
                            if (currentTypeName != (next ?? defaultType)?.AssemblyQualifiedName &&
                                !EditorUtility.DisplayDialog("替换工具实现",
                                    $"是否替换“{tool.label.text}”的工具实现？\n\n当前：{label}\n替换为：{value}\n\n修改在脚本重载后生效。",
                                    "替换", "取消")) return;
                            SetSelection(contract, next);
                        }));
                    }
                }
                using (new EditorGUI.DisabledScope(implementation == null))
                    if (GUI.Button(locateRect, new GUIContent("选择", "在 Project 窗口选中实现类的脚本文件")))
                    {
                        var script = CoreEditorUtility.FindMonoScriptForType(implementation);
                        if (script == null)
                            EditorUtility.DisplayDialog("无法定位脚本", $"未找到 {implementation.FullName} 对应的脚本资源。该类型可能来自 DLL，或 Unity 未将其关联到独立脚本。", "确定");
                        else
                        {
                            Selection.activeObject = script;
                            EditorGUIUtility.PingObject(script);
                        }
                    }
                if (selected != null && selectedIndex < 0)
                    EditorGUILayout.HelpBox("所选实现无法用于当前构建目标，请重新选择。", MessageType.Error);
            }
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (m_Settings != null)
                {
                    foreach (var selection in m_Settings.configuration.selections.ToArray())
                    {
                        if (m_Tools.All(tool => tool.contract.AssemblyQualifiedName != selection.contract))
                        {
                            EditorGUILayout.HelpBox("能力接口不存在或不适用于当前目标：" + selection.contract, MessageType.Error);
                            if (GUILayout.Button("移除此失效配置"))
                            {
                                Undo.RecordObject(m_Settings, "Remove tool selection");
                                m_Settings.configuration.selections.Remove(selection);
                                CoreEditorUtility.MarkDirtyAndSave(m_Settings);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///   <para>保存实现选择；首次修改时创建配置资源。</para>
        /// </summary>
        /// <param name="contract">工具接口。</param>
        /// <param name="implementation">项目实现；空值恢复默认。</param>
        internal void SetSelection(Type contract, Type implementation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Tool configuration cannot be edited while entering or running Play Mode.");
            GameToolConfiguration.Validate(contract, implementation);
            m_Settings = GameToolSettings.LoadAsset();
            if (m_Settings == null)
            {
                if (implementation == null) return;
                if (File.Exists(GameToolSettings.AssetPath))
                    throw new InvalidOperationException($"Cannot overwrite the existing file at {GameToolSettings.AssetPath}.");
                Directory.CreateDirectory(Path.GetDirectoryName(GameToolSettings.AssetPath));
                AssetDatabase.Refresh();
                m_Settings = ScriptableObject.CreateInstance<GameToolSettings>();
                try { AssetDatabase.CreateAsset(m_Settings, GameToolSettings.AssetPath); }
                catch { UnityEngine.Object.DestroyImmediate(m_Settings); throw; }
            }
            Undo.RecordObject(m_Settings, "Select tool implementation");
            m_Settings.configuration.Set(contract, implementation);
            CoreEditorUtility.MarkDirtyAndSave(m_Settings);
            Repaint();
        }

        /// <summary>
        ///   <para>生成可区分程序集的实现名称。</para>
        /// </summary>
        /// <param name="type">实现类型。</param>
        private static string GetTypeLabel(Type type) => $"{type.FullName} ({type.Assembly.GetName().Name})";
    }
}

#endif
