#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Gameplay
{
    using System;
    using UnityEngine;
    using System.Collections.Generic;

    
    /// <summary>
    /// AI巡逻路径 - 包含多个巡逻点
    /// </summary>
    [Serializable]
    public partial class PatrolPath : MonoBehaviour
    {
        [Serializable]
        public class PatrolPoint
        {
            [Tooltip("巡逻点位置")] public Vector3 Position;
            [Tooltip("停留时间(秒)"), Min(0)] public float WaitTime = 1f;
            [Tooltip("到达判定半径"), Min(0.1f)] public float Radius = 0.5f;
            public PatrolPoint(Vector3 position) => Position = position;
        }
        
        
        [SerializeField, Tooltip("巡逻点列表")] private List<PatrolPoint> m_Points = new List<PatrolPoint>();
        [SerializeField, Tooltip("是否循环路径")] private bool m_Loop = true;
        
        public IReadOnlyList<PatrolPoint> Points => m_Points;
        public bool Loop => m_Loop;
        

        /// <summary> 添加新巡逻点 </summary>
        public void AddPoint(Vector3 position) => m_Points.Add(new PatrolPoint(position));
        
        /// <summary> 删除指定索引的点 </summary>
        public void RemovePoint(int index) 
        {
            if (index >= 0 && index < m_Points.Count)
                m_Points.RemoveAt(index);
        }
        
        /// <summary> 获取指定索引的点 </summary>
        public PatrolPoint GetPoint(int index) 
            => (index >= 0 && index < m_Points.Count) ? m_Points[index] : null;
        
        /// <summary> 获取下一个点索引 </summary>
        public int GetNextIndex(int currentIndex)
        {
            if (m_Points.Count == 0) return -1;
            int next = currentIndex + 1;
            return m_Loop ? next % m_Points.Count : (next < m_Points.Count ? next : -1);
        }
        
        /// <summary> 获取世界空间位置 </summary>
        public Vector3 GetWorldPosition(int index)
        {
            var point = GetPoint(index);
            return point != null ? transform.TransformPoint(point.Position) : Vector3.zero;
        }
    }
}

#endif
