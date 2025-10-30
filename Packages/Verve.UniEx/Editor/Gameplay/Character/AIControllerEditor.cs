#if UNITY_EDITOR

namespace VerveEditor.Gameplay
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using Verve.UniEx.Gameplay;

    
    /// <summary>
    ///   <para>AI控制器编辑器</para>
    /// </summary>
    [CustomEditor(typeof(AIController))]
    public class AIControllerEditor : Editor
    {
        /// <summary>
        ///   <para>创建AI</para>
        /// </summary>
        [MenuItem("GameObject/Verve/Gameplay/Character/AI", false, 10)]
        private static void CreateAI(MenuCommand menuCommand)
        {
            var aiRoot = new GameObject();

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
                
                var parent = CoreEditorUtility.GetParentObject(menuCommand);
                
                if (parent != null)
                {
                    GameObjectUtility.SetParentAndAlign(aiRoot, parent);
                }
                
                aiRoot.name = GameObjectUtility.GetUniqueNameForSibling(parent?.transform, "AI");

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