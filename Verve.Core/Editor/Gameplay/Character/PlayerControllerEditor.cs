#if UNITY_EDITOR

namespace Verve.Editor.Gameplay
{
    using System;
    using UnityEngine;
    using UnityEditor;
    using Verve.Gameplay;

    
    /// <summary>
    ///   <para>玩家控制器编辑器</para>
    /// </summary>
    [CustomEditor(typeof(PlayerController))]
    public class PlayerControllerEditor : Editor
    {
        /// <summary>
        ///   <para>创建玩家实体（包含Pawn和Controller）</para>
        /// </summary>
        [MenuItem("GameObject/Verve/Gameplay/Character/Player", false, 10)]
        private static void CreatePlayer(MenuCommand menuCommand)
        {
            GameObject playerRoot = new GameObject()
            {
                tag = "Player"
            };

            try
            {
                // 添加必要组件
                var capsuleCollider = playerRoot.AddComponent<CapsuleCollider>();
                var rigidbody = playerRoot.AddComponent<Rigidbody>();
                var movement = playerRoot.AddComponent<PlayerMovement>();
                var pawn = playerRoot.AddComponent<PlayerPawn>();
#if UNITY_2019_4_OR_NEWER && ENABLE_INPUT_SYSTEM
                playerRoot.AddComponent(Type.GetType("UnityEngine.InputSystem.PlayerInput, Unity.InputSystem"));
#endif
            
                rigidbody.mass = 75f;
                rigidbody.drag = 0.1f;
                rigidbody.angularDrag = 0.05f;
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rigidbody.constraints = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationX;
            
                capsuleCollider.radius = 0.5f;
                capsuleCollider.height = 2f;
                capsuleCollider.center = new Vector3(0, 1.0f, 0);
                
                pawn.Movement = movement;
            
                // 创建临时模型
                GameObject meshContainer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                meshContainer.name = "Mesh";
                Undo.RegisterCreatedObjectUndo(meshContainer, "Create Player Mesh");
            
                DestroyImmediate(meshContainer.GetComponent<Collider>());
                meshContainer.transform.SetParent(playerRoot.transform);
                meshContainer.transform.localPosition = new Vector3(0, 1.0f, 0);

                var controller = playerRoot.AddComponent<PlayerController>();
            
                controller.Possess(pawn);

                GameObject cameraGO = new GameObject("Player Camera");
                Undo.RegisterCreatedObjectUndo(cameraGO, "Create Player Camera");
            
                cameraGO.transform.SetParent(playerRoot.transform);
                cameraGO.transform.localPosition = new Vector3(0, 1.5f, -5f);
                cameraGO.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
                
                // 创建和初始化相机
                if (Type.GetType("Cinemachine.CinemachineVirtualCamera, Cinemachine") != null)
                {
                    cameraGO.name = "Virtual Camera";
                    cameraGO.AddComponent(Type.GetType("Cinemachine.CinemachineVirtualCamera, Cinemachine"));
                    pawn.PlayerCamera = Camera.main;
                }
                else
                {
                    var cameraComp = cameraGO.AddComponent<Camera>();
                    cameraComp.fieldOfView = 75f;
                    cameraComp.nearClipPlane = 0.3f;
                    cameraComp.farClipPlane = 1000f;
                    cameraComp.cullingMask = ~LayerMask.GetMask("Ignore Raycast");
                    pawn.PlayerCamera = cameraComp;
                }

                var parent = CoreEditorUtility.GetParentObject(menuCommand);
                
                if (parent != null)
                {
                    GameObjectUtility.SetParentAndAlign(playerRoot, parent);
                }
                
                playerRoot.name = GameObjectUtility.GetUniqueNameForSibling(parent?.transform, "Player");
                
                Undo.RegisterCreatedObjectUndo(playerRoot, "Create Player");
                Selection.activeObject = playerRoot;
            }
            catch (Exception e)
            {
                DestroyImmediate(playerRoot);
                Debug.LogException(e);
            }
        }
    }
}

#endif