namespace Verve.MVC
{
    using System;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;
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
        
#if UNITY_5_3_OR_NEWER
        protected void AddEventTrigger(EventTrigger eventTrigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            if (eventTrigger == null || callback == null) return;
            var entry = System.Array.Find(eventTrigger.triggers.ToArray(), 
                e => e.eventID == type);

            if (entry == null)
            {
                entry = new EventTrigger.Entry { eventID = type };
                eventTrigger.triggers.Add(entry);
            }

            entry.callback.AddListener(callback);
        }

        protected void AddEventTrigger(MaskableGraphic graphic, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            AddEventTrigger(graphic?.gameObject.GetComponent<EventTrigger>() ?? graphic?.gameObject.AddComponent<EventTrigger>(), type, callback);
        }
        
        protected void RemoveEventTrigger(EventTrigger eventTrigger, EventTriggerType type)
        {
            if (eventTrigger == null) return;
            eventTrigger.triggers.RemoveAll(entry => entry.eventID == type);
        }
        
        protected void RemoveEventTrigger(EventTrigger eventTrigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            if (eventTrigger == null || callback == null) return;

            foreach (var entry in eventTrigger.triggers)
            {
                if (entry.eventID == type)
                {
                    entry.callback.RemoveListener(callback);
                }
            }
        }
        
        protected void RemoveEventTrigger(MaskableGraphic graphic, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            if (graphic == null || !graphic.TryGetComponent<EventTrigger>(out var eventTrigger)) return;

            if (callback == null)
            {
                RemoveEventTrigger(eventTrigger, type);
            }
            else
            {
                RemoveEventTrigger(eventTrigger, type, callback);
            }
        }
        
        /// <summary>
        /// 清除所有事件监听
        /// </summary>
        public void ClearAllTriggers(EventTrigger eventTrigger)
        {
            if (eventTrigger != null)
            {
                foreach (var entry in eventTrigger.triggers)
                {
                    entry.callback.RemoveAllListeners();
                }
                eventTrigger.triggers.Clear();
            }
        }
        
        public void ClearAllTriggers(MaskableGraphic graphic)
        {
            if (graphic == null || !graphic.TryGetComponent<EventTrigger>(out var eventTrigger)) return;

            foreach (var entry in eventTrigger.triggers)
            {
                entry.callback.RemoveAllListeners();
            }
            eventTrigger.triggers.Clear();
        }

        /// <summary>
        /// 批量添加多个事件类型
        /// </summary>
        public void AddMultipleEvents(
            EventTrigger eventTrigger,
            params (EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)[] events)
        {
            foreach (var e in events)
            {
                AddEventTrigger(eventTrigger, e.type, e.callback);
            }
        }
#endif
        
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(m_ViewName))
            {
                m_ViewName = gameObject.name;
            }
        }
#endif
    }
}