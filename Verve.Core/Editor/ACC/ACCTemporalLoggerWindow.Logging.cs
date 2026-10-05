// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using UnityEditor;
    using UnityEngine;

    internal sealed partial class ACCTemporalLoggerWindow
    {
        [SerializeField] private bool m_WriteLog;
        [NonSerialized] private CapabilityDebugTrace m_LogTrace;
        private StreamWriter m_LogWriter;

        /// <summary>
        ///   <para>仅写入启用后的新事件；切换世界或重新启用记录时创建独立日志。</para>
        /// </summary>
        private void UpdateFileLogging()
        {
            if (!m_WriteLog || !m_RecordingEnabled || !HasWorld ||
                !EditorApplication.isPlaying || m_PlayModeTransition)
            {
                StopFileLogging();
                return;
            }
            var trace = m_SelectedWorld.DebugTrace;
            if (ReferenceEquals(m_LogTrace, trace)) return;
            StopFileLogging();
            if (!m_WriteLog) return;
            try
            {
                var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
                Directory.CreateDirectory(directory);
                var filename = "ACC-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fffffff", CultureInfo.InvariantCulture) + ".log";
                m_LogWriter = new StreamWriter(new FileStream(Path.Combine(directory, filename),
                    FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
                m_LogWriter.WriteLine("# ACC | World: " + EscapeCell(m_SelectedWorld.Name));
                m_LogWriter.WriteLine("# Started: " + DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture));
                m_LogWriter.WriteLine("Sequence\tActor\tCapability\tKind\tFrame\tTime\tMessage");
                m_LogTrace = trace;
                m_LogTrace.Recorded += WriteLogEvent;
            }
            catch (Exception exception) { DisableFileLogging(exception); }
        }

        private void WriteLogEvent(CapabilityDebugEvent item)
        {
            try
            {
                m_LogWriter.WriteLine(string.Join("\t", item.sequence.ToString(CultureInfo.InvariantCulture),
                    item.actor.ToString(), EscapeCell(item.capabilityType?.FullName), item.kind.ToString(),
                    item.frame.ToString(CultureInfo.InvariantCulture), item.time.ToString("R", CultureInfo.InvariantCulture),
                    EscapeCell(item.error)));
            }
            catch (Exception exception) { DisableFileLogging(exception); }
        }

        private void FlushLog()
        {
            try { m_LogWriter?.Flush(); }
            catch (Exception exception) { DisableFileLogging(exception); }
        }

        private void StopFileLogging(bool reportErrors = true)
        {
            if (m_LogTrace != null) m_LogTrace.Recorded -= WriteLogEvent;
            m_LogTrace = null;
            var writer = m_LogWriter;
            m_LogWriter = null;
            try { writer?.Dispose(); }
            catch (Exception exception)
            {
                if (reportErrors) DisableFileLogging(exception);
            }
        }

        private void DisableFileLogging(Exception exception)
        {
            m_WriteLog = false;
            StopFileLogging(false);
            Debug.LogWarning("ACC 日志写入已关闭：" + exception.Message);
        }
    }
}

#endif
