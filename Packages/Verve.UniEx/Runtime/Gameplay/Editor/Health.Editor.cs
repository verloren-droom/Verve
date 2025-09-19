#if UNITY_EDITOR

namespace Verve.UniEx.Gameplay
{
    using System;
    using UnityEditor;
    using UnityEngine;
    
    
    [AddComponentMenu("Verve/Gameplay/Health"), DisallowMultipleComponent]
    public partial class Health
    {
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Handles.Label(transform.position + Vector3.up * 0.5f, $"当前生命值：{m_CurrentHealth}\n最大生命值：{m_MaxHealth}");
        }
    }
}

#endif