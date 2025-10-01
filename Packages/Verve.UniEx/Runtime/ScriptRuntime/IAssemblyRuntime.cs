#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.ScriptRuntime
{
    using System.Threading;
    using System.Reflection;
    using System.Threading.Tasks;

    
    /// <summary>
    /// 程序集运行时接口
    /// </summary>
    public interface IAssemblyRuntime
    {
        /// <summary>
        /// 异步加载程序集
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        /// <param name="assemblyData">程序集数据</param>
        /// <param name="pdbData">程序集调试符号数据</param>
        /// <param name="ct">取消令牌</param>
        Task<Assembly> LoadAssemblyAsync(
            string assemblyName,
            byte[] assemblyData,
            byte[] pdbData = null,
            CancellationToken ct = default);
        
        /// <summary>
        /// 创建热更类型实例
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <param name="args">参数</param>
        object CreateInstance(string typeName, params object[] args);
        
        /// <summary>
        /// 调用静态方法
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="args">参数</param>
        object InvokeStaticMethod(string typeName, string methodName, params object[] args);

        /// <summary>
        /// 调用实例方法
        /// </summary>
        /// <param name="instance">实例</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="args">参数</param>
        object InvokeInstanceMethod(object instance, string methodName, params object[] args);
    }
}

#endif