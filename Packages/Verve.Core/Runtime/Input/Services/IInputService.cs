namespace Verve.Input
{
    using System;

    
    /// <summary>
    /// 输入系统接口
    /// </summary>
    public interface IInputService : IDisposable, Unit.IUnitService
    {
        /// <summary> 是否有效 </summary>
        bool IsValid { get; }
        /// <summary> 是否启用 </summary>
        bool enabled { get; }
        
        /// <summary>
        /// 启用
        /// </summary>
        void Enable();
        /// <summary>
        /// 禁用
        /// </summary>
        void Disable();

        /// <summary>
        /// 添加监听输入事件
        /// </summary>
        /// <param name="actionName"></param>
        void AddListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Started) where T : struct;
        /// <summary>
        /// 移除监听输入事件
        /// </summary>
        /// <param name="actionName"></param>
        void RemoveListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Started) where T : struct;
        void RemoveListener(string actionName, InputServicePhase phase = InputServicePhase.Started);
        
        void RemoveAllListener();
        
        /// <summary>
        /// 重绑输入
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="rebind"></param>
        /// <returns></returns>
        void RebindingAction(string actionName, InputServiceRebinding rebind, Action<bool> onCompleted = null);

        /// <summary>
        /// 从JSON中加载绑定数据
        /// </summary>
        /// <param name="json"></param>
        void LoadBindingsFromJson(string json);
        /// <summary>
        /// 保存绑定数据为JSON
        /// </summary>
        /// <returns></returns>
        string SaveBindingsAsJson();

        /// <summary>
        /// 模拟输入
        /// </summary>
        void SimulateInputAction<T>(string actionName, T value) where T : struct;
    }
}