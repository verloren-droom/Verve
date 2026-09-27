namespace Verve
{
    using System;
    using System.IO;
    using System.Globalization;
    using System.Collections.Generic;
    
    /// <summary>
    ///   <para>配置表；索引并只读访问记录。</para>
    /// </summary>
    public sealed class ConfigTable
    {
        /// <summary>
        ///   <para>名称。</para>
        /// </summary>
        private readonly string m_Name;
        /// <summary>
        ///   <para>字段索引。</para>
        /// </summary>
        private readonly Dictionary<string, int> m_FieldIndexes;
        /// <summary>
        ///   <para>按字符串标识索引的行。</para>
        /// </summary>
        private readonly Dictionary<string, ConfigTableRow> m_RowsById;
        /// <summary>
        ///   <para>按整数标识索引的行。</para>
        /// </summary>
        private readonly Dictionary<int, ConfigTableRow> m_RowsByIntegerId;
        /// <summary>
        ///   <para>行。</para>
        /// </summary>
        private readonly List<ConfigTableRow> m_Rows;
        /// <summary>
        ///   <para>行列表的只读视图。</para>
        /// </summary>
        private readonly IReadOnlyList<ConfigTableRow> m_ReadOnlyRows;
        /// <summary>
        ///   <para>生成行缓存。</para>
        /// </summary>
        private Dictionary<ConfigTableRow, object> m_GeneratedRows;
        /// <summary>
        ///   <para>生成行列表。</para>
        /// </summary>
        private object m_GeneratedRowsList;
    
        /// <summary>
        ///   <para>创建配置表并建立记录索引。</para>
        /// </summary>
        /// <param name="asset">配置表数据。</param>
        internal ConfigTable(ConfigTableAsset asset)
        {
            ConfigTableSchema.ValidateAsset(asset);
    
            m_Name = asset.tableName;
            m_FieldIndexes = new Dictionary<string, int>(asset.fields.Length, StringComparer.OrdinalIgnoreCase);
            bool hasIntegerId = ConfigTableSchema.NormalizeType(asset.types[0]) == "int";
            m_RowsById = hasIntegerId
                ? null
                : new Dictionary<string, ConfigTableRow>(asset.rows?.Length ?? 0, StringComparer.OrdinalIgnoreCase);
            m_RowsByIntegerId = hasIntegerId
                ? new Dictionary<int, ConfigTableRow>(asset.rows?.Length ?? 0)
                : null;
            m_Rows = new List<ConfigTableRow>(asset.rows?.Length ?? 0);
    
            for (int i = 0; i < asset.fields.Length; i++)
            {
                m_FieldIndexes.Add(asset.fields[i], i);
            }
    
            var sourceRows = asset.rows ?? Array.Empty<ConfigTableRecord>();
            for (int i = 0; i < sourceRows.Length; i++)
            {
                var row = new ConfigTableRow(this, sourceRows[i]);
                m_Rows.Add(row);
                if (m_RowsByIntegerId != null)
                {
                    m_RowsByIntegerId.Add(int.Parse(row.Id, NumberStyles.Integer, CultureInfo.InvariantCulture), row);
                }
                else
                {
                    m_RowsById.Add(row.Id, row);
                }
            }
    
            m_ReadOnlyRows = m_Rows.AsReadOnly();
        }
    
        /// <summary>
        ///   <para>获取配置表名称。</para>
        /// </summary>
        public string Name => m_Name;
    
        /// <summary>
        ///   <para>获取记录数量。</para>
        /// </summary>
        public int Count => m_Rows.Count;
    
        /// <summary>
        ///   <para>获取只读记录列表。</para>
        /// </summary>
        public IReadOnlyList<ConfigTableRow> Rows => m_ReadOnlyRows;
    
        /// <summary>
        ///   <para>按字符串 ID 获取记录；找不到时返回 null。</para>
        /// </summary>
        /// <param name="id">记录字符串 ID。</param>
        /// <returns>匹配的记录；不存在时返回 null。</returns>
        public ConfigTableRow Get(string id)
        {
            if (TryGet(id, out var row))
            {
                return row;
            }
    
            return null;
        }
    
        /// <summary>
        ///   <para>按整数 ID 获取记录。</para>
        /// </summary>
        /// <param name="id">记录整数 ID。</param>
        /// <returns>匹配的记录；不存在时返回 null。</returns>
        public ConfigTableRow Get(int id)
        {
            if (m_RowsByIntegerId != null)
            {
                return m_RowsByIntegerId.TryGetValue(id, out var row) ? row : null;
            }

            return Get(id.ToString(CultureInfo.InvariantCulture));
        }
    
        /// <summary>
        ///   <para>按字符串 ID 尝试获取记录。</para>
        /// </summary>
        /// <param name="id">记录字符串 ID。</param>
        /// <param name="row">表记录。</param>
        /// <returns>找到记录返回 true，否则返回 false。</returns>
        public bool TryGet(string id, out ConfigTableRow row)
        {
            if (string.IsNullOrEmpty(id))
            {
                row = null;
                return false;
            }
    
            if (m_RowsById != null)
            {
                return m_RowsById.TryGetValue(id, out row);
            }

            if (int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericId) &&
                string.Equals(id, numericId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
            {
                return m_RowsByIntegerId.TryGetValue(numericId, out row);
            }

            row = null;
            return false;
        }
    
        /// <summary>
        ///   <para>按整数 ID 尝试获取记录。</para>
        /// </summary>
        /// <param name="id">记录整数 ID。</param>
        /// <param name="row">表记录。</param>
        /// <returns>找到记录返回 true，否则返回 false。</returns>
        public bool TryGet(int id, out ConfigTableRow row)
        {
            if (m_RowsByIntegerId != null)
            {
                return m_RowsByIntegerId.TryGetValue(id, out row);
            }

            return TryGet(id.ToString(CultureInfo.InvariantCulture), out row);
        }
    
        /// <summary>
        ///   <para>获取字段名称对应的索引。</para>
        /// </summary>
        /// <param name="fieldName">字段名称。</param>
        /// <returns>字段索引；不存在时返回 -1。</returns>
        internal int GetFieldIndex(string fieldName) => m_FieldIndexes[fieldName];

        /// <summary>
        ///   <para>获取或创建与原始记录对应的类型化行对象。</para>
        /// </summary>
        /// <remarks>缓存归属于底层表，重载后旧表和其缓存可一起回收。</remarks>
        /// <param name="service">服务。</param>
        /// <param name="row">表记录。</param>
        /// <param name="factory">行对象创建委托。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        public T GetOrCreateRow<T>(IConfigTables service, ConfigTableRow row,
            Func<IConfigTables, ConfigTableRow, T> factory)
            where T : class
        {
            if (row == null)
            {
                return null;
            }
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (m_GeneratedRows == null)
            {
                m_GeneratedRows = new Dictionary<ConfigTableRow, object>(m_Rows.Count);
            }
            if (m_GeneratedRows.TryGetValue(row, out var value))
            {
                return (T)value;
            }

            var generatedRow = factory(service, row);
            m_GeneratedRows.Add(row, generatedRow);
            return generatedRow;
        }

        /// <summary>
        ///   <para>获取或创建与原始记录对应的类型化行列表。</para>
        /// </summary>
        /// <param name="service">服务。</param>
        /// <param name="factory">行对象创建委托。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        public IReadOnlyList<T> GetOrCreateRows<T>(IConfigTables service,
            Func<IConfigTables, ConfigTableRow, T> factory)
            where T : class
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            if (m_GeneratedRowsList != null)
            {
                if (m_GeneratedRowsList is IReadOnlyList<T> cachedRows)
                {
                    return cachedRows;
                }
                throw new InvalidOperationException("同一个配置表不能绑定多个类型化行类型。");
            }

            var rows = new T[m_Rows.Count];
            for (int i = 0; i < m_Rows.Count; i++)
            {
                rows[i] = GetOrCreateRow(service, m_Rows[i], factory);
            }
            m_GeneratedRowsList = Array.AsReadOnly(rows);
            return (IReadOnlyList<T>)m_GeneratedRowsList;
        }
    }
    
    /// <summary>
    ///   <para>配置表行数据。</para>
    /// </summary>
    public sealed class ConfigTableRow
    {
        /// <summary>
        ///   <para>表。</para>
        /// </summary>
        private readonly ConfigTable m_Table;
        /// <summary>
        ///   <para>记录。</para>
        /// </summary>
        private readonly ConfigTableRecord m_Record;
    
        /// <summary>
        ///   <para>创建配置表行包装对象。</para>
        /// </summary>
        /// <param name="table">所属配置表。</param>
        /// <param name="record">原始记录数据。</param>
        internal ConfigTableRow(ConfigTable table, ConfigTableRecord record)
        {
            m_Table = table;
            m_Record = record;
        }
    
        /// <summary>
        ///   <para>获取记录 ID。</para>
        /// </summary>
        public string Id => m_Record.id;
    
        /// <summary>
        ///   <para>读取原始字符串；字段不存在时直接抛出。</para>
        /// </summary>
        /// <param name="fieldName">字段名称。</param>
        public string GetString(string fieldName) => GetString(m_Table.GetFieldIndex(fieldName));
        /// <summary>
        ///   <para>读取字符串。</para>
        /// </summary>
        /// <param name="fieldIndex">字段索引。</param>
        public string GetString(int fieldIndex) => GetRaw(fieldIndex);
        /// <summary>
        ///   <para>读取整数。</para>
        /// </summary>
        /// <param name="fieldName">字段名称。</param>
        public int GetInt(string fieldName) => GetInt(m_Table.GetFieldIndex(fieldName));
        /// <summary>
        ///   <para>读取整数。</para>
        /// </summary>
        /// <param name="fieldIndex">字段索引。</param>
        public int GetInt(int fieldIndex) => ParseInt(GetRaw(fieldIndex));
        /// <summary>
        ///   <para>读取长整数。</para>
        /// </summary>
        /// <param name="fieldName">字段名称。</param>
        public long GetLong(string fieldName) => GetLong(m_Table.GetFieldIndex(fieldName));
        /// <summary>
        ///   <para>读取长整数。</para>
        /// </summary>
        /// <param name="fieldIndex">字段索引。</param>
        public long GetLong(int fieldIndex) => ParseLong(GetRaw(fieldIndex));
        /// <summary>
        ///   <para>读取浮点数。</para>
        /// </summary>
        /// <param name="fieldName">字段名称。</param>
        public float GetFloat(string fieldName) => GetFloat(m_Table.GetFieldIndex(fieldName));
        /// <summary>
        ///   <para>读取浮点数。</para>
        /// </summary>
        /// <param name="fieldIndex">字段索引。</param>
        public float GetFloat(int fieldIndex) => ParseFloat(GetRaw(fieldIndex));
        /// <summary>
        ///   <para>读取布尔值。</para>
        /// </summary>
        /// <param name="fieldName">字段名称。</param>
        public bool GetBool(string fieldName) => GetBool(m_Table.GetFieldIndex(fieldName));
        /// <summary>
        ///   <para>读取布尔值。</para>
        /// </summary>
        /// <param name="fieldIndex">字段索引。</param>
        public bool GetBool(int fieldIndex) => ParseBool(GetRaw(fieldIndex));

        /// <summary>
        ///   <para>按字段名读取字符串数组。</para>
        /// </summary>
        /// <param name="fieldName">字段名称。</param>
        /// <returns>解析后的字符串数组。</returns>
        public string[] GetStringArray(string fieldName) => GetStringArray(m_Table.GetFieldIndex(fieldName));
    
        /// <summary>
        ///   <para>按字段索引读取字符串数组。</para>
        /// </summary>
        /// <param name="fieldIndex">字段索引。</param>
        /// <returns>解析后的字符串数组。</returns>
        public string[] GetStringArray(int fieldIndex) => ConfigTableSchema.SplitArrayValues(GetRaw(fieldIndex));
    
        /// <summary>
        ///   <para>按字段名读取整数数组。</para>
        /// </summary>
        /// <param name="fieldName">字段名称。</param>
        /// <returns>解析后的整数数组。</returns>
        public int[] GetIntArray(string fieldName) => GetIntArray(m_Table.GetFieldIndex(fieldName));
    
        /// <summary>
        ///   <para>按字段索引读取整数数组。</para>
        /// </summary>
        /// <param name="fieldIndex">字段索引。</param>
        /// <returns>解析后的整数数组。</returns>
        public int[] GetIntArray(int fieldIndex) => ParseArray(GetRaw(fieldIndex), ParseInt);

        /// <summary>
        ///   <para>按字段名读取长整数数组。</para>
        /// </summary>
        /// <param name="fieldName">字段名称。</param>
        /// <returns>解析后的长整数数组。</returns>
        public long[] GetLongArray(string fieldName) => GetLongArray(m_Table.GetFieldIndex(fieldName));

        /// <summary>
        ///   <para>按字段索引读取长整数数组。</para>
        /// </summary>
        /// <param name="fieldIndex">字段索引。</param>
        /// <returns>解析后的长整数数组。</returns>
        public long[] GetLongArray(int fieldIndex) => ParseArray(GetRaw(fieldIndex), ParseLong);
    
        /// <summary>
        ///   <para>按字段名读取浮点数组。</para>
        /// </summary>
        /// <param name="fieldName">字段名称。</param>
        /// <returns>解析后的浮点数组。</returns>
        public float[] GetFloatArray(string fieldName) => GetFloatArray(m_Table.GetFieldIndex(fieldName));
    
        /// <summary>
        ///   <para>按字段索引读取浮点数组。</para>
        /// </summary>
        /// <param name="fieldIndex">字段索引。</param>
        /// <returns>解析后的浮点数组。</returns>
        public float[] GetFloatArray(int fieldIndex) => ParseArray(GetRaw(fieldIndex), ParseFloat);
    
        /// <summary>
        ///   <para>按字段名读取布尔数组。</para>
        /// </summary>
        /// <param name="fieldName">字段名称。</param>
        /// <returns>解析后的布尔数组。</returns>
        public bool[] GetBoolArray(string fieldName) => GetBoolArray(m_Table.GetFieldIndex(fieldName));
    
        /// <summary>
        ///   <para>按字段索引读取布尔数组。</para>
        /// </summary>
        /// <param name="fieldIndex">字段索引。</param>
        /// <returns>解析后的布尔数组。</returns>
        public bool[] GetBoolArray(int fieldIndex) => ParseArray(GetRaw(fieldIndex), ParseBool);
    
        /// <summary>
        ///   <para>按字段名读取二元组数组。</para>
        /// </summary>
        /// <typeparam name="T1">第一个元组元素类型。</typeparam>
        /// <typeparam name="T2">第二个元组元素类型。</typeparam>
        /// <param name="fieldName">字段名称。</param>
        /// <returns>解析后的二元组数组。</returns>
        public ValueTuple<T1, T2>[] GetTupleArray<T1, T2>(string fieldName) => GetTupleArray<T1, T2>(m_Table.GetFieldIndex(fieldName));
    
        /// <summary>
        ///   <para>按字段索引读取二元组数组；元组元素仅支持 string、int、long、float 和 bool。</para>
        /// </summary>
        /// <typeparam name="T1">第一个元组元素类型。</typeparam>
        /// <typeparam name="T2">第二个元组元素类型。</typeparam>
        /// <param name="fieldIndex">字段索引。</param>
        /// <returns>解析后的二元组数组。</returns>
        public ValueTuple<T1, T2>[] GetTupleArray<T1, T2>(int fieldIndex)
        {
            if (!IsSupportedTupleElementType(typeof(T1)) || !IsSupportedTupleElementType(typeof(T2)))
            {
                throw new NotSupportedException("配置表元组值仅支持 string、int、long、float 和 bool 类型：" +
                    typeof(T1).FullName + "、" + typeof(T2).FullName + "。");
            }
    
            var values = SplitTupleArray(GetRaw(fieldIndex));
            var result = new ValueTuple<T1, T2>[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].Length != 2)
                {
                    throw new InvalidDataException(
                        "配置表 " + m_Table.Name + " 的记录 " + Id +
                        " 在字段索引 " + fieldIndex.ToString(CultureInfo.InvariantCulture) +
                        " 中的元组值无效：" + string.Join("#", values[i]) + "。");
                }
    
                result[i] = new ValueTuple<T1, T2>(ParseValue<T1>(values[i][0]), ParseValue<T2>(values[i][1]));
            }
            return result;
        }
    
        /// <summary>
        ///   <para>读取原始字段文本。</para>
        /// </summary>
        /// <param name="fieldIndex">字段索引。</param>
        private string GetRaw(int fieldIndex) => m_Record.values[fieldIndex];
    
        /// <summary>
        ///   <para>拆分元组数组。</para>
        /// </summary>
        /// <param name="value">值。</param>
        private static string[][] SplitTupleArray(string value)
        {
            var tuples = ConfigTableSchema.SplitArrayValues(value);
            var values = new string[tuples.Length][];
            for (int i = 0; i < tuples.Length; i++)
            {
                values[i] = ConfigTableSchema.SplitTupleValues(tuples[i]);
            }
            return values;
        }
    
        /// <summary>
        ///   <para>解析数组。</para>
        /// </summary>
        /// <param name="value">值。</param>
        /// <param name="parser">解析器。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        private static T[] ParseArray<T>(string value, Func<string, T> parser)
        {
            var values = ConfigTableSchema.SplitArrayValues(value);
            var result = new T[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = parser(values[i]);
            }
            return result;
        }
    
        /// <summary>
        ///   <para>解析整数。</para>
        /// </summary>
        /// <param name="value">值。</param>
        private static int ParseInt(string value) => int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        /// <summary>
        ///   <para>解析长整数。</para>
        /// </summary>
        /// <param name="value">值。</param>
        private static long ParseLong(string value) => long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

        /// <summary>
        ///   <para>解析浮点数。</para>
        /// </summary>
        /// <param name="value">值。</param>
        private static float ParseFloat(string value)
        {
            if (ConfigTableSchema.TryParseFloat(value, out var result)) return result;
            throw new FormatException($"Invalid config table float: '{value}'.");
        }

        /// <summary>
        ///   <para>解析布尔值。</para>
        /// </summary>
        /// <param name="value">值。</param>
        private static bool ParseBool(string value)
        {
            if (ConfigTableSchema.TryParseBool(value, out var result)) return result;
            throw new FormatException($"Invalid config table boolean: '{value}'.");
        }

        /// <summary>
        ///   <para>解析值。</para>
        /// </summary>
        /// <param name="value">值。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        private static T ParseValue<T>(string value)
        {
            var type = typeof(T);
            if (type == typeof(string))
            {
                return (T)(object)value;
            }
            if (type == typeof(int)) return (T)(object)ParseInt(value);
            if (type == typeof(long)) return (T)(object)ParseLong(value);
            if (type == typeof(float)) return (T)(object)ParseFloat(value);
            if (type == typeof(bool)) return (T)(object)ParseBool(value);
            throw new NotSupportedException("配置表元组值仅支持 string、int、long、float 和 bool 类型：" +
                type.FullName + "。");
        }
    
        /// <summary>
        ///   <para>判断是否为支持的元组元素类型。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static bool IsSupportedTupleElementType(Type type)
        {
            return type == typeof(string) || type == typeof(int) || type == typeof(long) || type == typeof(float) ||
                type == typeof(bool);
        }
    }
}