#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Text;
    using System.Collections.Generic;
    
    /// <summary>
    ///   <para>搜索匹配的配置表内容类别。</para>
    /// </summary>
    internal enum ConfigTableSearchTarget
    {
        /// <summary>
        ///   <para>字段名称。</para>
        /// </summary>
        Field,
        /// <summary>
        ///   <para>字段注释。</para>
        /// </summary>
        Comment,
        /// <summary>
        ///   <para>记录内容。</para>
        /// </summary>
        Content,
        /// <summary>
        ///   <para>指定字段列。</para>
        /// </summary>
        Column
    }
    
    /// <summary>
    ///   <para>一条解析后的搜索条件。</para>
    /// </summary>
    internal readonly struct ConfigTableSearchClause
    {
        /// <summary>
        ///   <para>创建一条搜索条件。</para>
        /// </summary>
        /// <param name="target">匹配目标；为空时匹配全部内容。</param>
        /// <param name="fieldName">列匹配使用的字段名。</param>
        /// <param name="value">待匹配文本。</param>
        /// <param name="isNegated">是否排除此条件。</param>
        /// <param name="requiresExactLength">是否要求文本长度相同。</param>
        /// <param name="isCaseSensitive">是否区分大小写。</param>
        public ConfigTableSearchClause(ConfigTableSearchTarget? target, string fieldName, string value, bool isNegated,
            bool requiresExactLength, bool isCaseSensitive)
        {
            Target = target;
            FieldName = fieldName;
            Value = value;
            IsNegated = isNegated;
            RequiresExactLength = requiresExactLength;
            IsCaseSensitive = isCaseSensitive;
        }
    
        /// <summary>
        ///   <para>限定的匹配目标，未指定时匹配全部内容。</para>
        /// </summary>
        public ConfigTableSearchTarget? Target { get; }
    
        /// <summary>
        ///   <para>列搜索使用的字段名。</para>
        /// </summary>
        public string FieldName { get; }
    
        /// <summary>
        ///   <para>待匹配文本。</para>
        /// </summary>
        public string Value { get; }
    
        /// <summary>
        ///   <para>是否为排除条件。</para>
        /// </summary>
        public bool IsNegated { get; }
    
        /// <summary>
        ///   <para>是否要求源文本长度相同。</para>
        /// </summary>
        public bool RequiresExactLength { get; }
    
        /// <summary>
        ///   <para>是否区分大小写。</para>
        /// </summary>
        public bool IsCaseSensitive { get; }
    }
    
    /// <summary>
    ///   <para>搜索条件组；组内条件按 AND 连接。</para>
    /// </summary>
    internal sealed class ConfigTableSearchGroup
    {
        /// <summary>
        ///   <para>创建一个搜索条件组。</para>
        /// </summary>
        /// <param name="clauses">组内搜索条件。</param>
        public ConfigTableSearchGroup(ConfigTableSearchClause[] clauses) => Clauses = clauses ?? Array.Empty<ConfigTableSearchClause>();
    
        /// <summary>
        ///   <para>该组包含的条件。</para>
        /// </summary>
        public ConfigTableSearchClause[] Clauses { get; }
    }
    
    /// <summary>
    ///   <para>搜索查询；条件组按 OR 连接。</para>
    /// </summary>
    internal sealed class ConfigTableSearchQuery
    {
        /// <summary>
        ///   <para>不包含搜索条件的查询。</para>
        /// </summary>
        public static readonly ConfigTableSearchQuery Empty = new ConfigTableSearchQuery(Array.Empty<ConfigTableSearchGroup>());
    
        /// <summary>
        ///   <para>创建一个搜索查询。</para>
        /// </summary>
        /// <param name="groups">由 OR 连接的条件组。</param>
        public ConfigTableSearchQuery(ConfigTableSearchGroup[] groups) => Groups = groups ?? Array.Empty<ConfigTableSearchGroup>();
    
        /// <summary>
        ///   <para>查询包含的条件组。</para>
        /// </summary>
        public ConfigTableSearchGroup[] Groups { get; }
    
        /// <summary>
        ///   <para>指示查询是否不包含有效条件。</para>
        /// </summary>
        public bool IsEmpty => Groups.Length == 0;
    }
    
    /// <summary>
    ///   <para>配置表搜索解析与匹配工具。</para>
    /// </summary>
    internal static class ConfigTableSearch
    {
        /// <summary>
        ///   <para>将搜索文本解析为可复用的查询对象。</para>
        /// </summary>
        /// <param name="searchText">搜索文本。</param>
        /// <param name="requiresExactLength">是否要求匹配文本长度相同。</param>
        /// <param name="isCaseSensitive">是否区分大小写。</param>
        public static ConfigTableSearchQuery Parse(string searchText, bool requiresExactLength = false, bool isCaseSensitive = false)
        {
            var tokens = Tokenize(searchText);
            if (tokens.Count == 0)
            {
                return ConfigTableSearchQuery.Empty;
            }
    
            var groups = new List<ConfigTableSearchGroup>();
            var clauses = new List<ConfigTableSearchClause>();
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token == "&")
                {
                    continue;
                }
    
                if (token == "|")
                {
                    AddGroup(groups, clauses);
                    continue;
                }
    
                if (TryParseClause(token, requiresExactLength, isCaseSensitive, out var clause))
                {
                    clauses.Add(clause);
                }
            }
            AddGroup(groups, clauses);
            return groups.Count == 0 ? ConfigTableSearchQuery.Empty : new ConfigTableSearchQuery(groups.ToArray());
        }
    
        /// <summary>
        ///   <para>判断配置表是否匹配查询。</para>
        /// </summary>
        /// <param name="table">待匹配的配置表。</param>
        /// <param name="basicSearchText">表名等基础搜索文本。</param>
        /// <param name="query">解析后的搜索查询。</param>
        public static bool MatchesTable(ConfigTableAsset table, string basicSearchText, ConfigTableSearchQuery query)
        {
            if (query == null || query.IsEmpty)
            {
                return true;
            }
    
            for (int i = 0; i < query.Groups.Length; i++)
            {
                if (MatchesGroup(table, basicSearchText, query.Groups[i]))
                {
                    return true;
                }
            }
            return false;
        }
    
        /// <summary>
        ///   <para>判断单元格文本是否匹配查询。</para>
        /// </summary>
        /// <param name="value">单元格文本。</param>
        /// <param name="target">单元格所属内容类别。</param>
        /// <param name="query">解析后的搜索查询。</param>
        public static bool MatchesCell(string value, ConfigTableSearchTarget target, ConfigTableSearchQuery query)
        {
            if (string.IsNullOrEmpty(value) || query == null || query.IsEmpty)
            {
                return false;
            }
    
            for (int groupIndex = 0; groupIndex < query.Groups.Length; groupIndex++)
            {
                var clauses = query.Groups[groupIndex].Clauses;
                for (int clauseIndex = 0; clauseIndex < clauses.Length; clauseIndex++)
                {
                    var clause = clauses[clauseIndex];
                    if (!clause.IsNegated && (!clause.Target.HasValue || clause.Target.Value == target) && Contains(value, clause))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    
        /// <summary>
        ///   <para>判断指定字段的单元格文本是否匹配列条件。</para>
        /// </summary>
        /// <param name="fieldName">字段名称。</param>
        /// <param name="value">单元格文本。</param>
        /// <param name="query">解析后的搜索查询。</param>
        public static bool MatchesColumnValue(string fieldName, string value, ConfigTableSearchQuery query)
        {
            if (string.IsNullOrEmpty(fieldName) || string.IsNullOrEmpty(value) || query == null || query.IsEmpty)
            {
                return false;
            }
    
            for (int groupIndex = 0; groupIndex < query.Groups.Length; groupIndex++)
            {
                var clauses = query.Groups[groupIndex].Clauses;
                for (int clauseIndex = 0; clauseIndex < clauses.Length; clauseIndex++)
                {
                    var clause = clauses[clauseIndex];
                    if (!clause.IsNegated && clause.Target == ConfigTableSearchTarget.Column &&
                        string.Equals(fieldName, clause.FieldName, GetStringComparison(clause)) && Contains(value, clause))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    
        /// <summary>
        ///   <para>匹配分组。</para>
        /// </summary>
        /// <param name="table">表。</param>
        /// <param name="basicSearchText">表名等基础搜索文本。</param>
        /// <param name="group">分组。</param>
        private static bool MatchesGroup(ConfigTableAsset table, string basicSearchText, ConfigTableSearchGroup group)
        {
            var clauses = group.Clauses;
            for (int i = 0; i < clauses.Length; i++)
            {
                var clause = clauses[i];
                if (clause.Target == ConfigTableSearchTarget.Column)
                {
                    if (clause.IsNegated && MatchesColumnClause(table, clause))
                    {
                        return false;
                    }
                    continue;
                }
    
                var isMatch = MatchesTarget(table, basicSearchText, clause);
                if (clause.IsNegated ? isMatch : !isMatch)
                {
                    return false;
                }
            }
    
            return MatchesPositiveColumnClauses(table, clauses);
        }
    
        /// <summary>
        ///   <para>匹配目标。</para>
        /// </summary>
        /// <param name="table">表。</param>
        /// <param name="basicSearchText">表名等基础搜索文本。</param>
        /// <param name="clause">条件。</param>
        private static bool MatchesTarget(ConfigTableAsset table, string basicSearchText, ConfigTableSearchClause clause)
        {
            if (!clause.Target.HasValue)
            {
                return Contains(table?.tableName, clause) || Contains(basicSearchText, clause) || MatchesValues(table?.fields, clause) ||
                    MatchesValues(table?.comments, clause) || MatchesRows(table?.rows, clause);
            }
    
            switch (clause.Target.Value)
            {
                case ConfigTableSearchTarget.Field:
                    return MatchesValues(table?.fields, clause);
                case ConfigTableSearchTarget.Comment:
                    return MatchesValues(table?.comments, clause);
                case ConfigTableSearchTarget.Content:
                    return MatchesRows(table?.rows, clause);
                default:
                    return false;
            }
        }
    
        /// <summary>
        ///   <para>匹配未取反的列条件。</para>
        /// </summary>
        /// <param name="table">表。</param>
        /// <param name="clauses">组内搜索条件。</param>
        private static bool MatchesPositiveColumnClauses(ConfigTableAsset table, ConfigTableSearchClause[] clauses)
        {
            var count = 0;
            for (int i = 0; i < clauses.Length; i++)
            {
                if (clauses[i].Target == ConfigTableSearchTarget.Column && !clauses[i].IsNegated)
                {
                    count++;
                }
            }
            if (count == 0)
            {
                return true;
            }
            if (table?.fields == null || table.rows == null)
            {
                return false;
            }
    
            var fieldIndexes = new int[count];
            var columnClauses = new ConfigTableSearchClause[count];
            var columnIndex = 0;
            for (int i = 0; i < clauses.Length; i++)
            {
                if (clauses[i].Target != ConfigTableSearchTarget.Column || clauses[i].IsNegated)
                {
                    continue;
                }
    
                columnClauses[columnIndex] = clauses[i];
                fieldIndexes[columnIndex] = FindFieldIndex(table.fields, clauses[i]);
                if (fieldIndexes[columnIndex] < 0)
                {
                    return false;
                }
                columnIndex++;
            }
    
            for (int rowIndex = 0; rowIndex < table.rows.Length; rowIndex++)
            {
                var values = table.rows[rowIndex]?.values;
                if (values == null)
                {
                    continue;
                }
    
                var matchesRow = true;
                for (int clauseIndex = 0; clauseIndex < columnClauses.Length; clauseIndex++)
                {
                    var valueIndex = fieldIndexes[clauseIndex];
                    if (valueIndex >= values.Length || !Contains(values[valueIndex], columnClauses[clauseIndex]))
                    {
                        matchesRow = false;
                        break;
                    }
                }
                if (matchesRow)
                {
                    return true;
                }
            }
            return false;
        }
    
        /// <summary>
        ///   <para>匹配列条件。</para>
        /// </summary>
        /// <param name="table">表。</param>
        /// <param name="clause">条件。</param>
        private static bool MatchesColumnClause(ConfigTableAsset table, ConfigTableSearchClause clause)
        {
            if (table?.fields == null || table.rows == null)
            {
                return false;
            }
    
            var fieldIndex = FindFieldIndex(table.fields, clause);
            if (fieldIndex < 0)
            {
                return false;
            }
    
            for (int rowIndex = 0; rowIndex < table.rows.Length; rowIndex++)
            {
                var values = table.rows[rowIndex]?.values;
                if (values != null && fieldIndex < values.Length && Contains(values[fieldIndex], clause))
                {
                    return true;
                }
            }
            return false;
        }
    
        /// <summary>
        ///   <para>查找字段索引。</para>
        /// </summary>
        /// <param name="fields">字段名称。</param>
        /// <param name="clause">条件。</param>
        private static int FindFieldIndex(string[] fields, ConfigTableSearchClause clause)
        {
            if (fields == null)
            {
                return -1;
            }
    
            for (int i = 0; i < fields.Length; i++)
            {
                if (string.Equals(fields[i], clause.FieldName, GetStringComparison(clause)))
                {
                    return i;
                }
            }
            return -1;
        }
    
        /// <summary>
        ///   <para>匹配值。</para>
        /// </summary>
        /// <param name="values">值。</param>
        /// <param name="clause">条件。</param>
        private static bool MatchesValues(string[] values, ConfigTableSearchClause clause)
        {
            if (values == null)
            {
                return false;
            }
    
            for (int i = 0; i < values.Length; i++)
            {
                if (Contains(values[i], clause))
                {
                    return true;
                }
            }
            return false;
        }
    
        /// <summary>
        ///   <para>匹配行。</para>
        /// </summary>
        /// <param name="rows">行。</param>
        /// <param name="clause">条件。</param>
        private static bool MatchesRows(ConfigTableRecord[] rows, ConfigTableSearchClause clause)
        {
            if (rows == null)
            {
                return false;
            }
    
            for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                var row = rows[rowIndex];
                if (row != null && (Contains(row.id, clause) || MatchesValues(row.values, clause)))
                {
                    return true;
                }
            }
            return false;
        }
    
        /// <summary>
        ///   <para>包含。</para>
        /// </summary>
        /// <param name="source">源。</param>
        /// <param name="clause">条件。</param>
        private static bool Contains(string source, ConfigTableSearchClause clause)
        {
            return !string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(clause.Value) &&
                (!clause.RequiresExactLength || source.Length == clause.Value.Length) &&
                source.IndexOf(clause.Value, GetStringComparison(clause)) >= 0;
        }
    
        /// <summary>
        ///   <para>获取字符串比较规则。</para>
        /// </summary>
        /// <param name="clause">条件。</param>
        private static StringComparison GetStringComparison(ConfigTableSearchClause clause) => clause.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    
        /// <summary>
        ///   <para>分词。</para>
        /// </summary>
        /// <param name="searchText">搜索文本。</param>
        private static List<string> Tokenize(string searchText)
        {
            var tokens = new List<string>();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return tokens;
            }
    
            var builder = new StringBuilder();
            var inQuotes = false;
            for (int i = 0; i < searchText.Length; i++)
            {
                var character = searchText[i];
                if (character == '\"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }
    
                if (!inQuotes && (char.IsWhiteSpace(character) || character == '&' || character == '|'))
                {
                    AddToken(tokens, builder);
                    if (character == '&' || character == '|')
                    {
                        tokens.Add(character.ToString());
                    }
                    continue;
                }
                builder.Append(character);
            }
            AddToken(tokens, builder);
            return tokens;
        }
    
        /// <summary>
        ///   <para>添加词元。</para>
        /// </summary>
        /// <param name="tokens">搜索词元列表。</param>
        /// <param name="builder">构建器。</param>
        private static void AddToken(List<string> tokens, StringBuilder builder)
        {
            if (builder.Length > 0)
            {
                tokens.Add(builder.ToString());
                builder.Length = 0;
            }
        }
    
        /// <summary>
        ///   <para>添加分组。</para>
        /// </summary>
        /// <param name="groups">由 OR 连接的条件组。</param>
        /// <param name="clauses">组内搜索条件。</param>
        private static void AddGroup(List<ConfigTableSearchGroup> groups, List<ConfigTableSearchClause> clauses)
        {
            if (clauses.Count == 0)
            {
                return;
            }
    
            groups.Add(new ConfigTableSearchGroup(clauses.ToArray()));
            clauses.Clear();
        }
    
        /// <summary>
        ///   <para>尝试解析条件。</para>
        /// </summary>
        /// <param name="token">搜索词元。</param>
        /// <param name="requiresExactLength">要求精确长度。</param>
        /// <param name="isCaseSensitive">是否区分大小写。</param>
        /// <param name="clause">条件。</param>
        private static bool TryParseClause(string token, bool requiresExactLength, bool isCaseSensitive, out ConfigTableSearchClause clause)
        {
            clause = default(ConfigTableSearchClause);
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }
    
            var isNegated = token[0] == '!';
            if (isNegated)
            {
                token = token.Substring(1);
            }
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }
    
            var separatorIndex = token.IndexOf('=');
            if (separatorIndex < 1 || separatorIndex >= token.Length - 1)
            {
                clause = new ConfigTableSearchClause(null, string.Empty, token, isNegated, requiresExactLength, isCaseSensitive);
                return true;
            }
    
            var key = token.Substring(0, separatorIndex).Trim();
            var value = token.Substring(separatorIndex + 1).Trim();
            if (key.Length == 0 || value.Length == 0)
            {
                return false;
            }
    
            if (key[0] == '@')
            {
                if (TryGetTarget(key.Substring(1), out var target))
                {
                    clause = new ConfigTableSearchClause(target, string.Empty, value, isNegated, requiresExactLength, isCaseSensitive);
                    return true;
                }
                key = key.Substring(1);
            }
    
            clause = new ConfigTableSearchClause(ConfigTableSearchTarget.Column, key, value, isNegated, requiresExactLength, isCaseSensitive);
            return true;
        }
    
        /// <summary>
        ///   <para>尝试获取目标。</para>
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="target">目标。</param>
        private static bool TryGetTarget(string key, out ConfigTableSearchTarget target)
        {
            if (string.Equals(key, "field", StringComparison.OrdinalIgnoreCase))
            {
                target = ConfigTableSearchTarget.Field;
                return true;
            }
            if (string.Equals(key, "comment", StringComparison.OrdinalIgnoreCase))
            {
                target = ConfigTableSearchTarget.Comment;
                return true;
            }
            if (string.Equals(key, "content", StringComparison.OrdinalIgnoreCase))
            {
                target = ConfigTableSearchTarget.Content;
                return true;
            }
            target = default;
            return false;
        }
    }
}

#endif