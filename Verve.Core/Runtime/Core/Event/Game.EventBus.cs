namespace Verve
{
    using System;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
#endif
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;


    /// <summary>
    ///   <para>游戏入口：事件总线部分</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>事件分发器，使用整数作为事件键</para>
        /// </summary>
        [ThreadStatic] private static EventDispatcher<int> s_EventDispatcher;
        
        /// <summary>
        ///   <para>字符串转哈希缓存</para>
        /// </summary>
        private static Dictionary<string, int> s_StringToHashCache;
        private static readonly object s_StringToHashLock = new();
        
        private static EventDispatcher<int> EventDispatcher
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_EventDispatcher ??= new EventDispatcher<int>();
        }
        
        /// <summary>
        ///   <para>所有事件处理函数</para>
        /// </summary>
        public static IReadOnlyDictionary<int, List<Delegate>> EventHandlers => EventDispatcher.Handlers;
        
        /// <summary>
        ///   <para>字符串转哈希缓存</para>
        /// </summary>
        public static IReadOnlyDictionary<string, int> StringToHashCache
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                lock (s_StringToHashLock)
                {
                    return s_StringToHashCache ??= new Dictionary<string, int>();
                }
            }
        }
        
#if DEBUG
        /// <summary>
        ///   <para>事件记录回调</para>
        /// </summary>
        internal static event Action<EventDispatcher<int>.EventRecord> OnEventRecorded
        {
            add => EventDispatcher.OnEventRecorded += value;
            remove => EventDispatcher.OnEventRecorded -= value;
        }
#endif

        #region 使用字符串作为事件键

        /// <summary>
        ///   <para>监听事件（无参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On(string eventKey, Action handler)
            => EventDispatcher.On(GetEventHash(eventKey), handler);
        
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
            => EventDispatcher.On(GetEventHash(eventKey), handler);
        
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
            => EventDispatcher.On(GetEventHash(eventKey), handler);

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
            => EventDispatcher.On(GetEventHash(eventKey), handler);
        
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>监听事件（三个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        /// <param name="owner">事件处理器所属对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void On<T1, T2, T3>(string eventKey, Action<T1, T2, T3> handler, MonoBehaviour owner)
            => owner?.gameObject.GetOrAddComponent<EventHandlerManager>().AddDisposable(On(eventKey, handler));
#endif
        
        /// <summary>
        ///   <para>监听事件（四个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On<T1, T2, T3, T4>(string eventKey, Action<T1, T2, T3, T4> handler)
            => EventDispatcher.On(GetEventHash(eventKey), handler);
        
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>监听事件（四个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        /// <param name="owner">事件处理器所属对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void On<T1, T2, T3, T4>(string eventKey, Action<T1, T2, T3, T4> handler, MonoBehaviour owner)
            => owner?.gameObject.GetOrAddComponent<EventHandlerManager>().AddDisposable(On(eventKey, handler));
#endif
        
        /// <summary>
        ///   <para>监听事件（五个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On<T1, T2, T3, T4, T5>(string eventKey, Action<T1, T2, T3, T4, T5> handler)
            => EventDispatcher.On(GetEventHash(eventKey), handler);
        
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>监听事件（五个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        /// <param name="owner">事件处理器所属对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void On<T1, T2, T3, T4, T5>(string eventKey, Action<T1, T2, T3, T4, T5> handler, MonoBehaviour owner)
            => owner?.gameObject.GetOrAddComponent<EventHandlerManager>().AddDisposable(On(eventKey, handler));
#endif
        
        /// <summary>
        ///   <para>监听事件（六个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On<T1, T2, T3, T4, T5, T6>(string eventKey, Action<T1, T2, T3, T4, T5, T6> handler)
            => EventDispatcher.On(GetEventHash(eventKey), handler);
        
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
            => EventDispatcher.Off(GetEventHash(eventKey));
        
        /// <summary>
        ///   <para>取消监听事件（无参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off(string eventKey, Action handler)
            => EventDispatcher.Off(GetEventHash(eventKey), handler);
        
        /// <summary>
        ///   <para>取消监听事件（一个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T>(string eventKey, Action<T> handler)
            => EventDispatcher.Off(GetEventHash(eventKey), handler);
        
        /// <summary>
        ///   <para>取消监听事件（两个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T1, T2>(string eventKey, Action<T1, T2> handler)
            => EventDispatcher.Off(GetEventHash(eventKey), handler);

        /// <summary>
        ///   <para>取消监听事件（三个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T1, T2, T3>(string eventKey, Action<T1, T2, T3> handler)
            => EventDispatcher.Off(GetEventHash(eventKey), handler);

        /// <summary>
        ///   <para>取消监听事件（四个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T1, T2, T3, T4>(string eventKey, Action<T1, T2, T3, T4> handler)
            => EventDispatcher.Off(GetEventHash(eventKey), handler);

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
        public static void Emit(string eventKey
#if DEBUG
            , [CallerMemberName] string emitMember = null
            , [CallerFilePath] string emitFile = null
            , [CallerLineNumber] int emitLine = 0
#endif
            )
            => EventDispatcher.Emit(GetEventHash(eventKey), emitMember, emitFile, emitLine);

        /// <summary>
        ///   <para>发送事件（一个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg">参数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T>(string eventKey, T arg
