// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>标签注册表。</para>
    /// </summary>
    internal static class TagRegistry
    {
        /// <summary>
        ///   <para>是否已经读取项目标签设置。</para>
        /// </summary>
        private static bool s_SettingsLoaded;
        /// <summary>
        ///   <para>下一个可分配标签 ID。</para>
        /// </summary>
        private static int s_NextTagId = 1;
        /// <summary>
        ///   <para>标签名称到标识的映射。</para>
        /// </summary>
        private static readonly Dictionary<string, TagId> s_NameToId = new(32);
        /// <summary>
        ///   <para>标签标识到名称的映射。</para>
        /// </summary>
        private static readonly Dictionary<int, string> s_IdToName = new(32);
        /// <summary>
        ///   <para>保护双向映射和 ID 分配的锁。</para>
        /// </summary>
        private static readonly object s_Lock = new();

        /// <summary>
        ///   <para>获取或注册标签标识。</para>
        /// </summary>
        /// <param name="tagName">标签名称。</param>
        public static TagId GetTagId(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
                throw new ArgumentException("Tag name cannot be null or empty");

            lock (s_Lock)
            {
                EnsureSettingsLoadedNoLock();
                return RegisterTagNoLock(tagName);
            }
        }

        /// <summary>
        ///   <para>获取标签名称；未注册时返回 <see langword="null"/>。</para>
        /// </summary>
        /// <param name="tagId">标签标识。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetTagName(TagId tagId)
        {
            lock (s_Lock)
            {
                EnsureSettingsLoadedNoLock();
                return s_IdToName.TryGetValue(tagId, out var name) ? name : null;
            }
        }

        /// <summary>
        ///   <para>预先注册一组标签名称。</para>
        /// </summary>
        /// <param name="tagNames">待注册的标签名称。</param>
        public static void PreRegister(params string[] tagNames)
        {
            if (tagNames == null) throw new ArgumentNullException(nameof(tagNames));
            if (tagNames.Length == 0) return;

            lock (s_Lock)
            {
                EnsureSettingsLoadedNoLock();
                foreach (var name in tagNames)
                {
                    if (string.IsNullOrWhiteSpace(name))
                        throw new ArgumentException("Tag names cannot be null or empty.", nameof(tagNames));
                    RegisterTagNoLock(name);
                }
            }
        }

        /// <summary>
        ///   <para>清除当前进程中的标签缓存；设置变更或测试隔离时使用。</para>
        /// </summary>
        internal static void Reset()
        {
            lock (s_Lock)
            {
                s_SettingsLoaded = false;
                s_NextTagId = 1;
                s_NameToId.Clear();
                s_IdToName.Clear();
            }
        }

        /// <summary>
        ///   <para>读取项目设置中的固定标签；必须在标签锁内调用。</para>
        /// </summary>
        private static void EnsureSettingsLoadedNoLock()
        {
            if (s_SettingsLoaded) return;
            if (!TagRegistrySettings.TryGetInstance(out var settings))
            {
                s_SettingsLoaded = true;
                return;
            }

            var entries = settings.entries;
            if (entries == null)
                throw new InvalidOperationException("The ACC tag registry contains no entry list.");

            if (settings.nextId <= 0)
                throw new InvalidOperationException("The ACC tag registry contains an invalid next identifier.");

            var names = new HashSet<string>(StringComparer.Ordinal);
            var ids = new HashSet<int>();
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.name) || entry.id <= 0)
                    throw new InvalidOperationException("The ACC tag registry contains an invalid entry.");
                if (!names.Add(entry.name) || !ids.Add(entry.id))
                    throw new InvalidOperationException($"The ACC tag registry contains a duplicate entry: {entry.name} ({entry.id}).");
            }

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                s_NameToId.Add(entry.name, new TagId(entry.id));
                s_IdToName.Add(entry.id, entry.name);
                if (entry.id >= s_NextTagId)
                    s_NextTagId = entry.id == int.MaxValue ? int.MaxValue : entry.id + 1;
            }

            if (settings.nextId > s_NextTagId)
                s_NextTagId = settings.nextId;
            s_SettingsLoaded = true;
        }

        /// <summary>
        ///   <para>在锁内注册动态标签。</para>
        /// </summary>
        /// <param name="tagName">标签名称。</param>
        private static TagId RegisterTagNoLock(string tagName)
        {
            if (s_NameToId.TryGetValue(tagName, out var existing)) return existing;
            if (s_NextTagId == int.MaxValue)
                throw new InvalidOperationException("The ACC tag registry has exhausted all available identifiers.");

            var tagId = new TagId(s_NextTagId++);
            s_NameToId.Add(tagName, tagId);
            s_IdToName.Add(tagId, tagName);
            return tagId;
        }
    }
}