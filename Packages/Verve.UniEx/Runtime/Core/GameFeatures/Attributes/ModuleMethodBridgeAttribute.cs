namespace Verve.UniEx
{
    using System;
    
    /// <summary>
    /// 模块方法桥接特性 - 标记需要生成桥接扩展的方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ModuleMethodBridgeAttribute : Attribute
    {
        /// <summary> 目标模块名 </summary>
        public string TargetModule { get; }
        
        /// <summary> 目标子模块名 </summary>
        public string TargetSubmodule { get; }
        
        /// <summary> 目标方法名 </summary>
        public string TargetMethod { get; }
        
        /// <summary> 扩展方法后缀 </summary>
        public string ExtensionSuffix { get; }

        public ModuleMethodBridgeAttribute(string targetMethod, string extensionSuffix = null)
        {
            if (string.IsNullOrEmpty(targetMethod))
                throw new ArgumentException("Target method cannot be null or empty");
                
            var parts = targetMethod.Split('.');
            if (parts.Length != 3)
                throw new ArgumentException("Target method must be in format: Module.Submodule.Method");
                
            TargetModule = parts[0];
            TargetSubmodule = parts[1];
            TargetMethod = parts[2];
            ExtensionSuffix = extensionSuffix ?? TargetSubmodule;
        }
        
        public ModuleMethodBridgeAttribute(string targetModule, string targetSubmodule, string targetMethod, string extensionSuffix = null)
        {
            TargetModule = targetModule ?? throw new ArgumentException("targetModule");
            TargetSubmodule = targetSubmodule ?? throw new ArgumentException("targetSubmodule");
            TargetMethod = targetMethod ?? throw new ArgumentException("targetMethod");
            ExtensionSuffix = extensionSuffix ?? targetSubmodule;
        }
    }
    
    /// <summary>
    /// 桥接参数特性 - 标记方法中需要桥接的参数
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class BridgeParameterAttribute : Attribute
    {
        /// <summary> 桥接参数名 </summary>
        public string ParameterName { get; }
        
        public BridgeParameterAttribute(string parameterName = null)
        {
            ParameterName = parameterName;
        }
    }
}