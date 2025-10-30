#if UNITY_EDITOR

namespace VerveEditor
{
    using System;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using Verve.UniEx;
    using System.Reflection;
    using System.Collections.Generic;

    
    /// <summary>
    ///   <para>游戏功能运行器编辑器</para>
    /// </summary>
    [CustomEditor(typeof(GameFeaturesRunner))]
    internal class GameFeaturesRunnerEditor : Editor
    {
        private GameFeaturesRunner m_Runner;
        
        // private SerializedProperty m_ModuleProfileProperty;
        // private SerializedProperty m_ComponentProfileProperty;
        private SerializedProperty m_ModulesProperty;
        
        private readonly List<GameFeatureModuleProfileEditor.ModuleEditor> m_Editors = new List<GameFeatureModuleProfileEditor.ModuleEditor>();

        /// <summary>
        ///   <para>排除绘制的字段</para>
        /// </summary>
        private static readonly string[] s_ExcludedFields =
        {
            "m_Script",
            "m_ModuleProfile",
            "m_ComponentProfile",
            "m_Modules",
        };

        /// <summary>
        ///   <para>样式</para>
        /// </summary>
        private static class Styles
        {
            /// <summary>
            ///   <para>添加游戏功能按钮</para>
            /// </summary>
            public static GUIContent AddModuleButton { get; } = EditorGUIUtility.TrTextContent("Add Game Feature");
            
            /// <summary>
            ///   <para>没有模块信息</para>
            /// </summary>
            public static GUIContent NoModulesInfo { get; } = EditorGUIUtility.TrTextContent("No modules available. Assign a Module profile asset first.");
            
            /// <summary>
            ///   <para>没有添加的模块信息</para>
            /// </summary>
            public static string NoAddedModulesInfo { get; } = L10n.Tr("No modules. Click 'Add Game Feature' to add modules.");
            
            /// <summary>
            ///   <para>运行器信息</para>
            /// </summary>
            public static string RunnerInfo { get; } = L10n.Tr("Game Features Runner is responsible for managing and running game feature modules.");
        }

        public void OnEnable()
        {
            m_Runner = target as GameFeaturesRunner;
            if (m_Runner == null || target == null) return;

            m_ModulesProperty = serializedObject.FindProperty("m_Modules");
            // m_ModuleProfileProperty = serializedObject.FindProperty("m_ModuleProfile");
            // m_ComponentProfileProperty = serializedObject.FindProperty("m_ComponentProfile");
            
            RefreshEditors();
        }

        public void OnDisable()
        {
            ClearEditors();
        }

        public override void OnInspectorGUI()
        {
            if (m_Runner == null || target == null) return;
            
            serializedObject.Update();
            
            DrawPropertiesExcluding(serializedObject, s_ExcludedFields);

            // using (new EditorGUI.DisabledGroupScope(true))
            // {
            //     EditorGUILayout.PropertyField(m_ModuleProfileProperty);
            //     EditorGUILayout.PropertyField(m_ComponentProfileProperty);
            // }
            
            EditorGUILayout.HelpBox(Styles.RunnerInfo, MessageType.Info);

            DrawModulesList();
            
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        ///   <para>绘制模块列表</para>
        /// </summary>
        private void DrawModulesList()
        {
            if (m_Runner.ModuleProfile == null)
            {
                EditorGUILayout.HelpBox("Assign a Module Profile asset to enable module selection.", MessageType.Warning);
                return;
            }
            
            if (m_Runner.ModuleProfile.Modules.Count == 0)
            {
                EditorGUILayout.HelpBox(Styles.NoModulesInfo.text, MessageType.Info);
                return;
            }
            
            EditorGUILayout.Space();
            
            if (m_ModulesProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox(Styles.NoAddedModulesInfo, MessageType.Info);
            }
            else
            {
                if (m_Editors.Count != m_ModulesProperty.arraySize)
                {
                    RefreshEditors();
                }
                
                for (int i = 0; i < m_ModulesProperty.arraySize; i++)
                {
                    var moduleProperty = m_ModulesProperty.GetArrayElementAtIndex(i);
                    if (moduleProperty.objectReferenceValue == null)
                    {
                        m_ModulesProperty.DeleteArrayElementAtIndex(i);
                        serializedObject.ApplyModifiedProperties();
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
                        editor.ActiveProperty,
                        position => OnContextClick((Vector2)position, i)
                    );

                    if (displayContent)
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            editor.OnInspectorGUI();
                        }
                    }
                }

                if (m_ModulesProperty.arraySize > 0)
                    CoreEditorUtility.DrawSplitter();
            }
            
