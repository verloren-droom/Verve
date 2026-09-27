#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.ComponentModel;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>变量代理；监听值变化。</para>
    /// </summary>
    /// <typeparam name="T">变量类型。</typeparam>
    [Serializable]
    public partial class PropertyProxy<T> : INotifyPropertyChanged, IEquatable<PropertyProxy<T>>
    {
        /// <summary>
        ///   <para>值。</para>
        /// </summary>
        [SerializeField, Tooltip("当前值")] protected T m_Value;
        /// <summary>
        ///   <para>值比较委托。</para>
        /// </summary>
        protected Func<T, T, bool> m_Comparer;
        
        /// <summary>
        ///   <para>值变化通知参数；复用同一实例。</para>
        /// </summary>
        private static readonly PropertyChangedEventArgs s_ValueChangedEventArgs = new(nameof(Value));

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///   <para>值变化事件。</para>
        /// </summary>
        public event Action<T> ValueChanged;

        /// <summary>
        ///   <para>当前存入的值。</para>
        /// </summary>
        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Value;
            set
            {
                if (m_Comparer?.Invoke(value, m_Value) ?? EqualityComparer<T>.Default.Equals(value, m_Value))
                    return;
                    
                m_Value = value;
                
                ValueChanged?.Invoke(value);
                PropertyChanged?.Invoke(this, s_ValueChangedEventArgs);
            }
        }

        /// <summary>
        ///   <para>创建变量代理。</para>
        /// </summary>
        /// <param name="defaultValue">默认值。</param>
        /// <param name="comparer">比较函数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PropertyProxy(T defaultValue = default, Func<T, T, bool> comparer = null)
        {
            m_Value = defaultValue;
            m_Comparer = comparer;
        }
        
        /// <summary>
        ///   <para>设置值；不触发变化事件。</para>
        /// </summary>
        /// <param name="value">新值。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValueWithoutEvent(T value) => m_Value = value;
        
        /// <summary>
        ///   <para>移除所有监听。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAllListeners()
        {
            PropertyChanged = null;
            ValueChanged = null;
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(PropertyProxy<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<T>.Default.Equals(m_Value, other.m_Value);
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
            => Equals(obj as PropertyProxy<T>);
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() 
            => m_Value?.GetHashCode() ?? 0;
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
            => m_Value?.ToString() ?? "null";
        
        /// <summary>
        ///   <para>转换代理值。</para>
        /// </summary>
        /// <param name="proxy">代理。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(PropertyProxy<T> proxy) => proxy.m_Value;
        
        /// <summary>
        ///   <para>转换代理值。</para>
        /// </summary>
        /// <param name="value">值。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PropertyProxy<T>(T value) => new PropertyProxy<T>(value);
    }
}

#endif