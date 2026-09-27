#if (DEBUG || DEVELOPMENT_BUILD) && UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.Scripting;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>事件总线调试窗口。</para>
    /// </summary>
    [Preserve, DebugItem("EventBus")]
    sealed class EventBusDebugTabWindow : DebugTabWindow
    {
        /// <summary>
        ///   <para>记录数量上限。</para>
        /// </summary>
        private const int MaxRecords = 1000;
        /// <summary>
        ///   <para>搜索筛选。</para>
        /// </summary>
        private string m_SearchFilter = "";
        /// <summary>
        ///   <para>滚动位置。</para>
        /// </summary>
        private Vector2 m_ScrollPosition;
        /// <summary>
        ///   <para>显示取消订阅事件。</para>
        /// </summary>
        private bool m_ShowOffEvents = true;
        /// <summary>
        ///   <para>取消订阅事件数量。</para>
        /// </summary>
        private int m_OffEventCount;
        /// <summary>
        ///   <para>事件折叠栏。</para>
        /// </summary>
        private readonly Dictionary<string, bool> m_EventFoldouts = new();
        /// <summary>
        ///   <para>处理函数折叠栏。</para>
        /// </summary>
        private readonly Dictionary<string, bool> m_HandlerFoldouts = new();
        /// <summary>
        ///   <para>事件记录。</para>
        /// </summary>
        private readonly List<EventDispatcher<EventKey>.EventRecord> m_EventRecords = new();
        /// <summary>
        ///   <para>表头样式。</para>
        /// </summary>
        private GUIStyle m_HeaderStyle;
        /// <summary>
        ///   <para>标记样式。</para>
        /// </summary>
        private GUIStyle m_TagStyle;
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
        ///   <para>创建事件总线调试标签页窗口。</para>
        /// </summary>
        /// <param name="settings">设置。</param>
        [Preserve]
        public EventBusDebugTabWindow(DebugTabWindowSettings settings) : base(settings) { }

        /// <inheritdoc />
        public override void OnShow() => Game.OnEventRecorded += OnEventRecorded;

        /// <inheritdoc />
        public override void OnHide() => Game.OnEventRecorded -= OnEventRecorded;

        /// <inheritdoc />
        public override void Draw()
        {
            EnsureStyles();
            DrawToolbar();
            DrawEventStates();
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
                m_TagStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(6, 6, 2, 2) };
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
            m_DetailStyle.fontSize = Mathf.Max(10, fontSize - 1);
            m_HeaderStyle.normal.textColor = fontColor;
            m_DetailStyle.normal.textColor = fontColor;
            m_TagStyle.normal.textColor = fontColor;
            m_TagStyle.hover.textColor = fontColor;
            m_TagStyle.active.textColor = fontColor;
            m_TagStyle.focused.textColor = fontColor;
        }

        /// <summary>
        ///   <para>更新事件记录。</para>
        /// </summary>
        /// <param name="record">记录。</param>
        private void OnEventRecorded(EventDispatcher<EventKey>.EventRecord record)
        {
            m_EventRecords.Add(record);
            if (m_EventRecords.Count > MaxRecords)
            {
                m_EventRecords.RemoveAt(0);
            }
        }

        /// <summary>
        ///   <para>绘制工具栏。</para>
        /// </summary>
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

        /// <summary>
        ///   <para>更新取消订阅事件数量。</para>
        /// </summary>
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

        /// <summary>
        ///   <para>绘制事件状态。</para>
        /// </summary>
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

        /// <summary>
        ///   <para>绘制事件分组卡片。</para>
        /// </summary>
        /// <param name="group">分组。</param>
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

        /// <summary>
        ///   <para>绘制事件详情。</para>
        /// </summary>
        /// <param name="group">分组。</param>
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

        /// <summary>
        ///   <para>绘制处理函数分组。</para>
        /// </summary>
        /// <param name="handlerGroup">处理函数分组。</param>
        private void DrawHandlerGroup(HandlerGroup handlerGroup)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            var activeText = handlerGroup.isActive ? "Active" : "Inactive";
            GUILayout.Label(activeText, GUILayout.Width(60));
            GUILayout.Label(handlerGroup.handlerInfo ?? "Unknown Handler", m_DetailStyle);
            GUILayout.EndHorizontal();

            var handlerKey = handlerGroup.handlerKey;
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

        /// <summary>
        ///   <para>绘制事件记录。</para>
        /// </summary>
        /// <param name="record">记录。</param>
        private void DrawEventRecord(EventDispatcher<EventKey>.EventRecord record)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"[{record.timestamp:HH:mm:ss.fff}] {record.recordStatus}", m_DetailStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

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

        /// <summary>
        ///   <para>获取事件分组。</para>
        /// </summary>
        private List<EventRecordGroup> GetEventGroups()
        {
            var groups = new Dictionary<EventKey, EventRecordGroup>();

            var activeEvents = Game.EventHandlers;
            foreach (var kvp in activeEvents)
            {
                var eventKey = kvp.Key;
                if (!groups.TryGetValue(eventKey, out var group))
                {
                    group = new EventRecordGroup
                    {
                        eventKey = eventKey.ToString(),
                        eventKeyId = eventKey,
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
                        eventKey = eventKey.ToString(),
                        eventKeyId = eventKey,
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
                if (activeEvents.TryGetValue(group.eventKeyId, out var activeHandlers))
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

        /// <summary>
        ///   <para>获取筛选后的事件分组。</para>
        /// </summary>
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
        
        /// <summary>
        ///   <para>处理函数分组。</para>
        /// </summary>
        private class HandlerGroup
        {
            /// <summary>
            ///   <para>处理函数。</para>
            /// </summary>
            public Delegate handler;
            /// <summary>
            ///   <para>处理函数键。</para>
            /// </summary>
            public string handlerKey;
            /// <summary>
            ///   <para>处理函数信息。</para>
            /// </summary>
            public string handlerInfo;
            /// <summary>
            ///   <para>是否激活。</para>
            /// </summary>
            public bool isActive;
            /// <summary>
            ///   <para>记录。</para>
            /// </summary>
            public readonly List<EventDispatcher<EventKey>.EventRecord> records = new();
        }
        
        /// <summary>
        ///   <para>事件记录分组。</para>
        /// </summary>
        private class EventRecordGroup
        {
            /// <summary>
            ///   <para>事件键。</para>
            /// </summary>
            public string eventKey;
            /// <summary>
            ///   <para>事件键标识。</para>
            /// </summary>
            public EventKey eventKeyId;
            /// <summary>
            ///   <para>包含监听器。</para>
            /// </summary>
            public bool hasListeners;
            /// <summary>
            ///   <para>发布数量。</para>
            /// </summary>
            public int emitCount;
            /// <summary>
            ///   <para>最后发布时间。</para>
            /// </summary>
            public DateTime lastEmitTime;
            /// <summary>
            ///   <para>处理函数。</para>
            /// </summary>
            public readonly List<HandlerGroup> handlers = new();
        }
    }
}

#endif