#if UNITY_EDITOR

namespace Verve.Gameplay
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
            Handles.Label(transform.position + Vector3.up * 0.5f, $"速度：{m_Speed}\n半径：{m_Radius}");
        }

        /// <summary>
        /// 创建发射物
        /// </summary>
        [MenuItem("GameObject/Verve/Gameplay/Projectile", false, 10)]
        private static void CreateProjectile(MenuCommand menuCommand)
        {
            var root = new GameObject("Projectile");
            try
            {
                var projectile = root.AddComponent<Projectile>();

                projectile.m_Speed = 200.0f;
                projectile.m_Radius = 0.1f;
                projectile.m_Damage = 10.0f;
                projectile.m_Lifetime = 5.0f;

                var rb = root.GetComponent<Rigidbody>() ?? root.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = false;

                var mesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mesh.name = "Mesh";
                Undo.RegisterCreatedObjectUndo(mesh, $"Create {root.name} Mesh");
            
                DestroyImmediate(mesh.GetComponent<Collider>());
                mesh.transform.SetParent(root.transform);
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
                
                GameObjectUtility.SetParentAndAlign(root, parent);
                Undo.RegisterCreatedObjectUndo(root, "Create " + root.name);
                Selection.activeObject = root;
            }
            catch (Exception e)
            {
                DestroyImmediate(root);
                Debug.LogException(e);
            }
        }
    }
}

#endif