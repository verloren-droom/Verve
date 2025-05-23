namespace Verve
{
    /// <summary>
    /// 单例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class InstanceBase<T> where T : class, new()
    {
        private static T m_Instance;

        public static T Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new T();
                    (m_Instance as InstanceBase<T>).OnInitialized();
                }
                return m_Instance;
            }
            private set => m_Instance = value;
        }

        protected InstanceBase() { }
        
        /// <summary>
        /// 初始化（单例首次被创建后调用）
        /// </summary>
        protected virtual void OnInitialized() { }
    }
}