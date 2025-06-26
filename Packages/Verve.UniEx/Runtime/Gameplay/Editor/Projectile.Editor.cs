#if UNITY_EDITOR

namespace VerveUniEx.Gameplay
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using UnityEditor.SceneManagement;
    
    
    [AddComponentMenu("Verve/Gameplay/Projectile"), DisallowMultipleComponent]
    public partial class Projectile
    {
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, m_Radius);
        }

        /// <summary>
        /// 创建发射物
        /// </summary>
        [MenuItem("GameObject/Verve/Gameplay/Projectile", false, 10)]
        private static void CreateProjectile(MenuCommand menuCommand)
        {
            var prefab = new GameObject("Projectile");
            try
            {
                var projectile = prefab.AddComponent<Projectile>();

                projectile.m_Speed = 200.0f;
                projectile.m_Radius = 0.1f;
                projectile.m_Damage = 10.0f;
                projectile.m_Lifetime = 5.0f;
                
                var mesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mesh.name = "Mesh";
                Undo.RegisterCreatedObjectUndo(mesh, "Create Projectile Mesh");
            
                DestroyImmediate(mesh.GetComponent<Collider>());
                mesh.transform.SetParent(prefab.transform);
                mesh.transform.localScale = Vector3.one * 0.1f;

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