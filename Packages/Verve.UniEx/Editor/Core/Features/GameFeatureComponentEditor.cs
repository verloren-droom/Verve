#if UNITY_EDITOR

namespace VerveEditor.UniEx
{
    using Verve;
    using Verve.UniEx;
    using UnityEditor;
    using UnityEngine;
    using System.Linq;
    using System.Reflection;

    
    [CustomEditor(typeof(GameFeatureComponent), true), CanEditMultipleObjects]
    internal class GameFeatureComponentEditor : Editor
    {
        private GameFeatureComponent m_Component;
        
        private string[] m_ExcludedFields =
        {
            "m_Script",
            "m_IsActive",
            "m_Parameters"
        };

        private static class Styles
        {
            public static string NoParametersInfo { get; } = L10n.Tr("No parameter found in this component.");
            public static string ComponentNotSupportMultiEditInfo { get; } = L10n.Tr("Component cannot be edited in multi-editing mode.");
        }

        
        private void OnEnable()
        {
            m_Component = target as GameFeatureComponent;
            
            var parameterFields = m_Component.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => typeof(IGameFeatureParameter).IsAssignableFrom(f.FieldType))
                .Select(f => f.Name)
                .ToArray();

            m_ExcludedFields = m_ExcludedFields.Concat(parameterFields).ToHashSet().ToArray();
        }

        public override void OnInspectorGUI()
        {
            if (m_Component == null || target == null)
                return;

            serializedObject.Update();
            
            DrawPropertiesExcluding(serializedObject, m_ExcludedFields);
            
            EditorGUILayout.Space();
            
            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox(Styles.ComponentNotSupportMultiEditInfo, MessageType.Info);
            }
            else
            {
                DrawParameters(m_Component);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawParameters(GameFeatureComponent component)
        {
            var fields = component.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => typeof(IGameFeatureParameter).IsAssignableFrom(f.FieldType))
                .OrderBy(f => f.MetadataToken);
            
            if (!fields.Any())
            {
                EditorGUILayout.HelpBox(Styles.NoParametersInfo, MessageType.Info);
                return;
            }

            foreach (var field in fields)
            {
                string fieldName = ObjectNames.NicifyVariableName(field.Name);
                
                SerializedProperty parameterProperty = serializedObject.FindProperty(field.Name);
                
                if (parameterProperty != null)
                {
                    SerializedProperty valueProperty = parameterProperty.FindPropertyRelative("m_Value");
                    
                    if (valueProperty != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(valueProperty, new GUIContent(fieldName), true);
                        if (EditorGUI.EndChangeCheck())
                        {
                            parameterProperty.serializedObject.ApplyModifiedProperties();
                        }
                    }
                }
            }
        }
    }
}

#endif
