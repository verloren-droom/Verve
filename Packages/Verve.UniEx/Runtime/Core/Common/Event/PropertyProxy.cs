#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Threading;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    ///   <para>变量代理</para>
    ///   <para>用于监听值改变</para>
    /// </summary>
    /// <typeparam name="T">变量类型</typeparam>
    [Serializable]
    public partial class PropertyProxy<T> : INotifyPropertyChanged
    {
        [SerializeField] protected T m_Value;
        protected Func<T, T, bool> m_Comparer;
        private event PropertyChangedEventHandler m_PropertyChanged;

        /// <summary>
        ///   <para>属性改变事件</para>
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add => Interlocked.CompareExchange(ref m_PropertyChanged, (PropertyChangedEventHandler)Delegate.Combine(m_PropertyChanged, value), null);
            remove => Interlocked.CompareExchange(ref m_PropertyChanged, (PropertyChangedEventHandler)Delegate.Remove(m_PropertyChanged, value), null);
        }

        /// <summary>
        ///   <para>当前存入的值</para>
        /// </summary>
        public T Value
        {
            get => m_Value;
            set
            {
                if (m_Comparer(value, m_Value)) return;
                m_Value = value;
                OnPropertyChanged(nameof(m_Value));
            }
        }

        /// <summary>
        ///   <para>构造函数</para>
        /// </summary>
        /// <param name="comparer">比较函数</param>
        public PropertyProxy(Func<T, T, bool> comparer = null)
        {
            m_Comparer = comparer ?? ((v1, v2) => v1?.Equals(v2) ?? false);
        }
        
        /// <summary>
        ///   <para>构造函数</para>
        /// </summary>
        /// <param name="defaultValue">默认值</param>
        /// <param name="comparer">比较函数</param>
        public PropertyProxy(T defaultValue, Func<T, T, bool> comparer = null) : this(comparer)
        {
            m_Value = defaultValue;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            m_PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString() => m_Value?.ToString() ?? "null";

        /// <summary>
        ///   <para>添加监听</para>
        /// </summary>
        /// <param name="propertyChanged">监听函数</param>
        public void AddListener(PropertyChangedEventHandler propertyChanged)
        {
            m_PropertyChanged += propertyChanged;
        }
        
        /// <summary>
        ///   <para>移除监听</para>
        /// </summary>
        /// <param name="propertyChanged">监听函数</param>
        public void RemoveListener(PropertyChangedEventHandler propertyChanged)
        {
            m_PropertyChanged -= propertyChanged;
        }

        /// <summary>
        ///   <para>移除所有监听</para>
        /// </summary>
        public void RemoveAllListeners()
        {
            m_PropertyChanged = null;
        }
        
        public static implicit operator T(PropertyProxy<T> proxy) => proxy.Value;
    }
}

#endif