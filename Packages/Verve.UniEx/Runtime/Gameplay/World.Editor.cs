#if UNITY_EDITOR

namespace VerveUniEx.Gameplay
{
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    
    
    [DisallowMultipleComponent]
    public partial class World
    {
        /// <summary>
        /// 创建场景世界组件（每个场景仅限一个）
        /// </summary>
        [MenuItem("GameObject/Verve/Gameplay/World", false, 10)]
        private static void CreateWorld(MenuCommand menuCommand)
        {
            var existingWorld = Object.FindObjectsOfType<World>();
            if (existingWorld.Length > 0)
            {
                EditorUtility.DisplayDialog(
                    "World 组件已存在", 
                    $"场景中已存在 {existingWorld.Length} 个 World 组件，每个场景仅允许存在一个！", 
                    "确定"
                );
                Selection.activeObject = existingWorld[0];
                return;
            }
            
            var prefab = new GameObject("World");
            var playerStart = prefab.AddComponent<World>();
            
            GameObject parent = null;
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                parent = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
            }
            else if (menuCommand.context is GameObject context)
            {
                parent = context;
            }
            else
            {
                parent = Selection.activeGameObject;
            }
            
            GameObjectUtility.SetParentAndAlign(prefab, parent);
            Undo.RegisterCreatedObjectUndo(prefab, "Create " + prefab.name);
            Selection.activeObject = prefab;
        }
    }
}

#endif