namespace Verve
{
    using System;
    using System.Reflection;
    
    
    /// <summary>
    /// 功能依赖注入特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FeatureDependencyAttribute : Attribute { }
    
    
    internal static class FeatureDependencyInjector
    {
        public static void InjectDependencies(IGameFeature feature)
        {
            var type = feature.GetType();
            var fields = type.GetFields(
                BindingFlags.Instance | 
                BindingFlags.NonPublic | 
                BindingFlags.Public |
                BindingFlags.Static
            );
            
            foreach (var field in fields)
            {
                if (!field.IsDefined(typeof(FeatureDependencyAttribute), false))
                    continue;
                
                var dependencyType = field.FieldType;

                var getFeatureMethod = typeof(GameFeaturesRuntime).GetMethod(
                    nameof(GameFeaturesRuntime.GetFeature),
                    1,
                    Type.EmptyTypes
                )?.MakeGenericMethod(dependencyType);

                
                var dependency = getFeatureMethod.Invoke(
                    GameFeaturesSystem.Runtime, 
                    null
                );
                    
                field.SetValue(feature, dependency);
            }
        }
    }
}