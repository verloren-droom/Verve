#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using UnityEngine.Scripting;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>游戏模块调试窗口。</para>
    /// </summary>
    [Preserve, DebugItem("Module")]
    sealed class GameModuleDebugTabWindow : DebugTabWindow
    {
        /// <summary>
        ///   <para>滚动位置。</para>
        /// </summary>
        private Vector2 m_ScrollPosition;
        /// <summary>
        ///   <para>句柄。</para>
        /// </summary>
        private readonly List<GameModulesHandle> m_Handles = new(8);
        /// <summary>
        ///   <para>模块容器。</para>
        /// </summary>
        private readonly List<IGameModule> m_Modules = new(32);
        /// <summary>
        ///   <para>模块注册的 Tick 对象。</para>
        /// </summary>
        private readonly List<GameModuleTickRegistration> m_OwnedTicks = new(16);
        /// <summary>
        ///   <para>搜索。</para>
        /// </summary>
        private string m_Search = "";
        /// <summary>
        ///   <para>模块折叠栏。</para>
        /// </summary>
        private bool m_ModulesFoldout = true;
        /// <summary>
        ///   <para>句柄总数。</para>
        /// </summary>
        private int m_HandlesTotal;
        /// <summary>
        ///   <para>显示的句柄数量。</para>
        /// </summary>
        private int m_HandlesShown;
        /// <summary>
        ///   <para>模块总数。</para>
        /// </summary>
        private int m_ModulesTotal;
        /// <summary>
        ///   <para>显示的模块数量。</para>
        /// </summary>
        private int m_ModulesShown;
        /// <summary>
        ///   <para>已安装模块数量。</para>
        /// </summary>
        private int m_ModulesInstalled;
        /// <summary>
        ///   <para>模块注册的 Tick 对象总数。</para>
        /// </summary>
        private int m_OwnedTicksTotal;
        /// <summary>
        ///   <para>表头样式。</para>
        /// </summary>
        private GUIStyle m_HeaderStyle;
        /// <summary>
        ///   <para>详情样式。</para>
        /// </summary>
        private GUIStyle m_DetailStyle;
        /// <summary>
        ///   <para>区域样式。</para>
        /// </summary>
        private GUIStyle m_SectionStyle;
        /// <summary>
        ///   <para>样式就绪。</para>
        /// </summary>
        private bool m_StylesReady;
        /// <summary>
        ///   <para>样式字体大小。</para>
        /// </summary>
        private int m_StyleFontSize = -1;
        /// <summary>
        ///   <para>样式字体颜色。</para>
        /// </summary>
        private Color m_StyleFontColor;

        /// <summary>
        ///   <para>创建模块调试标签页窗口。</para>
        /// </summary>
        /// <param name="settings">设置。</param>
        [Preserve]
        public GameModuleDebugTabWindow(DebugTabWindowSettings settings) : base(settings) { }

        /// <inheritdoc />
        public override void OnShow() { }

        /// <inheritdoc />
        public override void OnHide() { }

        /// <inheritdoc />
        public override void Draw()
        {
            EnsureStyles();

            Game.CopyModuleHandlesTo(m_Handles);
            if (m_Handles.Count == 0)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("No active GameModules handles.", GUI.skin.label);
                GUILayout.EndVertical();
                return;
            }

            CalculateStats();
            DrawToolbar();
            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
            DrawModulesSection();
            GUILayout.EndScrollView();
        }

        /// <summary>
        ///   <para>初始化尚未创建的样式。</para>
        /// </summary>
        private void EnsureStyles()
        {
            if (!m_StylesReady)
            {
                m_StylesReady = true;
                m_HeaderStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                m_DetailStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
                m_SectionStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(8, 8, 6, 6) };
            }
            ApplyStyleSettings();
        }

        /// <summary>
        ///   <para>应用样式设置。</para>
        /// </summary>
        private void ApplyStyleSettings()
        {
            var fontSize = Settings?.FontSize ?? 12;
            var fontColor = Settings?.FontColor ?? Color.white;
            if (m_StyleFontSize == fontSize && m_StyleFontColor == fontColor) return;
            m_StyleFontSize = fontSize;
            m_StyleFontColor = fontColor;
            m_HeaderStyle.fontSize = fontSize + 1;
            m_HeaderStyle.normal.textColor = fontColor;
            m_DetailStyle.fontSize = Mathf.Max(10, fontSize - 1);
            m_DetailStyle.normal.textColor = fontColor;
        }

        /// <summary>
        ///   <para>绘制工具栏。</para>
        /// </summary>
        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            m_ModulesFoldout = GUILayout.Toggle(
                m_ModulesFoldout,
                $"Containers({m_HandlesShown}/{m_HandlesTotal}) Modules({m_ModulesShown}/{m_ModulesTotal})",
                GUILayout.Width(320));
            GUILayout.FlexibleSpace();
            m_Search = GUILayout.TextField(m_Search ?? "", GUILayout.Width(220));
            GUILayout.EndHorizontal();

            if (m_ModulesFoldout)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(
                    $"Installed: {m_ModulesInstalled} / {m_ModulesTotal}   Owned Ticks: {m_OwnedTicksTotal}   Active Containers: {m_HandlesTotal}",
                    m_DetailStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        ///   <para>计算显示统计。</para>
        /// </summary>
        private void CalculateStats()
        {
            m_HandlesTotal = m_Handles.Count;
            m_HandlesShown = 0;
            m_ModulesTotal = 0;
            m_ModulesShown = 0;
            m_ModulesInstalled = 0;
            m_OwnedTicksTotal = 0;

            for (int i = 0; i < m_Handles.Count; i++)
            {
                var handle = m_Handles[i];
                if (!TryCopyHandleModules(handle, out var modules)) continue;
                bool handleMatches = MatchesSearch(handle);
                bool handleShown = handleMatches;
                for (int j = 0; j < m_Modules.Count; j++)
                {
                    var module = m_Modules[j];
                    if (module is GameModule gameModule && gameModule.IsInstalled) m_ModulesInstalled++;
                    m_OwnedTicksTotal += CountOwnedTicks(modules, module);

                    m_ModulesTotal++;
                    if (handleMatches || MatchesModuleSearch(modules, module))
                    {
                        m_ModulesShown++;
                        handleShown = true;
                    }
                }

                if (!handleShown && string.IsNullOrEmpty(m_Search) && m_Modules.Count == 0)
                {
                    handleShown = true;
                }

                if (handleShown) m_HandlesShown++;
            }
        }

        /// <summary>
        ///   <para>匹配搜索。</para>
        /// </summary>
        /// <param name="instance">实例。</param>
        private bool MatchesSearch(object instance)
        {
            if (string.IsNullOrEmpty(m_Search)) return true;
            var type = instance?.GetType();
            var name = type?.FullName ?? "null";
            var assembly = type?.Assembly.GetName().Name ?? "Unknown";
            var filter = m_Search;
            if (name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (assembly.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (instance is IGameModule && GameModuleExposedTypeUtility.GetExposedTypesText(type).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        /// <summary>
        ///   <para>匹配模块搜索。</para>
        /// </summary>
        /// <param name="modules">模块。</param>
        /// <param name="module">模块。</param>
        private bool MatchesModuleSearch(GameModules modules, IGameModule module)
        {
            if (MatchesSearch(module)) return true;
            if (MatchesDependencySearch(module)) return true;
            return MatchesOwnedTickSearch(modules, module);
        }

        /// <summary>
        ///   <para>匹配依赖搜索。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        private bool MatchesDependencySearch(IGameModule module)
        {
            if (module == null || string.IsNullOrEmpty(m_Search)) return false;

            try
            {
                var dependencies = GameModuleDependencyUtility.GetDependencies(module.GetType());
                for (int i = 0; i < dependencies.Length; i++)
                {
                    if (MatchesTypeSearch(dependencies[i])) return true;
                }
            }
            catch (Exception ex)
            {
                return ex.Message != null &&
                       ex.Message.IndexOf(m_Search, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return false;
        }

        /// <summary>
        ///   <para>匹配模块注册的 Tick 对象。</para>
        /// </summary>
        /// <param name="modules">模块。</param>
        /// <param name="module">模块。</param>
        private bool MatchesOwnedTickSearch(GameModules modules, IGameModule module)
        {
            if (modules == null || module == null || string.IsNullOrEmpty(m_Search)) return false;

            try
            {
                modules.CopyOwnedTickRegistrationsTo(module, m_OwnedTicks);
                for (int i = 0; i < m_OwnedTicks.Count; i++)
                {
                    var tickSystem = m_OwnedTicks[i].tickSystem;
                    if (MatchesSearch(tickSystem)) return true;
                    if (GameModules.GetTickPhasesText(tickSystem).IndexOf(m_Search, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                }
            }
            catch (ObjectDisposedException)
            {
                return false;
            }

            return false;
        }

        /// <summary>
        ///   <para>匹配类型搜索。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private bool MatchesTypeSearch(Type type)
        {
            if (type == null || string.IsNullOrEmpty(m_Search)) return false;

            var filter = m_Search;
            var name = type.FullName ?? type.Name ?? "null";
            if (name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) return true;

            var assembly = type.Assembly.GetName().Name ?? "Unknown";
            return assembly.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        ///   <para>匹配搜索。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
        private bool MatchesSearch(GameModulesHandle handle)
        {
            if (handle == null) return false;
            if (string.IsNullOrEmpty(m_Search)) return true;

            var filter = m_Search;
            if (handle.Id.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return handle.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        ///   <para>统计模块注册的 Tick 对象。</para>
        /// </summary>
        /// <param name="modules">模块。</param>
        /// <param name="module">模块。</param>
        private int CountOwnedTicks(GameModules modules, IGameModule module)
        {
            if (modules == null || module == null) return 0;

            try
            {
                modules.CopyOwnedTickRegistrationsTo(module, m_OwnedTicks);
                return m_OwnedTicks.Count;
            }
            catch (ObjectDisposedException)
            {
                return 0;
            }
        }

        /// <summary>
        ///   <para>绘制模块区域。</para>
        /// </summary>
        private void DrawModulesSection()
        {
            if (!m_ModulesFoldout) return;
            for (int i = 0; i < m_Handles.Count; i++)
            {
                DrawHandleSection(m_Handles[i]);
            }
        }

        /// <summary>
        ///   <para>绘制句柄区域。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
        private void DrawHandleSection(GameModulesHandle handle)
        {
            if (!TryCopyHandleModules(handle, out var modules)) return;
            bool handleMatches = MatchesSearch(handle);
            int shownCount = 0;
            for (int i = 0; i < m_Modules.Count; i++)
            {
                if (handleMatches || MatchesModuleSearch(modules, m_Modules[i]))
                {
                    shownCount++;
                }
            }

            if (!handleMatches && shownCount == 0 && !(string.IsNullOrEmpty(m_Search) && m_Modules.Count == 0))
            {
                return;
            }

            GUILayout.BeginVertical(m_SectionStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Container #{handle.Id}", m_HeaderStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"State: {GetContainerState(modules)}", m_DetailStyle);
            GUILayout.Space(12f);
            GUILayout.Label($"Modules: {shownCount}/{m_Modules.Count}", m_DetailStyle);
            GUILayout.EndHorizontal();

            if (m_Modules.Count == 0)
            {
                GUILayout.Label("No modules.", m_DetailStyle);
                GUILayout.EndVertical();
                return;
            }

            int displayIndex = 0;
            for (int i = 0; i < m_Modules.Count; i++)
            {
                var module = m_Modules[i];
                if (!handleMatches && !MatchesModuleSearch(modules, module)) continue;
                DrawModuleCard(handle, modules, ++displayIndex, module);
            }

            GUILayout.EndVertical();
        }

        /// <summary>
        ///   <para>尝试读取句柄中的模块引用。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
        /// <param name="modules">模块。</param>
        private bool TryCopyHandleModules(GameModulesHandle handle, out GameModules modules)
        {
            modules = null;
            if (handle == null || !handle.TryGetModules(out modules))
            {
                m_Modules.Clear();
                return false;
            }

            try
            {
                modules.CopyModulesTo(m_Modules);
                return true;
            }
            catch (ObjectDisposedException)
            {
                m_Modules.Clear();
                return false;
            }
        }

        /// <summary>
        ///   <para>绘制模块卡片。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
        /// <param name="modules">模块。</param>
        /// <param name="displayIndex">显示索引。</param>
        /// <param name="module">模块。</param>
        private void DrawModuleCard(GameModulesHandle handle, GameModules modules, int displayIndex, IGameModule module)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            var type = module?.GetType();
            var name = type?.FullName ?? "null";
            var assembly = type?.Assembly.GetName().Name ?? "Unknown";
            var tickText = GameModules.GetTickPhasesText(module);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{displayIndex}. {name}", m_DetailStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Copy", GUILayout.Width(60)))
            {
                GUIUtility.systemCopyBuffer = name;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label($"Container: #{handle.Id}", m_DetailStyle);
            GUILayout.Label($"Assembly: {assembly}", m_DetailStyle);
            GUILayout.Label($"State: {GetModuleState(module)}", m_DetailStyle);
            GUILayout.Label($"Exposed Types: {GameModuleExposedTypeUtility.GetExposedTypesText(type)}", m_DetailStyle);
            GUILayout.Label($"Ticks: {tickText}", m_DetailStyle);
            DrawDependencies(module);
            DrawOwnedTicks(modules, module);
            GUILayout.EndVertical();
        }

        /// <summary>
        ///   <para>绘制依赖。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        private void DrawDependencies(IGameModule module)
        {
            if (module == null)
            {
                GUILayout.Label("Dependencies: None", m_DetailStyle);
                return;
            }

            Type[] dependencies;
            try
            {
                dependencies = GameModuleDependencyUtility.GetDependencies(module.GetType());
            }
            catch (Exception ex)
            {
                GUILayout.Label($"Dependencies: Error - {ex.Message}", m_DetailStyle);
                return;
            }

            if (dependencies == null || dependencies.Length == 0)
            {
                GUILayout.Label("Dependencies: None", m_DetailStyle);
                return;
            }

            GUILayout.Label("Dependencies:", m_DetailStyle);
            for (int i = 0; i < dependencies.Length; i++)
            {
                var dependencyType = dependencies[i];
                var status = IsDependencyInstalled(dependencyType) ? "Installed" : "Missing";
                GUILayout.Label($"  - {GameModuleUtility.GetTypeDisplayName(dependencyType)} [{status}]", m_DetailStyle);
            }
        }

        /// <summary>
        ///   <para>绘制模块注册的 Tick 对象。</para>
        /// </summary>
        /// <param name="modules">模块。</param>
        /// <param name="module">模块。</param>
        private void DrawOwnedTicks(GameModules modules, IGameModule module)
        {
            if (modules == null || module == null)
            {
                GUILayout.Label("Owned Ticks: None", m_DetailStyle);
                return;
            }

            try
            {
                modules.CopyOwnedTickRegistrationsTo(module, m_OwnedTicks);
            }
            catch (ObjectDisposedException)
            {
                GUILayout.Label("Owned Ticks: Container disposed", m_DetailStyle);
                return;
            }

            if (m_OwnedTicks.Count == 0)
            {
                GUILayout.Label("Owned Ticks: None", m_DetailStyle);
                return;
            }

            GUILayout.Label($"Owned Ticks: {m_OwnedTicks.Count}", m_DetailStyle);
            for (int i = 0; i < m_OwnedTicks.Count; i++)
            {
                var registration = m_OwnedTicks[i];
                var tickSystem = registration.tickSystem;
                var phases = GameModules.GetTickPhasesText(tickSystem);
                GUILayout.Label(
                    $"  - {GameModuleUtility.GetTypeDisplayName(tickSystem?.GetType())} Phases: {phases}",
                    m_DetailStyle);
            }
        }

        /// <summary>
        ///   <para>判断依赖模块是否已安装。</para>
        /// </summary>
        /// <param name="dependencyType">依赖类型。</param>
        private bool IsDependencyInstalled(Type dependencyType)
        {
            if (dependencyType == null) return false;

            for (int i = 0; i < m_Modules.Count; i++)
            {
                var moduleType = m_Modules[i]?.GetType();
                if (moduleType != null && dependencyType.IsAssignableFrom(moduleType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>获取容器状态。</para>
        /// </summary>
        /// <param name="modules">模块。</param>
        private static string GetContainerState(GameModules modules)
        {
            if (modules == null) return "Disposed";
            if (modules.IsDisposed) return "Disposed";
            if (modules.IsDisposing) return "Disposing";
            return modules.IsChanging ? "Changing" : "Alive";
        }

        /// <summary>
        ///   <para>获取模块状态。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        private static string GetModuleState(IGameModule module)
        {
            if (module == null) return "Null";
            if (module is GameModule gameModule)
            {
                return gameModule.GetStateText();
            }
            return "Unknown";
        }
    }
}

#endif