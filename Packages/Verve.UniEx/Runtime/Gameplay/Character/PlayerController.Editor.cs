#if UNITY_EDITOR

namespace VerveUniEx.Gameplay
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEditor.SceneManagement;
    
    
    [AddComponentMenu("Verve/Gameplay/Character/PlayerController"), DisallowMultipleComponent, ExecuteAlways]
    public partial class PlayerController
    {
        /// <summary>
        /// 创建玩家实体（包含Pawn和Controller）
        /// </summary>
        [MenuItem("GameObject/Verve/Gameplay/Character/Player", false, 10)]
        private static void CreatePlayer(MenuCommand menuCommand)
        {
            GameObject playerRoot = new GameObject("Player (Clone)")
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
                
                // 创建和初始化相机
                GameObject cameraGO = new GameObject("Player Camera");
                Undo.RegisterCreatedObjectUndo(cameraGO, "Create Player Camera");
            
                cameraGO.transform.SetParent(playerRoot.transform);
                cameraGO.transform.localPosition = new Vector3(0, 1.5f, -5f);
                cameraGO.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
            
                var cameraComp = cameraGO.AddComponent<Camera>();
                cameraComp.fieldOfView = 75f;
                cameraComp.nearClipPlane = 0.3f;
                cameraComp.farClipPlane = 1000f;
                cameraComp.cullingMask = ~LayerMask.GetMask("Ignore Raycast");
            
                pawn.PlayerCamera = cameraComp;
                
                SetParentObject(playerRoot, menuCommand);
                
                Undo.RegisterCreatedObjectUndo(playerRoot, "Create Player");
                Selection.activeObject = playerRoot;
            }
            catch (Exception e)
            {
                DestroyImmediate(playerRoot);
                Debug.LogException(e);
            }
        }

        
        /// <summary>
        /// 设置父级对象
        /// </summary>
        private static void SetParentObject(GameObject obj, MenuCommand menuCommand)
        {
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
            
            if (parent != null)
            {
                GameObjectUtility.SetParentAndAlign(obj, parent);
            }
        }
        
        // [Button("保存为预制体")]
        // private void SaveAsPrefab()
        // {
        //     string path = EditorUtility.SaveFilePanelInProject(
        //         "保存玩家预制体",
        //         "Player.prefab",
        //         "prefab",
        //         "另存为"
        //     );
        //
        //     if (string.IsNullOrEmpty(path)) return;
        //
        //     GameObject prefab = gameObject;
        //     GameObject prefabRoot = PrefabUtility.SaveAsPrefabAsset(prefab, path);
        //
        //     if (prefabRoot != null)
        //     {
        //         Undo.DestroyObjectImmediate(gameObject);
        //         GameObject newInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabRoot);
        //         Undo.RegisterCreatedObjectUndo(newInstance, "Replace with Prefab");
        //         Selection.activeObject = newInstance;
        //     }
        //
        //     AssetDatabase.Refresh();
        // }
    }
}

#endif