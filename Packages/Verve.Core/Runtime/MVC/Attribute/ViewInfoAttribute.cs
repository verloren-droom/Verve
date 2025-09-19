namespace Verve.MVC
{
    using System;
    using Loader;
    
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ViewInfoAttribute : Attribute
    {
        public string ResourcePath { get; }

        public ViewInfoAttribute(string resourcePath)
        {
            ResourcePath = resourcePath;
        }
    }
}