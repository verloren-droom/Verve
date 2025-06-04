namespace Verve.AI
{
    using System;
    using System.Threading;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    /// AI共享数据存储
    /// </summary>
    [Serializable]
    public sealed class Blackboard : IDisposable
    {
        private struct BBEntry
        {
            public int KeyHash;
            public Type ValueType;
            public object Value;
        }

        
        private BBEntry[] m_Data;
        private int m_Count;
        private Dictionary<int, int> m_KeyIndexMap;
        public event Action<string, object> OnValueChanged;
        
        
        public Blackboard(int initialSize = 32)
        {
            m_Data = new BBEntry[initialSize];
            m_KeyIndexMap = new Dictionary<int, int>(initialSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue<T>(string key, in T value)
        {
            int hash = key.GetHashCode();
            
            if (m_KeyIndexMap.TryGetValue(hash, out var index))
            {
                ref var entry = ref m_Data[index];
                entry.Value = value;
                entry.ValueType = typeof(T);
            }
            else
            {
                if (m_Count >= m_Data.Length)
                {
                    Array.Resize(ref m_Data, m_Data.Length * 2);
                }
                
                m_Data[m_Count] = new BBEntry 
                {
                    KeyHash = hash,
                    ValueType = typeof(T),
                    Value = value
                };
                m_KeyIndexMap[hash] = m_Count;
                m_Count++;
            }
            
            OnValueChanged?.Invoke(key, value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue<T>(in T value)
        {
            SetValue(nameof(value), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetValue<T>(string key, T defaultValue = default)
        {
            int hash = key.GetHashCode();
            if (m_KeyIndexMap.TryGetValue(hash, out var index))
            {
                var entry = m_Data[index];
                if (entry.Value is T typedValue)
                {
                    return typedValue;
                }
                
                try
                {
                    return (T)Convert.ChangeType(entry.Value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(string key)
        {
            return m_KeyIndexMap.ContainsKey(key.GetHashCode());
        }

        public void Dispose()
        {
            Array.Clear(m_Data, 0, m_Count);
            m_KeyIndexMap.Clear();
        }
    }
}
