#if UNITY_EDITOR
namespace Verve.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;

    /// <summary>
    ///   <para>工具编辑器查询；按工具约束和构建目标筛选接口与实现。</para>
    /// </summary>
    public static class GameToolEditorUtility
    {
        /// <summary>
        ///   <para>查询可由无参构造函数创建的运行时实现。</para>
        /// </summary>
        /// <param name="contract">能力接口。</param>
        /// <param name="includeInternal">是否包含框架默认使用的非公开实现。</param>
        /// <returns>当前构建目标包含的普通工具类；排除编辑器、测试及需要释放的类型。</returns>
        public static Type[] FindRuntimeImplementations(Type contract, bool includeInternal = false)
        {
            if (contract == null) throw new ArgumentNullException(nameof(contract));
            var assemblies = GetPlayerAssemblyNames();
            return TypeCache.GetTypesDerivedFrom(contract)
                .Where(type => GameToolConfiguration.IsSupported(type, contract, includeInternal) && assemblies.Contains(type.Assembly.GetName().Name))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ThenBy(type => type.Assembly.GetName().Name, StringComparer.Ordinal).ToArray();
        }

        /// <summary>
        ///   <para>查询当前构建目标可使用的工具能力接口。</para>
        /// </summary>
        public static Type[] FindToolContracts()
        {
            var assemblies = GetPlayerAssemblyNames();
            return TypeCache.GetTypesDerivedFrom<IGameTool>()
                .Where(type => GameToolConfiguration.IsContract(type) && assemblies.Contains(type.Assembly.GetName().Name))
                .OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray();
        }

        /// <summary>
        ///   <para>取得当前 Player 程序集名称；排除编辑器与测试程序集。</para>
        /// </summary>
        private static HashSet<string> GetPlayerAssemblyNames()
        {
            var assemblies = new HashSet<string>(UnityEditor.Compilation.CompilationPipeline
                .GetAssemblies(UnityEditor.Compilation.AssembliesType.PlayerWithoutTestAssemblies)
                .Select(assembly => assembly.name), StringComparer.Ordinal);
            foreach (var plugin in PluginImporter.GetAllImporters())
            {
                if (plugin.isNativePlugin || !plugin.GetCompatibleWithPlatform(EditorUserBuildSettings.activeBuildTarget)) continue;
                assemblies.Add(System.IO.Path.GetFileNameWithoutExtension(plugin.assetPath));
            }
            return assemblies;
        }

    }
}
#endif
