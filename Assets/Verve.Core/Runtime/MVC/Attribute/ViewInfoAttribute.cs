namespace Verve.MVC
{
    using System;
    using Loader;
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ViewInfoAttribute : Attribute
    {
        public string ResourcePath { get; }
        public Type LoaderType { get; }

        public ViewInfoAttribute(string resourcePath, Type loaderType)
        {
            LoaderType = loaderType ?? throw new ArgumentException($"{nameof(LoaderType)} must implement ${nameof(IAssetLoader)}");;
            ResourcePath = resourcePath;
        }
    }
}