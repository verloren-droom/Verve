#if UNITY_EDITOR

namespace VerveEditor
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using Verve.UniEx;
    using System.Collections.Generic;
    
    
    /// <summary>
    ///   <para>参数绘制器特性</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class GameFeatureParameterDrawerAttribute : Attribute
    {
        /// <summary>
        ///   <para>参数类型</para>
        /// </summary>
        public readonly Type parameterType;

        
        public GameFeatureParameterDrawerAttribute(Type parameterType)
        {
            this.parameterType = parameterType;
        }
    }

    
    /// <summary>
    ///   <para>参数绘制器基类</para>
    /// </summary>
    public abstract class GameFeatureParameterDrawer
    {
        public abstract bool OnGUI(SerializedDataParameter parameter, GUIContent title);
    }
    

    /// <summary>
    ///   <para>浮点数限制参数绘制器</para>
    /// </summary>
    [GameFeatureParameterDrawer(typeof(ClampedFloatParameter))]
    internal sealed class ClampedFloatParameterDrawer : GameFeatureParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var clampedFloatParameter = parameter.GetTargetObject<ClampedFloatParameter>();
            if (clampedFloatParameter == null)
                return false;
            EditorGUILayout.Slider(parameter.value, clampedFloatParameter.minValue, clampedFloatParameter.maxValue, title);
            return true;
        }
    }
    
    
    /// <summary>
    ///   <para>整数限制参数绘制器</para>
    /// </summary>
    [GameFeatureParameterDrawer(typeof(ClampedIntParameter))]
    internal sealed class ClampedIntParameterDrawer : GameFeatureParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var clampedFloatParameter = parameter.GetTargetObject<ClampedIntParameter>();
            if (clampedFloatParameter == null)
                return false;
            EditorGUILayout.Slider(parameter.value, clampedFloatParameter.minValue, clampedFloatParameter.maxValue, title);
            return true;
        }
    }

    
    /// <summary>
    ///   <para>路径参数绘制器</para>
    /// </summary>
    [GameFeatureParameterDrawer(typeof(PathParameter))]
    internal sealed class PathParameterDrawer : GameFeatureParameterDrawer
    {
        private static Dictionary<string, string> s_PathOptionsMap;
    
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var pathParameter = parameter.GetTargetObject<PathParameter>();
            if (pathParameter == null)
                return false;
            
            s_PathOptionsMap ??= new()
            {
                [nameof(Application.dataPath)] = Application.dataPath,
                [nameof(Application.streamingAssetsPath)] = Application.streamingAssetsPath,
                [nameof(Application.persistentDataPath)] = Application.persistentDataPath,
                [nameof(Application.temporaryCachePath)] = Application.temporaryCachePath
            };
    
            string currentValue = parameter.value?.stringValue ?? "";
            
            var prefixProperty = parameter.property.FindPropertyRelative("prefix");
            string currentPrefix = prefixProperty?.stringValue ?? nameof(Application.dataPath);
    
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(title);
                
                var prefixKeys = new List<string>(s_PathOptionsMap.Keys);
                var prefixValues = new List<string>(s_PathOptionsMap.Values);
                
                int selectedIndex = 0;
                for (int i = 0; i < prefixValues.Count; i++)
                {
                    if (prefixValues[i] == currentPrefix)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
                
                int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, prefixKeys.ToArray(), GUILayout.Width(150));
                
                if (newSelectedIndex != selectedIndex)
                {
                    string newPrefix = prefixValues[newSelectedIndex];
                    prefixProperty.stringValue = newPrefix;
                }
                
                string newPath = EditorGUILayout.TextField(currentValue);
                if (newPath != currentValue)
                {
                    parameter.value.stringValue = newPath;
                }

            }
            return true;
        }
    }
}

#endif