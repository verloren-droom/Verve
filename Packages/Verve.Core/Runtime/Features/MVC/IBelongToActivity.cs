namespace Verve.MVC
{
    /// <summary>
    ///   <para>从属于哪个活动接口</para>
    /// </summary>
    public interface IBelongToActivity
    {
        /// <summary>
        ///   <para>获取活动接口</para>
        /// </summary>
        /// <returns>
        ///   <para>活动接口实例</para>
        /// </returns>
        IActivity GetActivity();
    }
}