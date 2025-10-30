#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using UnityEngine;
    
    
    /// <summary>
    ///   <para>游戏对象对象池</para>
    /// </summary>
    [System.Serializable]
    public class GameObjectPool : ObjectPool<GameObject>
    {
        /// <summary>
        ///   <para>游戏对象对象池构造函数</para>
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="parent">父级</param>
        /// <param name="preSize">预创建数量</param>
        /// <param name="maxCapacity">最大容量</param>
        public GameObjectPool(
            GameObject prefab, 
            Transform parent = null,
            int preSize = 5, 
            int maxCapacity = 20) 
            : base(
                onCreateObject: () => Object.Instantiate(prefab, parent),
                onGetFromPool: (go) => go.SetActive(true),
                onReleaseToPool: (go) => 
                {
                    go.SetActive(false);
                    go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                },
                onDestroyObject: GameObject.Destroy,
                preSize: preSize,
                maxCapacity: maxCapacity) { }

        /// <summary>
        ///   <para>获取游戏对象</para>
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="rotation">旋转</param>
        /// <returns>
        ///   <para>游戏对象实例</para>
        /// </returns>
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            var obj = Get();
            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(position, rotation);
            }
            return obj;
        }
    }
}

#endif