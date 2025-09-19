#if UNITY_EDITOR

namespace VerveEditor.UniEx
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using Verve.UniEx;
    using UnityEngine.SceneManagement;
    
    
    internal static class GameFeatureProfileFactory
    {
        public static GameFeatureComponentProfile CreateGameFeaturesProfile(string path = null, bool saveAsset = true)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                path = "Assets";
            }

            var profile = ScriptableObject.CreateInstance<GameFeatureComponentProfile>();
            AssetDatabase.CreateAsset(profile, Path.Combine(path, "GameFeaturesProfile.asset"));
            if (saveAsset)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            return profile;
        }
        
        public static T CreateGameFeatureComponent<T>(GameFeatureComponentProfile profile, bool overrides = false, bool saveAsset = true)
            where T : GameFeatureComponent 
            => (T)CreateGameFeatureComponent(typeof(T), profile, overrides, saveAsset);
        
        public static GameFeatureComponent CreateGameFeatureComponent(Type type, GameFeatureComponentProfile profile, bool overrides = false, bool saveAsset = true)
        {
            var comp = profile.Add(type, overrides);
            if (EditorUtility.IsPersistent(profile))
            {
                AssetDatabase.AddObjectToAsset(comp, profile);
            }

            if (saveAsset && EditorUtility.IsPersistent(profile))
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return comp;
        }
    }
}

#endif