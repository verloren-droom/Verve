#if UNITY_EDITOR

namespace VerveEditor
{
    using Verve;
    using System;
    using Verve.UniEx;
    using UnityEditor;
    using UnityEngine;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;

    
    /// <summary>
    ///   <para>游戏功能组件编辑</para>
    /// </summary>
    [CustomEditor(typeof(GameFeatureComponent), true), CanEditMultipleObjects]
    internal class GameFeatureComponentEditor : Editor
    {
        private GameFeatureComponent m_Component;
        
        /// <summary>
        ///   <para>排除绘制的字段</para>
        /// </summary>
        private string[] m_ExcludedFields =
        {
            "m_Script",
            "m_IsActive",
            "m_Parameters"
        };

        /// <summary>
        ///   <para>样式</para>
        /// </summary>
        private static class Styles
        {
            /// <summary>
            ///   <para>没有参数信息</para>
            /// </summary>
            public static string NoParametersInfo { get; } = L10n.Tr("No parameter found in this component.");
            
            /// <summary>
            ///   <para>组件不支持多编辑信息</para>
            /// </summary>
            public static string ComponentNotSupportMultiEditInfo { get; } = L10n.Tr("Component cannot be edited in multi-editing mode.");
        }
        
        private static readonly Dictionary<Type, GameFeatureParameterDrawer> s_ParameterDrawers = new Dictionary<Type, GameFeatureParameterDrawer>();
        
        
        static GameFeatureComponentEditor()
        {
            var drawerTypes = TypeCache.GetTypesDerivedFrom<GameFeatureParameterDrawer>().Where(type => type.IsClass && !type.IsAbstract).ToArray();

            foreach (var drawerType in drawerTypes)
            {
                var attribute = drawerType.GetCustomAttribute<GameFeatureParameterDrawerAttribute>();
                if (attribute != null && !s_ParameterDrawers.ContainsKey(attribute.parameterType))
                {
                    if (Activator.CreateInstance(drawerType) is GameFeatureParameterDrawer drawerInstance)
                    {
                        s_ParameterDrawers[attribute.parameterType] = drawerInstance;
                    }
                }
            }
        }
        
        private void OnEnable()
        {
            m_Component = target as GameFeatureComponent;
            
            var parameterFields = m_Component?.GetType()
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

        /// <summary>
        ///   <para>绘制参数</para>
        /// </summary>
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
                    EditorGUI.BeginChangeCheck();
                    
                    SerializedProperty valueProperty = parameterProperty.FindPropertyRelative("m_Value");

                    if (valueProperty != null)
                    {
                        if (s_ParameterDrawers.TryGetValue(field.FieldType, out var drawer))
                        {
                            drawer.OnGUI(new SerializedDataParameter(parameterProperty), new GUIContent(fieldName, parameterProperty.tooltip));
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(valueProperty, new GUIContent(fieldName, parameterProperty.tooltip), true);
                        }
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(parameterProperty, new GUIContent(fieldName, parameterProperty.tooltip), true);
                    }
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        parameterProperty.serializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }
    }
}

#endif