#if DEBUG
            , [CallerMemberName] string emitMember = null
            , [CallerFilePath] string emitFile = null
            , [CallerLineNumber] int emitLine = 0
#endif
            )
            => EventDispatcher.Emit(GetEventHash(eventKey), arg, emitMember, emitFile, emitLine);

        /// <summary>
        ///   <para>发送事件（两个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T1, T2>(string eventKey, T1 arg1, T2 arg2
#if DEBUG
            , [CallerMemberName] string emitMember = null
            , [CallerFilePath] string emitFile = null
            , [CallerLineNumber] int emitLine = 0
#endif
            )
            => EventDispatcher.Emit(GetEventHash(eventKey), arg1, arg2, emitMember, emitFile, emitLine);

        /// <summary>
        ///   <para>发送事件（三个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T1, T2, T3>(string eventKey, T1 arg1, T2 arg2, T3 arg3
#if DEBUG
            , [CallerMemberName] string emitMember = null
            , [CallerFilePath] string emitFile = null
            , [CallerLineNumber] int emitLine = 0
#endif
            )
            => EventDispatcher.Emit(GetEventHash(eventKey), arg1, arg2, arg3, emitMember, emitFile, emitLine);

        /// <summary>
        ///   <para>发送事件（四个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        /// <param name="arg4">参数4</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T1, T2, T3, T4>(string eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4
#if DEBUG
            , [CallerMemberName] string emitMember = null
            , [CallerFilePath] string emitFile = null
            , [CallerLineNumber] int emitLine = 0
#endif
            )
            => EventDispatcher.Emit(GetEventHash(eventKey), arg1, arg2, arg3, arg4, emitMember, emitFile, emitLine);

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
        public static void Emit<T1, T2, T3, T4, T5>(string eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5
#if DEBUG
            , [CallerMemberName] string emitMember = null
            , [CallerFilePath] string emitFile = null
            , [CallerLineNumber] int emitLine = 0
#endif
            )
            => EventDispatcher.Emit(GetEventHash(eventKey), arg1, arg2, arg3, arg4, arg5, emitMember, emitFile, emitLine);

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
        public static void Emit<T1, T2, T3, T4, T5, T6>(string eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6
#if DEBUG
            , [CallerMemberName] string emitMember = null
            , [CallerFilePath] string emitFile = null
            , [CallerLineNumber] int emitLine = 0
#endif
            )
            => EventDispatcher.Emit(GetEventHash(eventKey), arg1, arg2, arg3, arg4, arg5, arg6, emitMember, emitFile, emitLine);
        
        /// <summary>
        ///   <para>检测事件是否已监听</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        public static bool Has(string eventKey, Delegate handler = null)
            => EventDispatcher.Has(GetEventHash(eventKey), handler);

        #endregion

        #region 使用整数作为事件键

        /// <summary>
        ///   <para>监听事件（无参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable On(int eventKey, Action handler)
            => EventDispatcher.On(eventKey, handler);
        
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
            => EventDispatcher.On(eventKey, handler);

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
            => EventDispatcher.On(eventKey, handler);
        
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
            => EventDispatcher.On(eventKey, handler);
        
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
            => EventDispatcher.On(eventKey, handler);
        
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
            => EventDispatcher.On(eventKey, handler);

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
            => EventDispatcher.On(eventKey, handler);

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
            => EventDispatcher.Off(eventKey);
        
        /// <summary>
        ///   <para>取消监听事件（无参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off(int eventKey, Action handler)
            => EventDispatcher.Off(eventKey, handler);

        /// <summary>
        ///   <para>取消监听事件（一个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T>(int eventKey, Action<T> handler)
            => EventDispatcher.Off(eventKey, handler);

        /// <summary>
        ///   <para>取消监听事件（两个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T1, T2>(int eventKey, Action<T1, T2> handler)
            => EventDispatcher.Off(eventKey, handler);

        /// <summary>
        ///   <para>取消监听事件（三个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T1, T2, T3>(int eventKey, Action<T1, T2, T3> handler)
            => EventDispatcher.Off(eventKey, handler);

        /// <summary>
        ///   <para>取消监听事件（四个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Off<T1, T2, T3, T4>(int eventKey, Action<T1, T2, T3, T4> handler)
            => EventDispatcher.Off(eventKey, handler);
        
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
        public static void Emit(int eventKey
#if DEBUG
            , [CallerMemberName] string emitMember = null
            , [CallerFilePath] string emitFile = null
            , [CallerLineNumber] int emitLine = 0
#endif
            )
            => EventDispatcher.Emit(eventKey, emitMember, emitFile, emitLine);

        /// <summary>
        ///   <para>发送事件（一个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg">参数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T>(int eventKey, T arg
