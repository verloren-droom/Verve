#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using UnityEngine;
    using UnityEngine.Scripting;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>世界调试窗口。</para>
    /// </summary>
    [Preserve]
    [DebugItem("World")]
    sealed class WorldDebugTabWindow : DebugTabWindow
    {
        /// <summary>
        ///   <para>世界调试条目；关联模块句柄、管理器和世界。</para>
        /// </summary>
        private readonly struct WorldEntry
        {
            /// <summary>
            ///   <para>模块句柄标识。</para>
            /// </summary>
            public readonly int modulesHandleId;
            /// <summary>
            ///   <para>世界管理器。</para>
            /// </summary>
            public readonly IWorldManager manager;
            /// <summary>
            ///   <para>世界实例。</para>
            /// </summary>
            public readonly World world;

            /// <summary>
            ///   <para>创建世界调试条目。</para>
            /// </summary>
            /// <param name="modulesHandleId">模块句柄标识。</param>
            /// <param name="manager">世界管理器。</param>
            /// <param name="world">世界实例。</param>
            public WorldEntry(int modulesHandleId, IWorldManager manager, World world)
            {
                this.modulesHandleId = modulesHandleId;
                this.manager = manager;
                this.world = world;
            }
        }
        
        /// <summary>
        ///   <para>发现的模块句柄缓冲区。</para>
        /// </summary>
        private readonly List<GameModulesHandle> m_ModuleHandles = new(8);
        /// <summary>
        ///   <para>世界条目缓冲区。</para>
        /// </summary>
        private readonly List<WorldEntry> m_WorldsBuffer = new(8);
        /// <summary>
        ///   <para>行动者缓冲区。</para>
        /// </summary>
        private readonly List<Actor> m_ActorsBuffer = new(128);
        /// <summary>
        ///   <para>组件名称缓冲区。</para>
        /// </summary>
        private readonly List<string> m_ComponentsBuffer = new(64);
        /// <summary>
        ///   <para>标签阻塞信息缓冲区。</para>
        /// </summary>
        private readonly List<TagBlockInfo> m_TagBlocksBuffer = new(32);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        ///   <para>能力状态事件缓冲区。</para>
        /// </summary>
        private readonly List<CapabilityDebugEvent> m_EventsBuffer = new(256);
#endif

        /// <summary>
        ///   <para>世界列表滚动位置。</para>
        /// </summary>
        private Vector2 m_WorldScroll;
        /// <summary>
        ///   <para>行动者列表滚动位置。</para>
        /// </summary>
        private Vector2 m_ActorScroll;
        /// <summary>
        ///   <para>详情面板滚动位置。</para>
        /// </summary>
        private Vector2 m_DetailScroll;
        /// <summary>
        ///   <para>当前选中的世界。</para>
        /// </summary>
        private World m_SelectedWorld;
        /// <summary>
        ///   <para>当前选中的行动者。</para>
        /// </summary>
        private Actor m_SelectedActor = Actor.none;
        /// <summary>
        ///   <para>窗口标题样式。</para>
        /// </summary>
        private GUIStyle m_HeaderStyle;
        /// <summary>
        ///   <para>窗口分区样式。</para>
        /// </summary>
        private GUIStyle m_SectionStyle;
        /// <summary>
        ///   <para>样式是否已创建。</para>
        /// </summary>
        private bool m_StylesReady;
        /// <summary>
        ///   <para>当前样式字号。</para>
        /// </summary>
        private int m_StyleFontSize = -1;
        /// <summary>
        ///   <para>当前样式文字颜色。</para>
        /// </summary>
        private Color m_StyleFontColor;
        
        [Preserve]
        public WorldDebugTabWindow(DebugTabWindowSettings settings) : base(settings) { }

        /// <inheritdoc />
        public override void OnShow() { }

        /// <inheritdoc />
        public override void OnHide()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            for (var i = 0; i < m_WorldsBuffer.Count; i++)
                if (m_WorldsBuffer[i].world != null)
                    m_WorldsBuffer[i].world.DebugTrace.SetOwnerEnabled(this, false);
#endif
        }

        /// <summary>
        ///   <para>收集世界中的存活行动者。</para>
        /// </summary>
        /// <param name="world">目标世界。</param>
        /// <param name="output">接收行动者的列表。</param>
        private static void GetAliveActors(World world, List<Actor> output)
        {
            if (world?.Actors == null || output == null) return;
            world.Actors.GetAliveActors(output);
        }

        /// <summary>
        ///   <para>收集行动者拥有的组件名称。</para>
        /// </summary>
        /// <param name="world">目标世界。</param>
        /// <param name="actor">目标行动者。</param>
        /// <param name="output">接收组件名称的列表。</param>
        private static void GetComponents(World world, Actor actor, List<string> output)
        {
            if (world?.Actors == null || output == null) return;
            output.Clear();
            var mask = world.Actors.GetComponentMask(actor);
            if (mask.IsEmpty) return;
            var enumerator = mask.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var typeId = enumerator.Current;
                var type = ComponentTypeRegistry.GetType(typeId);
                if (type != null) output.Add(type.Name);
            }
        }

        /// <summary>
        ///   <para>收集行动者的标签阻塞信息。</para>
        /// </summary>
        /// <param name="world">目标世界。</param>
        /// <param name="actor">目标行动者。</param>
        /// <param name="output">接收阻塞信息的列表。</param>
        private static void GetTagBlocks(World world, Actor actor, List<TagBlockInfo> output)
        {
            if (world?.Actors == null || output == null) return;
            var mask = world.Actors.GetTagBlockMask(actor);
            mask.GetTagBlocks(output);
        }

        /// <summary>
        ///   <para>按标签标识比较阻塞信息。</para>
        /// </summary>
        /// <param name="left">左侧信息。</param>
        /// <param name="right">右侧信息。</param>
        private static int CompareTagBlocks(TagBlockInfo left, TagBlockInfo right)
        {
            var tagCompare = ((int)left.tagId).CompareTo((int)right.tagId);
            if (tagCompare != 0) return tagCompare;
            var leftInstigator = left.instigator?.GetType().Name ?? "";
            var rightInstigator = right.instigator?.GetType().Name ?? "";
            return string.CompareOrdinal(leftInstigator, rightInstigator);
        }

        /// <inheritdoc />
        public override void Draw()
        {
            EnsureStyles();
            GUILayout.BeginHorizontal();
            DrawWorldsPanel();
            DrawActorsPanel();
            DrawDetailsPanel();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        ///   <para>创建并刷新调试窗口样式。</para>
        /// </summary>
        private void EnsureStyles()
        {
            if (!m_StylesReady)
            {
                m_StylesReady = true;
                m_HeaderStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                m_SectionStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(8, 8, 6, 6) };
            }
            ApplyStyleSettings();
        }

        /// <summary>
        ///   <para>应用调试窗口设置中的字体参数。</para>
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
        }

        /// <summary>
        ///   <para>绘制世界选择面板。</para>
        /// </summary>
        private void DrawWorldsPanel()
        {
            GUILayout.BeginVertical(GUILayout.Width(220));
            GUILayout.Label("Worlds", m_HeaderStyle);
            m_WorldScroll = GUILayout.BeginScrollView(m_WorldScroll, GUI.skin.box, GUILayout.ExpandHeight(true));
            GetWorlds(m_WorldsBuffer);
            if (m_WorldsBuffer.Count == 0)
            {
                GUILayout.Label("No active worlds.", GUI.skin.label);
            }
            else
            {
                for (int i = 0; i < m_WorldsBuffer.Count; i++)
                {
                    var entry = m_WorldsBuffer[i];
                    var world = entry.world;
                    if (world == null || world.IsDisposed) continue;
                    var isSelected = ReferenceEquals(m_SelectedWorld, world);
                    var label = (isSelected ? $"> " : string.Empty) +
                        $"#{entry.modulesHandleId} {world.Name}" +
                        (ReferenceEquals(entry.manager.Active, world) ? " ◆" : string.Empty);
                    if (GUILayout.Button(label, GUI.skin.button))
                    {
                        m_SelectedWorld = world;
                        m_SelectedActor = Actor.none;
                    }
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        ///   <para>绘制最近的能力状态事件。</para>
        /// </summary>
        /// <param name="world">目标世界。</param>
        /// <param name="actor">当前行动者；为空时显示全部行动者。</param>
        private void DrawCapabilityEvents(World world, Actor actor)
        {
            if (world == null || world.IsDisposed) return;
            world.DebugTrace.CopyTo(m_EventsBuffer);
            GUILayout.Space(6f);
            GUILayout.BeginVertical(m_SectionStyle);
            GUILayout.Label($"Capability Events ({m_EventsBuffer.Count})", m_HeaderStyle);
            var shown = 0;
            for (var i = m_EventsBuffer.Count - 1; i >= 0 && shown < 16; i--)
            {
                var entry = m_EventsBuffer[i];
                if (!actor.IsNone && entry.actor != actor) continue;
                var error = string.IsNullOrEmpty(entry.error) ? string.Empty : $"  {entry.error}";
                GUILayout.Label($"F{entry.frame}  Actor {entry.actor.id}  {entry.kind}  {entry.capabilityType?.Name}{error}", GUI.skin.label);
                shown++;
            }
            if (shown == 0) GUILayout.Label("No capability events.", GUI.skin.label);
            GUILayout.EndVertical();
        }
#endif

        /// <summary>
        ///   <para>绘制行动者选择面板。</para>
        /// </summary>
        private void DrawActorsPanel()
        {
            GUILayout.BeginVertical(GUILayout.Width(260));
            GUILayout.Label("Actors", m_HeaderStyle);
            m_ActorScroll = GUILayout.BeginScrollView(m_ActorScroll, GUI.skin.box, GUILayout.ExpandHeight(true));

            if (m_SelectedWorld == null || m_SelectedWorld.IsDisposed)
            {
                GUILayout.Label("Select a world.", GUI.skin.label);
            }
            else
            {
                GetAliveActors(m_SelectedWorld, m_ActorsBuffer);
                if (m_ActorsBuffer.Count == 0)
                {
                    GUILayout.Label("No actors.", GUI.skin.label);
                }
                else
                {
                    for (int i = 0; i < m_ActorsBuffer.Count; i++)
                    {
                        var actor = m_ActorsBuffer[i];
                        var label = actor.Equals(m_SelectedActor) ? $"> Actor #{actor.id}" : $"Actor #{actor.id}";
                        if (GUILayout.Button(label, GUI.skin.button))
                        {
                            m_SelectedActor = actor;
                        }
                    }
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        ///   <para>绘制选中行动者的详情面板。</para>
        /// </summary>
        private void DrawDetailsPanel()
        {
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Label("Details", m_HeaderStyle);
            m_DetailScroll = GUILayout.BeginScrollView(m_DetailScroll, GUI.skin.box, GUILayout.ExpandHeight(true));

            if (m_SelectedWorld == null || m_SelectedWorld.IsDisposed)
            {
                GUILayout.Label("Select a world to inspect.", GUI.skin.label);
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginVertical(m_SectionStyle);
            GUILayout.Label($"Name: {m_SelectedWorld.Name}", GUI.skin.label);
            GUILayout.Label($"TimeScale: {m_SelectedWorld.TimeScale:F2}", GUI.skin.label);
            GUILayout.Label($"Actors: {m_SelectedWorld.Actors?.AliveActorCount ?? 0}", GUI.skin.label);
            GUILayout.EndVertical();

            if (m_SelectedActor.IsNone)
            {
                GUILayout.Label("Select an actor for details.", GUI.skin.label);
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Space(6f);
            GUILayout.BeginVertical(m_SectionStyle);
            GUILayout.Label($"Actor ID: {m_SelectedActor.id}", GUI.skin.label);
            GUILayout.Label($"Index: {m_SelectedActor.Index}", GUI.skin.label);
            GUILayout.Label($"Version: {m_SelectedActor.Version}", GUI.skin.label);
            GUILayout.EndVertical();

            GUILayout.Space(6f);
            GUILayout.BeginVertical(m_SectionStyle);
            GUILayout.Label("Components", m_HeaderStyle);
            GetComponents(m_SelectedWorld, m_SelectedActor, m_ComponentsBuffer);
            if (m_ComponentsBuffer.Count == 0)
            {
                GUILayout.Label("No components.", GUI.skin.label);
            }
            else
            {
                for (int i = 0; i < m_ComponentsBuffer.Count; i++)
                {
                    GUILayout.Label(m_ComponentsBuffer[i], GUI.skin.label);
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(6f);
            GUILayout.BeginVertical(m_SectionStyle);
            GUILayout.Label("Capabilities", m_HeaderStyle);
            var capabilities = m_SelectedWorld?.Actors.GetCapabilities(m_SelectedActor);
            if (capabilities == null || capabilities.Count == 0)
            {
                GUILayout.Label("No capabilities.", GUI.skin.label);
            }
            else
            {
                for (int i = 0; i < capabilities.Count; i++)
                {
                    var entry = capabilities[i];
                    var capability = entry.capability;
                    var statusText = capability == null
                        ? "Null"
                        : capability.IsReleased ? "Disposed" : (capability.IsActive ? "Active" : "Inactive");
                    GUILayout.Label($"{capability?.GetType().Name ?? "null"}  Status: {statusText}  TickGroup: {(TickGroup)entry.tickGroup}  TickOrder: {entry.tickOrder}", GUI.skin.label);
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(6f);
            GUILayout.BeginVertical(m_SectionStyle);
            GUILayout.Label("Tags", m_HeaderStyle);
            GetTagBlocks(m_SelectedWorld, m_SelectedActor, m_TagBlocksBuffer);
            if (m_TagBlocksBuffer.Count == 0)
            {
                GUILayout.Label("No tag blocks.", GUI.skin.label);
            }
            else
            {
                m_TagBlocksBuffer.Sort(CompareTagBlocks);
                var currentTag = TagId.none;
                var hasTag = false;
                for (int i = 0; i < m_TagBlocksBuffer.Count; i++)
                {
                    var block = m_TagBlocksBuffer[i];
                    if (!hasTag || block.tagId != currentTag)
                    {
                        currentTag = block.tagId;
                        hasTag = true;
                        var tagName = TagRegistry.GetTagName(block.tagId);
                        var tagText = string.IsNullOrEmpty(tagName) ? block.tagId.ToString() : $"{tagName} ({block.tagId})";
                        GUILayout.Label(tagText, GUI.skin.label);
                    }
                    var instigatorText = block.instigator?.GetType().Name ?? "null";
                    GUILayout.Label($"Instigator: {instigatorText}  Count: {block.blockCount}", GUI.skin.label);
                }
            }
            GUILayout.EndVertical();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DrawCapabilityEvents(m_SelectedWorld, m_SelectedActor);
#endif

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        ///   <para>收集当前模块容器中的世界。</para>
        /// </summary>
        /// <param name="output">接收世界条目的列表。</param>
        private void GetWorlds(List<WorldEntry> output)
        {
            if (output == null)
            {
                return;
            }

            for (var i = 0; i < output.Count; i++)
                if (output[i].world != null)
                    output[i].world.DebugTrace.SetOwnerEnabled(this, false);
            output.Clear();
            Game.CopyModuleHandlesTo(m_ModuleHandles);
            for (int i = 0; i < m_ModuleHandles.Count; i++)
            {
                var handle = m_ModuleHandles[i];
                if (handle == null || !handle.TryGetModules(out var modules))
                {
                    continue;
                }

                if (!modules.TryGetModule<IWorldManager>(out var manager))
                    continue;

                foreach (var world in manager.Worlds)
                {
                    if (world != null && !world.IsDisposed)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        world.DebugTrace.SetOwnerEnabled(this, true);
#endif
                        output.Add(new WorldEntry(handle.Id, manager, world));
                    }
                }
            }
        }
    }
}

#endif