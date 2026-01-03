#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using UnityEngine;
    using UnityEditor;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;

    
    /// <summary>
    ///   <para>事件总线调试器窗口</para>
    /// </summary>
    internal class EventBusDebuggerWindow : EditorWindow
    {
        private string m_SearchFilter;
        private Vector2 m_ScrollPosition;
        private bool m_ShowOffEvents = true;
        private const int MAX_RECORDS = 1000;
        private readonly Dictionary<string, bool> m_EventFoldouts = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> m_HandlerFoldouts = new Dictionary<string, bool>();
        private readonly List<EventDispatcher<int>.EventRecord> m_EventRecords = new List<EventDispatcher<int>.EventRecord>();

        /// <summary>
        ///   <para>清除模式</para>
        /// </summary>
        [Flags]
        private enum ClearMode
        {
            [InspectorName("Clear Records Only")] RecordsOnly = 1,
            [InspectorName("Clear All")] All = RecordsOnly
        }

        /// <summary>
        ///   <para>处理器分组</para>
        /// </summary>
        private class HandlerGroup
        {
            public Delegate Handler;
            public string HandlerKey;
            public string HandlerInfo;
            public bool IsActive;
            public readonly List<EventDispatcher<int>.EventRecord> Records = new List<EventDispatcher<int>.EventRecord>();
        }

        /// <summary>
        ///   <para>事件记录分组</para>
        /// </summary>
        private class EventRecordGroup
        {
            public string EventKey;
            public bool HasListeners;
            public int EmitCount;
            public DateTime LastEmitTime;
            public readonly List<HandlerGroup> Handlers = new List<HandlerGroup>();
        }

        /// <summary>
        ///   <para>样式</para>
        /// </summary>
        private static class Styles
        {
            /// <summary>
            ///   <para>标题样式</para>
            /// </summary>
            public static readonly GUIStyle HeaderStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };

            /// <summary>
            ///   <para>卡片样式</para>
            /// </summary>
            public static readonly GUIStyle CardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(2, 2, 3, 3)
            };

            /// <summary>
            ///   <para>标签样式</para>
            /// </summary>
            public static readonly GUIStyle TagStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                padding = new RectOffset(8, 8, 3, 3),
                margin = new RectOffset(2, 2, 0, 0),
                normal = { background = CoreEditorUtility.MakeTex(1, 1, new Color(0.2f, 0.2f, 0.2f, 0.4f)) },
                alignment = TextAnchor.MiddleCenter
            };

            /// <summary>
            ///   <para>折叠样式</para>
            /// </summary>
            public static readonly GUIStyle FoldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 11
            };

            /// <summary>
            ///   <para>详细信息样式</para>
            /// </summary>
            public static readonly GUIStyle DetailStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                wordWrap = true,
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.4f, 0.4f, 0.4f) }
            };

            /// <summary>
            ///   <para>类型样式</para>
            /// </summary>
            public static readonly GUIStyle TypeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.6f, 0.8f, 1f) : new Color(0.1f, 0.3f, 0.6f) }
            };

            /// <summary>
            ///   <para>可选项文本样式</para>
            /// </summary>
            public static readonly GUIStyle SelectableLabelStyle = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(4, 4, 2, 2),
                normal = { background = CoreEditorUtility.MakeTex(1, 1, new Color(0.1f, 0.1f, 0.1f, 0.3f)) }
            };

            /// <summary>
            ///   <para>链接样式</para>
            /// </summary>
            public static readonly GUIStyle LinkStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.3f, 0.6f, 1f) : new Color(0.1f, 0.3f, 0.8f) },
                fontStyle = FontStyle.Normal,
                richText = true
            };

            /// <summary>
            ///   <para>监听事件颜色</para>
            /// </summary>
            public static readonly Color OnEventColor = new Color(0.11f, 0.73f, 0.42f, 0.1f);
            
            /// <summary>
            ///   <para>取消监听事件颜色</para>
            /// </summary>
            public static readonly Color OffEventColor = new Color(0.3f, 0.3f, 0.3f, 0.15f);
            
            /// <summary>
            ///   <para>派发事件颜色</para>
            /// </summary>
            public static readonly Color EmitEventColor = new Color(0.2f, 0.4f, 0.8f, 0.1f);
            
            /// <summary>
            ///   <para>激活处理器颜色</para>
            /// </summary>
            public static readonly Color ActiveHandlerColor = new Color(0.11f, 0.73f, 0.42f, 0.05f);
            
            /// <summary>
            ///   <para>未激活处理器颜色</para>
            /// </summary>
            public static readonly Color InactiveHandlerColor = new Color(0.5f, 0.3f, 0.3f, 0.1f);
        }

        [MenuItem("Window/Verve/EventBus Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<EventBusDebuggerWindow>("EventBus Debugger");
            window.minSize = new Vector2(800, 500);
            window.Show();
        }

        private void OnEnable()
        {
            Game.OnEventRecorded += OnEventRecorded;
        }

        private void OnDisable()
        {
            Game.OnEventRecorded -= OnEventRecorded;
            m_EventFoldouts?.Clear();
            m_HandlerFoldouts?.Clear();
            m_EventRecords?.Clear();
        }

        private void OnEventRecorded(EventDispatcher<int>.EventRecord record)
        {
            m_EventRecords.Add(record);
            if (m_EventRecords.Count > MAX_RECORDS)
            {
                m_EventRecords.RemoveAt(0);
            }
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawEventStates();
        }

        /// <summary>
        ///   <para>绘制工具栏</para>
        /// </summary>
        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (EditorGUILayout.DropdownButton(new GUIContent("Clear"), FocusType.Passive, EditorStyles.toolbarDropDown))
                {
                    var menu = new GenericMenu();
                    foreach (var mode in Enum.GetNames(typeof(ClearMode)))
                    {
                        menu.AddItem(new GUIContent(mode), false, () => ClearEvents((ClearMode)Enum.Parse(typeof(ClearMode), mode)));
                    }
                    menu.ShowAsContext();
                }

                GUILayout.FlexibleSpace();

                var newSearchFilter = GUILayout.TextField(m_SearchFilter,
                    EditorStyles.toolbarSearchField, GUILayout.Width(225));
                if (newSearchFilter != m_SearchFilter)
                {
                    m_SearchFilter = newSearchFilter;
                }

                GUILayout.Space(10);

                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent(m_ShowOffEvents ? "d_VisibilityOn" : "d_VisibilityOff"))
                {
                    tooltip = "Show/Hide Off Events",
                    text = GetEventGroups().Count(g => !g.HasListeners).ToString()
                }, EditorStyles.toolbarButton))
                {
                    m_ShowOffEvents = !m_ShowOffEvents;
                }
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        ///   <para>清除事件</para>
        /// </summary>
        private void ClearEvents(ClearMode clearMode)
        {
            if ((clearMode & ClearMode.RecordsOnly) != 0)
            {
                m_EventRecords?.Clear();
            }
        }

        /// <summary>
        ///   <para>绘制事件状态列表</para>
        /// </summary>
        private void DrawEventStates()
        {
            var eventGroups = GetFilteredEventGroups();

            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
            {
                if (eventGroups.Any())
                {
                    foreach (var group in eventGroups)
                    {
                        DrawEventGroupCard(group);
                    }
                }
                else
                {
                    DrawEmptyState();
                }
            }
            GUILayout.EndScrollView();
        }

        /// <summary>
        ///   <para>绘制空状态</para>
        /// </summary>
        private void DrawEmptyState()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUILayout.Label(EditorGUIUtility.IconContent("console.infoicon"),
                GUILayout.Width(32), GUILayout.Height(32));
            GUILayout.Label("No events recorded yet.",
                Styles.HeaderStyle, GUILayout.Height(32));
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        /// <summary>
        ///   <para>绘制事件组卡片</para>
        /// </summary>
        private void DrawEventGroupCard(EventRecordGroup group)
        {
            var backgroundColor = group.HasListeners ? Styles.OnEventColor : Styles.OffEventColor;
            var originalBackground = GUI.backgroundColor;
            GUI.backgroundColor = backgroundColor;

            GUILayout.BeginVertical(Styles.CardStyle);
            GUI.backgroundColor = originalBackground;

            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(group.EventKey, Styles.HeaderStyle);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();

                DrawEventStatusTag(group);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            if (!m_EventFoldouts.ContainsKey(group.EventKey))
                m_EventFoldouts[group.EventKey] = false;

            m_EventFoldouts[group.EventKey] = EditorGUILayout.Foldout(
                m_EventFoldouts[group.EventKey], "Details", Styles.FoldoutStyle);

            if (m_EventFoldouts[group.EventKey])
            {
                EditorGUI.indentLevel++;
                GUILayout.Space(5);
                DrawEventDetails(group);
                EditorGUI.indentLevel--;
            }

            GUILayout.EndVertical();
        }

        /// <summary>
        ///   <para>绘制事件详情</para>
        /// </summary>
        private void DrawEventDetails(EventRecordGroup group)
        {
            if (group.Handlers?.Count > 0)
            {
                foreach (var handlerGroup in group.Handlers.OrderByDescending(h => h.IsActive))
                {
                    DrawHandlerGroup(handlerGroup);
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.Label("No handlers.", Styles.DetailStyle);
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        ///   <para>绘制处理器组</para>
        /// </summary>
        private void DrawHandlerGroup(HandlerGroup handlerGroup)
        {
            var backgroundColor = handlerGroup.IsActive ? Styles.ActiveHandlerColor : Styles.InactiveHandlerColor;
            var originalBackground = GUI.backgroundColor;
            GUI.backgroundColor = backgroundColor;

            GUILayout.BeginVertical(GUI.skin.box);
            GUI.backgroundColor = originalBackground;
            GUILayout.Space(2);

            var handler = handlerGroup.Handler;
            var handlerKey = handlerGroup.HandlerKey;

            if (!m_HandlerFoldouts.ContainsKey(handlerKey))
                m_HandlerFoldouts[handlerKey] = false;

            GUILayout.BeginHorizontal();
            {
                m_HandlerFoldouts[handlerKey] = EditorGUILayout.Foldout(
                    m_HandlerFoldouts[handlerKey], "", true);

                if (handler != null)
                {
                    var method = handler.Method;

                    var scriptIcon = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;

                    if (scriptIcon != null)
                    {
                        GUILayout.Label(new GUIContent(scriptIcon), GUILayout.Width(16), GUILayout.Height(16));
                    }

                    if (GUILayout.Button(new GUIContent(GenerateMethodSignature(method)), Styles.LinkStyle, GUILayout.ExpandWidth(false)))
                    {
                        JumpToMethod(method);
                    }
                }
                else
                {
                    var signatureContent = new GUIContent(EditorGUIUtility.IconContent("console.warnicon"))
                    {
                        text = string.IsNullOrEmpty(handlerGroup.HandlerInfo) ? 
                            "Unknown Handler" : handlerGroup.HandlerInfo,
                    };
                    GUILayout.Label(signatureContent, Styles.LinkStyle);
                }

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            if (m_HandlerFoldouts[handlerKey] && handlerGroup.Records?.Count > 0)
            {
                var recentRecords = handlerGroup.Records
                    .OrderByDescending(r => r.Timestamp)
                    .Take(20);

                foreach (var record in recentRecords)
                {
                    DrawEventRecord(record);
                }
            }

            GUILayout.EndVertical();
            GUILayout.Space(2);
        }

        /// <summary>
        ///   <para>绘制事件记录</para>
        /// </summary>
        private void DrawEventRecord(EventDispatcher<int>.EventRecord record)
        {
            var backgroundColor = record.RecordStatus switch
            {
                EventRecordStatus.Emit => Styles.EmitEventColor,
                EventRecordStatus.On => Styles.ActiveHandlerColor,
                EventRecordStatus.Off => Styles.InactiveHandlerColor,
                _ => Styles.OffEventColor
            };

            var originalBackground = GUI.backgroundColor;
            GUI.backgroundColor = backgroundColor;

            GUILayout.BeginVertical(GUI.skin.box);
            GUI.backgroundColor = originalBackground;
            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(10);

                var statusColor = record.RecordStatus switch
                {
                    EventRecordStatus.Emit => Color.blue,
                    EventRecordStatus.On => Color.green,
                    EventRecordStatus.Off => new Color(1f, 0.5f, 0f),
                    _ => Color.gray
                };

                var originalColor = GUI.color;
                GUI.color = statusColor;
                GUILayout.Label("●", GUILayout.Width(16));
                GUI.color = originalColor;

                GUILayout.Label($"[{record.Timestamp:HH:mm:ss.fff} {record.RecordStatus.ToString().ToUpper()}]", Styles.DetailStyle);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
            
            if (record.RecordStatus == EventRecordStatus.Emit && record.Arguments?.Length > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                GUILayout.Label("Arguments:", Styles.DetailStyle);
                GUILayout.EndHorizontal();
                
                for (int i = 0; i < record.Arguments.Length; i++)
                {
                    DrawArgumentDetail(i, record.Arguments[i]);
                }
            }

            GUILayout.EndVertical();
            GUILayout.Space(2);
        }

        /// <summary>
        ///   <para>绘制参数详情</para>
        /// </summary>
        private void DrawArgumentDetail(int index, object argument)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(40);

            var typeName = argument?.GetType().Name ?? "null";
            var valueText = argument?.ToString() ?? "null";
            valueText = valueText.Length > 50 ? valueText[..47] + "..." : valueText;
            
            GUILayout.Label($"[{index}] ", Styles.DetailStyle, GUILayout.Width(30));
            GUILayout.Label($"{typeName}: ", Styles.TypeStyle, GUILayout.Width(80));

            var valueRect = GUILayoutUtility.GetRect(300, 20);
            EditorGUI.SelectableLabel(valueRect, valueText, Styles.SelectableLabelStyle);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        ///   <para>绘制事件状态标签</para>
        /// </summary>
        private void DrawEventStatusTag(EventRecordGroup group)
        {
            var tagText = group.HasListeners ? "ON" : "OFF";
            var tagColor = group.HasListeners ? Color.green : new Color(0.8f, 0.3f, 0.3f);

            if (group.HasListeners && group.EmitCount > 0)
            {
                var timeSinceLastEmit = (DateTime.Now - group.LastEmitTime).TotalSeconds;
                if (timeSinceLastEmit < 5)
                {
                    var pulse = (float)(Math.Sin(EditorApplication.timeSinceStartup * 8) * 0.3 + 0.7);
                    tagColor = Color.Lerp(tagColor, Color.cyan, pulse);
                }
            }

            var originalColor = GUI.color;
            GUI.color = tagColor;

            var style = new GUIStyle(Styles.TagStyle)
            {
                normal = { textColor = Color.white }
            };

            GUILayout.Label(new GUIContent(tagText), style, GUILayout.ExpandWidth(false));

            GUI.color = originalColor;
        }

        /// <summary>
        ///   <para>获取事件键的字符串表示</para>
        /// </summary>
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

        /// <summary>
        ///   <para>获取事件分组</para>
        /// </summary>
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
                        EventKey = GetEventKeyText(eventKey),
                        HasListeners = true
                    };
                    groups[eventKey] = group;
                }

                foreach (var handler in kvp.Value)
                {
                    var method = handler.Method;
                    var handlerKey = $"{method.DeclaringType?.FullName}.{method.Name}";
                    
                    var handlerGroup = group.Handlers.FirstOrDefault(h => h.HandlerKey == handlerKey);
                    if (handlerGroup == null)
                    {
                        handlerGroup = new HandlerGroup
                        {
                            Handler = handler,
                            HandlerKey = handlerKey,
                            HandlerInfo = $"{method.DeclaringType?.Name}.{method.Name}",
                            IsActive = true
                        };
                        group.Handlers.Add(handlerGroup);
                    }
                }
            }

            foreach (var record in m_EventRecords)
            {
                var eventKey = record.EventKey;
                if (!groups.TryGetValue(eventKey, out var group))
                {
                    group = new EventRecordGroup
                    {
                        EventKey = GetEventKeyText(eventKey),
                        HasListeners = false
                    };
                    groups[eventKey] = group;
                }

                if (record.RecordStatus == EventRecordStatus.Emit)
                {
                    group.EmitCount++;
                    if (record.Timestamp > group.LastEmitTime)
                    {
                        group.LastEmitTime = record.Timestamp;
                    }
                    
                    if (record.Handler != null)
                    {
                        var method = record.Handler.Method;
                        var handlerKey = $"{method.DeclaringType?.FullName}.{method.Name}";
                        
                        var handlerGroup = group.Handlers.FirstOrDefault(h => h.HandlerKey == handlerKey);
                        if (handlerGroup == null)
                        {
                            handlerGroup = new HandlerGroup
                            {
                                Handler = record.Handler,
                                HandlerKey = handlerKey,
                                HandlerInfo = $"{method.DeclaringType?.Name}.{method.Name}",
                                IsActive = true
                            };
                            group.Handlers.Add(handlerGroup);
                        }
                        handlerGroup.Records.Add(record);
                    }
                }
                else if (record.Handler != null)
                {
                    var method = record.Handler.Method;
                    var handlerKey = $"{method.DeclaringType?.FullName}.{method.Name}";
                    
                    var handlerGroup = group.Handlers.FirstOrDefault(h => h.HandlerKey == handlerKey);
                    if (handlerGroup == null)
                    {
                        handlerGroup = new HandlerGroup
                        {
                            Handler = record.Handler,
                            HandlerKey = handlerKey,
                            HandlerInfo = $"{method.DeclaringType?.Name}.{method.Name}",
                            IsActive = false
                        };
                        group.Handlers.Add(handlerGroup);
                    }

                    handlerGroup.Records.Add(record);
                }
            }

            foreach (var group in groups.Values)
            {
                if (Game.EventHandlers.TryGetValue(GetEventHashFromKey(group.EventKey), out var activeHandlers))
                {
                    foreach (var handlerGroup in group.Handlers)
                    {
                        if (handlerGroup.Handler != null)
                        {
                            handlerGroup.IsActive = activeHandlers.Contains(handlerGroup.Handler);
                        }
                    }
                }
            }

            return groups.Values.ToList();
        }

        /// <summary>
        ///   <para>从事件键字符串获取哈希值</para>
        /// </summary>
        private int GetEventHashFromKey(string eventKey)
        {
            if (int.TryParse(eventKey, out var hash))
            {
                return hash;
            }
            if (Game.StringToHashCache != null && Game.StringToHashCache.TryGetValue(eventKey, out var hashValue))
            {
                return hashValue;
            }
            
            return eventKey.GetHashCode();
        }

        /// <summary>
        ///   <para>获取筛选后的事件分组</para>
        /// </summary>
        private List<EventRecordGroup> GetFilteredEventGroups()
        {
            var groups = GetEventGroups();

            if (!m_ShowOffEvents)
            {
                groups = groups.Where(g => g.HasListeners).ToList();
            }

            if (!string.IsNullOrEmpty(m_SearchFilter))
            {
                groups = groups.Where(g =>
                    g.EventKey.IndexOf(m_SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    g.Handlers.Any(hg =>
                        (hg.HandlerInfo?.IndexOf(m_SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (hg.Handler?.Method.Name.IndexOf(m_SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (hg.Handler?.Method.DeclaringType?.Name.IndexOf(m_SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0))
                ).ToList();
            }

            return groups;
        }

        /// <summary>
        ///   <para>跳转到方法定义</para>
        /// </summary>
        private void JumpToMethod(MethodInfo method)
        {
            var declaringType = method.DeclaringType;
            if (declaringType == null)
            {
                Debug.LogWarning($"Cannot jump to method - declaring type is null.");
                return;
            }

            var monoScript = CoreEditorUtility.FindMonoScriptForType(declaringType);
            if (monoScript != null)
            {
                AssetDatabase.OpenAsset(monoScript);
                
                var scriptPath = AssetDatabase.GetAssetPath(monoScript);
                if (!string.IsNullOrEmpty(scriptPath))
                {
                    UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(scriptPath, 1);
                }
            }
            else
            {
                Debug.LogWarning($"Cannot jump to {declaringType.FullName}.{method.Name} - Type is not a Unity script.");
            }
        }

        /// <summary>
        ///   <para>生成方法签名</para>
        /// </summary>
        private string GenerateMethodSignature(MethodInfo method)
        {
            try
            {
                var parameters = method.GetParameters();
                var paramStrings = new List<string>();

                foreach (var param in parameters)
                {
                    paramStrings.Add($"{param.ParameterType.Name} {param.Name}");
                }

                return $"{method.ReturnType.Name} {method.DeclaringType?.Name ?? "Unknown"}.{method.Name}({string.Join(", ", paramStrings)})";
            }
            catch
            {
                return $"{method.Name}()";
            }
        }
    }
}
#endif