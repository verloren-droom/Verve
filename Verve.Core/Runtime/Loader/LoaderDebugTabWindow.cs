#if (DEBUG || UNITY_EDITOR) && UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using UnityEngine.Scripting;
    using System.Collections.Generic;
#if UNITY_EDITOR
    using System.IO;
    using UnityEditor;
#endif
    
    /// <summary>
    ///   <para>加载器调试窗口；按资源类型展示引用状态。</para>
    /// </summary>
    [Preserve, DebugItem("Loader", 1)]
    sealed class LoaderDebugTabWindow : DebugTabWindow
    {
        /// <summary>
        ///   <para>模块句柄。</para>
        /// </summary>
        private readonly List<GameModulesHandle> m_ModuleHandles = new(8);
        /// <summary>
        ///   <para>模块容器。</para>
        /// </summary>
        private readonly List<IGameModule> m_Modules = new(32);
        /// <summary>
        ///   <para>加载器信息。</para>
        /// </summary>
        private readonly List<LoaderInfo> m_LoaderInfos = new(8);
        /// <summary>
        ///   <para>资源。</para>
        /// </summary>
        private readonly List<AssetItem> m_Assets = new(64);
        /// <summary>
        ///   <para>场景。</para>
        /// </summary>
        private readonly List<SceneItem> m_Scenes = new(16);
        /// <summary>
        ///   <para>资源包。</para>
        /// </summary>
        private readonly List<BundleItem> m_Bundles = new(16);
        /// <summary>
        ///   <para>临时资源。</para>
        /// </summary>
        private readonly List<LoaderDebugAssetBuffer> m_TempAssets = new(64);
        /// <summary>
        ///   <para>临时场景。</para>
        /// </summary>
        private readonly List<LoaderDebugSceneBuffer> m_TempScenes = new(16);
        /// <summary>
        ///   <para>临时资源包。</para>
        /// </summary>
        private readonly List<LoaderDebugBundleBuffer> m_TempBundles = new(16);

        /// <summary>
        ///   <para>滚动位置。</para>
        /// </summary>
        private Vector2 m_ScrollPosition;
        /// <summary>
        ///   <para>搜索。</para>
        /// </summary>
        private string m_Search = "";
        /// <summary>
        ///   <para>显示资源。</para>
        /// </summary>
        private bool m_ShowAssets = true;
        /// <summary>
        ///   <para>显示场景。</para>
        /// </summary>
        private bool m_ShowScenes = true;
        /// <summary>
        ///   <para>显示资源包。</para>
        /// </summary>
        private bool m_ShowBundles = true;
        /// <summary>
        ///   <para>资源展开。</para>
        /// </summary>
        private bool m_AssetsExpanded = true;
        /// <summary>
        ///   <para>场景展开。</para>
        /// </summary>
        private bool m_ScenesExpanded = true;
        /// <summary>
        ///   <para>资源包展开。</para>
        /// </summary>
        private bool m_BundlesExpanded = true;
        /// <summary>
        ///   <para>加载器总数。</para>
        /// </summary>
        private int m_LoadersTotal;
        /// <summary>
        ///   <para>表头样式。</para>
        /// </summary>
        private GUIStyle m_HeaderStyle;
        /// <summary>
        ///   <para>详情样式。</para>
        /// </summary>
        private GUIStyle m_DetailStyle;
        /// <summary>
        ///   <para>链接样式。</para>
        /// </summary>
        private GUIStyle m_LinkStyle;
        /// <summary>
        ///   <para>标记样式。</para>
        /// </summary>
        private GUIStyle m_TagStyle;
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
        ///   <para>创建加载器调试标签页窗口。</para>
        /// </summary>
        /// <param name="settings">设置。</param>
        [Preserve]
        public LoaderDebugTabWindow(DebugTabWindowSettings settings) : base(settings) { }

        /// <inheritdoc />
        public override void OnShow() { }

        /// <inheritdoc />
        public override void OnHide() { }

        /// <inheritdoc />
        public override void Draw()
        {
            EnsureStyles();
            CaptureSnapshots();
            DrawToolbar();

            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
            if (m_LoadersTotal == 0)
            {
                DrawEmptyState("No active loader modules. Install a loader module to inspect tracked assets, scenes and bundles.");
            }
            else
            {
                if (IsAllSnapshotTypesHidden())
                {
                    DrawEmptyState("All snapshot categories are hidden. Enable Assets, Scenes or Bundles.");
                }

                if (m_ShowAssets)
                {
                    DrawAssetSection();
                }

                if (m_ShowScenes)
                {
                    DrawSceneSection();
                }

                if (m_ShowBundles)
                {
                    DrawBundleSection();
                }
            }
            GUILayout.EndScrollView();
        }

        /// <summary>
        ///   <para>捕获快照。</para>
        /// </summary>
        private void CaptureSnapshots()
        {
            Game.CopyModuleHandlesTo(m_ModuleHandles);
            m_LoaderInfos.Clear();
            m_Assets.Clear();
            m_Scenes.Clear();
            m_Bundles.Clear();
            m_LoadersTotal = 0;

            for (int i = 0; i < m_ModuleHandles.Count; i++)
            {
                var handle = m_ModuleHandles[i];
                if (!TryCopyHandleModules(handle)) continue;

                for (int j = 0; j < m_Modules.Count; j++)
                {
                    if (m_Modules[j] is not LoaderModule loader || !loader.IsInstalled)
                    {
                        continue;
                    }

                    m_LoadersTotal++;
                    var loaderInfo = LoaderInfo.Create(loader);
                    m_LoaderInfos.Add(loaderInfo);
                    loader.CopyDebugAssetSnapshotsTo(m_TempAssets);
                    for (int k = 0; k < m_TempAssets.Count; k++)
                    {
                        m_Assets.Add(new AssetItem(loaderInfo, m_TempAssets[k]));
                    }

                    loader.CopyDebugSceneSnapshotsTo(m_TempScenes);
                    for (int k = 0; k < m_TempScenes.Count; k++)
                    {
                        m_Scenes.Add(new SceneItem(loaderInfo, m_TempScenes[k]));
                    }

                    loader.CopyDebugBundleSnapshotsTo(m_TempBundles);
                    for (int k = 0; k < m_TempBundles.Count; k++)
                    {
                        m_Bundles.Add(new BundleItem(loaderInfo, m_TempBundles[k]));
                    }
                }
            }
        }

        /// <summary>
        ///   <para>尝试读取句柄中的模块引用。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
        private bool TryCopyHandleModules(GameModulesHandle handle)
        {
            if (handle == null || !handle.TryGetModules(out var modules))
            {
                m_Modules.Clear();
                return false;
            }

            modules.CopyModulesTo(m_Modules);
            return true;
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
                m_LinkStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
                m_TagStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(6, 6, 2, 2) };
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
            m_DetailStyle.fontSize = Mathf.Max(10, fontSize - 1);
            m_LinkStyle.fontSize = Mathf.Max(10, fontSize - 1);
            m_HeaderStyle.normal.textColor = fontColor;
            m_DetailStyle.normal.textColor = fontColor;
            m_LinkStyle.normal.textColor = new Color(0.35f, 0.72f, 1f);
            m_LinkStyle.hover.textColor = new Color(0.55f, 0.85f, 1f);
            m_LinkStyle.active.textColor = new Color(0.2f, 0.55f, 0.9f);
            m_LinkStyle.focused.textColor = m_LinkStyle.normal.textColor;
            m_TagStyle.normal.textColor = fontColor;
            m_TagStyle.hover.textColor = fontColor;
            m_TagStyle.active.textColor = fontColor;
            m_TagStyle.focused.textColor = fontColor;
        }

        /// <summary>
        ///   <para>绘制工具栏。</para>
        /// </summary>
        private void DrawToolbar()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label(
                $"Loaders({m_LoadersTotal}) Assets({m_Assets.Count}) Scenes({m_Scenes.Count}) Bundles({m_Bundles.Count})",
                m_DetailStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Search", m_DetailStyle, GUILayout.Width(48));
            m_Search = GUILayout.TextField(m_Search ?? "", GUILayout.Width(240));
            if (GUILayout.Button("Clear", GUILayout.Width(58)))
            {
                m_Search = "";
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            m_ShowAssets = GUILayout.Toggle(m_ShowAssets, $"Assets({m_Assets.Count})", GUILayout.Width(112));
            m_ShowScenes = GUILayout.Toggle(m_ShowScenes, $"Scenes({m_Scenes.Count})", GUILayout.Width(112));
            m_ShowBundles = GUILayout.Toggle(m_ShowBundles, $"Bundles({m_Bundles.Count})", GUILayout.Width(124));
#if UNITY_EDITOR
            GUILayout.Label("Editor: click paths to ping/reveal.", m_DetailStyle);
#endif
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            DrawLoaderSummaryStrip();
            GUILayout.EndVertical();
        }

        /// <summary>
        ///   <para>判断所有快照类型是否均已隐藏。</para>
        /// </summary>
        private bool IsAllSnapshotTypesHidden() => !m_ShowAssets && !m_ShowScenes && !m_ShowBundles;

        /// <summary>
        ///   <para>绘制加载器统计栏。</para>
        /// </summary>
        private void DrawLoaderSummaryStrip()
        {
            if (m_LoaderInfos.Count == 0)
            {
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Loaders:", m_DetailStyle, GUILayout.Width(58));
            for (int i = 0; i < m_LoaderInfos.Count; i++)
            {
                var loader = m_LoaderInfos[i];
                GUILayout.Label($"{loader.title} [{loader.state}]", m_TagStyle, GUILayout.Width(Mathf.Min(220, 90 + loader.title.Length * 7)));
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        ///   <para>绘制空状态。</para>
        /// </summary>
        /// <param name="message">错误信息。</param>
        private void DrawEmptyState(string message)
        {
            GUILayout.BeginVertical(m_SectionStyle);
            GUILayout.Label(message, m_DetailStyle);
            GUILayout.EndVertical();
        }

        /// <summary>
        ///   <para>绘制资源区域。</para>
        /// </summary>
        private void DrawAssetSection()
        {
            DrawSectionHeader("Assets", CountVisibleAssets(), m_Assets.Count, ref m_AssetsExpanded);
            if (!m_AssetsExpanded) return;

            GUILayout.BeginVertical(m_SectionStyle);
            if (m_Assets.Count == 0)
            {
                GUILayout.Label(
                    "No active asset handles. Assets are listed while AssetLoadHandle is alive. Use AssetLoadScope or loader.LoadAsset<T>(path, owner) when you want automatic owner-bound lifetime.",
                    m_DetailStyle);
            }
            else
            {
                var shown = 0;
                for (int i = 0; i < m_Assets.Count; i++)
                {
                    if (!PassAssetSearch(m_Assets[i])) continue;
                    DrawAssetItem(m_Assets[i]);
                    shown++;
                }

                if (shown == 0)
                {
                    GUILayout.Label("No matching assets. Clear the search filter to show all active asset handles.", m_DetailStyle);
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        ///   <para>绘制场景区域。</para>
        /// </summary>
        private void DrawSceneSection()
        {
            DrawSectionHeader("Scenes", CountVisibleScenes(), m_Scenes.Count, ref m_ScenesExpanded);
            if (!m_ScenesExpanded) return;

            GUILayout.BeginVertical(m_SectionStyle);
            if (m_Scenes.Count == 0)
            {
                GUILayout.Label("No tracked scenes. Addressables scenes appear here while the loader is tracking them.", m_DetailStyle);
            }
            else
            {
                var shown = 0;
                for (int i = 0; i < m_Scenes.Count; i++)
                {
                    if (!PassSceneSearch(m_Scenes[i])) continue;
                    DrawSceneItem(m_Scenes[i]);
                    shown++;
                }

                if (shown == 0)
                {
                    GUILayout.Label("No matching scenes. Clear the search filter to show all tracked scenes.", m_DetailStyle);
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        ///   <para>绘制资源包区域。</para>
        /// </summary>
        private void DrawBundleSection()
        {
            DrawSectionHeader("Bundles", CountVisibleBundles(), m_Bundles.Count, ref m_BundlesExpanded);
            if (!m_BundlesExpanded) return;

            GUILayout.BeginVertical(m_SectionStyle);
            if (m_Bundles.Count == 0)
            {
                GUILayout.Label("No tracked bundles. AssetBundle entries appear here while the loader is holding bundle references.", m_DetailStyle);
            }
            else
            {
                var shown = 0;
                for (int i = 0; i < m_Bundles.Count; i++)
                {
                    if (!PassBundleSearch(m_Bundles[i])) continue;
                    DrawBundleItem(m_Bundles[i]);
                    shown++;
                }

                if (shown == 0)
                {
                    GUILayout.Label("No matching bundles. Clear the search filter to show all tracked bundles.", m_DetailStyle);
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        ///   <para>绘制区域表头。</para>
        /// </summary>
        /// <param name="title">标题。</param>
        /// <param name="visibleCount">可见数量。</param>
        /// <param name="totalCount">总计数量。</param>
        /// <param name="expanded">展开。</param>
        private void DrawSectionHeader(string title, int visibleCount, int totalCount, ref bool expanded)
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            if (GUILayout.Button(expanded ? "[-]" : "[+]", GUILayout.Width(34)))
            {
                expanded = !expanded;
            }
            GUILayout.Label($"{title} ({visibleCount}/{totalCount})", m_HeaderStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        ///   <para>绘制资源项。</para>
        /// </summary>
        /// <param name="item">项。</param>
        private void DrawAssetItem(AssetItem item)
        {
            var asset = item.buffer;
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
#if UNITY_EDITOR
            DrawEditorAssetLink(item);
#else
            GUILayout.Label(asset.path ?? "", m_DetailStyle);
#endif
            GUILayout.FlexibleSpace();
            GUILayout.Label(asset.alive ? "Alive" : "Lost", m_TagStyle, GUILayout.Width(62));
            GUILayout.Label($"Ref {asset.refCount}", m_TagStyle, GUILayout.Width(64));
            if (GUILayout.Button("Copy", GUILayout.Width(60)))
            {
                GUIUtility.systemCopyBuffer = asset.path ?? "";
            }
#if UNITY_EDITOR
            if (GUILayout.Button("Ping", GUILayout.Width(60)))
            {
                PingAssetItem(item);
            }
#endif
            GUILayout.EndHorizontal();

            GUILayout.Label($"Loader: {item.loader.title}   Type: {asset.assetType ?? "Unknown"}   InstanceId: {asset.instanceId}", m_DetailStyle);
            DrawLoaderDetails(item.loader);
            GUILayout.EndVertical();
        }

        /// <summary>
        ///   <para>绘制场景项。</para>
        /// </summary>
        /// <param name="item">项。</param>
        private void DrawSceneItem(SceneItem item)
        {
            var scene = item.buffer;
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
#if UNITY_EDITOR
            DrawEditorSceneLink(item);
#else
            GUILayout.Label(GetSceneLabel(scene), m_DetailStyle);
#endif
            GUILayout.FlexibleSpace();
            GUILayout.Label(scene.loaded ? "Loaded" : "Pending", m_TagStyle, GUILayout.Width(76));
            GUILayout.Label($"Ref {scene.refCount}", m_TagStyle, GUILayout.Width(64));
            if (GUILayout.Button("Copy", GUILayout.Width(60)))
            {
                GUIUtility.systemCopyBuffer = scene.path ?? scene.loadedPath ?? scene.name ?? "";
            }
#if UNITY_EDITOR
            if (GUILayout.Button("Ping", GUILayout.Width(60)))
            {
                PingSceneItem(item);
            }
#endif
            GUILayout.EndHorizontal();

            GUILayout.Label($"Loader: {item.loader.title}   Name: {scene.name ?? "Unknown"}   BuildIndex: {scene.buildIndex}   Valid: {scene.valid}", m_DetailStyle);
            GUILayout.Label($"Loaded Path: {scene.loadedPath ?? "Unknown"}", m_DetailStyle);
            DrawLoaderDetails(item.loader);
            GUILayout.EndVertical();
        }

        /// <summary>
        ///   <para>绘制资源包项。</para>
        /// </summary>
        /// <param name="item">项。</param>
        private void DrawBundleItem(BundleItem item)
        {
            var bundle = item.buffer;
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
#if UNITY_EDITOR
            DrawEditorBundleLink(item);
#else
            GUILayout.Label(bundle.name ?? "", m_DetailStyle);
#endif
            GUILayout.FlexibleSpace();
            GUILayout.Label(bundle.loaded ? "Loaded" : "Lost", m_TagStyle, GUILayout.Width(76));
            GUILayout.Label($"Ref {bundle.refCount}", m_TagStyle, GUILayout.Width(64));
            if (GUILayout.Button("Copy", GUILayout.Width(60)))
            {
                GUIUtility.systemCopyBuffer = bundle.name ?? bundle.path ?? "";
            }
#if UNITY_EDITOR
            if (GUILayout.Button("Reveal", GUILayout.Width(70)))
            {
                RevealBundleItem(item);
            }
#endif
            GUILayout.EndHorizontal();

            GUILayout.Label($"Loader: {item.loader.title}   Path: {bundle.path ?? "Unknown"}", m_DetailStyle);
            DrawLoaderDetails(item.loader);
            GUILayout.EndVertical();
        }

        /// <summary>
        ///   <para>绘制加载器详情。</para>
        /// </summary>
        /// <param name="loader">资源加载器。</param>
        private void DrawLoaderDetails(LoaderInfo loader)
        {
            if (!string.IsNullOrEmpty(loader.description))
            {
                GUILayout.Label(loader.description, m_DetailStyle);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        ///   <para>绘制编辑器资源链接。</para>
        /// </summary>
        /// <param name="item">项。</param>
        private void DrawEditorAssetLink(AssetItem item)
        {
            if (GUILayout.Button(item.buffer.path ?? "", m_LinkStyle))
            {
                PingAssetItem(item);
            }
        }

        /// <summary>
        ///   <para>绘制编辑器场景链接。</para>
        /// </summary>
        /// <param name="item">项。</param>
        private void DrawEditorSceneLink(SceneItem item)
        {
            if (GUILayout.Button(GetSceneLabel(item.buffer), m_LinkStyle))
            {
                PingSceneItem(item);
            }
        }

        /// <summary>
        ///   <para>绘制编辑器资源包链接。</para>
        /// </summary>
        /// <param name="item">项。</param>
        private void DrawEditorBundleLink(BundleItem item)
        {
            if (GUILayout.Button(item.buffer.name ?? "", m_LinkStyle))
            {
                RevealBundleItem(item);
            }
        }

        /// <summary>
        ///   <para>在项目窗口定位资源。</para>
        /// </summary>
        /// <param name="item">项。</param>
        private static void PingAssetItem(AssetItem item)
        {
            if (TryResolveAssetSnapshotPath(item.buffer, out var projectPath) && PingProjectAsset(projectPath))
            {
                return;
            }

            Debug.LogWarning($"Failed to locate loader asset in Project window. path={item.buffer.path}");
        }

        /// <summary>
        ///   <para>在项目窗口定位场景。</para>
        /// </summary>
        /// <param name="item">项。</param>
        private static void PingSceneItem(SceneItem item)
        {
            var scene = item.buffer;
            if (TryResolveProjectPath(scene.path, out var projectPath) && PingProjectAsset(projectPath))
            {
                return;
            }

            if (TryResolveProjectPath(scene.loadedPath, out projectPath) && PingProjectAsset(projectPath))
            {
                return;
            }

            Debug.LogWarning($"Failed to locate loader scene in Project window. path={scene.path}, loadedPath={scene.loadedPath}");
        }

        /// <summary>
        ///   <para>显示资源包项。</para>
        /// </summary>
        /// <param name="item">项。</param>
        private static void RevealBundleItem(BundleItem item)
        {
            var bundle = item.buffer;
            if (TryResolveProjectPath(bundle.path, out var projectPath) && PingProjectAsset(projectPath))
            {
                return;
            }

            if (!string.IsNullOrEmpty(bundle.path) && File.Exists(bundle.path))
            {
                EditorUtility.RevealInFinder(bundle.path);
                return;
            }

            Debug.LogWarning($"Failed to locate AssetBundle file. path={bundle.path}");
        }

        /// <summary>
        ///   <para>尝试解析资源快照路径。</para>
        /// </summary>
        /// <param name="asset">资源。</param>
        /// <param name="projectPath">项目路径。</param>
        private static bool TryResolveAssetSnapshotPath(
            LoaderDebugAssetBuffer asset,
            out string projectPath)
        {
            projectPath = null;
            if (string.IsNullOrEmpty(asset.path)) return false;

            if (TryResolveProjectPath(asset.path, out projectPath))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///   <para>尝试解析项目路径。</para>
        /// </summary>
        /// <param name="value">值。</param>
        /// <param name="projectPath">项目路径。</param>
        private static bool TryResolveProjectPath(string value, out string projectPath)
        {
            projectPath = null;
            if (string.IsNullOrEmpty(value)) return false;

            var candidate = Game.PathUtility.Normalize(value);
            if (!Game.PathUtility.TryNormalizeProjectPath(candidate, out var normalized))
            {
                var dataPath = Game.PathUtility.Normalize(Application.dataPath);
                if (candidate.StartsWith(dataPath, StringComparison.Ordinal))
                {
                    candidate = "Assets" + candidate.Substring(dataPath.Length);
                }
            }

            if (!Game.PathUtility.TryNormalizeProjectPath(normalized ?? candidate, out normalized))
            {
                return false;
            }

            if (AssetDatabase.LoadMainAssetAtPath(normalized) == null)
            {
                return false;
            }

            projectPath = normalized;
            return true;
        }

        /// <summary>
        ///   <para>在项目窗口定位项目资源。</para>
        /// </summary>
        /// <param name="projectPath">项目路径。</param>
        private static bool PingProjectAsset(string projectPath)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(projectPath);
            if (asset == null) return false;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            return true;
        }

#endif

        /// <summary>
        ///   <para>统计可见资源。</para>
        /// </summary>
        private int CountVisibleAssets()
        {
            var count = 0;
            for (int i = 0; i < m_Assets.Count; i++)
            {
                if (PassAssetSearch(m_Assets[i])) count++;
            }

            return count;
        }

        /// <summary>
        ///   <para>统计可见场景。</para>
        /// </summary>
        private int CountVisibleScenes()
        {
            var count = 0;
            for (int i = 0; i < m_Scenes.Count; i++)
            {
                if (PassSceneSearch(m_Scenes[i])) count++;
            }

            return count;
        }

        /// <summary>
        ///   <para>统计可见资源包。</para>
        /// </summary>
        private int CountVisibleBundles()
        {
            var count = 0;
            for (int i = 0; i < m_Bundles.Count; i++)
            {
                if (PassBundleSearch(m_Bundles[i])) count++;
            }

            return count;
        }

        /// <summary>
        ///   <para>匹配资源搜索。</para>
        /// </summary>
        /// <param name="item">项。</param>
        private bool PassAssetSearch(AssetItem item)
        {
            if (string.IsNullOrEmpty(m_Search)) return true;
            var asset = item.buffer;
            if (PassLoaderSearch(item.loader)) return true;
            if (MatchesSearch(asset.path)) return true;
            if (MatchesSearch(asset.assetType)) return true;
            if (MatchesSearch(asset.instanceId.ToString())) return true;
            return MatchesSearch(asset.refCount.ToString());
        }

        /// <summary>
        ///   <para>匹配场景搜索。</para>
        /// </summary>
        /// <param name="item">项。</param>
        private bool PassSceneSearch(SceneItem item)
        {
            if (string.IsNullOrEmpty(m_Search)) return true;
            var scene = item.buffer;
            if (PassLoaderSearch(item.loader)) return true;
            if (MatchesSearch(scene.name)) return true;
            if (MatchesSearch(scene.path)) return true;
            if (MatchesSearch(scene.loadedPath)) return true;
            if (MatchesSearch(scene.buildIndex.ToString())) return true;
            if (MatchesSearch(scene.refCount.ToString())) return true;
            return MatchesSearch(scene.loaded ? "loaded" : "pending");
        }

        /// <summary>
        ///   <para>匹配资源包搜索。</para>
        /// </summary>
        /// <param name="item">项。</param>
        private bool PassBundleSearch(BundleItem item)
        {
            if (string.IsNullOrEmpty(m_Search)) return true;
            var bundle = item.buffer;
            if (PassLoaderSearch(item.loader)) return true;
            if (MatchesSearch(bundle.name)) return true;
            if (MatchesSearch(bundle.path)) return true;
            return MatchesSearch(bundle.refCount.ToString());
        }

        /// <summary>
        ///   <para>匹配加载器搜索。</para>
        /// </summary>
        /// <param name="loader">资源加载器。</param>
        private bool PassLoaderSearch(LoaderInfo loader)
        {
            if (MatchesSearch(loader.title)) return true;
            if (MatchesSearch(loader.fullTypeName)) return true;
            if (MatchesSearch(loader.assemblyName)) return true;
            if (MatchesSearch(loader.state)) return true;
            return MatchesSearch(loader.description);
        }

        /// <summary>
        ///   <para>匹配搜索。</para>
        /// </summary>
        /// <param name="value">值。</param>
        private bool MatchesSearch(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(m_Search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        ///   <para>获取场景标签。</para>
        /// </summary>
        /// <param name="scene">操作关联的场景。</param>
        private static string GetSceneLabel(LoaderDebugSceneBuffer scene)
        {
            if (!string.IsNullOrEmpty(scene.path)) return scene.path;
            if (!string.IsNullOrEmpty(scene.loadedPath)) return scene.loadedPath;
            return scene.name ?? "";
        }

        /// <summary>
        ///   <para>获取加载器状态。</para>
        /// </summary>
        /// <param name="loader">资源加载器。</param>
        private static string GetLoaderState(LoaderModule loader)
        {
            if (loader == null) return "Null";
            if (loader.IsDisposed) return "Disposed";
            return loader.IsInstalled ? "Installed" : "Uninstalled";
        }

        /// <summary>
        ///   <para>加载器信息。</para>
        /// </summary>
        private readonly struct LoaderInfo
        {
            /// <summary>
            ///   <para>标题。</para>
            /// </summary>
            public readonly string title;
            /// <summary>
            ///   <para>完整类型名称。</para>
            /// </summary>
            public readonly string fullTypeName;
            /// <summary>
            ///   <para>程序集名称。</para>
            /// </summary>
            public readonly string assemblyName;
            /// <summary>
            ///   <para>状态。</para>
            /// </summary>
            public readonly string state;
            /// <summary>
            ///   <para>描述。</para>
            /// </summary>
            public readonly string description;

            /// <summary>
            ///   <para>创建加载器信息。</para>
            /// </summary>
            /// <param name="title">标题。</param>
            /// <param name="fullTypeName">完整类型名称。</param>
            /// <param name="assemblyName">程序集名称。</param>
            /// <param name="state">状态。</param>
            /// <param name="description">描述。</param>
            private LoaderInfo(
                string title,
                string fullTypeName,
                string assemblyName,
                string state,
                string description)
            {
                this.title = title;
                this.fullTypeName = fullTypeName;
                this.assemblyName = assemblyName;
                this.state = state;
                this.description = description;
            }

            /// <summary>
            ///   <para>创建实例。</para>
            /// </summary>
            /// <param name="loader">资源加载器。</param>
            public static LoaderInfo Create(LoaderModule loader)
            {
                var type = loader.GetType();
                return new LoaderInfo(
                    type.Name,
                    type.FullName ?? type.Name,
                    type.Assembly.GetName().Name ?? "Unknown",
                    GetLoaderState(loader),
                    loader.DebugDescription);
            }
        }

        /// <summary>
        ///   <para>资源项。</para>
        /// </summary>
        private readonly struct AssetItem
        {
            /// <summary>
            ///   <para>加载器。</para>
            /// </summary>
            public readonly LoaderInfo loader;
            /// <summary>
            ///   <para>缓冲区。</para>
            /// </summary>
            public readonly LoaderDebugAssetBuffer buffer;

            /// <summary>
            ///   <para>创建资源项。</para>
            /// </summary>
            /// <param name="loader">资源加载器。</param>
            /// <param name="buffer">缓冲区。</param>
            public AssetItem(LoaderInfo loader, LoaderDebugAssetBuffer buffer)
            {
                this.loader = loader;
                this.buffer = buffer;
            }
        }

        /// <summary>
        ///   <para>场景项。</para>
        /// </summary>
        private readonly struct SceneItem
        {
            /// <summary>
            ///   <para>加载器。</para>
            /// </summary>
            public readonly LoaderInfo loader;
            /// <summary>
            ///   <para>缓冲区。</para>
            /// </summary>
            public readonly LoaderDebugSceneBuffer buffer;

            /// <summary>
            ///   <para>创建场景项。</para>
            /// </summary>
            /// <param name="loader">资源加载器。</param>
            /// <param name="buffer">缓冲区。</param>
            public SceneItem(LoaderInfo loader, LoaderDebugSceneBuffer buffer)
            {
                this.loader = loader;
                this.buffer = buffer;
            }
        }

        /// <summary>
        ///   <para>资源包项。</para>
        /// </summary>
        private readonly struct BundleItem
        {
            /// <summary>
            ///   <para>加载器。</para>
            /// </summary>
            public readonly LoaderInfo loader;
            /// <summary>
            ///   <para>缓冲区。</para>
            /// </summary>
            public readonly LoaderDebugBundleBuffer buffer;

            /// <summary>
            ///   <para>创建资源包项。</para>
            /// </summary>
            /// <param name="loader">资源加载器。</param>
            /// <param name="buffer">缓冲区。</param>
            public BundleItem(LoaderInfo loader, LoaderDebugBundleBuffer buffer)
            {
                this.loader = loader;
                this.buffer = buffer;
            }
        }
    }
}

#endif
