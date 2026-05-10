#if DEBUG && UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Collections.Generic;
    using UnityEngine.Scripting;
    
    
    /// <summary>
    ///   <para>日志调试窗口</para>
    /// </summary>
    [Preserve, DebugItem("Log", -1)]
    sealed class LogDebugTabWindow : DebugTabWindow
    {
        /// <summary>
        ///   <para>日志快照</para>
        /// </summary>
        private readonly List<Game.LogRecord> m_Records = new(2000);
        /// <summary>
        ///   <para>记录每条日志的堆栈展开状态</para>
        /// </summary>
        private readonly Dictionary<long, bool> m_StackFoldouts = new();

        private Vector2 m_ScrollPosition;
        private string m_Search = "";
        private bool m_AutoScroll = true;

        private bool m_ShowLog = true;
        private bool m_ShowWarning = true;
        private bool m_ShowError = true;
        private bool m_ShowException = true;
        private bool m_ShowAssert = true;

        private int m_LogCount;
        private int m_WarningCount;
        private int m_ErrorCount;
        private int m_ExceptionCount;
        private int m_AssertCount;

        private GUIStyle m_LineStyle;
        private GUIStyle m_TagStyle;
        private bool m_StylesReady;
        private int m_StyleFontSize = -1;
        private Color m_StyleFontColor;

        [Preserve]
        public LogDebugTabWindow(DebugTabWindowSettings settings) : base(settings) { }

        public override void OnShow() { }

        public override void OnHide() { }

        public override void Draw()
        {
            EnsureStyles();
            UpdateRecords();
            DrawToolbar();
            DrawRecordsList();
        }

        private void EnsureStyles()
        {
            if (!m_StylesReady)
            {
                m_StylesReady = true;
                m_LineStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
                m_TagStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(6, 6, 2, 2) };
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
            m_LineStyle.fontSize = Mathf.Max(10, fontSize - 1);
            m_LineStyle.normal.textColor = fontColor;
            m_TagStyle.normal.textColor = fontColor;
            m_TagStyle.hover.textColor = fontColor;
            m_TagStyle.active.textColor = fontColor;
            m_TagStyle.focused.textColor = fontColor;
        }

        private void DrawToolbar()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear", GUILayout.Width(80)))
            {
                Game.DebugClearLogs();
                m_StackFoldouts.Clear();
                m_Records.Clear();
                m_LogCount = 0;
                m_WarningCount = 0;
                m_ErrorCount = 0;
                m_ExceptionCount = 0;
                m_AssertCount = 0;
            }
            m_AutoScroll = GUILayout.Toggle(m_AutoScroll, "Auto", GUILayout.Width(70));
            GUILayout.FlexibleSpace();
            m_Search = GUILayout.TextField(m_Search ?? "", GUILayout.Width(260));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            m_ShowLog = GUILayout.Toggle(m_ShowLog, $"Log({m_LogCount})", GUILayout.Width(78));
            m_ShowWarning = GUILayout.Toggle(m_ShowWarning, $"Warn({m_WarningCount})", GUILayout.Width(96));
            m_ShowError = GUILayout.Toggle(m_ShowError, $"Error({m_ErrorCount})", GUILayout.Width(96));
            m_ShowException = GUILayout.Toggle(m_ShowException, $"Exception({m_ExceptionCount})", GUILayout.Width(128));
            m_ShowAssert = GUILayout.Toggle(m_ShowAssert, $"Assert({m_AssertCount})", GUILayout.Width(108));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        /// <summary>
        ///   <para>更新日志快照</para>
        /// </summary>
        private void UpdateRecords()
        {
            m_Records.Clear();
            Game.DebugGetLogRecords(m_Records);
            m_LogCount = 0;
            m_WarningCount = 0;
            m_ErrorCount = 0;
            m_ExceptionCount = 0;
            m_AssertCount = 0;
            for (int i = 0; i < m_Records.Count; i++)
            {
                switch (m_Records[i].type)
                {
                    case LogType.Log:
                        m_LogCount++;
                        break;
                    case LogType.Warning:
                        m_WarningCount++;
                        break;
                    case LogType.Error:
                        m_ErrorCount++;
                        break;
                    case LogType.Exception:
                        m_ExceptionCount++;
                        break;
                    case LogType.Assert:
                        m_AssertCount++;
                        break;
                }
            }
            for (int i = m_Records.Count - 1; i >= 0; i--)
            {
                var record = m_Records[i];
                if (!PassType(record.type) || !PassSearch(record))
                {
                    m_Records.RemoveAt(i);
                }
            }
        }

        /// <summary>
        ///   <para>绘制日志快照列表</para>
        /// </summary>
        private void DrawRecordsList()
        {
            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
            for (int i = 0; i < m_Records.Count; i++)
            {
                DrawRecordCard(m_Records[i]);
            }
            GUILayout.EndScrollView();
            if (m_AutoScroll) m_ScrollPosition.y = float.MaxValue;
        }

        private bool PassType(LogType type)
        {
            return type switch
            {
                LogType.Log => m_ShowLog,
                LogType.Warning => m_ShowWarning,
                LogType.Error => m_ShowError,
                LogType.Exception => m_ShowException,
                LogType.Assert => m_ShowAssert,
                _ => true
            };
        }

        private bool PassSearch(Game.LogRecord record)
        {
            if (string.IsNullOrEmpty(m_Search)) return true;
            var filter = m_Search;
            if (record.message != null && record.message.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (record.stackTrace != null && record.stackTrace.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private void DrawRecordCard(Game.LogRecord record)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"[{record.time:HH:mm:ss.fff}] F:{record.frame} T:{record.threadId}", m_LineStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(record.type.ToString(), m_TagStyle, GUILayout.Width(70));
            if (GUILayout.Button("Copy", GUILayout.Width(60)))
            {
                GUIUtility.systemCopyBuffer = record.message ?? "";
            }
            GUILayout.EndHorizontal();

            var source = FormatCaller(record);
            if (!string.IsNullOrEmpty(source))
            {
                GUILayout.Label(source, m_LineStyle);
            }

            if (!string.IsNullOrEmpty(record.message))
            {
                GUILayout.Label(record.message, m_LineStyle);
            }

            var stack = record.stackTrace;
            if (string.IsNullOrEmpty(stack) && record.type == LogType.Exception)
            {
                stack = record.message;
            }

            if (!string.IsNullOrEmpty(stack))
            {
                if (!m_StackFoldouts.TryGetValue(record.sequence, out var open)) open = false;
                GUILayout.BeginHorizontal();
                open = GUILayout.Toggle(open, "Stack", GUILayout.Width(80));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("CopyStack", GUILayout.Width(90)))
                {
                    GUIUtility.systemCopyBuffer = stack;
                }
                GUILayout.EndHorizontal();
                m_StackFoldouts[record.sequence] = open;
                if (open)
                {
                    GUILayout.TextArea(stack);
                }
            }

            GUILayout.EndVertical();
        }

        private static string FormatCaller(Game.LogRecord record)
        {
            var member = record.callerMember;
            var file = record.callerFile;
            var line = record.callerLine;

            if (string.IsNullOrEmpty(member) && string.IsNullOrEmpty(file)) return null;

            if (!string.IsNullOrEmpty(file) && line > 0)
            {
                var anchor = $"{file}#L{line}";
                if (!string.IsNullOrEmpty(member)) return $"{member} ({anchor})";
                return anchor;
            }

            if (!string.IsNullOrEmpty(file))
            {
                if (!string.IsNullOrEmpty(member)) return $"{member} ({file})";
                return file;
            }

            return member;
        }
    }
}

#endif