#if UNITY_EDITOR

namespace Verve.Editor
{
    using Verve;
    using System;
    using UnityEditor;
    using UnityEngine;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;

    
    /// <summary>
    ///   <para>游戏功能模块配置文件编辑器</para>
    /// </summary>
    [CustomEditor(typeof(GameFeatureModuleProfile), true)]
    internal class GameFeatureModuleProfileEditor : Editor
    {
        private GameFeatureModuleProfile m_Profile;
        private SerializedProperty m_ModulesProperty;
        private readonly List<ModuleEditor> m_Editors = new List<ModuleEditor>();
        private bool m_NeedsRefresh;

        
        /// <summary>
        ///   <para>样式</para>
        /// </summary>
        private static class Styles
        {
            /// <summary>
            ///   <para>无模块信息</para>
            /// </summary>
            public static string NoModulesInfo { get; } = L10n.Tr("No modules added to this data.");
            
            /// <summary>
            ///   <para>模块显示文本</para>
            /// </summary>
            public static GUIContent ModulesText { get; } = EditorGUIUtility.TrTextContent("Modules");
            
            /// <summary>
            ///   <para>添加模块</para>
            /// </summary>
            public static GUIContent AddModule { get; } = EditorGUIUtility.TrTextContent("Add Game Feature Module", "Adds a new module to this profile.");
            
            /// <summary>
            ///   <para>删除模块</para>
            /// </summary>
            public static GUIContent RemoveModule { get; } = EditorGUIUtility.TrTextContent("Remove");
            
