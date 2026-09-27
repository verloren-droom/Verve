#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Linq;
    using UnityEngine;
    using UnityEditor;
    using System.Reflection;
    using System.Collections.Generic;
    using UnityEditor.AssetImporters;
    using Object = UnityEngine.Object;
    
    /// <summary>
    ///   <para>游戏模块清单导入编辑器。</para>
    /// </summary>
    [CustomEditor(typeof(GameModuleManifestImporter))]
    sealed class GameModuleManifestImporterEditor : ScriptedImporterEditor
    {
        /// <summary>
        ///   <para>模块按钮高度。</para>
        /// </summary>
        private const float k_ModuleActionButtonHeight = 28f;
        /// <summary>
        ///   <para>描述最小高度。</para>
        /// </summary>
        private const float k_DescriptionMinHeight = 64f;
        /// <summary>
        ///   <para>依赖句柄宽度。</para>
        /// </summary>
        private const float k_DependencyHandleWidth = 22f;
        /// <summary>
        ///   <para>依赖表头高度。</para>
        /// </summary>
        private const float k_DependencyHeaderHeight = 24f;
        /// <summary>
        ///   <para>依赖行高。</para>
        /// </summary>
        private const float k_DependencyRowHeight = 22f;

        /// <summary>
        ///   <para>单个模块条目的编辑上下文。</para>
        /// </summary>
        private sealed class ModuleEntryContext : DisposableObject
        {
            /// <summary>
            ///   <para>字段编辑器；由条目上下文独占。</para>
            /// </summary>
            private GameModuleEditor m_Editor;
            /// <summary>
            ///   <para>字段编辑器；首次展开时创建。</para>
            /// </summary>
            public GameModuleEditor Editor => m_Editor ??= GameModuleEditor.Create(ModuleType);
            /// <summary>
            ///   <para>模块类型。</para>
            /// </summary>
            public Type ModuleType { get; private set; }
            /// <summary>
            ///   <para>搜索文本。</para>
            /// </summary>
            public string SearchText { get; private set; }
            /// <summary>
            ///   <para>错误。</para>
            /// </summary>
            public string Error { get; set; }
            /// <summary>
            ///   <para>模块。</para>
            /// </summary>
            public GameModule Module { get; private set; }

            /// <summary>
            ///   <para>创建实例。</para>
            /// </summary>
            /// <param name="entry">条目。</param>
            public static ModuleEntryContext Create(GameModuleManifestEditorData.ModuleEntry entry)
            {
                var context = new ModuleEntryContext();
                if (entry == null)
                {
                    return context;
                }

                context.Module = entry.module;
                context.Error = entry.error;

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

            /// <summary>
            ///   <para>设置模块。</para>
            /// </summary>
            /// <param name="module">模块。</param>
            public void SetModule(GameModule module)
            {
                Module = module;
                var moduleType = module?.GetType();
                // Unity 会重建托管引用；编辑器按条目类型复用，每次绘制借用当前属性。
                if (ModuleType == moduleType) return;
                var previousEditor = m_Editor;
                m_Editor = null;
                ModuleType = moduleType;
                SearchText = BuildSearchText(ModuleType);
                previousEditor?.Dispose();
            }

            /// <inheritdoc />
            protected override void OnDispose()
            {
                var editor = m_Editor;
                m_Editor = null;
                Module = null;
                editor?.Dispose();
            }

            /// <summary>
            ///   <para>构建搜索文本。</para>
            /// </summary>
            /// <param name="moduleType">模块类型。</param>
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
        ///   <para>样式。</para>
        /// </summary>
        private static class Styles
        {
            /// <summary>
            ///   <para>添加模块按钮。</para>
            /// </summary>
            public static readonly GUIStyle AddModuleButton = new(EditorStyles.miniButton);
            /// <summary>
            ///   <para>搜索框。</para>
            /// </summary>
            public static readonly GUIStyle SearchField = new(EditorStyles.toolbarSearchField);
            /// <summary>
            ///   <para>区域容器。</para>
            /// </summary>
            public static readonly GUIStyle SectionBox = new(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 10),
                margin = new RectOffset(0, 0, 4, 6)
            };
            /// <summary>
            ///   <para>区域标题。</para>
            /// </summary>
            public static readonly GUIStyle SectionTitle = new(EditorStyles.boldLabel)
            {
                margin = new RectOffset(2, 0, 8, 4)
            };
            /// <summary>
            ///   <para>描述文本。</para>
            /// </summary>
            public static readonly GUIStyle DescriptionText = new(EditorStyles.textArea)
            {
                wordWrap = true
            };
            /// <summary>
            ///   <para>依赖表头标签。</para>
            /// </summary>
            public static readonly GUIStyle DependencyHeaderLabel = new(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(6, 4, 0, 0)
            };
            /// <summary>
            ///   <para>依赖句柄。</para>
            /// </summary>
            public static readonly GUIStyle DependencyHandle = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.gray }
            };
            /// <summary>
            ///   <para>添加模块。</para>
            /// </summary>
            public static readonly GUIContent AddModule = new("Add Module");
            /// <summary>
            ///   <para>高级。</para>
            /// </summary>
            public static readonly GUIContent Advanced = new("Advanced");
            /// <summary>
            ///   <para>重置模块。</para>
            /// </summary>
            public static readonly GUIContent ResetModule = new("Reset");
            /// <summary>
            ///   <para>移除模块。</para>
            /// </summary>
            public static readonly GUIContent RemoveModule = new("Remove");
            /// <summary>
            ///   <para>安装顺序。</para>
            /// </summary>
            public static readonly GUIContent InstallOrder = new("Install Order");
            /// <summary>
            ///   <para>字段。</para>
            /// </summary>
            public static readonly GUIContent Fields = new("Fields");
            /// <summary>
            ///   <para>依赖。</para>
            /// </summary>
            public static readonly GUIContent Dependencies = new("Dependencies");
            /// <summary>
            ///   <para>描述。</para>
            /// </summary>
            public static readonly GUIContent Description = new("Description");
            /// <summary>
            ///   <para>模块类型。</para>
            /// </summary>
            public static readonly GUIContent ModuleType = new("Module type");
            /// <summary>
            ///   <para>无可用模块。</para>
            /// </summary>
            public static readonly GUIContent NoAvailableModules = new("No available modules");
            /// <summary>
            ///   <para>添加依赖。</para>
            /// </summary>
            public static readonly GUIContent AddDependencies = new("Add Dependencies");
            /// <summary>
            ///   <para>取消。</para>
            /// </summary>
            public static readonly GUIContent Cancel = new("Cancel");
        }

        /// <summary>
        ///   <para>搜索筛选。</para>
        /// </summary>
        [NonSerialized] private string m_SearchFilter = string.Empty;
        /// <summary>
        ///   <para>显示高级选项。</para>
        /// </summary>
        [NonSerialized] private bool m_ShowAdvancedOptions;
        /// <summary>
        ///   <para>跳过下一次修改检查。</para>
        /// </summary>
        [NonSerialized] private bool m_SkipModifiedCheckOnce;
        /// <summary>
        ///   <para>编辑器数据。</para>
        /// </summary>
        [NonSerialized] private GameModuleManifestEditorData m_EditorData;
        /// <summary>
        ///   <para>安装顺序属性。</para>
        /// </summary>
        [NonSerialized] private SerializedProperty m_InstallOrderProperty;
        /// <summary>
        ///   <para>模块条目属性。</para>
        /// </summary>
        [NonSerialized] private SerializedProperty m_ModuleEntriesProperty;
        /// <summary>
        ///   <para>模块条目上下文。</para>
        /// </summary>
        [NonSerialized] private readonly List<ModuleEntryContext> m_ModuleEntryContexts = new();

        public override bool showImportedObject => false;
        protected override bool needsApplyRevert => true;
        protected override bool useAssetDrawPreview => false;
        protected override Type extraDataType => typeof(GameModuleManifestEditorData);

        /// <summary>
        ///   <para>当前编辑条目是否存在待应用修改。</para>
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

        /// <inheritdoc />
        public override void OnEnable()
        {
            base.OnEnable();
            BindModuleEntryProperties();
            RefreshModuleEntryContexts();
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        /// <inheritdoc />
        public override void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            try { base.OnDisable(); }
            finally { ClearModuleEntryContexts(); }
        }

        /// <summary>
        ///   <para>重建撤销后的编辑器；避免继续使用旧托管引用的编辑状态。</para>
        /// </summary>
        private void OnUndoRedo()
        {
            BindModuleEntryProperties();
            RefreshModuleEntryContexts();
            Repaint();
        }

        /// <inheritdoc />
        protected override void OnHeaderGUI()
        {
            ApplyHeaderIcon();
            base.OnHeaderGUI();
        }

        /// <summary>
        ///   <para>应用表头图标。</para>
        /// </summary>
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override bool HasModified()
        {
            if (m_SkipModifiedCheckOnce)
            {
                m_SkipModifiedCheckOnce = false;
                return false;
            }

            return HasPendingChanges || base.HasModified();
        }

