namespace Verve.UniEx
{
    using System;
    

    /// <summary>
    /// 模块方法桥接特性 - 标记桥接了当前方法和目标方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ModuleMethodBridgeAttribute : Attribute
    {
        /// <summary> 目标方法路径 </summary>
        public string TargetMethodPath { get; }
        

        public ModuleMethodBridgeAttribute(string method)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentException($"{nameof(method)} cannot be null or empty.");
            }
            TargetMethodPath = method;
        }
        
        public ModuleMethodBridgeAttribute(string moduleName, string submoduleName, string methodName)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
                throw new ArgumentException("moduleName");
            if (string.IsNullOrWhiteSpace(submoduleName))
                throw new ArgumentException("submoduleName");
            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentException("methodName");
            TargetMethodPath = $"{moduleName}.{submoduleName}.{methodName}";
        }
    }
}