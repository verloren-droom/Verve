namespace Verve.AI
{
    using System;
    using System.Threading;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    
    
    /// <summary>
    /// AI共享数据存储
    /// </summary>
    [Serializable]
    public sealed class Blackboard : IBlackboard
    {
        [Serializable]
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        private struct BBEntry
        {
            [FieldOffset(0)]
            public int KeyHash;
            [FieldOffset(4)]
            public TypeCode ValueTypeCode;
            [FieldOffset(8)] private long _padding;
            [FieldOffset(16)]
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
            int hash = GetStableHash(key);
            
            if (m_KeyIndexMap.TryGetValue(hash, out var index))
            {
                ref var entry = ref m_Data[index];
                entry.Value = value;
                entry.ValueTypeCode = Type.GetTypeCode(typeof(T));
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
                    ValueTypeCode = Type.GetTypeCode(typeof(T)),
                    Value = value
                };
                m_KeyIndexMap[hash] = m_Count;
                m_Count++;
            }
            
            OnValueChanged?.Invoke(key, value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue<T>(string key, out T value, T defaultValue = default)
        {
            value = GetValue(key, defaultValue);
            return !Equals(value, defaultValue);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue<T>(in T value)
        {
            SetValue(nameof(value), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetValue<T>(string key, T defaultValue = default)
        {
            int hash = GetStableHash(key);
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
            return m_KeyIndexMap.ContainsKey(GetStableHash(key));
        }
        
        private int GetStableHash(string key)
        {
            unchecked {
                int hash = 23;
                foreach (char c in key)
                 hash = hash * 31 + c;
                return hash;
            }
        }

        public void Dispose()
        {
            Array.Clear(m_Data, 0, m_Count);
            m_KeyIndexMap.Clear();
        }
    }
}
