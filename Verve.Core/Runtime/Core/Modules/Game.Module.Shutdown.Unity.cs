#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Threading.Tasks;
    using UnityEngine;

    public static partial class Game
    {
        /// <summary>
        ///   <para>注册应用退出时的模块清理。</para>
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterModuleShutdown()
        {
            Application.quitting -= OnModulesApplicationQuit;
            Application.quitting += OnModulesApplicationQuit;
#if !UNITY_EDITOR
            Application.wantsToQuit -= OnModulesWantsToQuit;
            Application.wantsToQuit += OnModulesWantsToQuit;
#endif
        }

        /// <summary>
        ///   <para>应用退出时完成清理。</para>
        /// </summary>
        private static async void OnModulesApplicationQuit() => await ShutdownModulesAsync();

#if !UNITY_EDITOR
        /// <summary>
        ///   <para>正常退出时保持循环运行，直到模块完成清理。</para>
        /// </summary>
        private static bool OnModulesWantsToQuit()
        {
            var shutdown = ShutdownModulesAsync();
            if (shutdown.IsCompleted) return true;
            FinishModulesQuitAsync(shutdown);
            return false;
        }

        /// <summary>
        ///   <para>清理完成后继续退出；释放错误仍报告到引擎。</para>
        /// </summary>
        /// <param name="shutdown">已经开始的清理任务。</param>
        private static async void FinishModulesQuitAsync(Task shutdown)
        {
            try { await shutdown; }
            catch (Exception error) { Debug.LogException(error); }
            Application.Quit();
        }
#endif

#if UNITY_EDITOR
        /// <summary>
        ///   <para>编辑器重载与停止运行时释放应用模块。</para>
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterModulesEditorLifetime()
        {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += OnModulesApplicationQuit;
            UnityEditor.EditorApplication.playModeStateChanged += OnModulesPlayModeChanged;
        }

        /// <summary>
        ///   <para>停止运行后允许创建新的模块会话。</para>
        /// </summary>
        /// <param name="state">运行模式状态。</param>
        private static async void OnModulesPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state != UnityEditor.PlayModeStateChange.EnteredEditMode) return;
            try { await ShutdownModulesAsync(); }
            finally
            {
                s_ModulesShutdown = null;
            }
        }
#endif
    }
}

#endif