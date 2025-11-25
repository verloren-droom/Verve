namespace Verve.Input
{
    /// <summary>
    ///   <para>输入服务上下文</para>
    /// </summary>
    /// <typeparam name="T">输入数据类型</typeparam>
    [System.Serializable]
    public struct InputServiceContext<T> where T : struct
    {
        /// <summary>
        ///   <para>输入数据</para>
        /// </summary>
        public T value;
        
        /// <summary>
        ///   <para>输入名</para>
        /// </summary>
        public string actionName;
        
        /// <summary>
        ///   <para>输入阶段</para>
        /// </summary>
        public InputServicePhase phase;
        
        /// <summary>
        ///   <para>输入设备类型</para>
        /// </summary>
        public InputServiceDeviceType deviceType;
        
        /// <summary>
        ///   <para>输入绑定</para>
        /// </summary>
        public InputServiceBinding binding;
    }
}