#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using UnityEngine;
    using System.Collections.Generic;
    using UnityEngine.Scripting;


    /// <summary>
    ///   <para>世界调试窗口</para>
    /// </summary>
    [Preserve]
    [DebugItem("World")]
    sealed class WorldDebugTabWindow : DebugTabWindow
    {
        private Vector2 m_WorldScroll;
        private Vector2 m_ActorScroll;
        private Vector2 m_DetailScroll;
        private World m_SelectedWorld;
        private Actor m_SelectedActor = Actor.none;
        private GUIStyle m_HeaderStyle;
        private GUIStyle m_SectionStyle;
        private bool m_StylesReady;
        private int m_StyleFontSize = -1;
        private Color m_StyleFontColor;
        private readonly List<World> m_WorldsBuffer = new(8);
        private readonly List<Actor> m_ActorsBuffer = new(128);
        private readonly List<string> m_ComponentsBuffer = new(64);
        private readonly List<TagBlockInfo> m_TagBlocksBuffer = new(32);

        [Preserve]
        public WorldDebugTabWindow(DebugTabWindowSettings settings) : base(settings) { }

        public override void OnShow() { }

        public override void OnHide() { }

        public override void Draw()
        {
            EnsureStyles();
            GUILayout.BeginHorizontal();
            DrawWorldsPanel();
            DrawActorsPanel();
            DrawDetailsPanel();
            GUILayout.EndHorizontal();
        }

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
                    var world = m_WorldsBuffer[i];
                    if (world == null || world.IsDisposed) continue; 
                    var isSelected = ReferenceEquals(m_SelectedWorld, world);
                    var label = (isSelected ? $"> {world.Name}" : world.Name) + (ReferenceEquals(Game.World, world) ? " ◆" : "");
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
                        : capability.IsDisposed ? "Disposed" : (capability.IsActive ? "Active" : "Inactive");
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

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private static void GetWorlds(List<World> output)
        {
            try
            {
                if (output == null) return;
                output.Clear();
                var worlds = Game.Worlds;
                if (worlds == null) return;
                foreach (var world in worlds)
                {
                    if (world != null && !world.IsDisposed) output.Add(world);
                }
            }
            catch { }
        }

        private static void GetAliveActors(World world, List<Actor> output)
        {
            if (world?.Actors == null || output == null) return;
            world.Actors.GetAliveActors(output);
        }

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

        private static void GetTagBlocks(World world, Actor actor, List<TagBlockInfo> output)
        {
            if (world?.Actors == null || output == null) return;
            var mask = world.Actors.GetTagBlockMask(actor);
            mask.GetTagBlocks(output);
        }

        private static int CompareTagBlocks(TagBlockInfo left, TagBlockInfo right)
        {
            var tagCompare = ((int)left.tagId).CompareTo((int)right.tagId);
            if (tagCompare != 0) return tagCompare;
            var leftInstigator = left.instigator?.GetType().Name ?? "";
            var rightInstigator = right.instigator?.GetType().Name ?? "";
            return string.CompareOrdinal(leftInstigator, rightInstigator);
        }
    }
}

#endif
