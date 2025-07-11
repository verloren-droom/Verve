#if UNITY_5_3_OR_NEWER
    
namespace VerveUniEx.MVC
{
    using Verve;
    using System;
    using Verve.MVC;
    using UnityEngine;
    using UnityEngine.UI;
    using System.Collections;
    using UnityEngine.EventSystems;
    using System.Text.RegularExpressions;
    
    
    /// <summary>
    /// MVC视图基类
    /// </summary>
    public abstract partial class ViewBase : MonoBehaviour, IView
    {
        [SerializeField, ReadOnly] private string m_ViewName;
        public virtual string ViewName
        {
            get => m_ViewName ??= 
                Regex.Replace(gameObject.name, @"View$", string.Empty, RegexOptions.IgnoreCase);
            protected set => m_ViewName = value;
        }
        
        private bool m_IsOpened;
    
        public event Action<IView> OnOpened;
        public event Action<IView> OnClosed;

        public abstract IActivity Activity { get; set; }
        
        
        protected virtual void OnOpening(params object[] args) { }
        protected virtual void OnClosing() { }

        protected virtual IEnumerator Start()
        {
            if (!m_IsOpened)
            {
                ((IView)this).Open();
            }
            yield break;
        }

        void IView.Open(params object[] args)
        {
            if (m_IsOpened) return;
            m_IsOpened = true;
            gameObject.SetActive(true);
            OnOpening(args);
            OnOpened?.Invoke(this);
        }

        public void Close()
        {
            OnClosing();
            gameObject.SetActive(false);
            OnClosed?.Invoke(this);
            m_IsOpened = false;
            OnOpened = null;
            OnClosed = null;
            Destroy(gameObject);
        }

        /// <summary>
        /// 添加事件监听
        /// </summary>
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

        /// <summary>
        /// 批量添加多个事件类型
        /// </summary>
        public void AddEventTriggerRange(
            EventTrigger eventTrigger,
            params (EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)[] events)
        {
            foreach (var e in events)
            {
                AddEventTrigger(eventTrigger, e.type, e.callback);
            }
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
        public void RemoveAllTriggers(EventTrigger eventTrigger)
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
        
        public void RemoveAllTriggers(MaskableGraphic graphic)
        {
            if (graphic == null || !graphic.TryGetComponent<EventTrigger>(out var eventTrigger)) return;

            foreach (var entry in eventTrigger.triggers)
            {
                entry.callback.RemoveAllListeners();
            }
            eventTrigger.triggers.Clear();
        }
    }
}

#endif