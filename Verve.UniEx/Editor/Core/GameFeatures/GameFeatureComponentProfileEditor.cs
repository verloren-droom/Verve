#if UNITY_EDITOR

namespace VerveEditor
{
    using System;
    using Verve.UniEx;
    using UnityEditor;
    using UnityEngine;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;


    /// <summary>
    ///   <para>游戏功能组件配置编辑</para>
    /// </summary>
    [CustomEditor(typeof(GameFeatureComponentProfile), true)]
    internal class GameFeatureComponentProfileEditor : Editor
    {
        private GameFeatureComponentProfile m_Profile;
        
        private SerializedProperty m_ComponentsProperty;
        private readonly List<ComponentEditor> m_Editors = new List<ComponentEditor>();
        private bool m_NeedsRefresh;

        
        /// <summary>
        ///   <para>样式</para>
        /// </summary>
        private static class Styles
        {
            /// <summary>
            ///   <para>没有组件信息</para>
            /// </summary>
            public static string NoComponentsInfo { get; } = L10n.Tr("No components added to this profile.");
            
            /// <summary>
            ///   <para>组件文本</para>
            /// </summary>
            public static GUIContent ComponentsText { get; } = EditorGUIUtility.TrTextContent("Components");
            
            /// <summary>
            ///   <para>添加组件</para>
            /// </summary>
            public static GUIContent RemoveComponent { get; } = EditorGUIUtility.TrTextContent("Remove");
            
            /// <summary>
            ///   <para>重置组件</para>
            /// </summary>
            public static GUIContent ResetComponent { get; } = EditorGUIUtility.TrTextContent("Reset");
            
            /// <summary>
            ///   <para>添加组件</para>
            /// </summary>
            public static GUIContent AddComponent { get; } = EditorGUIUtility.TrTextContent("Add Game Feature Component", "Adds a new component to this profile.");
        }

        
        private void OnEnable()
        {
            m_Profile = target as GameFeatureComponentProfile;

            m_ComponentsProperty = serializedObject.FindProperty("m_Components");
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
    
            if (m_NeedsRefresh || m_Editors.Count != m_ComponentsProperty.arraySize)
            {
                RefreshEditors();
                m_NeedsRefresh = false;
            }

            DrawComponentsList();
            DrawAddComponentButton();
            
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
        ///   <para>绘制添加组件</para>
        /// </summary>
        private void DrawComponentsList()
        {
            EditorGUILayout.LabelField(Styles.ComponentsText, EditorStyles.boldLabel);

            CleanNullElements();
            
            if (m_ComponentsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox(Styles.NoComponentsInfo, MessageType.Info);
                return;
            }
            
            if (m_Editors.Count != m_ComponentsProperty.arraySize)
            {
                RefreshEditors();
                return;
            }

            for (int i = 0; i < m_ComponentsProperty.arraySize; i++)
            {
                var componentProperty = m_ComponentsProperty.GetArrayElementAtIndex(i);
                if (componentProperty == null || componentProperty.objectReferenceValue == null)
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
                    componentProperty,
                    null,
                    position => OnContextClick((Vector2)position, i)
                );

                if (displayContent)
                {
                    editor.OnInspectorGUI();
                }
            }

            if (m_ComponentsProperty.arraySize > 0)
                CoreEditorUtility.DrawSplitter();
        }

        /// <summary>
        ///   <para>清理空元素</para>
        /// </summary>
        private void CleanNullElements()
        {
            if (m_ComponentsProperty == null) return;
            
            bool hasNulls = false;
            
            for (int i = m_ComponentsProperty.arraySize - 1; i >= 0; i--)
            {
                var element = m_ComponentsProperty.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == null)
                {
                    hasNulls = true;
                    break;
                }
            }
            
            if (hasNulls)
            {
                serializedObject.Update();
                
                for (int i = m_ComponentsProperty.arraySize - 1; i >= 0; i--)
                {
                    var element = m_ComponentsProperty.GetArrayElementAtIndex(i);
                    if (element.objectReferenceValue == null)
                    {
                        m_ComponentsProperty.DeleteArrayElementAtIndex(i);
                    }
                }
                
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }

        /// <summary>
        ///   <para>右键菜单</para>
        /// </summary>
        private void OnContextClick(Vector2 position, int index)
        {
            var menu = new GenericMenu();
            menu.AddItem(Styles.RemoveComponent, false, () => RemoveComponent(index));
            menu.AddItem(Styles.ResetComponent, false, () => ResetComponent(index));

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        /// <summary>
        ///   <para>绘制添加组件</para>
        /// </summary>
        private void DrawAddComponentButton()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button(Styles.AddComponent, EditorStyles.miniButton))
            {
                ShowAddComponentMenu();
            }
        }

        /// <summary>
        ///   <para>显示添加组件菜单</para>
        /// </summary>
        private void ShowAddComponentMenu()
        {
            var menu = new GenericMenu();

            var componentTypes = TypeCache.GetTypesDerivedFrom<GameFeatureComponent>()
                .Where(t => !t.IsAbstract);
            
            foreach (var type in componentTypes)
            {
                var menuPath = GetComponentMenuPath(type);

                if (m_Profile.Has(type))
                {
                    menu.AddDisabledItem(new GUIContent($"{menuPath} (Already Added)"));
                }
                else
                {
                    menu.AddItem(new GUIContent(menuPath), false, () => AddComponent(type));
                }
            }

            menu.ShowAsContext();
        }

