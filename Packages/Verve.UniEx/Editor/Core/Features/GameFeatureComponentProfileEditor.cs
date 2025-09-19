#if UNITY_EDITOR

namespace VerveEditor.UniEx
{
    using System;
    using Verve.UniEx;
    using UnityEditor;
    using UnityEngine;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;


    [CustomEditor(typeof(GameFeatureComponentProfile), true)]
    internal class GameFeatureComponentProfileEditor : Editor
    {
        private GameFeatureComponentProfile m_Profile;
        
        private SerializedProperty m_ComponentsProperty;
        private readonly List<ComponentEditor> m_Editors = new List<ComponentEditor>();
        private bool m_NeedsRefresh;

        
        private static class Styles
        {
            public static GUIContent ComponentsText { get; } = EditorGUIUtility.TrTextContent("Components");
            public static GUIContent RemoveComponent { get; } = EditorGUIUtility.TrTextContent("Remove");
            public static GUIContent ResetComponent { get; } = EditorGUIUtility.TrTextContent("Reset");
            public static GUIContent AddComponent { get; } = EditorGUIUtility.TrTextContent("Add Game Feature Component");
            public static GUIContent NoComponentsInfo { get; } = EditorGUIUtility.TrTextContent("No components added to this profile.");
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

        private void OnUndoRedoPerformed()
        {
            m_NeedsRefresh = true;
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            if (m_Profile == null || target == null)
                return;
            
            serializedObject.Update();
    
            if (m_NeedsRefresh || m_Editors.Count != m_ComponentsProperty.arraySize)
            {
                RefreshEditors();
                m_NeedsRefresh = false;
            }

            DrawComponentsList();
            DrawAddComponentButton();

            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawComponentsList()
        {
            EditorGUILayout.LabelField(Styles.ComponentsText, EditorStyles.boldLabel);

            CleanNullElements();
            
            if (m_ComponentsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox(Styles.NoComponentsInfo.text, MessageType.Info);
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

        private void OnContextClick(Vector2 position, int index)
        {
            var menu = new GenericMenu();
            menu.AddItem(Styles.RemoveComponent, false, () => RemoveComponent(index));
            menu.AddItem(Styles.ResetComponent, false, () => ResetComponent(index));

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        private void DrawAddComponentButton()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button(Styles.AddComponent, EditorStyles.miniButton))
            {
                ShowAddComponentMenu();
            }
        }

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

        private static string GetComponentMenuPath(Type type)
        {
            var attr = type.GetCustomAttribute<GameFeatureComponentMenuAttribute>();
            if (attr == null || string.IsNullOrEmpty(attr.MenuPath))
                return ObjectNames.NicifyVariableName(type.Name);

            if (attr.MenuPath.EndsWith("/"))
                return attr.MenuPath + ObjectNames.NicifyVariableName(type.Name);
                
            return attr.MenuPath;
        }

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

        private void ClearEditors()
        {
            foreach (var editor in m_Editors)
            {
                editor?.OnDisable();
            }
            m_Editors.Clear();
        }

        
        internal class ComponentEditor
        {
            private Editor m_Editor;
            private SerializedObject m_SerializedObject;

            internal GameFeatureComponent Target { get; private set; }
            internal SerializedProperty ActiveProperty { get; private set; }

            
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
