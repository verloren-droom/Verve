namespace Verve.Timer
{
    using System;
    
    
    /// <summary>
    ///   <para>计时器接口</para>
    /// </summary>
    public interface ITimer
    {
        /// <summary>
        ///   <para>时间缩放</para>
        /// </summary>
        float TimeScale { get; set; }
        
        /// <summary>
        ///   <para>是否正在运行</para>
        /// </summary>
        public bool IsRunning { get; set; }
        
        /// <summary>
        ///   <para>添加计时器</para>
        /// </summary>
        /// <param name="duration">持续时间</param>
        /// <param name="onComplete">完成回调</param>
        /// <param name="loop">是否循环</param>
        /// <returns>
        ///   <para>计时器ID</para>
        /// </returns>
        int AddTimer(float duration, Action onComplete, bool loop = false);
        
        /// <summary>
        ///   <para>移除计时器</para>
        /// </summary>
        /// <param name="id">计时器ID</param>
        void RemoveTimer(int id);
        
        /// <summary>
        ///   <para>移除计时器</para>
        /// </summary>
        /// <param name="onComplete">完成回调</param>
        void RemoveTimer(Action onComplete);
        
        /// <summary>
        ///   <para>清空计时器</para>
        /// </summary>
        void ClearTimer();
    }
}