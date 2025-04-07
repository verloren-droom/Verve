namespace Verve.MVC
{
    using System;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
#endif
    using System.Text.RegularExpressions;

    
    /// <summary>
    /// MVC视图接口
    /// </summary>
    public interface IView
    {
        /// <summary>
        /// 视图名
        /// </summary>
        string ViewName { get; }
        void Open();
        void Close();
        event Action<IView> OnOpened;
        event Action<IView> OnClosed;
    }

    
    /// <summary>
    /// MVC视图基类
    /// </summary>
#if UNITY_5_3_OR_NEWER
    [DisallowMultipleComponent]
#endif
    public abstract class ViewBase : MonoBehaviour, IView
    {
#if UNITY_5_3_OR_NEWER
        [SerializeField, PropertyDisable]
#endif
        private string m_ViewName;
        public virtual string ViewName => m_ViewName ??= 
            Regex.Replace(name, @"View$", string.Empty, RegexOptions.IgnoreCase);
    
        public event Action<IView> OnOpened;
        public event Action<IView> OnClosed;
        
        protected virtual void OnOpening() { }
        protected virtual void OnClosing() { }

        public void Open()
        {
            OnOpening();
            OnOpened?.Invoke(this);
        }

        public void Close()
        {
            OnClosing();
            OnClosed?.Invoke(this);
            Destroy(gameObject);
        }
    }
}