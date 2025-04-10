namespace Verve.MVC
{
    using System;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
#elif GODOT
    using Godot;
#endif
    using System.Text.RegularExpressions;

    
    /// <summary>
    /// MVC视图接口
    /// </summary>
    public interface IView : IBelongToActivity
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
    public abstract partial class ViewBase : MonoBehaviour, IView
#elif GODOT
    [Tool]
    public abstract class GodotViewBase : Node, IView
#else
    public abstract class ViewBase : IView
#endif
    {
#if UNITY_5_3_OR_NEWER
        [SerializeField, PropertyDisable]
#elif GODOT
        [Export]
#endif
        private string m_ViewName;

        public virtual string ViewName
        {
            get => m_ViewName ??= 
                Regex.Replace(
#if UNITY_5_3_OR_NEWER
                    gameObject.name
#elif GODOT
                GetName()
#endif
                    , @"View$", string.Empty, RegexOptions.IgnoreCase);
            set => m_ViewName = value;
        } 
    
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
#if UNITY_5_3_OR_NEWER
            Destroy(gameObject);
#elif GODOT
            if (IsInsideTree())
            {
                QueueFree();
            }
#endif
        }

        public abstract IActivity Activity { get; set; }
    }
}