#if UNITY_EDITOR

namespace Verve.Gameplay
{
    using System;
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using Object = UnityEngine.Object;
    
    
    [AddComponentMenu("Verve/Gameplay/PlayerStart"), DisallowMultipleComponent, ExecuteAlways]
    public partial class PlayerStart
    {
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.1f);
            Gizmos.DrawWireSphere(transform.position, m_CheckRadius);
            Handles.Label(transform.position + Vector3.up * 0.5f, $"出生点：{ID}\n半径：{m_CheckRadius}", 
                new GUIStyle { normal = { textColor = Color.red }, fontSize = 14 });
        }

        /// <summary>
        /// 创建玩家出生点
        /// </summary>
        [MenuItem("GameObject/Verve/Gameplay/PlayerStart", false, 10)]
        private static void CreatePlayerStart(MenuCommand menuCommand)
        {
            var prefab = new GameObject("PlayerStart");
            try
            {
                var playerStart = prefab.AddComponent<PlayerStart>();

                playerStart.m_CheckRadius = 1.0f;
                
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
            catch (Exception e)
            {
                DestroyImmediate(prefab);
                Debug.LogException(e);
            }
        }
    }
}

#endif