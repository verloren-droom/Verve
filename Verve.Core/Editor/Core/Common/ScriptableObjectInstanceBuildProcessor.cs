#if UNITY_EDITOR
namespace Verve.Editor
{
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;

    /// <summary>
    ///   <para>全局资源构建校验；拒绝重复或错放的资源。</para>
    /// </summary>
    internal sealed class ScriptableObjectInstanceBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(ScriptableObjectInstanceBase<>)))
            {
                if (type.IsAbstract || type.ContainsGenericParameters) continue;
                var parent = type.BaseType;
                while (parent != null && (!parent.IsGenericType || parent.GetGenericTypeDefinition() != typeof(ScriptableObjectInstanceBase<>)))
                    parent = parent.BaseType;
                if (parent == null || parent.GenericTypeArguments[0] != type)
                    throw new BuildFailedException($"{type.FullName} must use itself as the ScriptableObjectInstanceBase type parameter.");
                try { parent.GetMethod("LoadAsset", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null); }
                catch (TargetInvocationException error) { throw new BuildFailedException(error.InnerException); }
            }
        }
    }
}
#endif
