namespace Verve
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///   <para>配置表接口；安装完成即可查询，加载错误直接抛出。</para>
    /// </summary>
    public interface IConfigTables : IGameModule
    {
        /// <summary>
        ///   <para>获取配置表。</para>
        /// </summary>
        /// <param name="tableName">表名称。</param>
        ConfigTable Get(string tableName);
        /// <summary>
        ///   <para>异步重新加载。</para>
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        Task ReloadAsync(CancellationToken cancellationToken = default);
    }
}