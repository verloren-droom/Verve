namespace Verve.MVC
{
    using System;
    using UnityEngine;
    using System.Text.RegularExpressions;

    
    public interface IView
    {
        string ViewName { get; }
        void Open();
        void Close();
        event Action<IView> OnOpened;
        event Action<IView> OnClosed;
    }

    
    [DisallowMultipleComponent]
    public abstract class ViewBase : MonoBehaviour, IView
    {
        [SerializeField, PropertyDisable]
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