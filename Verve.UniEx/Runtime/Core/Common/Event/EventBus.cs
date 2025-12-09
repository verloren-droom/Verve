#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;


    /// <summary>
    ///   <para>事件总线</para>
    /// </summary>
    public static class EventBus
    {
        /// <summary>
        ///   <para>事件分发器，使用整数作为事件键</para>
        /// </summary>
        private static readonly EventDispatcher<int> s_EventDispatcher = new EventDispatcher<int>();
        
        /// <summary>
        ///   <para>字符串转哈希缓存</para>
        /// </summary>
        private static Dictionary<string, int> s_StringToHashCache;
        
        /// <summary>
        ///   <para>所有事件处理函数</para>
        /// </summary>
        public static IReadOnlyDictionary<int, List<Delegate>> Handlers => s_EventDispatcher.Handlers;
        
        /// <summary>
        ///   <para>字符串转哈希缓存</para>
        /// </summary>
        public static IReadOnlyDictionary<string, int> StringToHashCache => s_StringToHashCache ??= new Dictionary<string, int>();
        
#if UNITY_EDITOR
        /// <summary>
        ///   <para>事件记录回调</para>
        /// </summary>
        public static event Action<EventDispatcher<int>.EventRecord> OnEventRecorded
        {
            add => s_EventDispatcher.OnEventRecorded += value;
            remove => s_EventDispatcher.OnEventRecorded -= value;
        }
#endif
        
        static EventBus()
        {
#if UNITY_EDITOR
            Application.quitting += OffAll;
#endif
        }

        #region 使用字符串作为事件键

        /// <summary>
        ///   <para>监听事件（无参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On(string eventKey, Action handler)
            => s_EventDispatcher.On(GetEventHash(eventKey), handler);
        
        /// <summary>
        ///   <para>监听事件（无参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        /// <param name="owner">事件处理器所属对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void On(string eventKey, Action handler, MonoBehaviour owner)
            => owner?.gameObject.GetOrAddComponent<EventHandlerManager>().AddDisposable(On(eventKey, handler));

        /// <summary>
        ///   <para>监听事件（一个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On<T>(string eventKey, Action<T> handler)
            => s_EventDispatcher.On(GetEventHash(eventKey), handler);
        
        /// <summary>
        ///   <para>监听事件（一个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        /// <param name="owner">事件处理器所属对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void On<T>(string eventKey, Action<T> handler, MonoBehaviour owner)
            => owner?.gameObject.GetOrAddComponent<EventHandlerManager>().AddDisposable(On(eventKey, handler));
        
        /// <summary>
        ///   <para>监听事件（两个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On<T1, T2>(string eventKey, Action<T1, T2> handler)
            => s_EventDispatcher.On(GetEventHash(eventKey), handler);
        
        /// <summary>
        ///   <para>监听事件（两个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        /// <param name="owner">事件处理器所属对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void On<T1, T2>(string eventKey, Action<T1, T2> handler, MonoBehaviour owner)
            => owner?.gameObject.GetOrAddComponent<EventHandlerManager>().AddDisposable(On(eventKey, handler));


        /// <summary>
        ///   <para>监听事件（三个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On<T1, T2, T3>(string eventKey, Action<T1, T2, T3> handler)
            => s_EventDispatcher.On(GetEventHash(eventKey), handler);
        
        /// <summary>
        ///   <para>监听事件（三个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        /// <param name="owner">事件处理器所属对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void On<T1, T2, T3>(string eventKey, Action<T1, T2, T3> handler, MonoBehaviour owner)
            => owner?.gameObject.GetOrAddComponent<EventHandlerManager>().AddDisposable(On(eventKey, handler));
        
        /// <summary>
        ///   <para>监听事件（四个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On<T1, T2, T3, T4>(string eventKey, Action<T1, T2, T3, T4> handler)
            => s_EventDispatcher.On(GetEventHash(eventKey), handler);
        
        /// <summary>
        ///   <para>监听事件（四个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        /// <param name="owner">事件处理器所属对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void On<T1, T2, T3, T4>(string eventKey, Action<T1, T2, T3, T4> handler, MonoBehaviour owner)
            => owner?.gameObject.GetOrAddComponent<EventHandlerManager>().AddDisposable(On(eventKey, handler));
        
        /// <summary>
        ///   <para>监听事件（五个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On<T1, T2, T3, T4, T5>(string eventKey, Action<T1, T2, T3, T4, T5> handler)
            => s_EventDispatcher.On(GetEventHash(eventKey), handler);
        
        /// <summary>
        ///   <para>监听事件（五个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        /// <param name="owner">事件处理器所属对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void On<T1, T2, T3, T4, T5>(string eventKey, Action<T1, T2, T3, T4, T5> handler, MonoBehaviour owner)
            => owner?.gameObject.GetOrAddComponent<EventHandlerManager>().AddDisposable(On(eventKey, handler));
        
        /// <summary>
        ///   <para>监听事件（六个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On<T1, T2, T3, T4, T5, T6>(string eventKey, Action<T1, T2, T3, T4, T5, T6> handler)
            => s_EventDispatcher.On(GetEventHash(eventKey), handler);
        
        /// <summary>
        ///   <para>监听事件（六个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        /// <param name="owner">事件处理器所属对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void On<T1, T2, T3, T4, T5, T6>(string eventKey, Action<T1, T2, T3, T4, T5, T6> handler, MonoBehaviour owner)
            => owner?.gameObject.GetOrAddComponent<EventHandlerManager>().AddDisposable(On(eventKey, handler));
        
        /// <summary>
        ///   <para>取消监听事件</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off(string eventKey)
            => s_EventDispatcher.Off(GetEventHash(eventKey));
        
        /// <summary>
        ///   <para>取消监听事件（无参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off(string eventKey, Action handler)
            => s_EventDispatcher.Off(GetEventHash(eventKey), handler);
        
        /// <summary>
        ///   <para>取消监听事件（一个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T>(string eventKey, Action<T> handler)
            => s_EventDispatcher.Off(GetEventHash(eventKey), handler);
        
        /// <summary>
        ///   <para>取消监听事件（两个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T1, T2>(string eventKey, Action<T1, T2> handler)
            => s_EventDispatcher.Off(GetEventHash(eventKey), handler);

        /// <summary>
        ///   <para>取消监听事件（三个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T1, T2, T3>(string eventKey, Action<T1, T2, T3> handler)
            => s_EventDispatcher.Off(GetEventHash(eventKey), handler);

            /// <summary>
        ///   <para>取消监听事件（四个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T1, T2, T3, T4>(string eventKey, Action<T1, T2, T3, T4> handler)
            => s_EventDispatcher.Off(GetEventHash(eventKey), handler);

        /// <summary>
        ///   <para>取消监听事件（五个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T1, T2, T3, T4, T5>(string eventKey, Action<T1, T2, T3, T4, T5> handler)
            => s_EventDispatcher.Off(GetEventHash(eventKey), handler);

        /// <summary>
        ///   <para>取消监听事件（六个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T1, T2, T3, T4, T5, T6>(string eventKey, Action<T1, T2, T3, T4, T5, T6> handler)
            => s_EventDispatcher.Off(GetEventHash(eventKey), handler);

        /// <summary>
        ///   <para>发送事件（无参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit(string eventKey) 
            => s_EventDispatcher.Emit(GetEventHash(eventKey));

        /// <summary>
        ///   <para>发送事件（一个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg">参数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T>(string eventKey, T arg)
            => s_EventDispatcher.Emit(GetEventHash(eventKey), arg);

        /// <summary>
        ///   <para>发送事件（两个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T1, T2>(string eventKey, T1 arg1, T2 arg2)
            => s_EventDispatcher.Emit(GetEventHash(eventKey), arg1, arg2);

        /// <summary>
        ///   <para>发送事件（三个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T1, T2, T3>(string eventKey, T1 arg1, T2 arg2, T3 arg3)
            => s_EventDispatcher.Emit(GetEventHash(eventKey), arg1, arg2, arg3);

        /// <summary>
        ///   <para>发送事件（四个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        /// <param name="arg4">参数4</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T1, T2, T3, T4>(string eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => s_EventDispatcher.Emit(GetEventHash(eventKey), arg1, arg2, arg3, arg4);

        /// <summary>
        ///   <para>发送事件（五个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        /// <param name="arg4">参数4</param>
        /// <param name="arg5">参数5</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T1, T2, T3, T4, T5>(string eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => s_EventDispatcher.Emit(GetEventHash(eventKey), arg1, arg2, arg3, arg4, arg5);

        /// <summary>
        ///   <para>发送事件（六个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        /// <param name="arg4">参数4</param>
        /// <param name="arg5">参数5</param>
        /// <param name="arg6">参数6</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T1, T2, T3, T4, T5, T6>(string eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => s_EventDispatcher.Emit(GetEventHash(eventKey), arg1, arg2, arg3, arg4, arg5, arg6);

        #endregion

        #region 使用整数作为事件键

        /// <summary>
        ///   <para>监听事件（无参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On(int eventKey, Action handler)
            => s_EventDispatcher.On(eventKey, handler);
        
        /// <summary>
        ///   <para>监听事件（无参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        /// <param name="owner">事件处理器所属对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void On(int eventKey, Action handler, MonoBehaviour owner)
            => owner?.gameObject.GetOrAddComponent<EventHandlerManager>().AddDisposable(On(eventKey, handler));
        
        /// <summary>
        ///   <para>监听事件（一个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On<T>(int eventKey, Action<T> handler)
            => s_EventDispatcher.On(eventKey, handler);

        /// <summary>
        ///   <para>监听事件（一个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        /// <param name="owner">事件处理器所属对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void On<T>(int eventKey, Action<T> handler, MonoBehaviour owner)
            => owner?.gameObject.GetOrAddComponent<EventHandlerManager>().AddDisposable(On(eventKey, handler));

        /// <summary>
        ///   <para>监听事件（两个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On<T1, T2>(int eventKey, Action<T1, T2> handler)
            => s_EventDispatcher.On(eventKey, handler);
        
        /// <summary>
        ///   <para>监听事件（两个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        /// <param name="owner">事件处理器所属对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void On<T1, T2>(int eventKey, Action<T1, T2> handler, MonoBehaviour owner)
            => owner?.gameObject.GetOrAddComponent<EventHandlerManager>().AddDisposable(On(eventKey, handler));

        /// <summary>
        ///   <para>监听事件（三个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On<T1, T2, T3>(int eventKey, Action<T1, T2, T3> handler)
            => s_EventDispatcher.On(eventKey, handler);
        
        /// <summary>
        ///   <para>监听事件（三个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        /// <param name="owner">事件处理器所属对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void On<T1, T2, T3>(int eventKey, Action<T1, T2, T3> handler, MonoBehaviour owner)
            => owner?.gameObject.GetOrAddComponent<EventHandlerManager>().AddDisposable(On(eventKey, handler));

        /// <summary>
        ///   <para>监听事件（四个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On<T1, T2, T3, T4>(int eventKey, Action<T1, T2, T3, T4> handler)
            => s_EventDispatcher.On(eventKey, handler);
        
        /// <summary>
        ///   <para>监听事件（四个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        /// <param name="owner">事件处理器所属对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void On<T1, T2, T3, T4>(int eventKey, Action<T1, T2, T3, T4> handler, MonoBehaviour owner)
            => owner?.gameObject.GetOrAddComponent<EventHandlerManager>().AddDisposable(On(eventKey, handler));

        /// <summary>
        ///   <para>监听事件（五个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On<T1, T2, T3, T4, T5>(int eventKey, Action<T1, T2, T3, T4, T5> handler)
            => s_EventDispatcher.On(eventKey, handler);

        /// <summary>
        ///   <para>监听事件（五个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        /// <param name="owner">事件处理器所属对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void On<T1, T2, T3, T4, T5>(int eventKey, Action<T1, T2, T3, T4, T5> handler, MonoBehaviour owner)
            => owner?.gameObject.GetOrAddComponent<EventHandlerManager>().AddDisposable(On(eventKey, handler));

        /// <summary>
        ///   <para>监听事件（六个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On<T1, T2, T3, T4, T5, T6>(int eventKey, Action<T1, T2, T3, T4, T5, T6> handler)
            => s_EventDispatcher.On(eventKey, handler);

        /// <summary>
        ///   <para>监听事件（六个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        /// <param name="owner">事件处理器所属对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void On<T1, T2, T3, T4, T5, T6>(int eventKey, Action<T1, T2, T3, T4, T5, T6> handler, MonoBehaviour owner)
            => owner?.gameObject.GetOrAddComponent<EventHandlerManager>().AddDisposable(On(eventKey, handler));
        
        /// <summary>
        ///   <para>取消监听事件</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off(int eventKey)
            => s_EventDispatcher.Off(eventKey);
        
        /// <summary>
        ///   <para>取消监听事件（无参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off(int eventKey, Action handler)
            => s_EventDispatcher.Off(eventKey, handler);

        /// <summary>
        ///   <para>取消监听事件（一个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T>(int eventKey, Action<T> handler)
            => s_EventDispatcher.Off(eventKey, handler);

        /// <summary>
        ///   <para>取消监听事件（两个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T1, T2>(int eventKey, Action<T1, T2> handler)
            => s_EventDispatcher.Off(eventKey, handler);

        /// <summary>
        ///   <para>取消监听事件（三个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T1, T2, T3>(int eventKey, Action<T1, T2, T3> handler)
            => s_EventDispatcher.Off(eventKey, handler);

        /// <summary>
        ///   <para>取消监听事件（四个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T1, T2, T3, T4>(int eventKey, Action<T1, T2, T3, T4> handler)
            => s_EventDispatcher.Off(eventKey, handler);
        
        /// <summary>
        ///   <para>取消监听事件（五个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T1, T2, T3, T4, T5>(int eventKey, Action<T1, T2, T3, T4, T5> handler)
            => s_EventDispatcher.Off(eventKey, handler);
        
        /// <summary>
        ///   <para>取消监听事件（六个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T1, T2, T3, T4, T5, T6>(int eventKey, Action<T1, T2, T3, T4, T5, T6> handler)
            => s_EventDispatcher.Off(eventKey, handler);
        
        /// <summary>
        ///   <para>发送事件（无参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit(int eventKey)
            => s_EventDispatcher.Emit(eventKey);

        /// <summary>
        ///   <para>发送事件（一个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg">参数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T>(int eventKey, T arg)
            => s_EventDispatcher.Emit(eventKey, arg);

        /// <summary>
        ///   <para>发送事件（两个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T1, T2>(int eventKey, T1 arg1, T2 arg2)
            => s_EventDispatcher.Emit(eventKey, arg1, arg2);

        /// <summary>
        ///   <para>发送事件（三个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T1, T2, T3>(int eventKey, T1 arg1, T2 arg2, T3 arg3)
            => s_EventDispatcher.Emit(eventKey, arg1, arg2, arg3);

        /// <summary>
        ///   <para>发送事件（四个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        /// <param name="arg4">参数4</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T1, T2, T3, T4>(int eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => s_EventDispatcher.Emit(eventKey, arg1, arg2, arg3, arg4);

        /// <summary>
        ///   <para>发送事件（五个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        /// <param name="arg4">参数4</param>
        /// <param name="arg5">参数5</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T1, T2, T3, T4, T5>(int eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => s_EventDispatcher.Emit(eventKey, arg1, arg2, arg3, arg4, arg5);
        
        /// <summary>
        ///   <para>发送事件（六个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        /// <param name="arg4">参数4</param>
        /// <param name="arg5">参数5</param>
        /// <param name="arg6">参数6</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T1, T2, T3, T4, T5, T6>(int eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => s_EventDispatcher.Emit(eventKey, arg1, arg2, arg3, arg4, arg5, arg6);
        
        #endregion

        /// <summary>
        ///   <para>取消所有监听事件</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OffAll() => s_EventDispatcher.OffAll();

        /// <summary>
        ///   <para>获取字符串事件键的哈希值</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetEventHash(string eventKey)
        {
            if (string.IsNullOrEmpty(eventKey))
                throw new ArgumentException("Event Key cannot be null or empty", nameof(eventKey));

            s_StringToHashCache ??= new Dictionary<string, int>();
            if (!s_StringToHashCache.TryGetValue(eventKey, out var hash))
            {
                hash = eventKey.GetHashCode();
                s_StringToHashCache[eventKey] = hash;
            }
            return hash;
        }
    }
    
    
    /// <summary>
    ///   <para>事件处理器管理</para>
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class EventHandlerManager : MonoBehaviour
    {
        private readonly List<IDisposable> m_DisposableEvents = new List<IDisposable>();
        
        public void AddDisposable(IDisposable disposable)
        {
            if (disposable == null || m_DisposableEvents.Contains(disposable)) return;
            m_DisposableEvents.Add(disposable);
        }

        private void OnDestroy()
        {
            foreach (var disposable in m_DisposableEvents)
                disposable?.Dispose();
        }
    }
}

#endif