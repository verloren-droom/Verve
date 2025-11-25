namespace Verve.Input
{
    /// <summary>
    ///   <para>输入服务重绑定</para>
    /// </summary>
    [System.Serializable]
    public struct InputServiceRebinding
    {
        /// <summary>
        ///   <para>路径</para>
        /// </summary>
        public string path;
        public int bindingIndex;
        public string cancelKey;
        public string filter;
    }
}