#if UNITY_EDITOR

namespace VerveEditor.UniEx
{
    using Verve;
    using System;
    using Verve.UniEx;
    using UnityEditor;
    using UnityEngine;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;

    
    [CustomEditor(typeof(GameFeatureModuleProfile), true)]
    internal class GameFeatureModuleProfileEditor : Editor
    {
        private GameFeatureModuleProfile m_Profile;
        private SerializedProperty m_ModulesProperty;
        private readonly List<ModuleEditor> m_Editors = new List<ModuleEditor>();
        private bool m_NeedsRefresh;

        
        private static class Styles
        {
            public static GUIContent ModulesText { get; } = EditorGUIUtility.TrTextContent("Modules");
            public static GUIContent RemoveModule { get; } = EditorGUIUtility.TrTextContent("Remove");
            public static GUIContent AddModule { get; } = EditorGUIUtility.TrTextContent("Add Game Feature Module");
            public static GUIContent NoModulesInfo { get; } = EditorGUIUtility.TrTextContent("No modules added to this data.");
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

        private void OnUndoRedoPerformed()
        {
            m_NeedsRefresh = true;
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            if (m_Profile == null || target == null) return;
            
            serializedObject.Update();
    
            if (m_NeedsRefresh || m_Editors.Count != m_ModulesProperty.arraySize)
            {
                RefreshEditors();
                m_NeedsRefresh = false;
            }

            DrawModulesList();
            DrawAddModuleButton();

            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawModulesList()
        {
            EditorGUILayout.LabelField(Styles.ModulesText, EditorStyles.boldLabel);

            CleanNullElements();
            
            if (m_ModulesProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox(Styles.NoModulesInfo.text, MessageType.Info);
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
                
                GUIContent disabledItem = new GUIContent($"{Styles.RemoveModule.text} {dependentInfo}");
                menu.AddDisabledItem(disabledItem);
            }
            else
            {
                menu.AddItem(Styles.RemoveModule, false, () => RemoveModule(index));
            }
            
            menu.DropDown(new Rect(position, Vector2.zero));
        }

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

        private void DrawAddModuleButton()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button(Styles.AddModule, EditorStyles.miniButton))
            {
                ShowAddModuleMenu();
            }
        }

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

            var allSubmoduleTypes = TypeCache.GetTypesDerivedFrom<IGameFeatureSubmodule>()
                .Where(t => !t.IsAbstract && t.IsClass && 
                            t.GetCustomAttribute<GameFeatureSubmoduleAttribute>() != null &&
                            t.GetCustomAttribute<GameFeatureSubmoduleAttribute>().BelongsToModule == null);

            var submoduleGroups = allSubmoduleTypes
                .GroupBy(t => t.GetCustomAttribute<GameFeatureSubmoduleAttribute>().MenuPath)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var group in submoduleGroups)
            {
                string menuPath = group.Key;
    
                bool alreadyExists = m_Profile.Modules.Any(m => 
                {
                    if (m == null) return false;
        
                    var moduleMenuPath = CoreEditorUtility.GetGameFeatureMenuPath(m.GetType());
                    if (!string.IsNullOrEmpty(moduleMenuPath) && moduleMenuPath == menuPath)
                        return true;
        
                    if (m.Submodules != null && m.Submodules.Count > 0)
                    {
                        var firstSubmodule = m.Submodules.First();
                        var submoduleAttr = firstSubmodule.GetType().GetCustomAttribute<GameFeatureSubmoduleAttribute>();
                        if (submoduleAttr != null && submoduleAttr.MenuPath == menuPath)
                            return true;
                    }
        
                    return false;
                });
    
                if (alreadyExists)
                {
                    menu.AddDisabledItem(new GUIContent($"{menuPath} (Already Added)"));
                }
                else
                {
                    menu.AddItem(new GUIContent(menuPath), false, () => 
                        AddModuleFromSubmodules(menuPath, group.Value));
                }
            }

            menu.ShowAsContext();
        }

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
                
                m_Profile.IsDirty = true;
                
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

        private void AddModuleFromSubmodules(string menuPath, List<Type> submoduleTypes)
        {
            if (submoduleTypes == null || submoduleTypes.Count == 0) return;

            try
            {
                Undo.RecordObject(m_Profile, "Add Module from Submodules");
        
                var module = CreateInstance<GameFeatureModule>();
        
                var lastSlashIndex = menuPath.LastIndexOf('/');
                string moduleName = lastSlashIndex >= 0 ? menuPath.Substring(lastSlashIndex + 1) : menuPath;
                module.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                module.name = ObjectNames.NicifyVariableName(moduleName);
        
                // foreach (var submoduleType in submoduleTypes)
                // {
                //     module.Add(submoduleType);
                // }
        
                m_Profile.Add(module);
        
                if (EditorUtility.IsPersistent(m_Profile))
                {
                    AssetDatabase.AddObjectToAsset(module, m_Profile);
                    EditorUtility.SetDirty(m_Profile);
                }
        
                m_Profile.IsDirty = true;
        
                serializedObject.Update();
                m_NeedsRefresh = true;
                Repaint();
        
                CoreEditorUtility.MarkDirtyAndSave(m_Profile);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to create module for submodules: {e.Message}", "OK");
            }
        }
        
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
                    m_Profile.Remove(module);
                    m_NeedsRefresh = true;
                    Repaint();
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to remove module: {e.Message}", "OK");
                }
            }
        }

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

        private void ClearEditors()
        {
            foreach (var editor in m_Editors)
            {
                editor?.OnDisable();
            }
            m_Editors.Clear();
        }

        
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
    }
}

#endif