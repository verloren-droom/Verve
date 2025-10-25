namespace Verve.UniEx
{
    using System;
    

    /// <summary>
    /// 模块方法桥接特性 - 标记桥接了当前方法和目标方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ModuleMethodBridgeAttribute : Attribute
    {
        /// <summary> 模块名 </summary>
        public string ModuleName { get; }
        public string SubmoduleName { get; }
        public string MethodName { get; }
        

        public ModuleMethodBridgeAttribute(string method)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentException($"{nameof(method)} cannot be null or empty.");
            }
            ModuleName = method.Split('.')[0];
            SubmoduleName = method.Split('.')[1];
            MethodName = method.Split('.')[2];
        }
        
        public ModuleMethodBridgeAttribute(string moduleName, string submoduleName, string methodName)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
                throw new ArgumentException("moduleName");
            ModuleName = moduleName;
            if (string.IsNullOrWhiteSpace(submoduleName))
                throw new ArgumentException("submoduleName");
            SubmoduleName = submoduleName;
            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentException("methodName");
            MethodName = methodName;
        }
    }
}