        /// <summary>
        ///   <para>获取组件菜单路径</para>
        /// </summary>
        private static string GetComponentMenuPath(Type type)
        {
            var attr = type.GetCustomAttribute<GameFeatureComponentMenuAttribute>();
            if (attr == null || string.IsNullOrEmpty(attr.MenuPath))
                return ObjectNames.NicifyVariableName(type.Name);

            if (attr.MenuPath.EndsWith("/"))
                return attr.MenuPath + ObjectNames.NicifyVariableName(type.Name);
                
            return attr.MenuPath;
        }

        /// <summary>
        ///   <para>添加组件</para>
        /// </summary>
        private void AddComponent(Type type)
        {
            if (m_Profile == null) return;

            try
            {
                Undo.RecordObject(m_Profile, Styles.AddComponent.text);
                
                var component = m_Profile.Add(type);

                if (component != null)
                {
                    serializedObject.Update();
                    m_NeedsRefresh = true;
                    Repaint();
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to add component: {e.Message}", "OK");
            }
        }
        
        /// <summary>
        ///   <para>移除组件</para>
        /// </summary>
        private void RemoveComponent(int index)
        {
            if (index < 0 || index >= m_ComponentsProperty.arraySize) return;

            serializedObject.Update();

            var componentProperty = m_ComponentsProperty.GetArrayElementAtIndex(index);
            var component = componentProperty?.objectReferenceValue as GameFeatureComponent;

            if (component != null)
            {
                try
                {
                    Undo.RecordObject(m_Profile, Styles.RemoveComponent.text);
                    
                    if (EditorUtility.IsPersistent(component) && AssetDatabase.Contains(component))
                    {
                        Undo.DestroyObjectImmediate(component);
                    }
                    
                    m_Profile.Remove(component.GetType());
                    m_NeedsRefresh = true;
                    Repaint();
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to remove component: {e.Message}", "OK");
                }
            }
        }

        /// <summary>
        ///   <para>重置组件</para>
        /// </summary>
        private void ResetComponent(int index)
        {
            if (index < 0 || index >= m_ComponentsProperty.arraySize) return;

            serializedObject.Update();

            var componentProperty = m_ComponentsProperty.GetArrayElementAtIndex(index);
            var component = componentProperty?.objectReferenceValue as GameFeatureComponent;

            if (component != null)
            {
                try
                {
                    Undo.RecordObject(m_Profile, Styles.ResetComponent.text);
                    
                    if (EditorUtility.IsPersistent(component) && AssetDatabase.Contains(component))
                    {
                        Undo.DestroyObjectImmediate(component);
                    }
                    
                    m_Profile.Remove(component.GetType());
                    m_Profile.Add(component.GetType());
                    m_NeedsRefresh = true;
                    Repaint();
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to remove component: {e.Message}", "OK");
                }
            }
        }

        /// <summary>
        ///   <para>刷新编辑器</para>
        /// </summary>
        private void RefreshEditors()
        {
            ClearEditors();

            if (m_ComponentsProperty == null) 
            {
                serializedObject.Update();
                m_ComponentsProperty = serializedObject.FindProperty("m_Components");
            }

            CleanNullElements();
            
            for (int i = 0; i < m_ComponentsProperty.arraySize; i++)
            {
                var componentProperty = m_ComponentsProperty.GetArrayElementAtIndex(i);
                var component = componentProperty?.objectReferenceValue as GameFeatureComponent;

                if (component != null)
                {
                    var editor = new ComponentEditor();
                    editor.Init(component, this);
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
        ///   <para>组件编辑器</para>
        /// </summary>
        internal class ComponentEditor
        {
            private Editor m_Editor;
            private SerializedObject m_SerializedObject;

            internal GameFeatureComponent Target { get; private set; }
            internal SerializedProperty ActiveProperty { get; private set; }

            /// <summary>
            ///   <para>初始化组件编辑器</para>
            /// </summary>
            public void Init(GameFeatureComponent target, GameFeatureComponentProfileEditor parentEditor)
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
                    UnityEngine.Object.DestroyImmediate(m_Editor);
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

            /// <summary>
            ///   <para>获取显示标题</para>
            /// </summary>
            public GUIContent GetDisplayTitle()
            {
                if (Target == null)
                    return new GUIContent("Unknown Component");

                string typeName = ObjectNames.NicifyVariableName(Target.GetType().Name);
                
                var attr = Target.GetType().GetCustomAttribute<GameFeatureComponentMenuAttribute>();
                if (attr != null && !string.IsNullOrEmpty(attr.MenuPath))
                {
                    if (attr.MenuPath.EndsWith("/"))
                    {
                        return new GUIContent(typeName);
                    }
                    
                    if (attr.MenuPath.Contains("/"))
                    {
                        string[] pathParts = attr.MenuPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                        if (pathParts.Length > 0)
                        {
                            return new GUIContent(pathParts[pathParts.Length - 1]);
                        }
                    }
                    else
                    {
                        return new GUIContent(attr.MenuPath);
                    }
                }
                
                return new GUIContent(typeName);;
            }
        }
    }
}

#endif
