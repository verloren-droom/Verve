namespace Verve.UniEx.AI
{
    using System;
    using Verve.AI;
    using System.Threading;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Collections.ObjectModel;
    using System.Runtime.CompilerServices;
    
    
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
            public int keyHash;
            [FieldOffset(4)]
            public TypeCode valueTypeCode;
            [FieldOffset(8)] private long _padding;
            [FieldOffset(16)]
            public object value;
        }

        
        private int m_ID;
        private BBEntry[] m_Data;
        private int m_Count;
        private Dictionary<int, int> m_KeyIndexMap;
        public event Action<string, object> OnValueChanged;
        public int ID => m_ID;

        private static int s_NextBBId; 
        public static int GenerateBBId() => Interlocked.Increment(ref s_NextBBId);
        
        private static readonly List<Blackboard> s_AllBlackboards = new();
        /// <summary> 所有黑板数据 </summary>
        public static IReadOnlyList<Blackboard> AllBlackboards => new ReadOnlyCollection<Blackboard>(s_AllBlackboards);


        public Blackboard(int initialSize = 32)
        {
            m_ID = GenerateBBId();
            m_Data = new BBEntry[initialSize];
            m_KeyIndexMap = new Dictionary<int, int>(initialSize);
            s_AllBlackboards.Add(this);
        }

        ~Blackboard() => Dispose();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue<T>(string key, in T value)
        {
            if (string.IsNullOrEmpty(key)) return;
            
            int hash = GetStableHash(key);
            if (m_KeyIndexMap.TryGetValue(hash, out var index))
            {
                ref var entry = ref m_Data[index];
                entry.value = value;
                entry.valueTypeCode = Type.GetTypeCode(typeof(T));
            }
            else
            {
                if (m_Count >= m_Data.Length)
                {
                    Array.Resize(ref m_Data, m_Data.Length * 2);
                }
                
                m_Data[m_Count] = new BBEntry 
                {
                    keyHash = hash,
                    valueTypeCode = Type.GetTypeCode(typeof(T)),
                    value = value
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
        public bool TryGetValue<T>(string key, out T value, T defaultValue = default)
        {
            value = GetValue(key, defaultValue);
            return !Equals(value, defaultValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetValue<T>(string key, T defaultValue = default)
        {
            int hash = GetStableHash(key);
            if (m_KeyIndexMap.TryGetValue(hash, out var index))
            {
                var entry = m_Data[index];
                if (entry.value is T typedValue)
                {
                    return typedValue;
                }
                
                try
                {
                    return (T)Convert.ChangeType(entry.value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveValue(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            
            int hash = GetStableHash(key);
            if (!m_KeyIndexMap.TryGetValue(hash, out int index)) return;
        
            int lastIndex = m_Count - 1;
            if (index != lastIndex)
            {
                m_Data[index] = m_Data[lastIndex];
                m_KeyIndexMap[m_Data[index].keyHash] = index;
            }
        
            m_Data[lastIndex] = default;
            m_Count--;
            m_KeyIndexMap.Remove(hash);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasValue(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return m_KeyIndexMap.ContainsKey(GetStableHash(key));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            s_AllBlackboards.Remove(this);
        }
    }
}