#if DEBUG
            , [CallerMemberName] string emitMember = null
            , [CallerFilePath] string emitFile = null
            , [CallerLineNumber] int emitLine = 0
#endif
            )
            => EventDispatcher.Emit(eventKey, arg, emitMember, emitFile, emitLine);

        /// <summary>
        ///   <para>发送事件（两个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T1, T2>(int eventKey, T1 arg1, T2 arg2
#if DEBUG
            , [CallerMemberName] string emitMember = null
            , [CallerFilePath] string emitFile = null
            , [CallerLineNumber] int emitLine = 0
#endif
            )
            => EventDispatcher.Emit(eventKey, arg1, arg2, emitMember, emitFile, emitLine);

        /// <summary>
        ///   <para>发送事件（三个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T1, T2, T3>(int eventKey, T1 arg1, T2 arg2, T3 arg3
#if DEBUG
            , [CallerMemberName] string emitMember = null
            , [CallerFilePath] string emitFile = null
            , [CallerLineNumber] int emitLine = 0
#endif
            )
            => EventDispatcher.Emit(eventKey, arg1, arg2, arg3, emitMember, emitFile, emitLine);

        /// <summary>
        ///   <para>发送事件（四个参数）</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        /// <param name="arg4">参数4</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit<T1, T2, T3, T4>(int eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4
#if DEBUG
            , [CallerMemberName] string emitMember = null
            , [CallerFilePath] string emitFile = null
            , [CallerLineNumber] int emitLine = 0
#endif
            )
            => EventDispatcher.Emit(eventKey, arg1, arg2, arg3, arg4, emitMember, emitFile, emitLine);

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
        public static void Emit<T1, T2, T3, T4, T5>(int eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5
#if DEBUG
            , [CallerMemberName] string emitMember = null
            , [CallerFilePath] string emitFile = null
            , [CallerLineNumber] int emitLine = 0
#endif
            )
            => EventDispatcher.Emit(eventKey, arg1, arg2, arg3, arg4, arg5, emitMember, emitFile, emitLine);
        
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
        public static void Emit<T1, T2, T3, T4, T5, T6>(int eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6
#if DEBUG
            , [CallerMemberName] string emitMember = null
            , [CallerFilePath] string emitFile = null
            , [CallerLineNumber] int emitLine = 0
#endif
            )
            => EventDispatcher.Emit(eventKey, arg1, arg2, arg3, arg4, arg5, arg6, emitMember, emitFile, emitLine);
        
        /// <summary>
        ///   <para>判断事件是否已监听</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        /// <param name="handler">事件处理器</param>
        public static bool Has(int eventKey, Delegate handler)
            => EventDispatcher.Has(eventKey, handler);
        
        #endregion

        /// <summary>
        ///   <para>取消所有监听事件</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OffAll() => EventDispatcher.OffAll();

        /// <summary>
        ///   <para>获取字符串事件键的哈希值</para>
        /// </summary>
        /// <param name="eventKey">事件键</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetEventHash(string eventKey)
        {
            if (string.IsNullOrEmpty(eventKey))
                throw new ArgumentException("Event Key cannot be null or empty", nameof(eventKey));

            lock (s_StringToHashLock)
            {
                s_StringToHashCache ??= new Dictionary<string, int>();
                if (!s_StringToHashCache.TryGetValue(eventKey, out var hash))
                {
                    hash = ComputeStableHash(eventKey);
                    s_StringToHashCache[eventKey] = hash;
                }
                return hash;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ComputeStableHash(string value)
        {
            unchecked
            {
                const int fnvPrime = 16777619;
                int hash = (int)2166136261;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= fnvPrime;
                }
                return hash;
            }
        }

        /// <summary>
        ///   <para>清理事件部分</para>
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void CleanupEvent()
        {
            OffAll();
            lock (s_StringToHashLock)
            {
                s_StringToHashCache?.Clear();
            }
        }
    }

#if UNITY_5_3_OR_NEWER
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
#endif
}