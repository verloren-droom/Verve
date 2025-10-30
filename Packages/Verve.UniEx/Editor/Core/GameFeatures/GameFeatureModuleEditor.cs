#if UNITY_EDITOR

namespace VerveEditor
{
    using Verve;
    using System;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using Verve.UniEx;
    using System.Reflection;
    using Object = UnityEngine.Object;

    
    /// <summary>
    ///   <para>游戏功能模块编辑器</para>
    /// </summary>
    [CustomEditor(typeof(GameFeatureModule), true), CanEditMultipleObjects]
    internal class GameFeatureModuleEditor : Editor
    {
        private GameFeatureModule m_Module;
        
        /// <summary>
        ///   <para>排除绘制的字段</para>
        /// </summary>
        private static readonly string[] s_ExcludedFields =
        {
            "m_IsActive",
            "m_Script",
            "m_Submodules",
            "m_SubmoduleTypeNames"
        };

        /// <summary>
        ///   <para>样式</para>
        /// </summary>
        private static class Styles
        {
            /// <summary>
            ///   <para>模块不支持多编辑信息</para>
            /// </summary>
            public static string ModuleNotSupportMultiEditInfo { get; } = L10n.Tr("Modules cannot be edited in multi-editing mode.");
            
            /// <summary>
            ///   <para>没有可用子模块信息</para>
            /// </summary>
            public static string NoSubmoduleInfo { get; } = L10n.Tr("No available submodules found.");
            
            /// <summary>
            ///   <para>子模块锁定信息</para>
            /// </summary>
            public static string SubmoduleLockedInfo { get; } = L10n.Tr("This module is locked and cannot be modified.");
            
            /// <summary>
            ///   <para>所有文本</para>
            /// </summary>
            public static GUIContent AllText { get; } = EditorGUIUtility.TrTextContent("ALL", "Toggle all submodules on.");
            
            /// <summary>
            ///   <para>无文本</para>
            /// </summary>
            public static GUIContent NoneText { get; } = EditorGUIUtility.TrTextContent("NONE", "Toggle all submodules off.");
        }

        private void OnEnable()
        {
            m_Module = target as GameFeatureModule;
        }

        public override void OnInspectorGUI()
        {
            if (m_Module == null || target == null)
                return;

            serializedObject.Update();
            
            DrawPropertiesExcluding(serializedObject, s_ExcludedFields);
            
            EditorGUILayout.Space();
            
            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox(Styles.ModuleNotSupportMultiEditInfo, MessageType.Info);
            }
            else
            {
                DrawAvailableSubmodules(m_Module);
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        ///   <para>绘制可用子模块</para>
        /// </summary>
        private void DrawAvailableSubmodules(GameFeatureModule module)
        {
            var availableTypes = CoreEditorUtility.GetSubmoduleTypes(module);
        
            if (availableTypes.Length == 0)
            {
                EditorGUILayout.HelpBox(Styles.NoSubmoduleInfo, MessageType.Info);
                return;
            }
        
            var selectionMode = module.GetType().GetCustomAttribute<GameFeatureAttribute>()?.SelectionMode ?? SubmoduleSelectionMode.Multiple;
            if (selectionMode == SubmoduleSelectionMode.Multiple)
            {
                DrawMultipleSelection(availableTypes, module);
            }
            else if (selectionMode == SubmoduleSelectionMode.Single)
            {
                DrawSingleSelection(availableTypes, module);
            }
            else if (selectionMode == SubmoduleSelectionMode.Locked)
            {
                DrawLockedSelection(availableTypes, module);
            }
            else
            {
                EditorGUILayout.HelpBox("Invalid selection mode.", MessageType.Error);
            }
        }

        /// <summary>
        ///   <para>绘制多选子模块</para>
        /// </summary>
        private void DrawMultipleSelection(Type[] availableTypes, GameFeatureModule module)
        {
            int enabledCount = module.Submodules.Count;
            bool allEnabled = enabledCount == availableTypes.Length;
            bool noneEnabled = enabledCount == 0;
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(allEnabled);
                if (GUILayout.Button(Styles.AllText, CoreEditorUtility.MiniLabelButton, GUILayout.ExpandWidth(false)))
                {
                    foreach (var type in availableTypes)
                    {
                        if (!module.Has(type))
                        {
                            module.Add(type);
                        }
                    }
                }
                EditorGUI.EndDisabledGroup();
                
                EditorGUI.BeginDisabledGroup(noneEnabled);
                if (GUILayout.Button(Styles.NoneText, CoreEditorUtility.MiniLabelButton, GUILayout.ExpandWidth(false)))
                {
                    foreach (var type in availableTypes)
                    {
                        RemoveSubmodule(type, module);
                    }
                }
                EditorGUI.EndDisabledGroup();
            }

            foreach (var type in availableTypes)
            {
                DrawSubmoduleToggle(type, module.Has(type), module);
            }
        }
        
