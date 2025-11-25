namespace Verve.Input
{
    using System;

    
    /// <summary>
    ///   <para>输入接口</para>
    /// </summary>
    public interface IInput : IDisposable
    {
        /// <summary>
        ///   <para>是否有效</para>
        /// </summary>
        bool IsValid { get; }
        
        /// <summary>
        ///   <para>添加监听输入事件</para>
        /// </summary>
        /// <param name="actionName">事件名</param>
        /// <param name="onAction">回调</param>
        /// <param name="phase">输入阶段</param>
        /// <typeparam name="T">输入数据类型</typeparam>
        void AddListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Started) where T : struct;
        
        /// <summary>
        ///   <para>移除监听输入事件</para>
        /// </summary>
        /// <param name="actionName">事件名</param>
        /// <param name="onAction">回调</param>
        /// <param name="phase">输入阶段</param>
        /// <typeparam name="T">输入数据类型</typeparam>
        void RemoveListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Started) where T : struct;
        
        /// <summary>
        ///   <para>移除监听输入事件</para>
        /// </summary>
        /// <param name="actionName">事件名</param>
        /// <param name="phase">输入阶段</param>
        void RemoveListener(string actionName, InputServicePhase phase = InputServicePhase.Started);
        
        /// <summary>
        ///   <para>移除所有监听输入事件</para>
        /// </summary>
        void RemoveAllListener();
        
        /// <summary>
        ///   <para>重绑输入</para>
        /// </summary>
        /// <param name="actionName">事件名</param>
        /// <param name="rebind">重绑</param>
        /// <param name="onCompleted">完成回调</param>
        void RebindingAction(string actionName, InputServiceRebinding rebind, Action<bool> onCompleted = null);

        /// <summary>
        ///   <para>从JSON中加载绑定数据</para>
        /// </summary>
        /// <param name="json">绑定数据</param>
        void LoadBindingsFromJson(string json);
        
        /// <summary>
        ///   <para>保存绑定数据为JSON</para>
        /// </summary>
        /// <returns>
        ///   <para>绑定数据</para>
        /// </returns>
        string SaveBindingsAsJson();

        /// <summary>
        ///   <para>模拟输入</para>
        /// </summary>
        /// <param name="actionName">事件名</param>
        /// <param name="value">值</param>
        void SimulateInputAction<T>(string actionName, T value) where T : struct;
    }
}