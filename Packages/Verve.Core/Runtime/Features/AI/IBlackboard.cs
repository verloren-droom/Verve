namespace Verve.AI
{
    using System;
    
    
    /// <summary>
    ///   <para>黑板接口</para>
    /// </summary>
    public interface IBlackboard : IDisposable
    {
        /// <summary>
        ///   <para>设置黑板值</para>
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <typeparam name="T">值类型</typeparam>
        void SetValue<T>(string key, in T value);
        
        /// <summary>
        ///   <para>获取黑板值</para>
        /// </summary>
        /// /// <param name="key">键</param>
        /// <param name="defaultValue">默认值</param>
        /// <typeparam name="T">值类型</typeparam>
        T GetValue<T>(string key, T defaultValue = default);
        
        /// <summary>
        ///   <para>移除黑板值</para>
        /// </summary>
        /// <param name="key">键</param>
        void RemoveValue(string key);
        
        /// <summary>
        ///   <para>判断黑板是否包含指定键</para>
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>
        ///   <para>包含指定键</para>
        /// </returns>
        bool HasValue(string key);
    }
}