        /// <summary>
        ///   <para>绘制单选子模块</para>
        /// </summary>
        private void DrawSingleSelection(Type[] availableTypes, GameFeatureModule module)
        {
            if (module.Submodules.Count > 1)
            {
                var firstSubmodule = module.Submodules.First();
                var submodulesToRemove = module.Submodules.Skip(1).ToList();
        
                foreach (var sub in submodulesToRemove)
                {
                    RemoveSubmodule(sub.GetType(), module);
                }
            }
    
            int selectedIndex = -1;
            if (module.Submodules.Count > 0)
            {
                var selectedType = module.Submodules.First().GetType();
                selectedIndex = Array.IndexOf(availableTypes, selectedType);
            }
    
            for (int i = 0; i < availableTypes.Length; i++)
            {
                var type = availableTypes[i];
                string submoduleName = ObjectNames.NicifyVariableName(type.Name);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    bool isSelected = EditorGUILayout.Toggle(i == selectedIndex, EditorStyles.radioButton, GUILayout.Width(15));
                    EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent(submoduleName, type.GetType().GetCustomAttribute<GameFeatureAttribute>()?.Description), GUILayout.ExpandWidth(true));

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (isSelected)
                        {
                            foreach (var sub in module.Submodules.ToList())
                            {
                                RemoveSubmodule(sub.GetType(), module);
                            }
                            
                            AddSubmodule(type, module);
                        }
                        else if (i == selectedIndex)
                        {
                            RemoveSubmodule(type, module);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///   <para>绘制锁定子模块</para>
        /// </summary>
        private void DrawLockedSelection(Type[] availableTypes, GameFeatureModule module)
        {
            EditorGUILayout.HelpBox(Styles.SubmoduleLockedInfo, MessageType.Info);
            using (new EditorGUI.DisabledGroupScope(true))
            {
                foreach (var type in availableTypes)
                {
                    DrawSubmoduleToggle(type, module.Has(type), module);
                }
            }
        }
        
        /// <summary>
        ///   <para>绘制子模块开关</para>
        /// </summary>
        private void DrawSubmoduleToggle(Type submoduleType, bool isAdded, GameFeatureModule module)
        {
            string submoduleName = ObjectNames.NicifyVariableName(submoduleType.Name);

            using (new EditorGUILayout.HorizontalScope())
            {
                bool newState = EditorGUILayout.Toggle(isAdded, GUILayout.Width(20));
                
                if (newState != isAdded)
                {
                    if (newState)
                        AddSubmodule(submoduleType, module);
                    else 
                        RemoveSubmodule(submoduleType, module);
                }
                
                EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent(submoduleName, submoduleType.GetCustomAttribute<GameFeatureAttribute>()?.Description), GUILayout.ExpandWidth(true));
            }
        }

        /// <summary>
        ///   <para>添加子模块</para>
        /// </summary>
        private void AddSubmodule(Type submoduleType, GameFeatureModule module)
        {
            try
            {
                Undo.RecordObject(module, "Add Submodule");
                module.Add(submoduleType);
                EditorUtility.SetDirty(module);
                serializedObject.Update();
                Repaint();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to add submodule {submoduleType.Name}: {e.Message}", "OK");
            }
        }

        /// <summary>
        ///   <para>移除子模块</para>
        /// </summary>
        private void RemoveSubmodule(Type submoduleType, GameFeatureModule module)
        {
            try
            {
                Undo.RecordObject(module, "Remove Submodule");
                var removed = module.Remove(submoduleType);
                if (removed)
                {
                    EditorUtility.SetDirty(module);
                    serializedObject.Update();
                    Repaint();
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to remove submodule: {e.Message}", "OK");
            }
        }

        /// <summary>
        ///   <para>绘制子模块字段</para>
        /// </summary>
        private void DrawSubmoduleFields(IGameFeatureSubmodule submodule)
        {
            if (submodule == null) return;
            
            var fields = submodule.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(f => !f.IsInitOnly && !f.IsLiteral && !f.IsNotSerialized)
                .ToArray();
                
            if (fields.Length > 0)
            {
                EditorGUILayout.Space();
                
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    foreach (var field in fields)
                    {
                        DrawReadOnlyField(field.GetValue(submodule), field);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No fields to display.", MessageType.Info);
            }
        }
        
        /// <summary>
        ///   <para>将字段绘制为只读</para>
        /// </summary>
        private void DrawReadOnlyField(object value, FieldInfo field)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                try
                {
                    EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(field.Name), GUILayout.Width(EditorGUIUtility.labelWidth));
                    EditorGUILayout.LabelField(value.ToString(), EditorStyles.label);
                }
                catch (Exception e)
                {
                    EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(field.Name));
                    EditorGUILayout.HelpBox($"Error: {e.Message}", MessageType.Error);
                }
            }
        }
    }
}

#endif