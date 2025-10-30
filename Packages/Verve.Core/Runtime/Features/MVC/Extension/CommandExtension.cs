namespace Verve.MVC
{
    /// <summary>
    ///   <para>MVC命令扩展类</para>
    /// </summary>
    public static class CommandExtension
    {
        /// <summary>
        ///   <para>获取模型</para>
        /// </summary>
        /// <typeparam name="T">模型类型</typeparam>
        /// <returns>
        ///   <para>模型实例</para>
        /// </returns>
        public static T GetModel<T>(this ICommand self)
            where T : class, IModel, new()
            => self.GetActivity()?.GetModel<T>();
    }
}