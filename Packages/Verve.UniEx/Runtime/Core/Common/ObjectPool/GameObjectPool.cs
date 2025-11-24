#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    ///   <para>游戏对象对象池</para>
    /// </summary>
    [Serializable]
    public sealed class GameObjectPool : ObjectPool<GameObject>
    {
        /// <summary>
        ///   <para>游戏对象对象池构造函数</para>
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="parent">父级</param>
        /// <param name="preSize">预创建数量</param>
        /// <param name="capacity">最大容量</param>
        public GameObjectPool(
            GameObject prefab, 
            Transform parent = null,
            int preSize = 5, 
            int capacity = 20) 
            : base(
                onCreateObject: () => GameObject.Instantiate(prefab, parent),
                onGetFromPool: (go) => go.SetActive(true),
                onReleaseToPool: (go) => 
                {
                    go.SetActive(false);
                    go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                },
                onDestroyObject: GameObject.Destroy,
                preSize: preSize,
                capacity: capacity) { }

        /// <summary>
        ///   <para>获取游戏对象</para>
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="rotation">旋转</param>
        /// <returns>
        ///   <para>游戏对象实例</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameObject Get(Vector3 position, Quaternion rotation, Predicate<GameObject> predicate = null)
        {
            var obj = Get(predicate);
            obj?.transform.SetPositionAndRotation(position, rotation);
            return obj;
        }

        /// <summary>
        ///   <para>尝试获取游戏对象</para>
        /// </summary>
        /// <param name="obj">游戏对象实例</param>
        /// <param name="position">位置</param>
        /// <param name="rotation">旋转</param>
        /// <returns>
        ///   <para>是否成功</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(out GameObject obj, Vector3 position, Quaternion rotation, Predicate<GameObject> predicate = null)
        {
            obj = Get(position, rotation, predicate);
            return obj != null;
        }
    }
}

#endif