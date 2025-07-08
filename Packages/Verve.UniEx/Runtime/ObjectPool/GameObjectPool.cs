#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Pool
{
    using Verve.Pool;
    using UnityEngine;
    
    
    /// <summary>
    /// 游戏对象池
    /// </summary>
    [System.Serializable]
    public class GameObjectPool : ObjectPool<GameObject>
    {
        private readonly Transform m_Parent;
        private readonly GameObject m_Prefab;
        
        
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
                maxCapacity: maxCapacity)
        {
            m_Prefab = prefab;
            m_Parent = parent;
        }

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