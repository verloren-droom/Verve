namespace Verve.MVC
{
    using System;
    
    
    /// <summary>
    /// 视图信息
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ViewInfoAttribute : Attribute
    {
        /// <summary> 资源路径 </summary>
        public string ResourcePath { get; }

        
        public ViewInfoAttribute(string resourcePath)
        {
            ResourcePath = resourcePath;
        }
    }
}