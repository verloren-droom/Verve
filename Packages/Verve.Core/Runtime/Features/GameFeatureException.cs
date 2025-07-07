namespace Verve
{
    using System;
    
    
    /// <summary> 游戏功能异常 </summary>
    public class GameFeatureException : Exception
    {
        public GameFeatureException(string message) : base(message) { }
    }
    
    
    /// <summary> 游戏功能丢失依赖异常 </summary>
    public class GameFeatureMissingDependencyException : GameFeatureException
    {
        public GameFeatureMissingDependencyException(string featureName, string dependencyName)
            : base($"{featureName} missing dependency {dependencyName}") { }
    }

    
    /// <summary> 游戏功能未激活异常 </summary>
    public class GameFeatureNotActiveException : GameFeatureException
    {
        public GameFeatureNotActiveException(string featureName, FeatureState state)
            : base($"{featureName} is not active (state: {state})") { }
    }
    
    
    /// <summary> 游戏功能还存在激活引用异常 </summary>
    public class GameFeatureHasActiveReferencesException : GameFeatureException
    {
        public GameFeatureHasActiveReferencesException(string featureName, int referenceCount)
            : base($"Cannot unload feature '{featureName}' as it has {referenceCount} active references") { }
    }
    
    
    /// <summary> 游戏功能未注册异常 </summary>
    public class GameFeatureNotRegisteredException : GameFeatureException
    {
        public GameFeatureNotRegisteredException(string featureName)
            : base($"{featureName} is not registered") { }
    }
    
    
    /// <summary> 游戏功能加载异常 </summary>
    public class GameFeatureLoadException : GameFeatureException
    {
        public GameFeatureLoadException(string featureName, object message)
            : base($"Failed to load feature '{featureName}' because: {message}") { }
    }
    
    
    /// <summary> 游戏功能卸载异常 </summary>
    public class GameFeatureUnloadException : GameFeatureException
    {
        public GameFeatureUnloadException(string featureName, object message)
            : base($"Failed to unload feature '{featureName}' because: {message}") { }
    }
    
    
    /// <summary> 游戏功能激活异常 </summary>
    public class GameFeatureActivateException : GameFeatureException
    {
        public GameFeatureActivateException(string featureName, object message)
            : base($"Failed to active feature '{featureName}' because: {message}") { }
    }
    
    
    /// <summary> 游戏功能禁用异常 </summary>
    public class GameFeatureDeactivateException : GameFeatureException
    {
        public GameFeatureDeactivateException(string featureName, object message)
            : base($"Failed to deactivate feature '{featureName}' because: {message}") { }
    }
}