            /// <summary>
            ///   <para>模块设置</para>
            /// </summary>
            public static GUIContent ModuleSetting { get; } = EditorGUIUtility.TrTextContent("Setting...");
        }

        
        public void OnEnable()
        {
            m_Profile = target as GameFeatureModuleProfile;
            if (m_Profile == null || target == null) return;

            m_ModulesProperty = serializedObject.FindProperty("m_Modules");
            RefreshEditors();
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            ClearEditors();
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        public override void OnInspectorGUI()
        {
            if (m_Profile == null || target == null) return;
            
            using var change = new EditorGUI.ChangeCheckScope();

            serializedObject.Update();
    
            if (m_NeedsRefresh || m_Editors.Count != m_ModulesProperty.arraySize)
            {
                RefreshEditors();
                m_NeedsRefresh = false;
            }

            DrawModulesList();
            EditorGUILayout.Space();
            if (GUILayout.Button(Styles.AddModule, EditorStyles.miniButton))
            {
                ShowAddModuleMenu();
            }

            if (change.changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
        
        private void OnUndoRedoPerformed()
        {
            m_NeedsRefresh = true;
            Repaint();
        }

        /// <summary>
        ///   <para>绘制模块列表</para>
        /// </summary>
        private void DrawModulesList()
        {
            EditorGUILayout.LabelField(Styles.ModulesText, EditorStyles.boldLabel);

            CleanNullElements();
            
            if (m_ModulesProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox(Styles.NoModulesInfo, MessageType.Info);
                return;
            }
            
            if (m_Editors.Count != m_ModulesProperty.arraySize)
            {
                RefreshEditors();
                return;
            }

            for (int i = 0; i < m_ModulesProperty.arraySize; i++)
            {
                var moduleProperty = m_ModulesProperty.GetArrayElementAtIndex(i);
                if (moduleProperty == null || moduleProperty.objectReferenceValue == null)
                {
                    continue;
                }

                if (i >= m_Editors.Count || m_Editors[i] == null || m_Editors[i].Target == null)
                {
                    RefreshEditors();
                    break;
                }

                var editor = m_Editors[i];
                
                CoreEditorUtility.DrawSplitter();
                bool displayContent = CoreEditorUtility.DrawHeaderToggle(
                    editor.GetDisplayTitle(),
                    moduleProperty,
                    null,
                    position => OnContextClick((Vector2)position, i),
                    editor.Target.GetType().Assembly.GetName().Name.Equals("Assembly-CSharp") ? "" : editor.Target.GetType().Assembly.GetName().Version.ToString()
                );

                if (displayContent)
                {
                    using (new EditorGUI.DisabledScope(!editor.Target.IsActive))
                    {
                        editor.OnInspectorGUI();
                    }
                }
            }

            if (m_ModulesProperty.arraySize > 0)
                CoreEditorUtility.DrawSplitter();
        }

        /// <summary>
        ///   <para>清理空元素</para>
        /// </summary>
        private void CleanNullElements()
        {
            if (m_ModulesProperty == null) return;
            
            bool hasNulls = false;
            
            for (int i = m_ModulesProperty.arraySize - 1; i >= 0; i--)
            {
                var element = m_ModulesProperty.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == null)
                {
                    hasNulls = true;
                    break;
                }
            }
            
            if (hasNulls)
            {
                serializedObject.Update();
                
                for (int i = m_ModulesProperty.arraySize - 1; i >= 0; i--)
                {
                    var element = m_ModulesProperty.GetArrayElementAtIndex(i);
                    if (element.objectReferenceValue == null)
                    {
                        m_ModulesProperty.DeleteArrayElementAtIndex(i);
                    }
                }
                
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }

        /// <summary>
        ///   <para>菜单点击事件</para>
        /// </summary>
        private void OnContextClick(Vector2 position, int index)
        {
            var menu = new GenericMenu();
            
            var dependentModules = GetDependentModules(index);
            bool isRequiredByOtherModules = dependentModules.Count > 0;
            
            if (isRequiredByOtherModules)
            {
                const int maxDisplayDependencies = 2;
                string dependentInfo;
                
                if (dependentModules.Count <= maxDisplayDependencies)
                {
                    string dependentNames = string.Join(", ", dependentModules.Select(m => m.GetType().Name));
                    dependentInfo = $"(Required by: {dependentNames})";
                }
                else
                {
                    var firstFewNames = dependentModules.Take(maxDisplayDependencies).Select(m => m.GetType().Name);
                    string displayedNames = string.Join(", ", firstFewNames);
                    dependentInfo = $"(Required by: {displayedNames} and {dependentModules.Count - maxDisplayDependencies} more)";
                }
                
                menu.AddDisabledItem(new GUIContent($"{Styles.RemoveModule.text} {dependentInfo}"));
            }
            else
            {
                menu.AddItem(Styles.RemoveModule, false, () => RemoveModule(index));
            }
            
            menu.AddSeparator("");

            var moduleType = m_ModulesProperty.GetArrayElementAtIndex(index)?.objectReferenceValue?.GetType();
            if (GameFeaturesSettings.instance.TryGetModuleEditorFromModule(moduleType, out var editor))
            {
                menu.AddItem(Styles.ModuleSetting, false, () => ModuleSettingWindow.Show(editor));
            }
            else
            {
                menu.AddDisabledItem(Styles.ModuleSetting);
            }
            
            menu.DropDown(new Rect(position, Vector2.zero));
        }

        /// <summary>
        ///   <para>获取模块的依赖模块</para>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private List<GameFeatureModule> GetDependentModules(int index)
        {
            var dependentModules = new List<GameFeatureModule>();
            
            if (index < 0 || index >= m_ModulesProperty.arraySize) return dependentModules;

            var moduleProperty = m_ModulesProperty.GetArrayElementAtIndex(index);
            var module = moduleProperty?.objectReferenceValue as GameFeatureModule;

            if (module == null) return dependentModules;

            var moduleType = module.GetType();

            for (int i = 0; i < m_ModulesProperty.arraySize; i++)
            {
                if (i == index) continue;
                var otherModuleProperty = m_ModulesProperty.GetArrayElementAtIndex(i);
                var otherModule = otherModuleProperty?.objectReferenceValue as GameFeatureModule;

                if (otherModule == null) continue;

                var dependencies = m_Profile.GetDependencies(otherModule.GetType());
                if (dependencies.Contains(moduleType))
                {
                    dependentModules.Add(otherModule);
                }
            }

            return dependentModules;
        }

        /// <summary>
        ///   <para>显示添加模块菜单</para>
        /// </summary>
        private void ShowAddModuleMenu()
        {
            var menu = new GenericMenu();

            var moduleTypes = TypeCache.GetTypesDerivedFrom<GameFeatureModule>()
                .Where(t => !t.IsAbstract && t.IsClass);

            foreach (var type in moduleTypes)
            {
                var menuPath = CoreEditorUtility.GetGameFeatureMenuPath(type);
                if (string.IsNullOrEmpty(menuPath)) continue;
                bool alreadyExists = m_Profile.Has(type);

                if (alreadyExists)
                {
                    menu.AddDisabledItem(new GUIContent($"{menuPath} (Already Added)"));
                }
                else
                {
                    menu.AddItem(new GUIContent(menuPath), false, () => AddModule(type));
                }
            }

            menu.ShowAsContext();
        }

        /// <summary>
        ///   <para>添加模块的子模块</para>
        /// </summary>
        private void AddModule(Type moduleType)
        {
            if (moduleType == null) return;

            try
            {
                Undo.RecordObject(m_Profile, Styles.AddModule.text);
                
                var module = CreateInstance(moduleType) as GameFeatureModule;
                if (module == null) return;
                
                module.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                module.name = ObjectNames.NicifyVariableName(moduleType.Name);
                
                m_Profile.Add(module);
                
                if (EditorUtility.IsPersistent(m_Profile))
                {
                    AssetDatabase.AddObjectToAsset(module, m_Profile);
                    EditorUtility.SetDirty(m_Profile);
                }
                
                m_Profile.isDirty = true;
                
                serializedObject.Update();
                m_NeedsRefresh = true;
                Repaint();
                
                CoreEditorUtility.MarkDirtyAndSave(m_Profile);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to add module: {e.Message}", "OK");
            }
        }
        
        /// <summary>
        ///   <para>移除模块</para>
        /// </summary>
        private void RemoveModule(int index)
        {
            if (index < 0 || index >= m_ModulesProperty.arraySize) return;

            serializedObject.Update();

            var moduleProperty = m_ModulesProperty.GetArrayElementAtIndex(index);
            var module = moduleProperty?.objectReferenceValue as GameFeatureModule;

            if (module != null)
            {
                try
                {
                    Undo.RecordObject(m_Profile, Styles.RemoveModule.text);
                    if (m_Profile.Remove(module))
                    {
                        m_ModulesProperty.DeleteArrayElementAtIndex(index);

                        if (EditorUtility.IsPersistent(module) && AssetDatabase.Contains(module))
                        {
                            AssetDatabase.RemoveObjectFromAsset(module);
                            DestroyImmediate(module, true);
                            EditorUtility.UnloadUnusedAssetsImmediate();
                        }

                        AssetDatabase.SaveAssets();
                        EditorUtility.SetDirty(m_Profile);
                        m_NeedsRefresh = true;
                        Repaint();
                    }
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to remove module: {e.Message}", "OK");
                }
            }
        }

        /// <summary>
        ///   <para>刷新编辑器</para>
        /// </summary>
        private void RefreshEditors()
        {
            ClearEditors();

            if (m_ModulesProperty == null) 
            {
                serializedObject.Update();
            }

            CleanNullElements();
            
            for (int i = 0; i < m_ModulesProperty.arraySize; i++)
            {
                var moduleProperty = m_ModulesProperty.GetArrayElementAtIndex(i);
                var module = moduleProperty?.objectReferenceValue as GameFeatureModule;

                if (module != null)
                {
                    var editor = new ModuleEditor();
                    editor.Init(module, this);
                    m_Editors.Add(editor);
                }
            }
        }

        /// <summary>
        ///   <para>清理编辑器</para>
        /// </summary>
        private void ClearEditors()
        {
            foreach (var editor in m_Editors)
            {
                editor?.OnDisable();
            }
            m_Editors.Clear();
        }

        
        /// <summary>
        ///   <para>模块编辑器</para>
        /// </summary>
        internal class ModuleEditor
        {
            private Editor m_Editor;
            private SerializedObject m_SerializedObject;

            public GameFeatureModule Target { get; private set; }
            public SerializedProperty ActiveProperty { get; private set; }

            
            public void Init(GameFeatureModule target, GameFeatureModuleProfileEditor parentEditor)
            {
                Target = target;
                
                if (target != null)
                {
                    m_SerializedObject = new SerializedObject(target);
                    ActiveProperty = m_SerializedObject.FindProperty("m_IsActive");
                    m_Editor = Editor.CreateEditor(target);
                }
            }

            public void OnDisable()
            {
                if (m_Editor != null)
                {
                    DestroyImmediate(m_Editor);
                    m_Editor = null;
                }
                
                m_SerializedObject?.Dispose();
                m_SerializedObject = null;
            }

            public void OnInspectorGUI()
            {
                if (Target == null || m_SerializedObject == null || m_Editor == null) return;

                m_SerializedObject.Update();
                
                m_Editor.OnInspectorGUI();
                
                m_SerializedObject.ApplyModifiedProperties();
            }

            public GUIContent GetDisplayTitle()
            {
                if (Target == null)
                    return new GUIContent("Unknown Module");
            
                if (Target.GetType() == typeof(GameFeatureModule))
                {
                    return new GUIContent(Target.name);
                }
                
                string typeName = ObjectNames.NicifyVariableName(Target.GetType().Name);
                
                var attr = Target.GetType().GetCustomAttribute<GameFeatureAttribute>();
                if (attr != null && !string.IsNullOrEmpty(attr.MenuPath))
                {
                    if (attr.MenuPath.EndsWith("/"))
                    {
                        return new GUIContent(typeName, attr.Description);
                    }
                    
                    if (attr.MenuPath.Contains("/"))
                    {
                        string[] pathParts = attr.MenuPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                        if (pathParts.Length > 0)
                        {
                            return new GUIContent($"{pathParts[pathParts.Length - 1]} ({Target.name})", attr.Description);
                        }
                    }
                    else
                    {
                        return new GUIContent($"{attr.MenuPath} ({Target.name})", attr.Description);
                    }
                }
                
                if (Target.Submodules != null && Target.Submodules.Count > 0)
                {
                    var firstSubmodule = Target.Submodules.First();
                    var submoduleAttr = firstSubmodule.GetType().GetCustomAttribute<GameFeatureSubmoduleAttribute>();
                    
                    if (submoduleAttr != null && !string.IsNullOrEmpty(submoduleAttr.MenuPath))
                    {
                        if (submoduleAttr.MenuPath.Contains("/"))
                        {
                            string[] pathParts = submoduleAttr.MenuPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                            if (pathParts.Length > 0)
                            {
                                return new GUIContent(pathParts[pathParts.Length - 1], submoduleAttr.Description);
                            }
                        }
                        else
                        {
                            return new GUIContent(submoduleAttr.MenuPath, submoduleAttr.Description);
                        }
                    }
                }
                
                return new GUIContent(typeName);
            }
        }

        
        /// <summary>
        ///   <para>模块设置窗口</para>
        /// </summary>
        private class ModuleSettingWindow : EditorWindow
        {
            /// <summary>
            ///   <para>模块编辑器绘制器</para>
            /// </summary>
            [SerializeField]
#if UNITY_2019_4_OR_NEWER
            [SerializeReference]
#endif
            private GameFeatureModuleSettingDrawer m_Drawer;
            
            /// <summary>
            ///   <para>工具栏高度</para>
            /// </summary>
            private const float ToolbarHeight = 24f;
            
            /// <summary>
            ///   <para>显示模块设置窗口</para>
            /// </summary>
            /// <param name="drawer">模块编辑器绘制器</param>
            public static void Show(GameFeatureModuleSettingDrawer drawer)
            {
                var window = GetWindow<ModuleSettingWindow>();
                window.titleContent = new GUIContent("Module Settings");
                window.minSize = new Vector2(400, 300);
                window.m_Drawer = drawer;
                window.ShowUtility();
                window.Focus();
            }

            /// <summary>
            ///   <para>绘制窗口GUI</para>
            /// </summary>
            private void OnGUI()
            {
                DrawToolbar();
                
                if (m_Drawer != null && m_Drawer.GetType() != typeof(GameFeatureModuleEditor))
                {
                    Rect contentRect = new Rect(
                        0, 
                        ToolbarHeight, 
                        position.width, 
                        position.height - ToolbarHeight
                    );
                    
                    GUILayout.BeginArea(contentRect);
                    
                    m_Drawer.OnGUI();
                    GUILayout.EndArea();
                }
                else
                {
                    Rect contentRect = new Rect(
                        0, 
                        ToolbarHeight, 
                        position.width, 
                        position.height - ToolbarHeight
                    );
                    
                    GUILayout.BeginArea(contentRect);
                    EditorGUILayout.HelpBox("No module editor drawer available.", MessageType.Warning);
                    GUILayout.EndArea();
                }
            }
            
            /// <summary>
            ///   <para>绘制工具栏</para>
            /// </summary>
            private void DrawToolbar()
            {
                Rect toolbarRect = new Rect(0, 0, position.width, ToolbarHeight);
                GUI.Box(toolbarRect, GUIContent.none, EditorStyles.toolbar);

                using (new GUILayout.HorizontalScope())
                {
                    // TODO: 工具栏按钮
                }
            }
        }
    }
}

#endif