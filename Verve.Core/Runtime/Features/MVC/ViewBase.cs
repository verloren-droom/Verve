#if UNITY_5_3_OR_NEWER
    
namespace Verve.MVC
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;
    using System.Collections;
    using UnityEngine.EventSystems;
    using System.Text.RegularExpressions;
    
    
    /// <summary>
    ///   <para>MVC视图基类</para>
    /// </summary>
    public abstract partial class ViewBase : MonoBehaviour, IView
    {
        [SerializeField, Tooltip("视图名称"), ReadOnly] private string m_ViewName;
        
        public virtual string ViewName
        {
            get => m_ViewName ??= 
                Regex.Replace(gameObject.name, @"View$", string.Empty, RegexOptions.IgnoreCase);
            protected set => m_ViewName = value;
        }
        
        private bool m_IsOpened;
    
        public event Action<IView> OnOpened;
        
        public event Action<IView> OnClosed;

        public abstract IActivity GetActivity();

        /// <summary>
        ///   <para>当视图被打开</para>
        /// </summary>
        /// <param name="args">参数</param>
        protected virtual void OnOpening(params object[] args) { }
        
        /// <summary>
        ///   <para>当视图被关闭</para>
        /// </summary>
        protected virtual void OnClosing() { }

        private void OnEnable()
        {
            if (!m_IsOpened)
            {
                ((IView)this).Open();
            }
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
            if (!this || !gameObject)
                return;
            
            OnClosing();
            OnClosed?.Invoke(this);
            gameObject.SetActive(false);
            m_IsOpened = false;
            OnOpened = null;
            OnClosed = null;
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        ///   <para>添加事件监听</para>
        /// </summary>
        /// <param name="eventTrigger">事件触发器</param>
        /// <param name="type">事件类型</param>
        /// <param name="callback">回调方法</param>
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

        /// <summary>
        ///   <para>添加事件监听</para>
        /// </summary>
        /// <param name="graphic">UI元素</param>
        /// <param name="type">事件类型</param>
        /// <param name="callback">回调方法</param>
        protected void AddEventTrigger(MaskableGraphic graphic, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            AddEventTrigger(graphic?.gameObject.GetComponent<EventTrigger>() ?? graphic?.gameObject.AddComponent<EventTrigger>(), type, callback);
        }

        /// <summary>
        ///   <para>批量添加多个事件类型</para>
        /// </summary>
        /// <param name="graphic">UI元素</param>
        /// <param name="events">事件类型和回调方法</param>
        public void AddEventTriggerRange(
            EventTrigger eventTrigger,
            params (EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)[] events)
        {
            foreach (var e in events)
            {
                AddEventTrigger(eventTrigger, e.type, e.callback);
            }
        }
        
        /// <summary>
        ///   <para>移除事件监听</para>
        /// </summary>
        /// <param name="eventTrigger">事件触发器</param>
        /// <param name="type">事件类型</param>
        protected void RemoveEventTrigger(EventTrigger eventTrigger, EventTriggerType type)
        {
            if (eventTrigger == null) return;
            eventTrigger.triggers.RemoveAll(entry => entry.eventID == type);
        }
        
        /// <summary>
        ///   <para>移除事件监听</para>
        /// </summary>
        /// <param name="eventTrigger">事件触发器</param>
        /// <param name="type">事件类型</param>
        /// <param name="callback">回调方法</param>
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
        
        /// <summary>
        ///   <para>移除事件监听</para>
        /// </summary>
        /// <param name="graphic">UI元素</param>
        /// <param name="type">事件类型</param>
        /// <param name="callback">回调方法</param>
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
        ///   <para>清除所有事件监听</para>
        /// </summary>
        /// <param name="eventTrigger">事件触发器</param>
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
        
        /// <summary>
        ///   <para>清除所有事件监听</para>
        /// </summary>
        /// <param name="graphic">UI元素</param>
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