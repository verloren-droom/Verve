namespace Verve.Event
{
    
    using System;
    using System.Threading;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    /// 变量代理，用于监听值改变
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public partial class PropertyProxy<T> : INotifyPropertyChanged
    {
        protected T m_Value;
        protected Func<T, T, bool> m_Comparer;
        private event PropertyChangedEventHandler m_PropertyChanged;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => Interlocked.CompareExchange(ref m_PropertyChanged, (PropertyChangedEventHandler)Delegate.Combine(m_PropertyChanged, value), null);
            remove => Interlocked.CompareExchange(ref m_PropertyChanged, (PropertyChangedEventHandler)Delegate.Remove(m_PropertyChanged, value), null);
        }

        /// <summary>
        /// 当前存入的值
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

        public PropertyProxy(Func<T, T, bool> comparer = null)
        {
            m_Comparer = comparer ?? ((v1, v2) => v1?.Equals(v2) ?? false);
        }
        
        public PropertyProxy(T defaultValue, Func<T, T, bool> comparer = null) : this(comparer)
        {
            m_Value = defaultValue;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            m_PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString() => m_Value?.ToString() ?? "null";

        public void AddListener(PropertyChangedEventHandler propertyChanged)
        {
            m_PropertyChanged += propertyChanged;
        }
        
        public void RemoveListener(PropertyChangedEventHandler propertyChanged)
        {
            m_PropertyChanged -= propertyChanged;
        }

        public void RemoveAllListeners()
        {
            m_PropertyChanged = null;
        }
    }
}