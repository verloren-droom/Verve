#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using UnityEngine.Scripting;
    using System.Collections.Generic;
    
    
    /// <summary>
    ///   <para>游戏模块调试窗口</para>
    /// </summary>
    [Preserve, DebugItem("Module")]
    sealed class GameModuleDebugTabWindow : DebugTabWindow
    {
        private Vector2 m_ScrollPosition;
        private readonly List<GameModulesHandle> m_Handles = new(8);
        private readonly List<IGameModule> m_Modules = new(32);
        private readonly List<GameModules.OwnedTickRegistration> m_OwnedTicks = new(16);
        private string m_Search = "";
        private bool m_ModulesFoldout = true;
        private int m_HandlesTotal;
        private int m_HandlesShown;
        private int m_ModulesTotal;
        private int m_ModulesShown;
        private int m_ModulesInstalled;
        private int m_OwnedTicksTotal;
        private GUIStyle m_HeaderStyle;
        private GUIStyle m_DetailStyle;
        private GUIStyle m_SectionStyle;
        private bool m_StylesReady;
        private int m_StyleFontSize = -1;
        private Color m_StyleFontColor;

        [Preserve]
        public GameModuleDebugTabWindow(DebugTabWindowSettings settings) : base(settings) { }

        public override void OnShow() { }

        public override void OnHide() { }

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

        private bool MatchesModuleSearch(GameModules modules, IGameModule module)
        {
            if (MatchesSearch(module)) return true;
            if (MatchesDependencySearch(module)) return true;
            return MatchesOwnedTickSearch(modules, module);
        }

        private bool MatchesDependencySearch(IGameModule module)
        {
            if (module == null || string.IsNullOrEmpty(m_Search)) return false;

            try
            {
                var dependencies = GameModuleDependencyUtility.GetDependencies(module);
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

        private bool MatchesTypeSearch(Type type)
        {
            if (type == null || string.IsNullOrEmpty(m_Search)) return false;

            var filter = m_Search;
            var name = type.FullName ?? type.Name ?? "null";
            if (name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) return true;

            var assembly = type.Assembly.GetName().Name ?? "Unknown";
            return assembly.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool MatchesSearch(GameModulesHandle handle)
        {
            if (handle == null) return false;
            if (string.IsNullOrEmpty(m_Search)) return true;

            var filter = m_Search;
            if (handle.Id.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return handle.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

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

        private void DrawModulesSection()
        {
            if (!m_ModulesFoldout) return;
            for (int i = 0; i < m_Handles.Count; i++)
            {
                DrawHandleSection(m_Handles[i]);
            }
        }

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
                dependencies = GameModuleDependencyUtility.GetDependencies(module);
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
                var mode = registration.runInBackground ? "Background" : "Main";
                var phases = GameModules.GetTickPhasesText(tickSystem);
                GUILayout.Label(
                    $"  - {GameModuleUtility.GetTypeDisplayName(tickSystem?.GetType())} [{mode}] Phases: {phases}",
                    m_DetailStyle);
            }
        }

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

        private static string GetContainerState(GameModules modules)
        {
            if (modules == null) return "Disposed";
            if (modules.IsDisposed) return "Disposed";
            if (modules.IsDisposing) return "Disposing";
            return modules.IsChanging ? "Changing" : "Alive";
        }

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