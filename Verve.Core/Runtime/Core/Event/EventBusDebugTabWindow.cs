#if DEBUG && UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.Scripting;
    using System.Collections.Generic;
    
    
    /// <summary>
    ///   <para>事件总线调试窗口</para>
    /// </summary>
    [Preserve, DebugItem("EventBus")]
    sealed class EventBusDebugTabWindow : DebugTabWindow
    {
        private const int MaxRecords = 1000;
        private string m_SearchFilter = "";
        private Vector2 m_ScrollPosition;
        private bool m_ShowOffEvents = true;
        private int m_OffEventCount;
        private readonly Dictionary<string, bool> m_EventFoldouts = new();
        private readonly Dictionary<string, bool> m_HandlerFoldouts = new();
        private readonly List<EventDispatcher<int>.EventRecord> m_EventRecords = new();
        private GUIStyle m_HeaderStyle;
        private GUIStyle m_TagStyle;
        private GUIStyle m_DetailStyle;
        private GUIStyle m_SectionStyle;
        private bool m_StylesReady;
        private int m_StyleFontSize = -1;
        private Color m_StyleFontColor;

        [Preserve]
        public EventBusDebugTabWindow(DebugTabWindowSettings settings) : base(settings) { }

        public override void OnShow()
        {
            Game.OnEventRecorded += OnEventRecorded;
        }

        public override void OnHide()
        {
            Game.OnEventRecorded -= OnEventRecorded;
        }

        public override void Draw()
        {
            EnsureStyles();
            DrawToolbar();
            DrawEventStates();
        }

        private void EnsureStyles()
        {
            if (!m_StylesReady)
            {
                m_StylesReady = true;
                m_HeaderStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                m_TagStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(6, 6, 2, 2) };
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
            m_DetailStyle.fontSize = Mathf.Max(10, fontSize - 1);
            m_HeaderStyle.normal.textColor = fontColor;
            m_DetailStyle.normal.textColor = fontColor;
            m_TagStyle.normal.textColor = fontColor;
            m_TagStyle.hover.textColor = fontColor;
            m_TagStyle.active.textColor = fontColor;
            m_TagStyle.focused.textColor = fontColor;
        }

        private void OnEventRecorded(EventDispatcher<int>.EventRecord record)
        {
            m_EventRecords.Add(record);
            if (m_EventRecords.Count > MaxRecords)
            {
                m_EventRecords.RemoveAt(0);
            }
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            if (GUILayout.Button("Clear", GUILayout.Width(80)))
            {
                m_EventRecords.Clear();
            }
            UpdateOffEventCount();
            m_ShowOffEvents = GUILayout.Toggle(m_ShowOffEvents, $"Off({m_OffEventCount})", GUILayout.Width(120));
            GUILayout.FlexibleSpace();
            m_SearchFilter = GUILayout.TextField(m_SearchFilter, GUILayout.Width(220));
            GUILayout.EndHorizontal();
        }

        private void UpdateOffEventCount()
        {
            var groups = GetEventGroups();
            if (!string.IsNullOrEmpty(m_SearchFilter))
            {
                groups = groups.Where(g =>
                    g.eventKey.IndexOf(m_SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    g.handlers.Any(hg =>
                        (hg.handlerInfo?.IndexOf(m_SearchFilter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                        (hg.handler?.Method.Name.IndexOf(m_SearchFilter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                        (hg.handler?.Method.DeclaringType?.Name.IndexOf(m_SearchFilter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0)
                ).ToList();
            }
            m_OffEventCount = groups.Count(g => !g.hasListeners);
        }

        private void DrawEventStates()
        {
            var eventGroups = GetFilteredEventGroups();
            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
            if (eventGroups.Count == 0)
            {
                GUILayout.BeginVertical(m_SectionStyle);
                GUILayout.Label("No events recorded.", m_HeaderStyle);
                GUILayout.EndVertical();
            }
            else
            {
                for (int i = 0; i < eventGroups.Count; i++)
                {
                    DrawEventGroupCard(eventGroups[i]);
                }
            }
            GUILayout.EndScrollView();
        }

        private void DrawEventGroupCard(EventRecordGroup group)
        {
            GUILayout.BeginVertical(m_SectionStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(group.eventKey, m_HeaderStyle);
            GUILayout.FlexibleSpace();
            var tagText = group.hasListeners ? "ON" : "OFF";
            GUILayout.Label(tagText, m_TagStyle, GUILayout.Width(50));
            GUILayout.EndHorizontal();

            if (!m_EventFoldouts.TryGetValue(group.eventKey, out var folded))
            {
                folded = false;
            }
            folded = GUILayout.Toggle(folded, "Details", GUILayout.Width(90));
            m_EventFoldouts[group.eventKey] = folded;

            if (folded)
            {
                DrawEventDetails(group);
            }
            GUILayout.EndVertical();
        }

        private void DrawEventDetails(EventRecordGroup group)
        {
            if (group.handlers.Count == 0)
            {
                GUILayout.Label("No handlers.", m_DetailStyle);
                return;
            }

            foreach (var handlerGroup in group.handlers.OrderByDescending(h => h.isActive))
            {
                DrawHandlerGroup(handlerGroup);
            }
        }

        private void DrawHandlerGroup(HandlerGroup handlerGroup)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            var activeText = handlerGroup.isActive ? "Active" : "Inactive";
            GUILayout.Label(activeText, GUILayout.Width(60));
            GUILayout.Label(handlerGroup.handlerInfo ?? "Unknown Handler", m_DetailStyle);
            GUILayout.EndHorizontal();

            var handlerKey = handlerGroup.handlerKey ?? Guid.NewGuid().ToString();
            if (!m_HandlerFoldouts.TryGetValue(handlerKey, out var folded))
            {
                folded = false;
            }
            folded = GUILayout.Toggle(folded, "Records", GUILayout.Width(90));
            m_HandlerFoldouts[handlerKey] = folded;

            if (folded && handlerGroup.records.Count > 0)
            {
                var recentRecords = handlerGroup.records.OrderByDescending(r => r.timestamp).Take(12);
                foreach (var record in recentRecords)
                {
                    DrawEventRecord(record);
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawEventRecord(EventDispatcher<int>.EventRecord record)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"[{record.timestamp:HH:mm:ss.fff}] {record.recordStatus}", m_DetailStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (record.recordStatus == EventRecordStatus.Emit && !string.IsNullOrEmpty(record.emitSource))
            {
                var location = record.emitSource;
                if (!string.IsNullOrEmpty(record.emitFile))
                {
                    location = record.emitLine > 0 ? $"{location} ({record.emitFile}:{record.emitLine})" : $"{location} ({record.emitFile})";
                }
                GUILayout.Label($"Emit: {location}", m_DetailStyle);
            }

            if (record.recordStatus == EventRecordStatus.Emit && record.arguments != null && record.arguments.Length > 0)
            {
                for (int i = 0; i < record.arguments.Length; i++)
                {
                    var arg = record.arguments[i];
                    var typeName = arg?.GetType().Name ?? "null";
                    var valueText = arg?.ToString() ?? "null";
                    if (valueText.Length > 80) valueText = valueText.Substring(0, 77) + "...";
                    GUILayout.Label($"[{i}] {typeName}: {valueText}", m_DetailStyle);
                }
            }
            GUILayout.EndVertical();
        }

        private string GetEventKeyText(int eventKey)
        {
            if (Game.StringToHashCache != null)
            {
                foreach (var kvp in Game.StringToHashCache)
                {
                    if (kvp.Value == eventKey)
                    {
                        return kvp.Key;
                    }
                }
            }
            return eventKey.ToString();
        }

        private List<EventRecordGroup> GetEventGroups()
        {
            var groups = new Dictionary<int, EventRecordGroup>();

            foreach (var kvp in Game.EventHandlers)
            {
                var eventKey = kvp.Key;
                if (!groups.TryGetValue(eventKey, out var group))
                {
                    group = new EventRecordGroup
                    {
                        eventKey = GetEventKeyText(eventKey),
                        eventKeyHash = eventKey,
                        hasListeners = kvp.Value.Count > 0
                    };
                    groups[eventKey] = group;
                }

                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    var handler = kvp.Value[i];
                    if (handler == null) continue;
                    var method = handler.Method;
                    var handlerKey = $"{method.DeclaringType?.FullName}.{method.Name}";
                    if (group.handlers.All(h => h.handlerKey != handlerKey))
                    {
                        group.handlers.Add(new HandlerGroup
                        {
                            handler = handler,
                            handlerKey = handlerKey,
                            handlerInfo = $"{method.DeclaringType?.Name}.{method.Name}",
                            isActive = true
                        });
                    }
                }
            }

            for (int i = 0; i < m_EventRecords.Count; i++)
            {
                var record = m_EventRecords[i];
                var eventKey = record.eventKey;
                if (!groups.TryGetValue(eventKey, out var group))
                {
                    group = new EventRecordGroup
                    {
                        eventKey = GetEventKeyText(eventKey),
                        eventKeyHash = eventKey,
                        hasListeners = false
                    };
                    groups[eventKey] = group;
                }

                if (record.recordStatus == EventRecordStatus.Emit)
                {
                    group.emitCount++;
                    if (record.timestamp > group.lastEmitTime)
                    {
                        group.lastEmitTime = record.timestamp;
                    }
                }

                if (record.handler != null)
                {
                    var method = record.handler.Method;
                    var handlerKey = $"{method.DeclaringType?.FullName}.{method.Name}";
                    var handlerGroup = group.handlers.FirstOrDefault(h => h.handlerKey == handlerKey);
                    if (handlerGroup == null)
                    {
                        handlerGroup = new HandlerGroup
                        {
                            handler = record.handler,
                            handlerKey = handlerKey,
                            handlerInfo = $"{method.DeclaringType?.Name}.{method.Name}",
                            isActive = record.recordStatus != EventRecordStatus.Off
                        };
                        group.handlers.Add(handlerGroup);
                    }
                    handlerGroup.records.Add(record);
                }
            }

            foreach (var group in groups.Values)
            {
                if (Game.EventHandlers.TryGetValue(group.eventKeyHash, out var activeHandlers))
                {
                    for (int i = 0; i < group.handlers.Count; i++)
                    {
                        var handlerGroup = group.handlers[i];
                        if (handlerGroup.handler != null)
                        {
                            handlerGroup.isActive = activeHandlers.Contains(handlerGroup.handler);
                        }
                    }
                }
            }

            return groups.Values.ToList();
        }

        private List<EventRecordGroup> GetFilteredEventGroups()
        {
            var groups = GetEventGroups();
            if (!m_ShowOffEvents)
            {
                groups = groups.Where(g => g.hasListeners).ToList();
            }
            if (!string.IsNullOrEmpty(m_SearchFilter))
            {
                groups = groups.Where(g =>
                    g.eventKey.IndexOf(m_SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    g.handlers.Any(hg =>
                        (hg.handlerInfo?.IndexOf(m_SearchFilter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                        (hg.handler?.Method.Name.IndexOf(m_SearchFilter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                        (hg.handler?.Method.DeclaringType?.Name.IndexOf(m_SearchFilter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0)
                ).ToList();
            }
            return groups;
        }
        
        private class HandlerGroup
        {
            public Delegate handler;
            public string handlerKey;
            public string handlerInfo;
            public bool isActive;
            public readonly List<EventDispatcher<int>.EventRecord> records = new();
        }
        
        private class EventRecordGroup
        {
            public string eventKey;
            public int eventKeyHash;
            public bool hasListeners;
            public int emitCount;
            public DateTime lastEmitTime;
            public readonly List<HandlerGroup> handlers = new();
        }
    }
}

#endif