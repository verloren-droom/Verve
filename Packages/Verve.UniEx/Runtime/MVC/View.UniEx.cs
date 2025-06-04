#if UNITY_5_3_OR_NEWER
    
namespace VerveUniEx.MVC
{
    using Verve;
    using System;
    using Verve.MVC;
    using System.Text.RegularExpressions;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    
    
    /// <summary>
    /// MVC视图基类
    /// </summary>
    [DisallowMultipleComponent]
    public abstract partial class ViewBase : MonoBehaviour, IView
    {
        [SerializeField, PropertyDisable] private string m_ViewName;

        public virtual string ViewName
        {
            get => m_ViewName ??= 
                Regex.Replace(gameObject.name, @"View$", string.Empty, RegexOptions.IgnoreCase);
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
            Destroy(gameObject);
        }

        public abstract IActivity Activity { get; set; }
        
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
    }
}

#endif