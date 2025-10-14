#if UNITY_EDITOR

namespace VerveEditor.UniEx
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using System.Linq;
    using Verve.UniEx;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 参数绘制器属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class GameFeatureParameterDrawerAttribute : Attribute
    {
        public readonly Type parameterType;

        
        public GameFeatureParameterDrawerAttribute(Type parameterType)
        {
            this.parameterType = parameterType;
        }
    }

    
    /// <summary>
    /// 参数绘制器
    /// </summary>
    public abstract class GameFeatureParameterDrawer
    {
        public abstract bool OnGUI(SerializedDataParameter parameter, GUIContent title);
    }
    

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