namespace Verve
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
#endif

    /// <summary>
    ///   <para>游戏入口。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>事件分发器。</para>
        /// </summary>
        private static readonly EventDispatcher<EventKey> s_EventDispatcher = new();

        /// <summary>
        ///   <para>事件处理函数。</para>
        /// </summary>
        public static IReadOnlyDictionary<EventKey, IReadOnlyList<Delegate>> EventHandlers => s_EventDispatcher.Handlers;
#if (DEBUG || DEVELOPMENT_BUILD)
        /// <summary>
        ///   <para>事件记录通知。</para>
        /// </summary>
        internal static event Action<EventDispatcher<EventKey>.EventRecord> OnEventRecorded
        {
            add => s_EventDispatcher.OnEventRecorded += value;
            remove => s_EventDispatcher.OnEventRecorded -= value;
        }
#endif

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        public static IDisposable On(string eventKey, Action handler)
            => s_EventDispatcher.On(new EventKey(eventKey), handler);
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <param name="owner">所有者。</param>
        public static void On(string eventKey, Action handler, MonoBehaviour owner)
            => GetOwnerEventManager(owner).AddDisposable(On(eventKey, handler));
#endif
        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        public static void Off(string eventKey, Action handler)
            => s_EventDispatcher.Off(new EventKey(eventKey), handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        public static void Emit(string eventKey
            ) => s_EventDispatcher.Emit(new EventKey(eventKey)
            );

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        public static IDisposable On<T1>(string eventKey, Action<T1> handler)
            => s_EventDispatcher.On(new EventKey(eventKey), handler);
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <param name="owner">所有者。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        public static void On<T1>(string eventKey, Action<T1> handler, MonoBehaviour owner)
            => GetOwnerEventManager(owner).AddDisposable(On(eventKey, handler));
#endif
        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        public static void Off<T1>(string eventKey, Action<T1> handler)
            => s_EventDispatcher.Off(new EventKey(eventKey), handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="arg1">第一个参数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        public static void Emit<T1>(string eventKey, T1 arg1
            ) => s_EventDispatcher.Emit(new EventKey(eventKey), arg1
            );

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        public static IDisposable On<T1, T2>(string eventKey, Action<T1, T2> handler)
            => s_EventDispatcher.On(new EventKey(eventKey), handler);
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <param name="owner">所有者。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        public static void On<T1, T2>(string eventKey, Action<T1, T2> handler, MonoBehaviour owner)
            => GetOwnerEventManager(owner).AddDisposable(On(eventKey, handler));
#endif
        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        public static void Off<T1, T2>(string eventKey, Action<T1, T2> handler)
            => s_EventDispatcher.Off(new EventKey(eventKey), handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="arg1">第一个参数。</param>
        /// <param name="arg2">第二个参数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        public static void Emit<T1, T2>(string eventKey, T1 arg1, T2 arg2
            ) => s_EventDispatcher.Emit(new EventKey(eventKey), arg1, arg2
            );

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        public static IDisposable On<T1, T2, T3>(string eventKey, Action<T1, T2, T3> handler)
            => s_EventDispatcher.On(new EventKey(eventKey), handler);
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <param name="owner">所有者。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        public static void On<T1, T2, T3>(string eventKey, Action<T1, T2, T3> handler, MonoBehaviour owner)
            => GetOwnerEventManager(owner).AddDisposable(On(eventKey, handler));
#endif
        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        public static void Off<T1, T2, T3>(string eventKey, Action<T1, T2, T3> handler)
            => s_EventDispatcher.Off(new EventKey(eventKey), handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="arg1">第一个参数。</param>
        /// <param name="arg2">第二个参数。</param>
        /// <param name="arg3">第三个参数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        public static void Emit<T1, T2, T3>(string eventKey, T1 arg1, T2 arg2, T3 arg3
            ) => s_EventDispatcher.Emit(new EventKey(eventKey), arg1, arg2, arg3
            );

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        public static IDisposable On<T1, T2, T3, T4>(string eventKey, Action<T1, T2, T3, T4> handler)
            => s_EventDispatcher.On(new EventKey(eventKey), handler);
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <param name="owner">所有者。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        public static void On<T1, T2, T3, T4>(string eventKey, Action<T1, T2, T3, T4> handler, MonoBehaviour owner)
            => GetOwnerEventManager(owner).AddDisposable(On(eventKey, handler));
#endif
        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        public static void Off<T1, T2, T3, T4>(string eventKey, Action<T1, T2, T3, T4> handler)
            => s_EventDispatcher.Off(new EventKey(eventKey), handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="arg1">第一个参数。</param>
        /// <param name="arg2">第二个参数。</param>
        /// <param name="arg3">第三个参数。</param>
        /// <param name="arg4">第四个参数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        public static void Emit<T1, T2, T3, T4>(string eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4
            ) => s_EventDispatcher.Emit(new EventKey(eventKey), arg1, arg2, arg3, arg4
            );

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        public static IDisposable On<T1, T2, T3, T4, T5>(string eventKey, Action<T1, T2, T3, T4, T5> handler)
            => s_EventDispatcher.On(new EventKey(eventKey), handler);
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <param name="owner">所有者。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        public static void On<T1, T2, T3, T4, T5>(string eventKey, Action<T1, T2, T3, T4, T5> handler, MonoBehaviour owner)
            => GetOwnerEventManager(owner).AddDisposable(On(eventKey, handler));
#endif
        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        public static void Off<T1, T2, T3, T4, T5>(string eventKey, Action<T1, T2, T3, T4, T5> handler)
            => s_EventDispatcher.Off(new EventKey(eventKey), handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="arg1">第一个参数。</param>
        /// <param name="arg2">第二个参数。</param>
        /// <param name="arg3">第三个参数。</param>
        /// <param name="arg4">第四个参数。</param>
        /// <param name="arg5">第五个参数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        public static void Emit<T1, T2, T3, T4, T5>(string eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5
            ) => s_EventDispatcher.Emit(new EventKey(eventKey), arg1, arg2, arg3, arg4, arg5
            );

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        /// <typeparam name="T6">第6 个值的类型。</typeparam>
        public static IDisposable On<T1, T2, T3, T4, T5, T6>(string eventKey, Action<T1, T2, T3, T4, T5, T6> handler)
            => s_EventDispatcher.On(new EventKey(eventKey), handler);
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <param name="owner">所有者。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        /// <typeparam name="T6">第6 个值的类型。</typeparam>
        public static void On<T1, T2, T3, T4, T5, T6>(string eventKey, Action<T1, T2, T3, T4, T5, T6> handler, MonoBehaviour owner)
            => GetOwnerEventManager(owner).AddDisposable(On(eventKey, handler));
#endif
        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        /// <typeparam name="T6">第6 个值的类型。</typeparam>
        public static void Off<T1, T2, T3, T4, T5, T6>(string eventKey, Action<T1, T2, T3, T4, T5, T6> handler)
            => s_EventDispatcher.Off(new EventKey(eventKey), handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="arg1">第一个参数。</param>
        /// <param name="arg2">第二个参数。</param>
        /// <param name="arg3">第三个参数。</param>
        /// <param name="arg4">第四个参数。</param>
        /// <param name="arg5">第五个参数。</param>
        /// <param name="arg6">第六个参数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        /// <typeparam name="T6">第6 个值的类型。</typeparam>
        public static void Emit<T1, T2, T3, T4, T5, T6>(string eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6
            ) => s_EventDispatcher.Emit(new EventKey(eventKey), arg1, arg2, arg3, arg4, arg5, arg6
            );

        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        public static void Off(string eventKey) => s_EventDispatcher.Off(new EventKey(eventKey));
        /// <summary>
        ///   <para>判断事件是否存在订阅。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        public static bool Has(string eventKey, Delegate handler = null) => s_EventDispatcher.Has(new EventKey(eventKey), handler);

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        public static IDisposable On(int eventKey, Action handler)
            => s_EventDispatcher.On(new EventKey(eventKey), handler);
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <param name="owner">所有者。</param>
        public static void On(int eventKey, Action handler, MonoBehaviour owner)
            => GetOwnerEventManager(owner).AddDisposable(On(eventKey, handler));
#endif
        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        public static void Off(int eventKey, Action handler)
            => s_EventDispatcher.Off(new EventKey(eventKey), handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        public static void Emit(int eventKey
            ) => s_EventDispatcher.Emit(new EventKey(eventKey)
            );

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        public static IDisposable On<T1>(int eventKey, Action<T1> handler)
            => s_EventDispatcher.On(new EventKey(eventKey), handler);
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <param name="owner">所有者。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        public static void On<T1>(int eventKey, Action<T1> handler, MonoBehaviour owner)
            => GetOwnerEventManager(owner).AddDisposable(On(eventKey, handler));
#endif
        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        public static void Off<T1>(int eventKey, Action<T1> handler)
            => s_EventDispatcher.Off(new EventKey(eventKey), handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="arg1">第一个参数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        public static void Emit<T1>(int eventKey, T1 arg1
            ) => s_EventDispatcher.Emit(new EventKey(eventKey), arg1
            );

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        public static IDisposable On<T1, T2>(int eventKey, Action<T1, T2> handler)
            => s_EventDispatcher.On(new EventKey(eventKey), handler);
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <param name="owner">所有者。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        public static void On<T1, T2>(int eventKey, Action<T1, T2> handler, MonoBehaviour owner)
            => GetOwnerEventManager(owner).AddDisposable(On(eventKey, handler));
#endif
        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        public static void Off<T1, T2>(int eventKey, Action<T1, T2> handler)
            => s_EventDispatcher.Off(new EventKey(eventKey), handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="arg1">第一个参数。</param>
        /// <param name="arg2">第二个参数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        public static void Emit<T1, T2>(int eventKey, T1 arg1, T2 arg2
            ) => s_EventDispatcher.Emit(new EventKey(eventKey), arg1, arg2
            );

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        public static IDisposable On<T1, T2, T3>(int eventKey, Action<T1, T2, T3> handler)
            => s_EventDispatcher.On(new EventKey(eventKey), handler);
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <param name="owner">所有者。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        public static void On<T1, T2, T3>(int eventKey, Action<T1, T2, T3> handler, MonoBehaviour owner)
            => GetOwnerEventManager(owner).AddDisposable(On(eventKey, handler));
#endif
        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        public static void Off<T1, T2, T3>(int eventKey, Action<T1, T2, T3> handler)
            => s_EventDispatcher.Off(new EventKey(eventKey), handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="arg1">第一个参数。</param>
        /// <param name="arg2">第二个参数。</param>
        /// <param name="arg3">第三个参数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        public static void Emit<T1, T2, T3>(int eventKey, T1 arg1, T2 arg2, T3 arg3
            ) => s_EventDispatcher.Emit(new EventKey(eventKey), arg1, arg2, arg3
            );

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        public static IDisposable On<T1, T2, T3, T4>(int eventKey, Action<T1, T2, T3, T4> handler)
            => s_EventDispatcher.On(new EventKey(eventKey), handler);
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <param name="owner">所有者。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        public static void On<T1, T2, T3, T4>(int eventKey, Action<T1, T2, T3, T4> handler, MonoBehaviour owner)
            => GetOwnerEventManager(owner).AddDisposable(On(eventKey, handler));
#endif
        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        public static void Off<T1, T2, T3, T4>(int eventKey, Action<T1, T2, T3, T4> handler)
            => s_EventDispatcher.Off(new EventKey(eventKey), handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="arg1">第一个参数。</param>
        /// <param name="arg2">第二个参数。</param>
        /// <param name="arg3">第三个参数。</param>
        /// <param name="arg4">第四个参数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        public static void Emit<T1, T2, T3, T4>(int eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4
            ) => s_EventDispatcher.Emit(new EventKey(eventKey), arg1, arg2, arg3, arg4
            );

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        public static IDisposable On<T1, T2, T3, T4, T5>(int eventKey, Action<T1, T2, T3, T4, T5> handler)
            => s_EventDispatcher.On(new EventKey(eventKey), handler);
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <param name="owner">所有者。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        public static void On<T1, T2, T3, T4, T5>(int eventKey, Action<T1, T2, T3, T4, T5> handler, MonoBehaviour owner)
            => GetOwnerEventManager(owner).AddDisposable(On(eventKey, handler));
#endif
        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        public static void Off<T1, T2, T3, T4, T5>(int eventKey, Action<T1, T2, T3, T4, T5> handler)
            => s_EventDispatcher.Off(new EventKey(eventKey), handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="arg1">第一个参数。</param>
        /// <param name="arg2">第二个参数。</param>
        /// <param name="arg3">第三个参数。</param>
        /// <param name="arg4">第四个参数。</param>
        /// <param name="arg5">第五个参数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        public static void Emit<T1, T2, T3, T4, T5>(int eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5
            ) => s_EventDispatcher.Emit(new EventKey(eventKey), arg1, arg2, arg3, arg4, arg5
            );

        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        /// <typeparam name="T6">第6 个值的类型。</typeparam>
        public static IDisposable On<T1, T2, T3, T4, T5, T6>(int eventKey, Action<T1, T2, T3, T4, T5, T6> handler)
            => s_EventDispatcher.On(new EventKey(eventKey), handler);
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>订阅事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <param name="owner">所有者。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        /// <typeparam name="T6">第6 个值的类型。</typeparam>
        public static void On<T1, T2, T3, T4, T5, T6>(int eventKey, Action<T1, T2, T3, T4, T5, T6> handler, MonoBehaviour owner)
            => GetOwnerEventManager(owner).AddDisposable(On(eventKey, handler));
#endif
        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        /// <typeparam name="T6">第6 个值的类型。</typeparam>
        public static void Off<T1, T2, T3, T4, T5, T6>(int eventKey, Action<T1, T2, T3, T4, T5, T6> handler)
            => s_EventDispatcher.Off(new EventKey(eventKey), handler);

        /// <summary>
        ///   <para>发布事件。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="arg1">第一个参数。</param>
        /// <param name="arg2">第二个参数。</param>
        /// <param name="arg3">第三个参数。</param>
        /// <param name="arg4">第四个参数。</param>
        /// <param name="arg5">第五个参数。</param>
        /// <param name="arg6">第六个参数。</param>
        /// <typeparam name="T1">第1 个值的类型。</typeparam>
        /// <typeparam name="T2">第2 个值的类型。</typeparam>
        /// <typeparam name="T3">第3 个值的类型。</typeparam>
        /// <typeparam name="T4">第4 个值的类型。</typeparam>
        /// <typeparam name="T5">第5 个值的类型。</typeparam>
        /// <typeparam name="T6">第6 个值的类型。</typeparam>
        public static void Emit<T1, T2, T3, T4, T5, T6>(int eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6
            ) => s_EventDispatcher.Emit(new EventKey(eventKey), arg1, arg2, arg3, arg4, arg5, arg6
            );

        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        public static void Off(int eventKey) => s_EventDispatcher.Off(new EventKey(eventKey));
        /// <summary>
        ///   <para>判断事件是否存在订阅。</para>
        /// </summary>
        /// <param name="eventKey">事件键。</param>
        /// <param name="handler">处理函数。</param>
        public static bool Has(int eventKey, Delegate handler = null) => s_EventDispatcher.Has(new EventKey(eventKey), handler);

        /// <summary>
        ///   <para>取消全部事件订阅。</para>
        /// </summary>
        public static void OffAll() => s_EventDispatcher.OffAll();

#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>清理事件。</para>
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void CleanupEvent() => OffAll();

        /// <summary>
        ///   <para>获取所有者事件管理器。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        private static EventHandlerManager GetOwnerEventManager(MonoBehaviour owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            return owner.gameObject.GetOrAddComponent<EventHandlerManager>();
        }
#endif
    }

#if UNITY_5_3_OR_NEWER
    /// <summary>
    ///   <para>事件处理器管理。</para>
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("")]
    internal sealed class EventHandlerManager : MonoBehaviour
    {
        /// <summary>
        ///   <para>订阅句柄列表。</para>
        /// </summary>
        private readonly List<IDisposable> m_DisposableEvents = new();

        /// <summary>
        ///   <para>登记订阅句柄。</para>
        /// </summary>
        /// <param name="disposable">可释放对象。</param>
        public void AddDisposable(IDisposable disposable)
        {
            if (disposable == null || m_DisposableEvents.Contains(disposable)) return;
            m_DisposableEvents.Add(disposable);
        }

        /// <summary>
        ///   <para>销毁时清理。</para>
        /// </summary>
        private void OnDestroy() => ResourceUtility.ReleaseAll(m_DisposableEvents, subscription => subscription.Dispose());
    }
#endif
}