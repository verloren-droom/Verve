namespace Verve
{
    using System;
    using System.Threading;
    using System.Globalization;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
#endif
    
    
    /// <summary>
    ///   <para>多语言查找接口</para>
    /// </summary>
    public interface II18NLookup
    {
        /// <summary>
        ///   <para>读取指定语言下的键对应的字符串</para>
        /// </summary>
        bool TryGet(string language, string key, out string value);
        /// <summary>
        ///   <para>获取指定语言下的表</para>
        /// </summary>
        IReadOnlyDictionary<string, string> GetTable(string language);
    }
    
    /// <summary>
    ///   <para>游戏入口：多语言部分</para>
    /// </summary>
    public static partial class Game
    {
        private static readonly object s_I18NLock = new();
        private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> s_I18NTables = new (StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> s_I18NTableHashes = new (StringComparer.OrdinalIgnoreCase);
        
        private static string s_Language;
        private static IReadOnlyDictionary<string, string> s_CurrentI18NTable;
        private static IReadOnlyDictionary<string, string> s_CurrentLookupTable;
        private static II18NLookup s_I18NLookup;
        private static int s_I18NInitialized;
        
        /// <summary>
        ///   <para>当语言改变时事件</para>
        /// </summary>
        public static event Action<string> OnLanguageChanged;
        /// <summary>
        ///   <para>当多语言查找表改变时事件</para>
        /// </summary>
        public static event Action<II18NLookup> OnI18NLookupChanged;
        
#if UNITY_5_3_OR_NEWER
        /// <summary>
        /// 将 <see cref="SystemLanguage"/> 映射到语言
        /// </summary>
        public static Func<SystemLanguage, string> SystemLanguageToLanguage { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; set; }
#else
        /// <summary>
        /// 将 <see cref="CultureInfo"/> 映射到语言
        /// </summary>
        public static Func<CultureInfo, string> CultureToLanguage { get; set; }
#endif

        /// <summary>
        ///   <para>语言选择函数</para>
        /// </summary>
        public static Func<string> PickLanguage { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; set; }
        
        /// <summary>
        ///   <para>语言别名映射</para>
        /// </summary>
        public static Func<string, string> LanguageAliasMap { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; set; }
        
        /// <summary>
        ///   <para>当前语言</para>
        /// </summary>
        public static string Language
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                EnsureInitialized();
                return Volatile.Read(ref s_Language);
            }
            set
            {
                EnsureInitialized();
                var language = NormalizeLanguage(string.IsNullOrWhiteSpace(value) ? PickSystemLanguage() : value);
                
                bool changed;
                lock (s_I18NLock)
                {
                    changed = !string.Equals(s_Language, language, StringComparison.OrdinalIgnoreCase);
                    if (!changed) return;
                    
                    Volatile.Write(ref s_Language, language);
                    RefreshCachedI18NTablesLocked();
                }
                
                OnLanguageChanged?.Invoke(language);
            }
        }
        
        /// <summary>
        ///   <para>多语言查找表</para>
        /// </summary>
        public static II18NLookup I18NLookup
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref s_I18NLookup);
            set
            {
                EnsureInitialized();
                
                lock (s_I18NLock)
                {
                    Volatile.Write(ref s_I18NLookup, value);
                    RefreshCachedI18NTablesLocked();
                }
                
                OnI18NLookupChanged?.Invoke(value);
            }
        }

        internal static IReadOnlyDictionary<string, string> CurrentI18NTable => Volatile.Read(ref s_CurrentI18NTable);

        internal static IReadOnlyDictionary<string, string> CurrentLookupTable => Volatile.Read(ref s_CurrentLookupTable);

        internal static int LocalTableCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { lock (s_I18NLock) { return s_I18NTables?.Count ?? 0; } }
        }

        internal static string[] GetAvailableLanguages()
        {
            lock (s_I18NLock)
            {
                var count = s_I18NTables?.Count ?? 0;
                var current = s_Language;
                if (count == 0)
                {
                    if (string.IsNullOrWhiteSpace(current)) return Array.Empty<string>();
                    return new[] { current };
                }

                var includeCurrent = !string.IsNullOrWhiteSpace(current) && !s_I18NTables.ContainsKey(current);
                var result = new string[count + (includeCurrent ? 1 : 0)];
                var index = 0;
                foreach (var key in s_I18NTables.Keys)
                {
                    result[index++] = key;
                }
                if (includeCurrent) result[index] = current;
                return result;
            }
        }

        /// <summary>
        ///   <para>添加多语言表</para>
        /// </summary>
        /// <param name="language">语言</param>
        /// <param name="table">表</param>
        /// <param name="overwrite">是否覆盖</param>
        public static void AddI18NTable(string language, IReadOnlyDictionary<string, string> table, bool overwrite = true)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(language)) return;
            if (table == null) return;
            
            language = NormalizeLanguage(language);
            var copied = new Dictionary<string, string>(table.Count, StringComparer.Ordinal);
            var entryHash = 0;
            var count = 0;
            foreach (var kv in table)
            {
                if (string.IsNullOrEmpty(kv.Key)) continue;
                var value = kv.Value ?? string.Empty;
                copied[kv.Key] = value;
                entryHash ^= HashEntry(kv.Key, value);
                count++;
            }
            var tableHash = entryHash ^ count;
            
            lock (s_I18NLock)
            {
                if (!overwrite && s_I18NTables.ContainsKey(language)) return;

                if (s_I18NTables.TryGetValue(language, out var existing))
                {
                    if (s_I18NTableHashes.TryGetValue(language, out var existingHash) &&
                        existingHash == tableHash &&
                        CompareTablesEqual(existing, copied))
                        return;
                }

                s_I18NTables[language] = copied;
                s_I18NTableHashes[language] = tableHash;
                RefreshCachedI18NTablesLocked();
            }
        }
        
        /// <summary>
        ///   <para>添加或更新多语言表</para>
        /// </summary>
        /// <param name="language">语言</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static void AddOrUpdateI18N(string language, string key, string value)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(language)) return;
            if (string.IsNullOrEmpty(key)) return;
            
            language = NormalizeLanguage(language);
            value ??= string.Empty;
            
            lock (s_I18NLock)
            {
                if (!s_I18NTables.TryGetValue(language, out var existing) || existing == null)
                {
                    var fresh = new Dictionary<string, string>(StringComparer.Ordinal) { [key] = value };
                    s_I18NTables[language] = fresh;
                    s_I18NTableHashes[language] = HashEntry(key, value) ^ 1;
                    RefreshCachedI18NTablesLocked();
                    return;
                }
                
                if (existing.TryGetValue(key, out var currentValue) && string.Equals(currentValue, value, StringComparison.Ordinal)) return;

                var hashBase = s_I18NTableHashes.TryGetValue(language, out var existingHash)
                    ? existingHash
                    : CompareTablesHash(existing);
                var oldCount = existing.Count;
                var newCount = existing.ContainsKey(key) ? oldCount : oldCount + 1;
                var nextHash = hashBase ^ oldCount ^ newCount;
                if (existing.TryGetValue(key, out var oldValue))
                {
                    nextHash ^= HashEntry(key, oldValue ?? string.Empty);
                }
                nextHash ^= HashEntry(key, value);

                var cloned = new Dictionary<string, string>(existing.Count + 1, StringComparer.Ordinal);
                foreach (var kv in existing) cloned[kv.Key] = kv.Value;
                cloned[key] = value;
                s_I18NTables[language] = cloned;
                s_I18NTableHashes[language] = nextHash;
                RefreshCachedI18NTablesLocked();
            }
        }
        
        /// <summary>
        ///   <para>移除多语言表</para>
        /// </summary>
        /// <param name="language">语言</param>
        public static bool RemoveI18NTable(string language)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(language)) return false;
            language = NormalizeLanguage(language);
            
            lock (s_I18NLock)
            {
                var removed = s_I18NTables.Remove(language);
                if (removed) s_I18NTableHashes.Remove(language);
                if (removed) RefreshCachedI18NTablesLocked();
                return removed;
            }
        }
        
        /// <summary>
        ///   <para>清空多语言表</para>
        /// </summary>
        public static void ClearI18NTables()
        {
            EnsureInitialized();
            lock (s_I18NLock)
            {
                s_I18NTables.Clear();
                s_I18NTableHashes.Clear();
                RefreshCachedI18NTablesLocked();
            }
        }
        
        /// <summary>
        ///   <para>获取多语言文本</para>
        /// </summary>
        /// <param name="key">多语言键</param>
        public static string Tr(string key)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(key)) return string.Empty;
            
            var lookup = Volatile.Read(ref s_I18NLookup);
            var lookupTable = Volatile.Read(ref s_CurrentLookupTable);
            if (lookupTable != null && lookupTable.TryGetValue(key, out var sourceValue)) return sourceValue ?? string.Empty;
            if (lookup != null)
            {
                var language = Volatile.Read(ref s_Language);
                if (!string.IsNullOrEmpty(language) && lookup.TryGet(language, key, out var value)) return value ?? string.Empty;
            }
            
            var table = Volatile.Read(ref s_CurrentI18NTable);
            if (table != null && table.TryGetValue(key, out var tableValue)) return tableValue ?? string.Empty;
            
            return key;
        }
        
        /// <summary>
        ///   <para>尝试获取多语言文本</para>
        /// </summary>
        /// <param name="key">多语言键</param>
        /// <param name="value">多语言文本</param>
        public static bool TryTr(string key, out string value)
        {
            EnsureInitialized();
            value = null;
            if (string.IsNullOrEmpty(key)) return false;
            
            var lookup = Volatile.Read(ref s_I18NLookup);
            var lookupTable = Volatile.Read(ref s_CurrentLookupTable);
            if (lookupTable != null && lookupTable.TryGetValue(key, out value)) return true;
            if (lookup != null)
            {
                var language = Volatile.Read(ref s_Language);
                if (!string.IsNullOrEmpty(language) && lookup.TryGet(language, key, out value)) return true;
            }
            
            var table = Volatile.Read(ref s_CurrentI18NTable);
            if (table != null && table.TryGetValue(key, out value)) return true;
            
            value = null;
            return false;
        }
        
        private static string NormalizeLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language)) return null;
            language = language.Trim();
            var alias = LanguageAliasMap;
            if (alias != null)
            {
                var mapped = alias(language);
                if (!string.IsNullOrWhiteSpace(mapped)) language = mapped.Trim();
            }
            
            var underscoreIndex = language.IndexOf('_');
            if (underscoreIndex >= 0) language = language.Replace('_', '-');
            return language;
        }

        private static void RefreshCachedI18NTablesLocked()
        {
            var language = s_Language;
            var lookup = s_I18NLookup;
            
            IReadOnlyDictionary<string, string> current = null;
            if (!string.IsNullOrEmpty(language)) s_I18NTables.TryGetValue(language, out current);
            
            Volatile.Write(ref s_CurrentI18NTable, current);

            IReadOnlyDictionary<string, string> lookupTable = null;
            if (!string.IsNullOrEmpty(language) && lookup != null)
                lookupTable = lookup.GetTable(language);
            Volatile.Write(ref s_CurrentLookupTable, lookupTable);
        }
        
        private static void EnsureInitialized()
        {
            if (Volatile.Read(ref s_I18NInitialized) != 0) return;
            lock (s_I18NLock)
            {
                if (s_I18NInitialized != 0) return;
                s_Language = NormalizeLanguage(PickSystemLanguage());
                RefreshCachedI18NTablesLocked();
                Volatile.Write(ref s_I18NInitialized, 1);
            }
        }
        
        private static string PickSystemLanguage()
        {
#if UNITY_5_3_OR_NEWER
            try
            {
                var pick = PickLanguage;
                if (pick != null)
                {
                    var language = pick();
                    if (!string.IsNullOrWhiteSpace(language)) return language;
                }
                
                var map = SystemLanguageToLanguage;
                if (map == null) return null;
                
                var mapped = map(Application.systemLanguage);
                return string.IsNullOrWhiteSpace(mapped) ? null : mapped;
            }
            catch
            {
                return null;
            }
#else
            try
            {
                var pick = PickLanguage;
                if (pick != null)
                {
                    var language = pick();
                    if (!string.IsNullOrWhiteSpace(language)) return language;
                }
                
                var map = CultureToLanguage;
                if (map == null) return null;
                
                var mapped = map(CultureInfo.CurrentUICulture);
                return string.IsNullOrWhiteSpace(mapped) ? null : mapped;
            }
            catch
            {
                return null;
            }
#endif
        }

        /// <summary>
        ///   <para>比较两个字典是否相等</para>
        /// </summary>
        private static bool CompareTablesEqual(IReadOnlyDictionary<string, string> left, IReadOnlyDictionary<string, string> right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null) return false;
            if (left.Count != right.Count) return false;
            foreach (var kv in left)
            {
                if (!right.TryGetValue(kv.Key, out var value)) return false;
                if (!string.Equals(kv.Value ?? string.Empty, value ?? string.Empty, StringComparison.Ordinal)) return false;
            }
            return true;
        }

        private static int CompareTablesHash(IReadOnlyDictionary<string, string> table)
        {
            if (table == null) return 0;
            var entryHash = 0;
            foreach (var kv in table)
            {
                if (string.IsNullOrEmpty(kv.Key)) continue;
                entryHash ^= HashEntry(kv.Key, kv.Value ?? string.Empty);
            }
            return entryHash ^ table.Count;
        }

        private static int HashEntry(string key, string value)
        {
            unchecked
            {
                var hash = StringComparer.Ordinal.GetHashCode(key ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(value ?? string.Empty);
                return hash;
            }
        }
    }
}