            EditorGUILayout.Space();
            if (GUILayout.Button(Styles.AddModuleButton, EditorStyles.miniButton))
            {
                ShowAddModuleMenu();
            }
        }

        /// <summary>
        ///   <para>菜单点击</para>
        /// </summary>
        private void OnContextClick(Vector2 position, int index)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Remove"), false, () => RemoveModule(index));
            menu.DropDown(new Rect(position, Vector2.zero));
        }

        /// <summary>
        ///   <para>显示添加模块菜单</para>
        /// </summary>
        private void ShowAddModuleMenu()
        {
            var menu = new GenericMenu();
            
            var availableModules = m_Runner.ModuleProfile.Modules
                .Where(m => m != null && !IsModuleAdded((GameFeatureModule)m))
                .ToList();
            
            if (availableModules.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No available modules"));
            }
            else
            {
                var moduleGroups = availableModules
                    .GroupBy(m => m.GetType())
                    .OrderBy(g => g.Key.Name)
                    .ToList();
                
                foreach (var group in moduleGroups)
                {
                    string menuPath = CoreEditorUtility.GetGameFeatureMenuPath(group.Key);
                    
                    if (string.IsNullOrEmpty(menuPath))
                    {
                        menuPath = ObjectNames.NicifyVariableName(group.Key.Name);
                    }
                    
                    if (group.Count() > 1)
                    {
                        int index = 0;
                        foreach (var module in group)
                        {
                            string itemPath = $"{menuPath} {index}";
                            menu.AddItem(new GUIContent(itemPath), false, () => AddModule((GameFeatureModule)module));
                            index++;
                        }
                    }
                    else
                    {
                        menu.AddItem(new GUIContent(menuPath), false, () => AddModule((GameFeatureModule)group.First()));
                    }
                }
            }
            
            menu.ShowAsContext();
        }

        /// <summary>
        ///   <para>判断模块是否已添加</para>
        /// </summary>
        private bool IsModuleAdded(GameFeatureModule module)
        {
            for (int i = 0; i < m_ModulesProperty.arraySize; i++)
            {
                var element = m_ModulesProperty.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == module)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///   <para>添加模块</para>
        /// </summary>
        private void AddModule(GameFeatureModule module)
        {
            if (module == null) return;
            
            serializedObject.Update();
            m_ModulesProperty.arraySize++;
            var newElement = m_ModulesProperty.GetArrayElementAtIndex(m_ModulesProperty.arraySize - 1);
            newElement.objectReferenceValue = module;
            serializedObject.ApplyModifiedProperties();
            
            RefreshEditors();
        }

        /// <summary>
        ///   <para>移除模块</para>
        /// </summary>
        private void RemoveModule(int index)
        {
            if (index < 0 || index >= m_ModulesProperty.arraySize) return;
            
            serializedObject.Update();
            m_ModulesProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            
            RefreshEditors();
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
                m_ModulesProperty = serializedObject.FindProperty("m_Modules");
            }
            
            CleanNullElements();
            
            for (int i = 0; i < m_ModulesProperty.arraySize; i++)
            {
                var moduleProperty = m_ModulesProperty.GetArrayElementAtIndex(i);
                var module = moduleProperty?.objectReferenceValue as GameFeatureModule;

                if (module != null)
                {
                    var editor = new GameFeatureModuleProfileEditor.ModuleEditor();
                    editor.Init(module, null);
                    m_Editors.Add(editor);
                }
            }
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
    }
}

#endif