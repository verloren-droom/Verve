#if UNITY_EDITOR

namespace Verve.UniEx.Gameplay
{
    using System;
    using UnityEditor;
    using UnityEngine;
    
    
    [AddComponentMenu("Verve/Gameplay/Character/AIController"), DisallowMultipleComponent, ExecuteAlways]
    public partial class AIController
    {
        /// <summary>
        /// 创建AI
        /// </summary>
        [MenuItem("GameObject/Verve/Gameplay/Character/AI", false, 10)]
        private static void CreateAI(MenuCommand menuCommand)
        {
            var aiRoot = new GameObject("AI");

            try
            {
                var capsuleCollider = aiRoot.AddComponent<CapsuleCollider>();
                var rigidbody = aiRoot.AddComponent<Rigidbody>();
                aiRoot.AddComponent<AIPawn>();
                aiRoot.AddComponent<AIController>();
                
                rigidbody.mass = 75f;
                rigidbody.drag = 0.1f;
                rigidbody.angularDrag = 0.05f;
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rigidbody.constraints = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationX;
            
                capsuleCollider.radius = 0.5f;
                capsuleCollider.height = 2f;
                capsuleCollider.center = new Vector3(0, 1.0f, 0);
                
                GameObject meshContainer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                meshContainer.name = "Mesh";
                Undo.RegisterCreatedObjectUndo(meshContainer, "Create Player Mesh");
                
                DestroyImmediate(meshContainer.GetComponent<Collider>());
                meshContainer.transform.SetParent(aiRoot.transform);
                meshContainer.transform.localPosition = new Vector3(0, 1.0f, 0);
                
                Undo.RegisterCreatedObjectUndo(aiRoot, "Create AI");
                Selection.activeObject = aiRoot;
            }
            catch (Exception e)
            {
                DestroyImmediate(aiRoot);
                Debug.LogException(e);
            }
        }
    }
}

#endif