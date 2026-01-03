#if UNITY_5_3_OR_NEWER

namespace Verve.ScriptRuntime
{
    using System.Threading;
    using System.Reflection;
    using System.Threading.Tasks;

    
    /// <summary>
    ///   <para>程序集运行时接口</para>
    /// </summary>
    public interface IAssemblyRuntime
    {
        /// <summary>
        ///   <para>异步加载程序集</para>
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        /// <param name="assemblyData">程序集数据</param>
        /// <param name="pdbData">程序集调试符号数据</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>
        ///   <para>程序集</para>
        /// </returns>
        Task<Assembly> LoadAssemblyAsync(
            string assemblyName,
            byte[] assemblyData,
            byte[] pdbData = null,
            CancellationToken ct = default);
        
        /// <summary>
        ///   <para>创建热更类型实例</para>
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <param name="args">参数</param>
        /// <returns>
        ///   <para>实例</para>
        /// </returns>
        object CreateInstance(string typeName, params object[] args);
        
        /// <summary>
        ///   <para>调用静态方法</para>
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="args">参数</param>
        /// <returns>
        ///   <para>返回值</para>
        /// </returns>
        object InvokeStaticMethod(string typeName, string methodName, params object[] args);

        /// <summary>
        ///   <para>调用实例方法</para>
        /// </summary>
        /// <param name="instance">实例</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="args">参数</param>
        /// <returns>
        ///   <para>返回值</para>
        /// </returns>
        object InvokeInstanceMethod(object instance, string methodName, params object[] args);
    }
}

#endif