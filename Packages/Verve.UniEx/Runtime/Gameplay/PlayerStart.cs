#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Gameplay
{
    using Verve;
    using System;
    using UnityEngine;
    using System.Threading;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 玩家出生点标记组件
    /// </summary>
    public partial class PlayerStart : MonoBehaviour
    {
        [SerializeField, ReadOnly] private int m_ID;
        [SerializeField, Tooltip("用于检测出生点是否被占用的半径"), Min(0.01f)] private float m_CheckRadius = 1f;
        [SerializeField, Tooltip("需要检测的层掩码")] private LayerMask m_CheckLayers = ~0;
        [SerializeField, Tooltip("是否有效生成点"), ReadOnly] private bool m_IsValidSpawnPoint;
        [SerializeField, Tooltip("是否被占用"), ReadOnly] private bool m_IsOccupied;
        
        
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
        
        private static readonly HashSet<PlayerStart> s_PlayerStarts = new HashSet<PlayerStart>();
        public static IReadOnlyCollection<PlayerStart> AllPlayerStarts => s_PlayerStarts;

        public static event Action OnCreatePlayerStart;


        private void Awake()
        {
            m_ID = GenerateID();
        }

        private void OnEnable()
        {
            s_PlayerStarts.Add(this);
            OnCreatePlayerStart?.Invoke();
        }
        
        private void OnDisable()
        {
            s_PlayerStarts.Remove(this);
        }

        [Button("检查是否为有效生成点")]
        public bool CheckIsValidSpawnPoint()
        {
            Collider[] colliders = Physics.OverlapSphere(
                transform.position, 
                m_CheckRadius, 
                m_CheckLayers
            );
            
            m_IsValidSpawnPoint = true;
            for (int i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider == null) continue;
                if (collider.gameObject != gameObject && !collider.isTrigger)
                {
                    m_IsValidSpawnPoint = false;
                    break;
                }
            }
            
            return m_IsValidSpawnPoint;
        }
    }
}

#endif