#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Gameplay
{
    using System;
    using System.Linq;
    using UnityEngine;
    using System.Threading;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 玩家出生点 - 用于生成玩家
    /// </summary>
    [Serializable]
    public partial class PlayerStart : MonoBehaviour
    {
        [SerializeField, ReadOnly] private int m_ID;
        [SerializeField, Tooltip("用于检测出生点是否被占用的半径"), Min(0.01f)] private float m_CheckRadius = 1f;
        [SerializeField, Tooltip("需要检测的层掩码")] private LayerMask m_CheckLayers = ~0;
        [SerializeField, Tooltip("是否有效生成点"), ReadOnly] private bool m_IsValidSpawnPoint;
        [SerializeField, Tooltip("是否被占用"), ReadOnly] private bool m_IsOccupied;
        
        private Collider[] m_ColliderBuffer = new Collider[8];
        
        public int ID => m_ID;
        /// <summary> 检查半径 </summary>
        public float CheckRadius => m_CheckRadius;
        /// <summary> 是否为有效 </summary>
        public bool IsValidSpawnPoint => m_IsValidSpawnPoint;
        /// <summary> 是否被占用 </summary>
        public bool IsOccupied
        {
            get => m_IsOccupied;
            set
            {
                if (m_IsOccupied == value) return;
                m_IsOccupied = value;
            }
        }


        private static int s_NextSpawnID;
        private static int GenerateID() => Interlocked.Increment(ref s_NextSpawnID);
        
        private static readonly HashSet<PlayerStart> s_ActivePlayerStarts = new HashSet<PlayerStart>();
        /// <summary> 所有有效生成点 </summary>
        public static IReadOnlyCollection<PlayerStart> ActivePlayerStarts => s_ActivePlayerStarts;
        

        private void Awake()
        {
            m_ID = GenerateID();
        }

        private void OnEnable()
        {
            s_ActivePlayerStarts.Add(this);
        }
        
        private void OnDisable()
        {
            s_ActivePlayerStarts.Remove(this);
        }

        [Button("检查是否为有效生成点")]
        public bool CheckIsValidSpawnPoint()
        {
            m_ColliderBuffer ??= new Collider[8];
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position, 
                m_CheckRadius, 
                m_ColliderBuffer, 
                m_CheckLayers
            );
    
            m_IsValidSpawnPoint = true;
            for (int i = 0; i < hitCount; i++)
            {
                if (m_ColliderBuffer[i].gameObject != gameObject && !m_ColliderBuffer[i].isTrigger)
                {
                    m_IsValidSpawnPoint = false;
                    break;
                }
            }
            return m_IsValidSpawnPoint;
        }
        
        /// <summary> 找到第一个有效生成点 </summary>
        public static PlayerStart FindValidPlayerStart()
            => ActivePlayerStarts
                .Where(start => start.CheckIsValidSpawnPoint())
                .FirstOrDefault(start => !start.IsOccupied);
        
        /// <summary> 找到距离指定位置最近的有效生成点 </summary>
        public static PlayerStart FindNearValidPlayerStart(Vector3 position)
            => ActivePlayerStarts
                .Where(start => start.CheckIsValidSpawnPoint())
                .OrderBy(start => Vector3.Distance(position, start.transform.position))
                .FirstOrDefault(start => !start.IsOccupied);
    }
}

#endif