#if UNITY_2022_1_OR_NEWER
        /// <inheritdoc />
        public override void DiscardChanges()
        {
            m_SkipModifiedCheckOnce = false;
            base.DiscardChanges();
            if (!m_SkipModifiedCheckOnce)
            {
                FinishReset();
            }
        }
#endif

        /// <inheritdoc />
        [Obsolete]
        protected override void ResetValues()
        {
            base.ResetValues();
            FinishReset();
        }

        /// <inheritdoc />
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
        }

        /// <inheritdoc />
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

            DrawModules();
            var addModuleButtonRect = GUILayoutUtility.GetRect(
                Styles.AddModule,
                Styles.AddModuleButton,
                GUILayout.Height(k_ModuleActionButtonHeight));
            if (EditorGUI.DropdownButton(addModuleButtonRect, Styles.AddModule, FocusType.Keyboard, Styles.AddModuleButton))
            {
                ShowAddModulePopup(addModuleButtonRect);
            }

            DrawAdvancedOptions();
            extraDataSerializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }

        /// <summary>
        ///   <para>绘制高级选项。</para>
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
        ///   <para>绘制清单安装顺序选项。</para>
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
        ///   <para>绘制模块列表。</para>
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
                entryContext?.SetModule(GameModuleManifestEditorData.FindModuleProperty(entryProperty)?.managedReferenceValue as GameModule);
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
        ///   <para>绘制模块主体内容。</para>
        /// </summary>
        /// <param name="moduleIndex">模块索引。</param>
        /// <param name="entryProperty">条目属性。</param>
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
                    MessageType.Error
                );
            }

            DrawFields(moduleIndex, entryProperty, entryContext);
            DrawDependencies(entryContext);
        }

        /// <summary>
        ///   <para>绘制模块描述。</para>
        /// </summary>
        /// <param name="description">描述。</param>
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
        ///   <para>绘制模块字段编辑器（字段集合来源于模块可序列化字段）。</para>
        /// </summary>
        /// <param name="moduleIndex">模块索引。</param>
        /// <param name="entryProperty">条目属性。</param>
        /// <param name="entryContext">条目上下文。</param>
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

                using var moduleProperty = GameModuleManifestEditorData.FindModuleProperty(entryProperty);
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

                entryContext.Editor.OnInspectorGUI(moduleProperty);
                var changed = extraDataSerializedObject.hasModifiedProperties;
                extraDataSerializedObject.ApplyModifiedProperties();
                if (changed) SyncModuleEntry(moduleIndex, entryContext);
            }
        }

        /// <summary>
        ///   <para>绘制模块依赖项信息（依赖项集合直接来源于模块类型声明）。</para>
        /// </summary>
        /// <param name="entryContext">条目上下文。</param>
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

                Type[] dependencies;
                try
                {
                    dependencies = GameModuleDependencyUtility.GetDependencies(entryContext.ModuleType);
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

        /// <summary>
        ///   <para>绘制区域标题。</para>
        /// </summary>
        /// <param name="title">标题。</param>
        private static void DrawSectionTitle(GUIContent title)
        {
            using (new IndentLevelScope(0))
            {
                EditorGUILayout.LabelField(title, Styles.SectionTitle);
            }
        }

        /// <summary>
        ///   <para>缩进作用域。</para>
        /// </summary>
        private readonly struct IndentLevelScope : IDisposable
        {
            /// <summary>
            ///   <para>缩进级别。</para>
            /// </summary>
            private readonly int m_IndentLevel;

            /// <summary>
            ///   <para>创建缩进作用域。</para>
            /// </summary>
            /// <param name="indentLevel">缩进级别。</param>
            public IndentLevelScope(int indentLevel)
            {
                m_IndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = indentLevel;
            }

            /// <inheritdoc />
            public void Dispose() => EditorGUI.indentLevel = m_IndentLevel;
        }

        /// <summary>
        ///   <para>绘制依赖表。</para>
        /// </summary>
        /// <param name="dependencies">依赖。</param>
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
        ///   <para>模块右键菜单。</para>
        /// </summary>
        /// <param name="position">位置。</param>
        /// <param name="moduleIndex">模块索引。</param>
        private void OnModuleContextClick(Vector2 position, int moduleIndex)
        {
            var menu = new GenericMenu();
            menu.AddItem(Styles.ResetModule, false, () => ResetModuleFields(moduleIndex));
            menu.AddItem(Styles.RemoveModule, false, () => RemoveModule(moduleIndex));

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        /// <summary>
        ///   <para>重置模块字段为默认构造值。</para>
        /// </summary>
        /// <param name="index">索引。</param>
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
        ///   <para>显示可搜索的添加模块列表。</para>
        /// </summary>
        /// <param name="activatorRect">弹窗锚定区域。</param>
        private void ShowAddModulePopup(Rect activatorRect)
        {
            var moduleTypes = GetAddableModuleTypes();
            var duplicatedNames = new HashSet<string>(
                moduleTypes
                    .GroupBy(GetModuleMenuName, StringComparer.Ordinal)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key),
                StringComparer.Ordinal
            );

            var entries = new List<ModuleTypeListPopup.Entry>(moduleTypes.Count);
            for (int i = 0; i < moduleTypes.Count; i++)
            {
                var moduleType = moduleTypes[i];
                var displayName = GetModuleMenuName(moduleType);
                if (duplicatedNames.Contains(displayName))
                {
                    displayName = $"{displayName} ({moduleType.FullName})";
                }

                entries.Add(new ModuleTypeListPopup.Entry(
                    moduleType,
                    displayName,
                    GameModuleManifestEntryPropertyUtility.ContainsModuleType(m_ModuleEntriesProperty, moduleType)));
            }

            var popupAnchorRect = new Rect(activatorRect.xMin, activatorRect.yMax, activatorRect.width, 0f);
            PopupWindow.Show(popupAnchorRect, new ModuleTypeListPopup(entries, AddModule));
        }

        /// <summary>
        ///   <para>可搜索的模块类型选择弹窗。</para>
        /// </summary>
        private sealed class ModuleTypeListPopup : PopupWindowContent
        {
            /// <summary>
            ///   <para>最小宽度。</para>
            /// </summary>
            private const float MinWidth = 300f;
            /// <summary>
            ///   <para>最大宽度。</para>
            /// </summary>
            private const float MaxWidth = 520f;
            /// <summary>
            ///   <para>最大高度。</para>
            /// </summary>
            private const float MaxHeight = 480f;
            /// <summary>
            ///   <para>表头高度。</para>
            /// </summary>
            private const float HeaderHeight = 48f;
            /// <summary>
            ///   <para>空列表高度。</para>
            /// </summary>
            private const float EmptyListHeight = 112f;
            /// <summary>
            ///   <para>条目高度。</para>
            /// </summary>
            private const float EntryHeight = 42f;
            /// <summary>
            ///   <para>搜索控件名称。</para>
            /// </summary>
            private const string SearchControlName = "VerveModuleTypeListSearch";

            /// <summary>
            ///   <para>主要标签样式。</para>
            /// </summary>
            private static readonly GUIStyle PrimaryLabelStyle = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                fontStyle = FontStyle.Bold
            };

            /// <summary>
            ///   <para>次要标签样式。</para>
            /// </summary>
            private static readonly GUIStyle SecondaryLabelStyle = new(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };

            /// <summary>
            ///   <para>条目。</para>
            /// </summary>
            private readonly IReadOnlyList<Entry> entries;
            /// <summary>
            ///   <para>选中回调。</para>
            /// </summary>
            private readonly Action<Type> onSelected;
            /// <summary>
            ///   <para>滚动位置。</para>
            /// </summary>
            private Vector2 scrollPosition;
            /// <summary>
            ///   <para>搜索文本。</para>
            /// </summary>
            private string searchText = string.Empty;
            /// <summary>
            ///   <para>搜索框焦点状态。</para>
            /// </summary>
            private bool searchFocused;

            /// <summary>
            ///   <para>创建模块类型选择弹窗。</para>
            /// </summary>
            /// <param name="entries">条目。</param>
            /// <param name="onSelected">选中回调。</param>
            public ModuleTypeListPopup(IReadOnlyList<Entry> entries, Action<Type> onSelected)
            {
                this.entries = entries ?? Array.Empty<Entry>();
                this.onSelected = onSelected;
            }

            /// <inheritdoc />
            public override Vector2 GetWindowSize()
            {
                float width = MinWidth;
                for (int i = 0; i < entries.Count; i++)
                {
                    width = Mathf.Max(width,
                        EditorStyles.label.CalcSize(new GUIContent(entries[i].DisplayName)).x + 88f);
                }

                float height = entries.Count == 0
                    ? EmptyListHeight
                    : HeaderHeight + entries.Count * EntryHeight;
                return new Vector2(Mathf.Min(width, MaxWidth), Mathf.Min(height, MaxHeight));
            }

            /// <inheritdoc />
            public override void OnGUI(Rect rect)
            {
                EditorGUILayout.Space(2f);
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUI.SetNextControlName(SearchControlName);
                    EditorGUI.BeginChangeCheck();
                    string nextSearchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
                    if (EditorGUI.EndChangeCheck())
                    {
                        searchText = nextSearchText;
                        scrollPosition = Vector2.zero;
                    }
                }

                if (!searchFocused && Event.current.type == EventType.Repaint)
                {
                    EditorGUI.FocusTextInControl(SearchControlName);
                    searchFocused = true;
                }

                int matchedCount = 0;
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    if (!entry.Matches(searchText))
                    {
                        continue;
                    }

                    DrawEntry(entry, matchedCount++);
                }
                EditorGUILayout.EndScrollView();

                if (matchedCount == 0)
                {
                    EditorGUILayout.HelpBox(entries.Count == 0
                        ? Styles.NoAvailableModules.text
                        : "No modules match the current search.", MessageType.Info);
                }
            }

            /// <summary>
            ///   <para>绘制条目。</para>
            /// </summary>
            /// <param name="entry">条目。</param>
            /// <param name="index">索引。</param>
            private void DrawEntry(Entry entry, int index)
            {
                Rect rowRect = GUILayoutUtility.GetRect(0f, EntryHeight, GUILayout.ExpandWidth(true));
                rowRect.xMin += 4f;
                rowRect.xMax -= 4f;

                Event currentEvent = Event.current;
                bool isHovering = rowRect.Contains(currentEvent.mousePosition);
                bool isSelectable = !entry.IsAdded;
                if (currentEvent.type == EventType.Repaint)
                {
                    Color background = GetRowBackground(index, isHovering, isSelectable);
                    EditorGUI.DrawRect(rowRect, background);
                    EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.yMax - 1f, rowRect.width, 1f),
                        GetSeparatorColor());
                }

                Rect primaryRect = new Rect(rowRect.x + 8f, rowRect.y + 3f, rowRect.width - 16f, 18f);
                Rect secondaryRect = new Rect(rowRect.x + 8f, rowRect.y + 20f, rowRect.width - 16f, 18f);
                Color previousContentColor = GUI.contentColor;
                using (new EditorGUI.DisabledScope(!isSelectable))
                {
                    GUI.contentColor = GetTextColor(isHovering, isSelectable, false);
                    GUI.Label(primaryRect, new GUIContent(entry.DisplayName, entry.Tooltip), PrimaryLabelStyle);
                    GUI.contentColor = GetTextColor(isHovering, isSelectable, true);
                    GUI.Label(secondaryRect, entry.Type.FullName, SecondaryLabelStyle);
                }
                GUI.contentColor = previousContentColor;

                if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && isSelectable && isHovering)
                {
                    editorWindow.Close();
                    currentEvent.Use();
                    onSelected?.Invoke(entry.Type);
                    GUIUtility.ExitGUI();
                }
            }

            /// <summary>
            ///   <para>获取行背景。</para>
            /// </summary>
            /// <param name="index">索引。</param>
            /// <param name="isHovering">是否悬停。</param>
            /// <param name="isSelectable">是否可选择。</param>
            private static Color GetRowBackground(int index, bool isHovering, bool isSelectable)
            {
                if (EditorGUIUtility.isProSkin)
                {
                    if (isHovering && isSelectable)
                    {
                        return new Color(0.20f, 0.40f, 0.66f, 1f);
                    }

                    float shade = index % 2 == 0 ? 0.22f : 0.24f;
                    return isSelectable
                        ? new Color(shade, shade, shade, 1f)
                        : new Color(shade + 0.015f, shade + 0.015f, shade + 0.015f, 1f);
                }

                if (isHovering && isSelectable)
                {
                    return new Color(0.25f, 0.49f, 0.78f, 1f);
                }

                float lightShade = index % 2 == 0 ? 0.94f : 0.91f;
                return isSelectable
                    ? new Color(lightShade, lightShade, lightShade, 1f)
                    : new Color(lightShade - 0.025f, lightShade - 0.025f, lightShade - 0.025f, 1f);
            }

            /// <summary>
            ///   <para>获取分隔符颜色。</para>
            /// </summary>
            private static Color GetSeparatorColor()
            {
                return EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.10f)
                    : new Color(0f, 0f, 0f, 0.12f);
            }

            /// <summary>
            ///   <para>获取文本颜色。</para>
            /// </summary>
            /// <param name="isHovering">是否悬停。</param>
            /// <param name="isSelectable">是否可选择。</param>
            /// <param name="isSecondary">是否次要。</param>
            private static Color GetTextColor(bool isHovering, bool isSelectable, bool isSecondary)
            {
                if (isHovering && isSelectable)
                {
                    return isSecondary
                        ? new Color(0.88f, 0.93f, 1f, 1f)
                        : Color.white;
                }

                if (EditorGUIUtility.isProSkin)
                {
                    if (!isSelectable)
                    {
                        return isSecondary
                            ? new Color(0.44f, 0.44f, 0.44f, 1f)
                            : new Color(0.56f, 0.56f, 0.56f, 1f);
                    }

                    return isSecondary
                        ? new Color(0.68f, 0.68f, 0.68f, 1f)
                        : new Color(0.92f, 0.92f, 0.92f, 1f);
                }

                if (!isSelectable)
                {
                    return isSecondary
                        ? new Color(0.46f, 0.46f, 0.46f, 1f)
                        : new Color(0.36f, 0.36f, 0.36f, 1f);
                }

                return isSecondary
                    ? new Color(0.30f, 0.30f, 0.30f, 1f)
                    : new Color(0.12f, 0.12f, 0.12f, 1f);
            }

            /// <summary>
            ///   <para>条目。</para>
            /// </summary>
            internal sealed class Entry
            {
                /// <summary>
                ///   <para>创建条目。</para>
                /// </summary>
                /// <param name="type">类型。</param>
                /// <param name="displayName">显示名称。</param>
                /// <param name="isAdded">是否已添加。</param>
                public Entry(Type type, string displayName, bool isAdded)
                {
                    Type = type ?? throw new ArgumentNullException(nameof(type));
                    DisplayName = string.IsNullOrWhiteSpace(displayName) ? type.Name : displayName;
                    IsAdded = isAdded;
                    string description = GetModuleDescription(type);
                    SearchText = $"{DisplayName}\n{type.FullName}\n{description}";
                    Tooltip = $"{description}";
                }

                /// <summary>
                ///   <para>类型。</para>
                /// </summary>
                public Type Type { get; }
                /// <summary>
                ///   <para>显示名称。</para>
                /// </summary>
                public string DisplayName { get; }
                /// <summary>
                ///   <para>是否已添加。</para>
                /// </summary>
                public bool IsAdded { get; }
                /// <summary>
                ///   <para>搜索文本。</para>
                /// </summary>
                private string SearchText { get; }
                /// <summary>
                ///   <para>提示。</para>
                /// </summary>
                public string Tooltip { get; }

                /// <summary>
                ///   <para>匹配。</para>
                /// </summary>
                /// <param name="search">搜索。</param>
                public bool Matches(string search)
                {
                    return string.IsNullOrWhiteSpace(search) ||
                        SearchText.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
        }

        /// <summary>
        ///   <para>添加模块。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
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
        ///   <para>向编辑条目列表追加一个模块条目。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
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
        ///   <para>计算添加模块前需要补充的缺失依赖。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="addableModuleTypes">可添加模块类型。</param>
        /// <param name="error">错误。</param>
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
        ///   <para>递归追加缺失依赖，返回依赖优先的追加顺序。</para>
        /// </summary>
        /// <param name="rootType">根对象类型。</param>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="availableModuleTypes">可用模块类型。</param>
        /// <param name="plannedTypes">计划的类型。</param>
        /// <param name="missingDependencyTypes">缺失依赖类型。</param>
        /// <param name="visitingTypes">当前访问链中的类型。</param>
        /// <param name="error">错误。</param>
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
                Type[] dependencyTypes;
                try
                {
                    dependencyTypes = GameModuleDependencyUtility.GetDependencies(moduleType);
                }
                catch (Exception ex)
                {
                    error =
                        $"Failed to inspect module dependencies before adding '{GetModuleMenuName(rootType)}'. " +
                        $"module={GameModuleUtility.GetTypeDisplayName(moduleType)}, error={ex.Message}";
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
        ///   <para>判断某个依赖是否已被清单或本次追加计划纳入。</para>
        /// </summary>
        /// <param name="dependencyType">依赖类型。</param>
        /// <param name="plannedTypes">计划的类型。</param>
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
        ///   <para>查找依赖应该追加的具体模块类型。</para>
        /// </summary>
        /// <param name="rootType">根对象类型。</param>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="dependencyType">依赖类型。</param>
        /// <param name="availableModuleTypes">可用模块类型。</param>
        /// <param name="dependencyModuleType">依赖模块类型。</param>
        /// <param name="error">错误。</param>
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
        ///   <para>确认是否把缺失依赖一并追加到清单。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="dependencyTypes">依赖类型。</param>
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
        ///   <para>移除模块。</para>
        /// </summary>
        /// <param name="index">索引。</param>
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
        ///   <para>确认移除模块；移除会破坏依赖时给出明确提示。</para>
        /// </summary>
        /// <param name="index">索引。</param>
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
        ///   <para>重新从清单文件加载编辑数据。</para>
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
        ///   <para>完成一次 Revert/Reset 之后的本地状态重载与同步。</para>
        /// </summary>
        private void FinishReset()
        {
            ReloadFromSource();
            serializedObject?.Update();
            HasPendingChanges = false;
            m_SkipModifiedCheckOnce = true;
        }

        /// <summary>
        ///   <para>按资源路径加载清单编辑数据。</para>
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
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
        ///   <para>绑定额外数据序列化属性。</para>
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
        ///   <para>刷新模块条目上下文。</para>
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
        ///   <para>清空模块条目上下文。</para>
        /// </summary>
        private void ClearModuleEntryContexts() => ResourceUtility.ReleaseAll(m_ModuleEntryContexts, context => context?.Dispose());

        /// <summary>
        ///   <para>同步单个模块条目编辑结果到清单编辑数据。</para>
        /// </summary>
        /// <param name="moduleIndex">模块索引。</param>
        /// <param name="entryContext">条目上下文。</param>
        private void SyncModuleEntry(int moduleIndex, ModuleEntryContext entryContext)
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
                HasPendingChanges = true;
            }
            catch (Exception ex)
            {
                entryContext.Error = ex.Message;
            }
        }

        /// <summary>
        ///   <para>同步全部模块条目编辑结果到清单编辑数据。</para>
        /// </summary>
        private void SyncAllModuleEntries()
        {
            for (int i = 0; i < m_ModuleEntryContexts.Count; i++)
            {
                SyncModuleEntry(i, m_ModuleEntryContexts[i]);
            }
        }

        /// <summary>
        ///   <para>按条目声明类型重新创建模块实例，避免意外修改 <see cref="SerializeReference"/> 的具体类型。</para>
        /// </summary>
        /// <param name="entryProperty">条目属性。</param>
        /// <param name="declaredType">已声明类型。</param>
        private static GameModule RecreateModuleFromDeclaredType(SerializedProperty entryProperty, Type declaredType)
        {
            var fields = GameModuleManifestEditorData.ReadModuleEntry(entryProperty).fields;
            var module = GameModuleManifestAsset.CreateModuleInstance(declaredType, fields);
            GameModuleManifestEditorData.FindModuleProperty(entryProperty).managedReferenceValue = module;
            return module;
        }

        /// <summary>
        ///   <para>判断模块是否命中搜索筛选。</para>
        /// </summary>
        /// <param name="property">属性。</param>
        /// <param name="entryContext">条目上下文。</param>
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
        ///   <para>获取模块显示名称。</para>
        /// </summary>
        /// <param name="property">属性。</param>
        /// <param name="entryContext">条目上下文。</param>
        private static string GetModuleDisplayName(SerializedProperty property, ModuleEntryContext entryContext)
        {
            if (entryContext?.ModuleType != null)
            {
                return GetModuleMenuName(entryContext.ModuleType);
            }

            return GetUnresolvedModuleName(GameModuleManifestEditorData.ReadModuleType(property));
        }

        /// <summary>
        ///   <para>获取可添加模块类型列表。</para>
        /// </summary>
        private static List<Type> GetAddableModuleTypes()
        {
            return TypeCache.GetTypesDerivedFrom<GameModule>()
                .Where(CanAddModuleType)
                .OrderBy(GetModuleMenuName, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        ///   <para>获取模块菜单显示名称。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
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
        ///   <para>获取模块描述文本。</para>
        /// </summary>
        /// <param name="entryContext">条目上下文。</param>
        private static string GetModuleDescription(ModuleEntryContext entryContext) => entryContext?.ModuleType == null ? null : GetModuleDescription(entryContext.ModuleType);

        /// <summary>
        ///   <para>获取模块描述。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        private static string GetModuleDescription(Type moduleType)
        {
            if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));
            return moduleType.GetCustomAttribute<GameModuleAttribute>()?.description ?? string.Empty;
        }

        /// <summary>
        ///   <para>判断模块类型是否可以直接追加到清单。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        private static bool CanAddModuleType(Type moduleType)
        {
            return moduleType != null && moduleType.GetCustomAttribute<GameModuleAttribute>() != null && GameModuleManifestAsset.GetManifestModuleTypeError(moduleType) == null;
        }

        /// <summary>
        ///   <para>获取尚未解析类型的显示名。</para>
        /// </summary>
        /// <param name="typeName">类型显示名称。</param>
        private static string GetUnresolvedModuleName(string typeName)
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

        /// <summary>
        ///   <para>获取模块显示名称。</para>
        /// </summary>
        /// <param name="index">索引。</param>
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

        /// <summary>
        ///   <para>应用模块条目变更。</para>
        /// </summary>
        /// <param name="change">变更。</param>
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
        ///   <para>获取当前清单资源路径。</para>
        /// </summary>
        /// <param name="targetIndex">目标索引。</param>
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