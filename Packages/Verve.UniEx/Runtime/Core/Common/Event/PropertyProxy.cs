#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Threading;
    using System.ComponentModel;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>变量代理</para>
    ///   <para>用于监听值改变</para>
    /// </summary>
    /// <typeparam name="T">变量类型</typeparam>
    [Serializable]
    public partial class PropertyProxy<T> : INotifyPropertyChanged, IEquatable<PropertyProxy<T>>
    {
        [SerializeField, Tooltip("当前值")] protected T m_Value;
        protected Func<T, T, bool> m_Comparer;
        
        private PropertyChangedEventHandler m_PropertyChanged;
        private Action<T> m_ValueChanged;
        
        private static readonly PropertyChangedEventArgs s_ValueChangedEventArgs = new PropertyChangedEventArgs(nameof(Value));
        
        /// <summary>
        ///   <para>属性改变事件</para>
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            add
            {
                var current = m_PropertyChanged;
                PropertyChangedEventHandler comparand;
                do
                {
                    comparand = current;
                    var newHandler = (PropertyChangedEventHandler)Delegate.Combine(comparand, value);
                    current = Interlocked.CompareExchange(ref m_PropertyChanged, newHandler, comparand);
                } while (current != comparand);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            remove
            {
                var current = m_PropertyChanged;
                PropertyChangedEventHandler comparand;
                do
                {
                    comparand = current;
                    var newHandler = (PropertyChangedEventHandler)Delegate.Remove(comparand, value);
                    current = Interlocked.CompareExchange(ref m_PropertyChanged, newHandler, comparand);
                } while (current != comparand);
            }
        }

        /// <summary>
        ///   <para>值改变事件</para>
        /// </summary>
        public event Action<T> ValueChanged
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            add
            {
                var current = m_ValueChanged;
                Action<T> comparand;
                do
                {
                    comparand = current;
                    var newHandler = (Action<T>)Delegate.Combine(comparand, value);
                    current = Interlocked.CompareExchange(ref m_ValueChanged, newHandler, comparand);
                } while (current != comparand);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            remove
            {
                var current = m_ValueChanged;
                Action<T> comparand;
                do
                {
                    comparand = current;
                    var newHandler = (Action<T>)Delegate.Remove(comparand, value);
                    current = Interlocked.CompareExchange(ref m_ValueChanged, newHandler, comparand);
                } while (current != comparand);
            }
        }

        /// <summary>
        ///   <para>当前存入的值</para>
        /// </summary>
        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Value;
            set
            {
                if (m_Comparer != null ? m_Comparer(value, m_Value) : 
                    (value is IEquatable<T> equatable ? equatable.Equals(m_Value) : 
                     EqualityComparer<T>.Default.Equals(value, m_Value)))
                    return;
                    
                m_Value = value;
                
                m_ValueChanged?.Invoke(value);
                m_PropertyChanged?.Invoke(this, s_ValueChangedEventArgs);
            }
        }

        /// <summary>
        ///   <para>变量代理构造函数</para>
        /// </summary>
        /// <param name="defaultValue">默认值</param>
        /// <param name="comparer">比较函数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PropertyProxy(T defaultValue = default, Func<T, T, bool> comparer = null)
        {
            m_Value = defaultValue;
            m_Comparer = comparer;
        }
        
        /// <summary>
        ///   <para>设置值（不触发事件）</para>
        /// </summary>
        /// <param name="value">新值</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValueWithoutEvent(T value)
        {
            m_Value = value;
        }
        
        /// <summary>
        ///   <para>添加值改变监听</para>
        /// </summary>
        /// <param name="propertyChanged">监听函数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddListener(PropertyChangedEventHandler propertyChanged)
        {
            PropertyChanged += propertyChanged;
        }
        
        /// <summary>
        ///   <para>添加值改变监听</para>
        /// </summary>
        /// <param name="valueChanged">监听函数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddListener(Action<T> valueChanged)
        {
            ValueChanged += valueChanged;
        }
        
        /// <summary>
        ///   <para>移除值改变监听</para>
        /// </summary>
        /// <param name="propertyChanged">监听函数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveListener(PropertyChangedEventHandler propertyChanged)
        {
            PropertyChanged -= propertyChanged;
        }
        
        /// <summary>
        ///   <para>移除值改变监听</para>
        /// </summary>
        /// <param name="valueChanged">监听函数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveListener(Action<T> valueChanged)
        {
            ValueChanged -= valueChanged;
        }
        
        /// <summary>
        ///   <para>移除所有监听</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAllListeners()
        {
            m_PropertyChanged = null;
            m_ValueChanged = null;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(PropertyProxy<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<T>.Default.Equals(m_Value, other.m_Value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
            => Equals(obj as PropertyProxy<T>);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() 
            => m_Value?.GetHashCode() ?? 0;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
            => m_Value?.ToString() ?? "null";
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(PropertyProxy<T> proxy) => proxy.m_Value;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PropertyProxy<T>(T value) => new PropertyProxy<T>(value);
    }
}

#endif