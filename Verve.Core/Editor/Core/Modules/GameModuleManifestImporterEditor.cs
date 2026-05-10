#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Linq;
    using UnityEngine;
    using UnityEditor;
    using System.Reflection;
    using System.Collections.Generic;
#if UNITY_2020_2_OR_NEWER
    using UnityEditor.AssetImporters;
#else
    using UnityEditor.Experimental.AssetImporters;
#endif
    using Object = UnityEngine.Object;


    /// <summary>
    ///   <para>游戏模块清单导入编辑器</para>
    /// </summary>
    [CustomEditor(typeof(GameModuleManifestImporter))]
    sealed class GameModuleManifestImporterEditor : ScriptedImporterEditor
    {
        /// <summary>
        ///   <para>模块按钮高度</para>
        /// </summary>
        private const float k_ModuleActionButtonHeight = 28f;
        private const float k_DescriptionMinHeight = 64f;
        private const float k_DependencyHandleWidth = 22f;
        private const float k_DependencyHeaderHeight = 24f;
        private const float k_DependencyRowHeight = 22f;

        /// <summary>
        ///   <para>单个模块条目的编辑上下文</para>
        /// </summary>
        private sealed class ModuleEntryContext
        {
            public Type ModuleType { get; private set; }
            public string SearchText { get; private set; }
            public string Error { get; set; }
            public bool UsesFallbackModuleInstance { get; set; }
            public GameModule Module { get; private set; }

            public static ModuleEntryContext Create(GameModuleManifestEditorData.ModuleEntry entry)
            {
                var context = new ModuleEntryContext();
                if (entry == null)
                {
                    return context;
                }

                context.Module = entry.module;
                context.Error = entry.error;
                context.UsesFallbackModuleInstance = entry.usesFallbackModuleInstance;

                var moduleType = entry.module?.GetType();
                if (moduleType == null)
                {
                    if (!GameModuleManifestAsset.TryGetModuleType(entry.type, out moduleType, out var typeError))
                    {
                        context.Error ??= typeError ?? $"Failed to find module type: {entry.type ?? "<null>"}";
                        return context;
                    }
                }

                context.ModuleType = moduleType;
                context.SearchText = BuildSearchText(moduleType);
                return context;
            }

            public void SetModule(GameModule module)
            {
                Module = module;
                if (module == null)
                {
                    ModuleType = null;
                    SearchText = string.Empty;
                    return;
                }

                ModuleType = module.GetType();
                SearchText = BuildSearchText(ModuleType);
            }

            private static string BuildSearchText(Type moduleType)
            {
                if (moduleType == null)
                {
                    return string.Empty;
                }

                return
                    $"{GetModuleMenuName(moduleType)}\n" +
                    $"{GameModuleUtility.GetStableTypeName(moduleType)}\n" +
                    $"{GetModuleDescription(moduleType)}";
            }
        }

        /// <summary>
        ///   <para>样式</para>
        /// </summary>
        private static class Styles
        {
            public static readonly GUIStyle AddModuleButton = new(EditorStyles.miniButton);
            public static readonly GUIStyle SearchField = new(EditorStyles.toolbarSearchField);
            public static readonly GUIStyle SectionBox = new(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 10),
                margin = new RectOffset(0, 0, 4, 6)
            };
            public static readonly GUIStyle SectionTitle = new(EditorStyles.boldLabel)
            {
                margin = new RectOffset(2, 0, 8, 4)
            };
            public static readonly GUIStyle DescriptionText = new(EditorStyles.textArea)
            {
                wordWrap = true
            };
            public static readonly GUIStyle DependencyHeaderLabel = new(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(6, 4, 0, 0)
            };
            public static readonly GUIStyle DependencyHandle = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.gray }
            };
            public static readonly GUIContent AddModule = new("Add Module");
            public static readonly GUIContent Advanced = new("Advanced");
            public static readonly GUIContent ResetModule = new("Reset");
            public static readonly GUIContent RemoveModule = new("Remove");
            public static readonly GUIContent InstallOrder = new("Install Order");
            public static readonly GUIContent Fields = new("Fields");
            public static readonly GUIContent Dependencies = new("Dependencies");
            public static readonly GUIContent Description = new("Description");
            public static readonly GUIContent ModuleType = new("Module type");
            public static readonly GUIContent NoAvailableModules = new("No available modules");
            public static readonly GUIContent AddDependencies = new("Add Dependencies");
            public static readonly GUIContent Cancel = new("Cancel");
        }

        /// <summary>
        ///   <para>搜索筛选</para>
        /// </summary>
        [NonSerialized] private string m_SearchFilter = string.Empty;
        [NonSerialized] private bool m_ShowAdvancedOptions;
        [NonSerialized] private bool m_SkipModifiedCheckOnce;
        [NonSerialized] private GameModuleManifestEditorData m_EditorData;
        [NonSerialized] private SerializedProperty m_InstallOrderProperty;
        [NonSerialized] private SerializedProperty m_ModuleEntriesProperty;
        [NonSerialized] private readonly List<ModuleEntryContext> m_ModuleEntryContexts = new();

        public override bool showImportedObject => false;
        protected override bool useAssetDrawPreview => false;
        protected override Type extraDataType => typeof(GameModuleManifestEditorData);

        /// <summary>
        ///   <para>当前编辑条目是否存在待应用修改</para>
        /// </summary>
        private bool HasPendingChanges
        {
            get => m_EditorData != null && m_EditorData.HasPendingChanges;
            set
            {
                if (m_EditorData != null)
                {
                    m_EditorData.HasPendingChanges = value;
                }
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            BindModuleEntryProperties();
            RefreshModuleEntryContexts();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            ClearModuleEntryContexts();
        }

        protected override void OnHeaderGUI()
        {
            ApplyHeaderIcon();
            base.OnHeaderGUI();
        }

        private void ApplyHeaderIcon()
        {
            var icon = GameModuleManifestImporter.GetIconTexture();
            if (icon == null)
            {
                return;
            }

            GameModuleManifestImporter.ApplyIcon(target, icon);

            var assetPath = GetAssetPath();
            if (!string.IsNullOrWhiteSpace(assetPath))
            {
                GameModuleManifestImporter.ApplyIcon(
                    AssetDatabase.LoadAssetAtPath<GameModuleManifestAsset>(assetPath),
                    icon);
            }
        }

        protected override void InitializeExtraDataInstance(Object extraData, int targetIndex)
        {
            m_EditorData = extraData as GameModuleManifestEditorData;
            if (m_EditorData == null)
            {
                return;
            }

            var assetPath = GetAssetPath(targetIndex);
            LoadEditorData(assetPath);
        }

        public override bool HasModified()
        {
            if (m_SkipModifiedCheckOnce)
            {
                m_SkipModifiedCheckOnce = false;
                return false;
            }

            return HasPendingChanges || base.HasModified();
        }

        public override void DiscardChanges()
        {
            m_SkipModifiedCheckOnce = false;
            base.DiscardChanges();
            if (!m_SkipModifiedCheckOnce)
            {
                FinishReset();
            }
        }

        [Obsolete]
        protected override void ResetValues()
        {
            base.ResetValues();
            FinishReset();
        }

        protected override void Apply()
        {
            if (m_EditorData == null)
            {
                base.Apply();
                return;
            }

            try
            {
                extraDataSerializedObject?.ApplyModifiedProperties();
                SyncAllModuleEntries();

                var assetPath = GetAssetPath();
                if (!string.IsNullOrWhiteSpace(assetPath))
                {
                    GameModuleManifestImporter.WriteManifestData(assetPath, m_EditorData.CreateManifestData());
                }

                HasPendingChanges = false;
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Apply Manifest Failed", ex.Message, "OK");
                return;
            }

            base.Apply();
            ReloadFromSource();
        }

        public override void OnInspectorGUI()
        {
            if (m_EditorData == null || extraDataSerializedObject == null)
            {
                ApplyRevertGUI();
                return;
            }

            BindModuleEntryProperties();

            if (m_ModuleEntriesProperty != null && m_ModuleEntryContexts.Count != m_ModuleEntriesProperty.arraySize)
            {
                RefreshModuleEntryContexts();
            }

            m_SearchFilter = EditorGUILayout.TextField(m_SearchFilter, Styles.SearchField);

            CoreEditorUtility.DrawSplitter(true);
            DrawModules();
            CoreEditorUtility.DrawSplitter(true);

            if (GUILayout.Button(Styles.AddModule, Styles.AddModuleButton, GUILayout.Height(k_ModuleActionButtonHeight)))
            {
                ShowAddModuleMenu();
            }

            DrawAdvancedOptions();
            extraDataSerializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }

        /// <summary>
        ///   <para>绘制高级选项</para>
        /// </summary>
        private void DrawAdvancedOptions()
        {
            if (m_InstallOrderProperty == null)
            {
                return;
            }

            m_ShowAdvancedOptions = EditorGUILayout.Foldout(
                m_ShowAdvancedOptions,
                Styles.Advanced,
                true);

            if (!m_ShowAdvancedOptions)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                DrawInstallOrderOption();
            }
        }

        /// <summary>
        ///   <para>绘制清单安装顺序选项</para>
        /// </summary>
        private void DrawInstallOrderOption()
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var installOrder = (GameModuleInstallOrder)EditorGUILayout.EnumPopup(
                    Styles.InstallOrder,
                    (GameModuleInstallOrder)m_InstallOrderProperty.enumValueIndex);

                if (change.changed)
                {
                    m_InstallOrderProperty.enumValueIndex = (int)installOrder;
                    HasPendingChanges = true;
                }
            }

            if ((GameModuleInstallOrder)m_InstallOrderProperty.enumValueIndex == GameModuleInstallOrder.Declared)
            {
                EditorGUILayout.HelpBox(
                    "Declared order keeps the modules in the order shown below. Each dependency must already appear earlier in the manifest.",
                    MessageType.Info);
            }
        }

        /// <summary>
        ///   <para>绘制模块列表</para>
        /// </summary>
        private void DrawModules()
        {
            if (m_ModuleEntriesProperty == null || m_ModuleEntriesProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No modules added to this manifest.", MessageType.Info);
                return;
            }

            bool hasVisibleModule = false;

            for (int i = 0; i < m_ModuleEntriesProperty.arraySize; i++)
            {
                if (i >= m_ModuleEntryContexts.Count)
                {
                    RefreshModuleEntryContexts();
                    return;
                }

                var entryProperty = m_ModuleEntriesProperty.GetArrayElementAtIndex(i);
                if (entryProperty == null)
                {
                    continue;
                }

                var entryContext = m_ModuleEntryContexts[i];
                if (!MatchesSearchFilter(entryProperty, entryContext))
                {
                    continue;
                }

                hasVisibleModule = true;
                CoreEditorUtility.DrawSplitter();

                var title = new GUIContent(GetModuleDisplayName(entryProperty, entryContext), GetModuleDescription(entryContext));
                var expanded = CoreEditorUtility.DrawHeaderToggle(
                    title,
                    entryProperty,
                    null,
                    position => OnModuleContextClick((Vector2)position, i)
                );

                if (!expanded)
                {
                    continue;
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    DrawModuleBody(i, entryProperty);
                }
            }

            if (hasVisibleModule && m_ModuleEntriesProperty.arraySize > 0)
            {
                CoreEditorUtility.DrawSplitter();
            }

            if (!hasVisibleModule)
            {
                EditorGUILayout.HelpBox($"No module matches filter: '{m_SearchFilter}'.", MessageType.Info);
            }
        }

        /// <summary>
        ///   <para>绘制模块主体内容</para>
        /// </summary>
        private void DrawModuleBody(int moduleIndex, SerializedProperty entryProperty)
        {
            if (entryProperty == null || moduleIndex < 0 || moduleIndex >= m_ModuleEntryContexts.Count)
            {
                return;
            }

            var entryContext = m_ModuleEntryContexts[moduleIndex];

            var description = GetModuleDescription(entryContext);
            if (!string.IsNullOrWhiteSpace(description))
            {
                DrawDescription(description);
            }

            if (!string.IsNullOrWhiteSpace(entryContext?.Error))
            {
                EditorGUILayout.HelpBox(
                    entryContext.Error,
                    entryContext.UsesFallbackModuleInstance ? MessageType.Warning : MessageType.Error
                );
            }

            DrawFields(moduleIndex, entryProperty, entryContext);
            DrawDependencies(entryContext);
        }

        /// <summary>
        ///   <para>绘制模块描述</para>
        /// </summary>
        private static void DrawDescription(string description)
        {
            DrawSectionTitle(Styles.Description);
            using (new IndentLevelScope(0))
            using (new EditorGUILayout.VerticalScope(Styles.SectionBox))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextArea(
                        description,
                        Styles.DescriptionText,
                        GUILayout.MinHeight(k_DescriptionMinHeight));
                }
            }
        }

        /// <summary>
        ///   <para>绘制模块字段编辑器（字段集合来源于模块可序列化字段）</para>
        /// </summary>
        private void DrawFields(int moduleIndex, SerializedProperty entryProperty, ModuleEntryContext entryContext)
        {
            DrawSectionTitle(Styles.Fields);
            using (new IndentLevelScope(0))
            using (new EditorGUILayout.VerticalScope(Styles.SectionBox))
            {
                if (moduleIndex < 0 || moduleIndex >= m_ModuleEntryContexts.Count || entryProperty == null)
                {
                    EditorGUILayout.HelpBox("Module editor is unavailable.", MessageType.Warning);
                    return;
                }

                if (entryContext == null || entryContext.ModuleType == null)
                {
                    EditorGUILayout.HelpBox("Module type could not be found, fields are unavailable.", MessageType.Warning);
                    return;
                }

                var moduleProperty = GameModuleManifestEditorData.FindModuleProperty(entryProperty);
                if (moduleProperty == null)
                {
                    EditorGUILayout.HelpBox("Serialized module state is unavailable.", MessageType.Warning);
                    return;
                }

                var module = moduleProperty.managedReferenceValue as GameModule;
                entryContext.SetModule(module);
                if (module == null)
                {
                    EditorGUILayout.HelpBox("Module instance could not be restored, fields are unavailable.", MessageType.Warning);
                    return;
                }

                bool hasVisibleField = false;
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var childProperty = moduleProperty.Copy();
                    var endProperty = childProperty.GetEndProperty();
                    bool enterChildren = true;

                    while (childProperty.NextVisible(enterChildren) &&
                           !SerializedProperty.EqualContents(childProperty, endProperty))
                    {
                        hasVisibleField = true;
                        EditorGUILayout.PropertyField(childProperty, true);
                        enterChildren = false;
                    }

                    if (change.changed)
                    {
                        extraDataSerializedObject.ApplyModifiedProperties();
                        SyncModuleEntry(moduleIndex, entryContext, commitFallbackModuleInstance: true);
                    }
                }

                if (!hasVisibleField)
                {
                    EditorGUILayout.HelpBox("This module does not expose editable serialized fields.", MessageType.Info);
                }
            }
        }

        /// <summary>
        ///   <para>绘制模块依赖项信息（依赖项集合直接来源于模块实例的 <see cref="IGameModuleDependencies"/>）</para>
        /// </summary>
        private void DrawDependencies(ModuleEntryContext entryContext)
        {
            DrawSectionTitle(Styles.Dependencies);

            using (new IndentLevelScope(0))
            {
                if (entryContext?.ModuleType == null || entryContext.Module == null)
                {
                    EditorGUILayout.HelpBox("Dependency information is unavailable.", MessageType.Warning);
                    return;
                }

                if (entryContext.UsesFallbackModuleInstance)
                {
                    EditorGUILayout.HelpBox(string.IsNullOrWhiteSpace(entryContext.Error)
                        ? "Dependency information is unavailable because this module entry contains invalid serialized data."
                        : $"Dependency information is unavailable because this module entry contains invalid serialized data.\n{entryContext.Error}", MessageType.Warning);
                    return;
                }

                Type[] dependencies;
                try
                {
                    dependencies = GameModuleDependencyUtility.GetDependencies(entryContext.Module);
                }
                catch (Exception ex)
                {
                    EditorGUILayout.HelpBox(ex.Message, MessageType.Error);
                    return;
                }

                if (dependencies == null || dependencies.Length == 0)
                {
                    EditorGUILayout.HelpBox("This module does not declare dependencies.", MessageType.Info);
                    return;
                }

                DrawDependencyTable(dependencies);
            }
        }

        private static void DrawSectionTitle(GUIContent title)
        {
            using (new IndentLevelScope(0))
            {
                EditorGUILayout.LabelField(title, Styles.SectionTitle);
            }
        }

        private readonly struct IndentLevelScope : IDisposable
        {
            private readonly int m_IndentLevel;

            public IndentLevelScope(int indentLevel)
            {
                m_IndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = indentLevel;
            }

            public void Dispose()
            {
                EditorGUI.indentLevel = m_IndentLevel;
            }
        }

        private static void DrawDependencyTable(Type[] dependencies)
        {
            if (dependencies == null || dependencies.Length == 0)
            {
                return;
            }

            var totalHeight = k_DependencyHeaderHeight + (dependencies.Length * k_DependencyRowHeight);
            var tableRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                GUIStyle.none,
                GUILayout.Height(totalHeight),
                GUILayout.ExpandWidth(true));

            var headerRect = new Rect(
                tableRect.x + 1f,
                tableRect.y + 1f,
                tableRect.width - 2f,
                k_DependencyHeaderHeight);

            var background = EditorGUIUtility.isProSkin
                ? new Color(0.23f, 0.23f, 0.23f, 1f)
                : new Color(0.88f, 0.88f, 0.88f, 1f);
            var border = EditorGUIUtility.isProSkin
                ? new Color(0.12f, 0.12f, 0.12f, 1f)
                : new Color(0.62f, 0.62f, 0.62f, 1f);
            var headerColor = EditorGUIUtility.isProSkin
                ? new Color(0.19f, 0.19f, 0.19f, 1f)
                : new Color(0.79f, 0.79f, 0.79f, 1f);
            var separator = EditorGUIUtility.isProSkin
                ? new Color(0.32f, 0.32f, 0.32f, 1f)
                : new Color(0.72f, 0.72f, 0.72f, 1f);

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(tableRect, background);
                EditorGUI.DrawRect(new Rect(tableRect.x, tableRect.y, tableRect.width, 1f), border);
                EditorGUI.DrawRect(new Rect(tableRect.x, tableRect.yMax - 1f, tableRect.width, 1f), border);
                EditorGUI.DrawRect(new Rect(tableRect.x, tableRect.y, 1f, tableRect.height), border);
                EditorGUI.DrawRect(new Rect(tableRect.xMax - 1f, tableRect.y, 1f, tableRect.height), border);
                EditorGUI.DrawRect(headerRect, headerColor);
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 1f, headerRect.width, 1f), separator);
            }

            GUI.Label(
                new Rect(headerRect.x, headerRect.y, k_DependencyHandleWidth, headerRect.height),
                GUIContent.none);
            EditorGUI.LabelField(
                new Rect(
                    headerRect.x + k_DependencyHandleWidth,
                    headerRect.y,
                    headerRect.width - k_DependencyHandleWidth,
                    headerRect.height),
                Styles.ModuleType,
                Styles.DependencyHeaderLabel);

            var rowY = headerRect.yMax;
            using (new EditorGUI.DisabledScope(true))
            {
                for (int i = 0; i < dependencies.Length; i++)
                {
                    var rowRect = new Rect(
                        headerRect.x,
                        rowY + (i * k_DependencyRowHeight),
                        headerRect.width,
                        k_DependencyRowHeight);

                    if (i > 0 && Event.current.type == EventType.Repaint)
                    {
                        EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y, rowRect.width, 1f), separator);
                    }

                    GUI.Label(
                        new Rect(rowRect.x, rowRect.y, k_DependencyHandleWidth, rowRect.height),
                        "=",
                        Styles.DependencyHandle);
                    EditorGUI.TextField(
                        new Rect(
                            rowRect.x + k_DependencyHandleWidth,
                            rowRect.y + 1f,
                            rowRect.width - k_DependencyHandleWidth - 1f,
                            rowRect.height - 2f),
                        GameModuleUtility.GetTypeDisplayName(dependencies[i]));
                }
            }
        }

        /// <summary>
        ///   <para>模块右键菜单</para>
        /// </summary>
        private void OnModuleContextClick(Vector2 position, int moduleIndex)
        {
            var menu = new GenericMenu();
            menu.AddItem(Styles.ResetModule, false, () => ResetModuleFields(moduleIndex));
            menu.AddItem(Styles.RemoveModule, false, () => RemoveModule(moduleIndex));

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        /// <summary>
        ///   <para>重置模块字段为默认构造值</para>
        /// </summary>
        private void ResetModuleFields(int index)
        {
            BindModuleEntryProperties();
            if (m_ModuleEntriesProperty == null || index < 0 || index >= m_ModuleEntriesProperty.arraySize)
            {
                return;
            }

            var entryProperty = m_ModuleEntriesProperty.GetArrayElementAtIndex(index);
            if (!GameModuleManifestEntryPropertyUtility.TryGetModuleType(entryProperty, out var moduleType))
            {
                EditorUtility.DisplayDialog(
                    "Reset Module Failed",
                    "Module type could not be found, fields cannot be reset.",
                    "OK");
                return;
            }

            var moduleName = GetModuleDisplayName(entryProperty, index < m_ModuleEntryContexts.Count ? m_ModuleEntryContexts[index] : null);
            if (!EditorUtility.DisplayDialog(
                    "Reset Module Fields",
                    $"Reset all fields of '{moduleName}' to default values?",
                    Styles.ResetModule.text,
                    Styles.Cancel.text))
            {
                return;
            }

            try
            {
                var entry = GameModuleManifestEditorData.CreateEntryFromType(moduleType, GameModuleManifestAsset.EmptyFields);
                ApplyModuleEntriesChange(() =>
                {
                    GameModuleManifestEditorData.WriteModuleEntry(
                        m_ModuleEntriesProperty.GetArrayElementAtIndex(index),
                        entry);
                });
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Reset Module Failed", ex.Message, "OK");
            }
        }

        /// <summary>
        ///   <para>显示添加模块菜单</para>
        /// </summary>
        private void ShowAddModuleMenu()
        {
            var menu = new GenericMenu();
            var moduleTypes = GetAddableModuleTypes();

            if (moduleTypes.Count == 0)
            {
                menu.AddDisabledItem(Styles.NoAvailableModules);
                menu.ShowAsContext();
                return;
            }

            var duplicatedNames = new HashSet<string>(
                moduleTypes
                    .GroupBy(GetModuleMenuName, StringComparer.Ordinal)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key),
                StringComparer.Ordinal
            );

            for (int i = 0; i < moduleTypes.Count; i++)
            {
                var moduleType = moduleTypes[i];
                var menuName = GetModuleMenuName(moduleType);
                if (duplicatedNames.Contains(menuName))
                {
                    menuName = $"{menuName} ({moduleType.FullName})";
                }

                if (GameModuleManifestEntryPropertyUtility.ContainsModuleType(m_ModuleEntriesProperty, moduleType))
                {
                    menu.AddDisabledItem(new GUIContent($"{menuName} (Already Added)"));
                    continue;
                }

                menu.AddItem(new GUIContent(menuName), false, () => AddModule(moduleType));
            }

            menu.ShowAsContext();
        }

        /// <summary>
        ///   <para>添加模块</para>
        /// </summary>
        private void AddModule(Type moduleType)
        {
            if (moduleType == null || m_ModuleEntriesProperty == null)
            {
                return;
            }

            try
            {
                var addableModuleTypes = GetAddableModuleTypes();
                var missingDependencyTypes = FindMissingDependenciesToAdd(moduleType, addableModuleTypes, out var dependencyError);
                if (!string.IsNullOrWhiteSpace(dependencyError))
                {
                    EditorUtility.DisplayDialog("Add Module Failed", dependencyError, "OK");
                    return;
                }

                if (missingDependencyTypes.Count > 0 &&
                    !ConfirmAddDependencies(moduleType, missingDependencyTypes))
                {
                    return;
                }

                ApplyModuleEntriesChange(() =>
                {
                    for (int i = 0; i < missingDependencyTypes.Count; i++)
                    {
                        AppendModuleEntry(missingDependencyTypes[i]);
                    }

                    AppendModuleEntry(moduleType);
                });
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Add Module Failed", ex.Message, "OK");
            }
        }

        /// <summary>
        ///   <para>向编辑条目列表追加一个模块条目</para>
        /// </summary>
        private void AppendModuleEntry(Type moduleType)
        {
            if (moduleType == null ||
                m_ModuleEntriesProperty == null ||
                GameModuleManifestEntryPropertyUtility.ContainsModuleType(m_ModuleEntriesProperty, moduleType))
            {
                return;
            }

            var entry = GameModuleManifestEditorData.CreateEntryFromType(moduleType, GameModuleManifestAsset.EmptyFields);
            int insertIndex = m_ModuleEntriesProperty.arraySize;
            m_ModuleEntriesProperty.arraySize++;
            GameModuleManifestEditorData.WriteModuleEntry(m_ModuleEntriesProperty.GetArrayElementAtIndex(insertIndex), entry);
        }

        /// <summary>
        ///   <para>计算添加模块前需要补充的缺失依赖</para>
        /// </summary>
        private List<Type> FindMissingDependenciesToAdd(Type moduleType, IReadOnlyList<Type> addableModuleTypes, out string error)
        {
            error = null;
            var missingDependencyTypes = new List<Type>();
            var plannedTypes = new List<Type>();
            var visitingTypes = new HashSet<RuntimeTypeHandle>();

            if (!TryAppendMissingDependencies(
                    rootType: moduleType,
                    moduleType: moduleType,
                    addableModuleTypes,
                    plannedTypes,
                    missingDependencyTypes,
                    visitingTypes,
                    out error))
            {
                missingDependencyTypes.Clear();
            }

            return missingDependencyTypes;
        }

        /// <summary>
        ///   <para>递归追加缺失依赖，返回依赖优先的追加顺序</para>
        /// </summary>
        private bool TryAppendMissingDependencies(
            Type rootType,
            Type moduleType,
            IReadOnlyList<Type> availableModuleTypes,
            List<Type> plannedTypes,
            List<Type> missingDependencyTypes,
            HashSet<RuntimeTypeHandle> visitingTypes,
            out string error)
        {
            error = null;
            if (moduleType == null) return true;

            var moduleHandle = moduleType.TypeHandle;
            if (!visitingTypes.Add(moduleHandle))
            {
                error = $"Module dependency cycle detected while preparing add list. module={moduleType.FullName}";
                return false;
            }

            try
            {
                if (!GameModuleDependencyUtility.TryInspectDependencies(
                        moduleType,
                        GameModuleManifestAsset.EmptyFields,
                        out var dependencyTypes,
                        out error))
                {
                    error =
                        $"Failed to inspect module dependencies before adding '{GetModuleMenuName(rootType)}'. " +
                        $"module={GameModuleUtility.GetTypeDisplayName(moduleType)}, error={error}";
                    return false;
                }

                if (dependencyTypes == null || dependencyTypes.Length == 0)
                {
                    return true;
                }

                for (int i = 0; i < dependencyTypes.Length; i++)
                {
                    var dependencyType = dependencyTypes[i];
                    if (IsDependencyIncluded(dependencyType, plannedTypes))
                    {
                        continue;
                    }

                    if (!TryFindDependencyModuleType(
                            rootType,
                            moduleType,
                            dependencyType,
                            availableModuleTypes,
                            out var dependencyModuleType,
                            out error))
                    {
                        return false;
                    }

                    if (plannedTypes.Contains(dependencyModuleType))
                    {
                        continue;
                    }

                    if (!TryAppendMissingDependencies(
                            rootType,
                            dependencyModuleType,
                            availableModuleTypes,
                            plannedTypes,
                            missingDependencyTypes,
                            visitingTypes,
                            out error))
                    {
                        return false;
                    }

                    plannedTypes.Add(dependencyModuleType);
                    missingDependencyTypes.Add(dependencyModuleType);
                }

                return true;
            }
            finally
            {
                visitingTypes.Remove(moduleHandle);
            }
        }

        /// <summary>
        ///   <para>判断某个依赖是否已被清单或本次追加计划纳入</para>
        /// </summary>
        private bool IsDependencyIncluded(Type dependencyType, IReadOnlyList<Type> plannedTypes)
        {
            if (dependencyType == null)
            {
                return true;
            }

            if (GameModuleManifestEntryPropertyUtility.ContainsModuleForDependency(m_ModuleEntriesProperty, dependencyType))
            {
                return true;
            }

            if (plannedTypes == null)
            {
                return false;
            }

            for (int i = 0; i < plannedTypes.Count; i++)
            {
                var plannedType = plannedTypes[i];
                if (plannedType != null && dependencyType.IsAssignableFrom(plannedType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>查找依赖应该追加的具体模块类型</para>
        /// </summary>
        private bool TryFindDependencyModuleType(
            Type rootType,
            Type moduleType,
            Type dependencyType,
            IReadOnlyList<Type> availableModuleTypes,
            out Type dependencyModuleType,
            out string error)
        {
            dependencyModuleType = null;
            error = null;

            if (dependencyType == null)
            {
                return true;
            }

            if (CanAddModuleType(dependencyType) && dependencyType != moduleType)
            {
                dependencyModuleType = dependencyType;
                return true;
            }

            var candidateTypes = availableModuleTypes
                .Where(type => type != null && type != moduleType && dependencyType.IsAssignableFrom(type))
                .ToList();

            if (candidateTypes.Count == 1)
            {
                dependencyModuleType = candidateTypes[0];
                return true;
            }

            if (candidateTypes.Count == 0)
            {
                error =
                    $"Module '{GetModuleMenuName(rootType)}' requires '{dependencyType.FullName}', " +
                    "but no addable module type matches this dependency.";
                return false;
            }

            error =
                $"Module '{GetModuleMenuName(rootType)}' requires '{dependencyType.FullName}', " +
                $"but multiple module types match this dependency: {string.Join(", ", candidateTypes.Select(GetModuleMenuName))}.";
            return false;
        }

        /// <summary>
        ///   <para>确认是否把缺失依赖一并追加到清单</para>
        /// </summary>
        private bool ConfirmAddDependencies(Type moduleType, IReadOnlyList<Type> dependencyTypes)
        {
            var dependencyLines = string.Join("\n", dependencyTypes.Select(type => $"- {GetModuleMenuName(type)}"));
            var message =
                $"'{GetModuleMenuName(moduleType)}' declares missing dependencies:\n{dependencyLines}\n\n" +
                "Add these dependencies to the manifest before adding the module?";

            return EditorUtility.DisplayDialog(
                "Add Module Dependencies",
                message,
                Styles.AddDependencies.text,
                Styles.Cancel.text);
        }

        /// <summary>
        ///   <para>移除模块</para>
        /// </summary>
        private void RemoveModule(int index)
        {
            if (m_ModuleEntriesProperty == null || index < 0 || index >= m_ModuleEntriesProperty.arraySize)
            {
                return;
            }

            if (!ConfirmRemoveModule(index))
            {
                return;
            }

            ApplyModuleEntriesChange(() => m_ModuleEntriesProperty.DeleteArrayElementAtIndex(index));
        }

        /// <summary>
        ///   <para>确认移除模块；移除会破坏依赖时给出明确提示</para>
        /// </summary>
        private bool ConfirmRemoveModule(int index)
        {
            var dependentTypes = new List<Type>();
            if (!GameModuleManifestEntryPropertyUtility.TryFindBrokenDependents(
                    m_ModuleEntriesProperty,
                    index,
                    dependentTypes,
                    out var dependencyError))
            {
                return EditorUtility.DisplayDialog(
                    "Remove Module",
                    "Could not verify module dependencies before removing this module.\n\n" +
                    $"{dependencyError}\n\nRemove anyway?",
                    Styles.RemoveModule.text,
                    Styles.Cancel.text);
            }

            if (dependentTypes.Count == 0)
            {
                return true;
            }

            var removedModuleName = GetModuleDisplayName(index);
            var dependentLines = string.Join("\n", dependentTypes.Select(type => $"- {GetModuleMenuName(type)}"));
            var message =
                $"'{removedModuleName}' is required by:\n{dependentLines}\n\n" +
                "Removing it will leave these dependencies unresolved. Remove anyway?";

            return EditorUtility.DisplayDialog(
                "Remove Depended Module",
                message,
                Styles.RemoveModule.text,
                Styles.Cancel.text);
        }

        /// <summary>
        ///   <para>重新从清单文件加载编辑数据</para>
        /// </summary>
        private void ReloadFromSource()
        {
            ClearModuleEntryContexts();

            if (m_EditorData == null)
            {
                return;
            }

            var assetPath = GetAssetPath();
            LoadEditorData(assetPath);
            BindModuleEntryProperties();
            RefreshModuleEntryContexts();
            Repaint();
        }

        /// <summary>
        ///   <para>完成一次 Revert/Reset 之后的本地状态重载与同步</para>
        /// </summary>
        private void FinishReset()
        {
            ReloadFromSource();
            serializedObject?.Update();
            HasPendingChanges = false;
            m_SkipModifiedCheckOnce = true;
        }

        /// <summary>
        ///   <para>按资源路径加载清单编辑数据</para>
        /// </summary>
        private void LoadEditorData(string assetPath)
        {
            if (m_EditorData == null)
            {
                return;
            }

            var manifestData = string.IsNullOrWhiteSpace(assetPath)
                ? GameModuleManifestAsset.CreateEmptyManifestData()
                : GameModuleManifestImporter.ReadManifestData(assetPath);

            var moduleEntries = GameModuleManifestEditorData.CreateModuleEntriesFromManifestData(manifestData);
            m_EditorData.LoadModuleEntries(manifestData, moduleEntries);
        }

        /// <summary>
        ///   <para>绑定额外数据序列化属性</para>
        /// </summary>
        private void BindModuleEntryProperties()
        {
            if (extraDataSerializedObject == null)
            {
                m_InstallOrderProperty = null;
                m_ModuleEntriesProperty = null;
                return;
            }

            extraDataSerializedObject.Update();
            m_InstallOrderProperty = extraDataSerializedObject.FindProperty(GameModuleManifestEditorData.InstallOrderPropertyName);
            m_ModuleEntriesProperty = extraDataSerializedObject.FindProperty(GameModuleManifestEditorData.ModuleEntriesPropertyName);
        }

        /// <summary>
        ///   <para>刷新模块条目上下文</para>
        /// </summary>
        private void RefreshModuleEntryContexts()
        {
            ClearModuleEntryContexts();

            if (m_ModuleEntriesProperty == null)
            {
                return;
            }

            for (int i = 0; i < m_ModuleEntriesProperty.arraySize; i++)
            {
                var entryProperty = m_ModuleEntriesProperty.GetArrayElementAtIndex(i);
                if (entryProperty == null)
                {
                    m_ModuleEntryContexts.Add(null);
                    continue;
                }

                m_ModuleEntryContexts.Add(ModuleEntryContext.Create(GameModuleManifestEditorData.ReadModuleEntry(entryProperty)));
            }
        }

        /// <summary>
        ///   <para>清空模块条目上下文</para>
        /// </summary>
        private void ClearModuleEntryContexts()
        {
            m_ModuleEntryContexts.Clear();
        }

        /// <summary>
        ///   <para>同步单个模块条目编辑结果到清单编辑数据</para>
        /// </summary>
        private void SyncModuleEntry(int moduleIndex, ModuleEntryContext entryContext, bool commitFallbackModuleInstance)
        {
            if (entryContext == null || m_ModuleEntriesProperty == null || moduleIndex < 0 || moduleIndex >= m_ModuleEntriesProperty.arraySize)
            {
                return;
            }

            var entryProperty = m_ModuleEntriesProperty.GetArrayElementAtIndex(moduleIndex);
            var declaredType = GameModuleManifestEditorData.ReadModuleType(entryProperty);
            var moduleProperty = GameModuleManifestEditorData.FindModuleProperty(entryProperty);
            var module = moduleProperty?.managedReferenceValue as GameModule;
            entryContext.SetModule(module);

            if (module == null || entryContext.ModuleType == null)
            {
                return;
            }

            var usesFallbackModuleInstance = entryProperty
                .FindPropertyRelative(GameModuleManifestEditorData.ModuleEntry.UsesFallbackModuleInstancePropertyName)
                ?.boolValue ?? false;
            if (usesFallbackModuleInstance && !commitFallbackModuleInstance)
            {
                return;
            }

            try
            {
                if (GameModuleManifestAsset.TryGetModuleType(declaredType, out var declaredModuleType) &&
                    declaredModuleType != module.GetType())
                {
                    var declaredModule = RecreateModuleFromDeclaredType(entryProperty, declaredModuleType);
                    extraDataSerializedObject.ApplyModifiedProperties();
                    entryContext.SetModule(declaredModule);

                    throw new InvalidOperationException(
                        "Changing module type inside the manifest inspector is not supported. Remove the current module and add the target module type.");
                }

                var entry = GameModuleManifestEditorData.CreateEntryFromModule(module);
                GameModuleManifestEditorData.WriteModuleEntry(entryProperty, entry);
                extraDataSerializedObject.ApplyModifiedProperties();

                entryContext.Error = null;
                entryContext.UsesFallbackModuleInstance = false;
                HasPendingChanges = true;
            }
            catch (Exception ex)
            {
                entryContext.Error = ex.Message;
            }
        }

        /// <summary>
        ///   <para>同步全部模块条目编辑结果到清单编辑数据</para>
        /// </summary>
        private void SyncAllModuleEntries()
        {
            for (int i = 0; i < m_ModuleEntryContexts.Count; i++)
            {
                SyncModuleEntry(i, m_ModuleEntryContexts[i], commitFallbackModuleInstance: false);
            }
        }

        /// <summary>
        ///   <para>按条目声明类型重新创建模块实例，避免意外修改 <see cref="SerializeReference"/> 的具体类型</para>
        /// </summary>
        private static GameModule RecreateModuleFromDeclaredType(SerializedProperty entryProperty, Type declaredType)
        {
            if (entryProperty == null) return null;
            if (declaredType == null) throw new ArgumentNullException(nameof(declaredType));

            var moduleProperty = GameModuleManifestEditorData.FindModuleProperty(entryProperty);
            if (moduleProperty == null)
            {
                return null;
            }

            var fields = GameModuleManifestEditorData.ReadModuleEntry(entryProperty).fields;
            try
            {
                var declaredEntry = GameModuleManifestEditorData.CreateEntryFromManifestEntry(
                    new GameModuleManifestAsset.GameModuleEntry
                    {
                        type = GameModuleUtility.GetStableTypeName(declaredType),
                        fields = fields
                    });
                moduleProperty.managedReferenceValue =
                    declaredEntry.module ?? GameModuleManifestAsset.CreateModuleInstance(declaredType, GameModuleManifestAsset.EmptyFields);
            }
            catch (Exception restoreException)
            {
                try
                {
                    moduleProperty.managedReferenceValue =
                        GameModuleManifestAsset.CreateModuleInstance(declaredType, GameModuleManifestAsset.EmptyFields);
                }
                catch (Exception fallbackException)
                {
                    throw new InvalidOperationException(
                        $"Failed to recreate declared module type {declaredType.FullName}.",
                        GameModuleUtility.CombineErrors(restoreException, fallbackException));
                }
            }

            return (GameModule)moduleProperty.managedReferenceValue;
        }

        /// <summary>
        ///   <para>判断模块是否命中搜索筛选</para>
        /// </summary>
        private bool MatchesSearchFilter(SerializedProperty property, ModuleEntryContext entryContext)
        {
            if (string.IsNullOrWhiteSpace(m_SearchFilter))
            {
                return true;
            }

            var searchSource = entryContext?.SearchText;
            if (string.IsNullOrWhiteSpace(searchSource))
            {
                searchSource = GameModuleManifestEditorData.ReadModuleType(property);
            }

            return searchSource.IndexOf(m_SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        ///   <para>获取模块显示名称</para>
        /// </summary>
        private static string GetModuleDisplayName(SerializedProperty property, ModuleEntryContext entryContext)
        {
            if (entryContext?.ModuleType != null)
            {
                return GetModuleMenuName(entryContext.ModuleType);
            }

            return GetFallbackModuleName(GameModuleManifestEditorData.ReadModuleType(property));
        }

        /// <summary>
        ///   <para>获取可添加模块类型列表</para>
        /// </summary>
        private static List<Type> GetAddableModuleTypes()
        {
            return TypeCache.GetTypesDerivedFrom<GameModule>()
                .Where(CanAddModuleType)
                .OrderBy(GetModuleMenuName, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        ///   <para>获取模块菜单显示名称</para>
        /// </summary>
        private static string GetModuleMenuName(Type moduleType)
        {
            if (moduleType == null)
            {
                return "Unknown Module";
            }

            var typeName = moduleType.Name;
            const string suffix = nameof(GameModule);
            return typeName.EndsWith(suffix, StringComparison.Ordinal) && typeName.Length > suffix.Length
                ? typeName.Substring(0, typeName.Length - suffix.Length)
                : typeName;
        }

        /// <summary>
        ///   <para>获取模块描述文本</para>
        /// </summary>
        private static string GetModuleDescription(ModuleEntryContext entryContext)
        {
            return entryContext?.ModuleType == null ? null : GetModuleDescription(entryContext.ModuleType);
        }

        private static string GetModuleDescription(Type moduleType)
        {
            if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));
            return moduleType.GetCustomAttribute<GameModuleAttribute>()?.description ?? string.Empty;
        }

        /// <summary>
        ///   <para>判断模块类型是否可以直接追加到清单</para>
        /// </summary>
        private static bool CanAddModuleType(Type moduleType)
        {
            return moduleType != null && moduleType.GetCustomAttribute<GameModuleAttribute>() != null && GameModuleManifestAsset.GetManifestModuleTypeError(moduleType) == null;
        }

        /// <summary>
        ///   <para>获取兜底显示名</para>
        /// </summary>
        private static string GetFallbackModuleName(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return "Unknown Module";
            }

            var commaIndex = typeName.IndexOf(',');
            var displayTypeName = commaIndex >= 0 ? typeName.Substring(0, commaIndex) : typeName;
            var dotIndex = displayTypeName.LastIndexOf('.');
            return dotIndex >= 0 ? displayTypeName.Substring(dotIndex + 1) : displayTypeName;
        }

        private string GetModuleDisplayName(int index)
        {
            if (m_ModuleEntriesProperty == null || index < 0 || index >= m_ModuleEntriesProperty.arraySize)
            {
                return "Unknown Module";
            }

            var entryProperty = m_ModuleEntriesProperty.GetArrayElementAtIndex(index);
            var entryContext = index < m_ModuleEntryContexts.Count ? m_ModuleEntryContexts[index] : null;
            return GetModuleDisplayName(entryProperty, entryContext);
        }

        private void ApplyModuleEntriesChange(Action change)
        {
            if (extraDataSerializedObject == null || change == null)
            {
                return;
            }

            BindModuleEntryProperties();
            if (m_ModuleEntriesProperty == null)
            {
                return;
            }

            change.Invoke();
            extraDataSerializedObject.ApplyModifiedProperties();

            HasPendingChanges = true;
            RefreshModuleEntryContexts();
            Repaint();
        }

        /// <summary>
        ///   <para>获取当前清单资源路径</para>
        /// </summary>
        private string GetAssetPath(int targetIndex = 0)
        {
            if (targets == null || targetIndex < 0 || targetIndex >= targets.Length)
            {
                return null;
            }

            return AssetDatabase.GetAssetPath(targets[targetIndex]);
        }
    }
}

#endif