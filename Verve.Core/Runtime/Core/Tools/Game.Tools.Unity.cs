#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using UnityEngine;

    public static partial class Game
    {
        /// <summary>
        ///   <para>读取项目工具配置；编辑态与运行态共用同一份设置。</para>
        /// </summary>
        private static GameToolConfiguration LoadToolConfiguration()
        {
            return GameToolSettings.TryGetInstance(out var settings) ? settings.configuration : null;
        }

        /// <summary>
        ///   <para>预读实现映射；工具仍在首次调用时构造。</para>
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void LoadToolsOnStartup() => _ = Tools;

#if UNITY_EDITOR
        /// <summary>
        ///   <para>资源导入完成后预读配置，供编辑器后台任务使用。</para>
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void LoadToolsInEditor() => UnityEditor.EditorApplication.delayCall += LoadToolsOnStartup;
#endif
    }
}

#endif