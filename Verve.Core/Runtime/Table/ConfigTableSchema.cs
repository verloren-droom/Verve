namespace Verve
{
    using System;
    using System.IO;
    using System.Globalization;
    using System.Collections.Generic;
    
    /// <summary>
    ///   <para>分表合并条目；保存分表及源表列映射。</para>
    /// </summary>
    public readonly struct ConfigTableMergePartition
    {
        /// <summary>
        ///   <para>创建分表列映射。</para>
        /// </summary>
        /// <param name="tableName">分表名称。</param>
        /// <param name="table">分表数据。</param>
        /// <param name="sourceColumnIndexes">对应源表的列索引。</param>
        public ConfigTableMergePartition(string tableName, ConfigTableAsset table, int[] sourceColumnIndexes)
        {
            TableName = tableName;
            Table = table;
            SourceColumnIndexes = sourceColumnIndexes;
        }
    
        /// <summary>
        ///   <para>分表名称。</para>
        /// </summary>
        public string TableName { get; }
    
        /// <summary>
        ///   <para>分表数据。</para>
        /// </summary>
        public ConfigTableAsset Table { get; }
    
        /// <summary>
        ///   <para>分表列对应的源表列索引。</para>
        /// </summary>
        public int[] SourceColumnIndexes { get; }
    }
    
    /// <summary>
    ///   <para>配置表结构统一校验与转换工具。</para>
    /// </summary>
    public static class ConfigTableSchema
    {
        /// <summary>
        ///   <para>标量类型。</para>
        /// </summary>
        private static readonly string[] ScalarTypes =
        {
            "string",
            "int",
            "long",
            "float",
            "bool"
        };
    
        /// <summary>
        ///   <para>数组元素分隔符。</para>
        /// </summary>
        public const char ArraySeparator = '|';
        /// <summary>
        ///   <para>元组元素分隔符。</para>
        /// </summary>
        public const char TupleSeparator = '#';
    
        /// <summary>
        ///   <para>数组分隔符。</para>
        /// </summary>
        private static readonly char[] ArraySeparators = { ArraySeparator };
        /// <summary>
        ///   <para>元组分隔符。</para>
        /// </summary>
        private static readonly char[] TupleSeparators = { TupleSeparator };
        /// <summary>
        ///   <para>无效表文件名称字符。</para>
        /// </summary>
        private static readonly char[] InvalidTableFileNameCharacters = { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
        /// <summary>
        ///   <para>支持的类型。</para>
        /// </summary>
        private static readonly string[] SupportedTypes = BuildSupportedTypes();
    
        /// <summary>
        ///   <para>获取当前支持的字段类型副本。</para>
        /// </summary>
        public static string[] GetSupportedTypes() => (string[])SupportedTypes.Clone();
    
        /// <summary>
        ///   <para>判断名称是否是有效配置表文件名。</para>
        /// </summary>
        /// <param name="tableName">待校验的表名。</param>
        public static bool IsValidTableFileName(string tableName)
        {
            if (string.IsNullOrEmpty(tableName) ||
                !string.Equals(tableName, tableName.Trim(), StringComparison.Ordinal) ||
                string.Equals(tableName, ".", StringComparison.Ordinal) ||
                string.Equals(tableName, "..", StringComparison.Ordinal) ||
                tableName.IndexOfAny(InvalidTableFileNameCharacters) >= 0)
            {
                return false;
            }
    
            for (int i = 0; i < tableName.Length; i++)
            {
                if (char.IsControl(tableName[i]))
                {
                    return false;
                }
            }
    
            return true;
        }
    
        /// <summary>
        ///   <para>校验完整配置表资产及其所有记录。</para>
        /// </summary>
        /// <param name="asset">待校验的配置表数据。</param>
        public static void ValidateAsset(ConfigTableAsset asset)
        {
            if (asset == null)
            {
                throw new InvalidDataException("配置表资产为空。");
            }
    
            ValidateSchema(asset.tableName, asset.fields, asset.types, asset.comments, asset.references);
    
            var rows = asset.rows ?? Array.Empty<ConfigTableRecord>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rows.Length; i++)
            {
                ValidateRecord(asset.tableName, asset.fields, asset.types, rows[i], ids,
                    "第 " + (i + 1).ToString(CultureInfo.InvariantCulture) + " 行");
            }
        }
    
        /// <summary>
        ///   <para>按清单映射合并分表，并校验合并结果。</para>
        /// </summary>
        /// <param name="source">源表结构数据。</param>
        /// <param name="partitions">参与合并的分表映射。</param>
        public static ConfigTableAsset MergePartitions(ConfigTableAsset source, ConfigTableMergePartition[] partitions)
        {
            if (source == null)
            {
                throw new InvalidDataException("源配置表元数据为空。");
            }
    
            ValidateSchema(source.tableName, source.fields, source.types, source.comments, source.references);
            if (partitions == null || partitions.Length == 0)
            {
                throw new InvalidDataException(source.tableName + "：缺少分表映射。");
            }
    
            ConfigTableRecord[] records = null;
            bool[][] assignedColumnsByRow = null;
    
            for (int partitionIndex = 0; partitionIndex < partitions.Length; partitionIndex++)
            {
                var partition = partitions[partitionIndex];
                ValidatePartitionSchema(source, partition.TableName, partition.SourceColumnIndexes, partition.Table);
    
                var rows = partition.Table.rows ?? Array.Empty<ConfigTableRecord>();
                if (records == null)
                {
                    records = new ConfigTableRecord[rows.Length];
                    assignedColumnsByRow = new bool[rows.Length][];
                    for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
                    {
                        records[rowIndex] = new ConfigTableRecord
                        {
                            id = rows[rowIndex].id,
                            values = new string[source.fields.Length]
                        };
                        assignedColumnsByRow[rowIndex] = new bool[source.fields.Length];
                    }
                }
                else if (rows.Length != records.Length)
                {
                    throw new InvalidDataException(source.tableName + "：分表 " + partition.TableName +
                        " 的 ID 行数与其他分表不一致。");
                }
    
                for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
                {
                    var row = rows[rowIndex];
                    var record = records[rowIndex];
                    if (!string.Equals(row.id, record.id, StringComparison.Ordinal))
                    {
                        throw new InvalidDataException(source.tableName + "：分表 " + partition.TableName +
                            " 的第 " + (rowIndex + 1).ToString(CultureInfo.InvariantCulture) +
                            " 行 ID 与其他分表不一致，期望值为 " +
                            record.id + "，实际值为 " + row.id + "。");
                    }
    
                    var values = record.values;
                    var assignedColumns = assignedColumnsByRow[rowIndex];
                    for (int columnIndex = 0; columnIndex < partition.SourceColumnIndexes.Length; columnIndex++)
                    {
                        int sourceColumnIndex = partition.SourceColumnIndexes[columnIndex];
                        string value = row.values[columnIndex];
                        if (assignedColumns[sourceColumnIndex] &&
                            !string.Equals(values[sourceColumnIndex], value, StringComparison.Ordinal))
                        {
                            throw new InvalidDataException(source.tableName + "：分表 " + partition.TableName +
                                " 的记录 " + row.id + " 在字段 " + source.fields[sourceColumnIndex] +
                                " 中存在不一致的值。");
                        }
    
                        values[sourceColumnIndex] = value;
                        assignedColumns[sourceColumnIndex] = true;
                    }
                }
            }
    
            for (int rowIndex = 0; rowIndex < records.Length; rowIndex++)
            {
                var assignedColumns = assignedColumnsByRow[rowIndex];
                for (int columnIndex = 0; columnIndex < assignedColumns.Length; columnIndex++)
                {
                    if (!assignedColumns[columnIndex])
                    {
                        throw new InvalidDataException(source.tableName + "：无法从分表中合并字段 " +
                        source.fields[columnIndex] + "。");
                    }
                }
            }
    
            var mergedTable = new ConfigTableAsset
            {
                tableName = source.tableName,
                fields = source.fields,
                types = source.types,
                comments = source.comments,
                references = source.references,
                rows = records
            };
            ValidateAsset(mergedTable);
            return mergedTable;
        }
    
        /// <summary>
        ///   <para>校验分表结构与源表映射。</para>
        /// </summary>
        /// <param name="source">源表结构数据。</param>
        /// <param name="partitionTableName">分表名称。</param>
        /// <param name="sourceColumnIndexes">分表列对应的源表列索引。</param>
        /// <param name="partition">分表数据。</param>
        public static void ValidatePartitionSchema(ConfigTableAsset source, string partitionTableName,
            int[] sourceColumnIndexes, ConfigTableAsset partition)
        {
            if (source == null)
            {
                throw new InvalidDataException("源配置表元数据为空。");
            }
    
            ValidateSchema(source.tableName, source.fields, source.types, source.comments, source.references);
            if (string.IsNullOrEmpty(partitionTableName) ||
                !string.Equals(partitionTableName, partitionTableName.Trim(), StringComparison.Ordinal) ||
                sourceColumnIndexes == null || sourceColumnIndexes.Length < 2 || sourceColumnIndexes[0] != 0)
            {
                throw new InvalidDataException(source.tableName + "：分表元数据无效。");
            }
            if (partition == null)
            {
                throw new InvalidDataException(source.tableName + "：分表 " + partitionTableName + " 资产为空。");
            }
            if (!string.Equals(partition.tableName, partitionTableName, StringComparison.Ordinal))
            {
                throw new InvalidDataException(source.tableName + "：分表名称与资产名称不一致：" +
                    partitionTableName + " != " + (partition.tableName ?? string.Empty) + "。");
            }
    
            ValidateAsset(partition);
    
            var indexes = new HashSet<int>();
            for (int columnIndex = 0; columnIndex < sourceColumnIndexes.Length; columnIndex++)
            {
                int sourceColumnIndex = sourceColumnIndexes[columnIndex];
                if (sourceColumnIndex < 0 || sourceColumnIndex >= source.fields.Length || !indexes.Add(sourceColumnIndex))
                {
                    throw new InvalidDataException(source.tableName + "：分表 " + partitionTableName + " 的字段映射无效。");
                }
            }
    
            if (partition.fields.Length != sourceColumnIndexes.Length)
            {
                throw new InvalidDataException(source.tableName + "：分表 " + partitionTableName +
                    " 的字段数量与映射不一致。");
            }
    
            for (int columnIndex = 0; columnIndex < sourceColumnIndexes.Length; columnIndex++)
            {
                int sourceColumnIndex = sourceColumnIndexes[columnIndex];
                if (!string.Equals(partition.fields[columnIndex], source.fields[sourceColumnIndex], StringComparison.Ordinal) ||
                    !string.Equals(partition.types[columnIndex], source.types[sourceColumnIndex], StringComparison.Ordinal) ||
                    !string.Equals(partition.comments[columnIndex], source.comments[sourceColumnIndex], StringComparison.Ordinal) ||
                    !string.Equals(partition.references[columnIndex], source.references[sourceColumnIndex], StringComparison.Ordinal))
                {
                    throw new InvalidDataException(source.tableName + "：分表 " + partitionTableName +
                        " 的字段定义与源表不一致，字段索引 " +
                        sourceColumnIndex.ToString(CultureInfo.InvariantCulture) + "。");
                }
            }
        }
    
        /// <summary>
        ///   <para>校验表名、字段定义、类型和引用定义。</para>
        /// </summary>
        /// <param name="tableName">表名。</param>
        /// <param name="fields">字段名称。</param>
        /// <param name="types">字段类型。</param>
        /// <param name="comments">字段注释。</param>
        /// <param name="references">字段引用。</param>
        public static void ValidateSchema(string tableName, string[] fields, string[] types, string[] comments, string[] references)
        {
            if (string.IsNullOrEmpty(tableName) || !string.Equals(tableName, tableName.Trim(), StringComparison.Ordinal))
            {
                throw new InvalidDataException("配置表名称为空或包含首尾空白字符。");
            }
            if (fields == null || fields.Length == 0)
            {
                throw new InvalidDataException(tableName + "：字段为空。");
            }
            if (fields.Length < 2)
            {
                throw new InvalidDataException(tableName + "：配置表至少需要 ID 列和一个数据列。");
            }
            if (types == null || types.Length != fields.Length)
            {
                throw new InvalidDataException(tableName + "：类型数量与字段数量不一致。");
            }
            if (comments == null || comments.Length != fields.Length)
            {
                throw new InvalidDataException(tableName + "：注释数量与字段数量不一致。");
            }
            if (references == null || references.Length != fields.Length)
            {
                throw new InvalidDataException(tableName + "：引用数量与字段数量不一致。");
            }
            if (!string.Equals(fields[0], "id", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(tableName + "：第一个字段必须为 id。");
            }
            var idType = NormalizeType(types[0]);
            if (idType != "int" && idType != "string")
            {
                throw new InvalidDataException(tableName + "：ID 字段类型仅支持 int 或 string。");
            }
    
            var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < fields.Length; i++)
            {
                var fieldName = fields[i];
                if (string.IsNullOrEmpty(fieldName) || !string.Equals(fieldName, fieldName.Trim(), StringComparison.Ordinal))
                {
                    throw new InvalidDataException(tableName + "：第 " +
                        (i + 1).ToString(CultureInfo.InvariantCulture) + " 列的字段名称为空或包含首尾空白字符。");
                }
                if (!fieldNames.Add(fieldName))
                {
                    throw new InvalidDataException(tableName + "：字段名称重复：" + fieldName + "。");
                }
                if (comments[i] == null)
                {
                    throw new InvalidDataException(tableName + "：第 " +
                        (i + 1).ToString(CultureInfo.InvariantCulture) + " 列的注释为 null。");
                }
    
                var type = types[i];
                if (string.IsNullOrEmpty(type) || !string.Equals(type, NormalizeType(type), StringComparison.Ordinal) || !IsSupportedType(type))
                {
                    throw new InvalidDataException(tableName + "：字段 " + fieldName + " 的类型不受支持：" + (type ?? string.Empty) +
                        "。");
                }
                ValidateReference(tableName, fieldName, type, references[i]);
            }
        }
    
        /// <summary>
        ///   <para>从引用表达式中提取目标表名称。</para>
        /// </summary>
        /// <param name="reference">字段引用表达式。</param>
        public static string GetReferenceTargetName(string reference)
        {
            if (string.IsNullOrEmpty(reference))
            {
                return string.Empty;
            }
    
            var indexStart = reference.IndexOf('[');
            return indexStart < 0 ? reference : reference.Substring(0, indexStart);
        }
    
        /// <summary>
        ///   <para>校验引用。</para>
        /// </summary>
        /// <param name="tableName">表名称。</param>
        /// <param name="fieldName">字段名称。</param>
        /// <param name="type">类型。</param>
        /// <param name="reference">引用。</param>
        private static void ValidateReference(string tableName, string fieldName, string type, string reference)
        {
            if (string.IsNullOrEmpty(reference))
            {
                return;
            }
            if (!string.Equals(reference, reference.Trim(), StringComparison.Ordinal))
            {
                throw new InvalidDataException(tableName + "：字段 " + fieldName + " 的引用包含首尾空白字符。");
            }
    
            var targetName = GetReferenceTargetName(reference);
            if (string.IsNullOrEmpty(targetName) || !IsValidReferenceTargetName(targetName))
            {
                throw new InvalidDataException(tableName + "：字段 " + fieldName + " 的引用无效：" + reference + "。");
            }
    
            var isTuple = TryGetTupleArrayElementTypes(type, out var tupleElementTypes);
            var hasTupleIndex = TryGetReferenceTupleIndex(reference, out var tupleIndex);
            if (isTuple && (!hasTupleIndex || tupleIndex < 0 || tupleIndex >= tupleElementTypes.Length))
            {
                throw new InvalidDataException(tableName + "：字段 " + fieldName + " 的元组引用 " + reference + " 必须指定有效的元素索引。");
            }
            if (isTuple && tupleElementTypes[tupleIndex] != "int" && tupleElementTypes[tupleIndex] != "string")
            {
                throw new InvalidDataException(tableName + "：字段 " + fieldName + " 的元组引用 " +
                    reference + " 必须使用 int 或 string 类型的 ID。");
            }
            if (!isTuple && (hasTupleIndex || reference.IndexOf('[') >= 0))
            {
                throw new InvalidDataException(tableName + "：字段 " + fieldName + " 的引用无效：" + reference + "。");
            }
        }
    
        /// <summary>
        ///   <para>判断是否为有效引用目标名称。</para>
        /// </summary>
        /// <param name="targetName">目标名称。</param>
        private static bool IsValidReferenceTargetName(string targetName)
        {
            for (int i = 0; i < targetName.Length; i++)
            {
                if (char.IsWhiteSpace(targetName[i]) || targetName[i] == '[' || targetName[i] == ']')
                {
                    return false;
                }
            }
            return true;
        }
    
        /// <summary>
        ///   <para>尝试读取元组引用中的元素索引。</para>
        /// </summary>
        /// <param name="reference">字段引用表达式。</param>
        /// <param name="tupleIndex">输出元组元素索引。</param>
        public static bool TryGetReferenceTupleIndex(string reference, out int tupleIndex)
        {
            tupleIndex = -1;
            if (string.IsNullOrEmpty(reference))
            {
                return false;
            }
    
            var indexStart = reference.IndexOf('[');
            if (indexStart < 0 || !reference.EndsWith("]", StringComparison.Ordinal))
            {
                return false;
            }
            return int.TryParse(reference.Substring(indexStart + 1, reference.Length - indexStart - 2), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out tupleIndex);
        }
        
        /// <summary>
        ///   <para>校验单条记录的 ID、列数和字段值。</para>
        /// </summary>
        /// <param name="tableName">表名。</param>
        /// <param name="fields">字段名称。</param>
        /// <param name="types">字段类型。</param>
        /// <param name="record">待校验的记录。</param>
        /// <param name="ids">已出现的记录 ID。</param>
        /// <param name="location">记录位置描述。</param>
        public static void ValidateRecord(string tableName, string[] fields, string[] types, ConfigTableRecord record,
            HashSet<string> ids, string location)
        {
            if (record == null)
            {
                throw new InvalidDataException(tableName + " " + location + "：记录为空。");
            }
            if (record.values == null || record.values.Length != fields.Length)
            {
                throw new InvalidDataException(tableName + " " + location + "：值数量与字段数量不一致。");
            }
            if (string.IsNullOrEmpty(record.id) || !string.Equals(record.id, record.id.Trim(), StringComparison.Ordinal))
            {
                throw new InvalidDataException(tableName + " " + location + "：ID 为空或包含首尾空白字符。");
            }
            if (!string.Equals(record.id, record.values[0], StringComparison.Ordinal))
            {
                throw new InvalidDataException(tableName + " " + location + "：记录 ID 与 values[0] 不一致。");
            }
            if (types[0] == "int" &&
                (!int.TryParse(record.id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericId) ||
                 !string.Equals(record.id, numericId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)))
            {
                throw new InvalidDataException(tableName + " " + location +
                    "：int 类型 ID 必须使用规范的十进制格式：" + record.id + "。");
            }
            if (!ids.Add(record.id))
            {
                throw new InvalidDataException(tableName + " " + location + "：ID 重复：" + record.id + "。");
            }
    
            for (int i = 0; i < record.values.Length; i++)
            {
                var value = record.values[i];
                if (value == null)
                {
                    throw new InvalidDataException(tableName + " " + location + "：第 " +
                        (i + 1).ToString(CultureInfo.InvariantCulture) + " 列的值为 null。");
                }
                if (!IsValidValue(types[i], value))
                {
                    throw new InvalidDataException(tableName + " " + location + "：字段 " + fields[i] + " 的 " + types[i] +
                        " 类型值无效。");
                }
            }
        }
    
        /// <summary>
        ///   <para>规范化字段类型名称及元组格式。</para>
        /// </summary>
        /// <param name="type">原始类型文本。</param>
        public static string NormalizeType(string type)
        {
            type = NormalizeScalarType(type);
            if (!TryGetTupleArrayElementTypes(type, out var tupleElementTypes))
            {
                return type;
            }
    
            return "(" + tupleElementTypes[0] + TupleSeparator + tupleElementTypes[1] + ")[]";
        }
    
        /// <summary>
        ///   <para>将配置表类型转换为 C# 类型表达式。</para>
        /// </summary>
        /// <param name="type">配置表类型文本。</param>
        public static string GetCSharpType(string type)
        {
            type = NormalizeType(type);
    
            if (TryGetTupleArrayElementTypes(type, out var tupleElementTypes))
            {
                ValidateTupleElementTypes(type, tupleElementTypes);
                return "(" + tupleElementTypes[0] + ", " + tupleElementTypes[1] + ")[]";
            }
    
            ValidateSupportedType(type);
            return type;
        }
    
        /// <summary>
        ///   <para>获取读取指定字段类型所需的行读取方法名称。</para>
        /// </summary>
        /// <param name="type">配置表类型文本。</param>
        public static string GetGetterName(string type)
        {
            type = NormalizeType(type);
    
            if (TryGetTupleArrayElementTypes(type, out var tupleElementTypes))
            {
                ValidateTupleElementTypes(type, tupleElementTypes);
                return "GetTupleArray<" + tupleElementTypes[0] + ", " + tupleElementTypes[1] + ">";
            }
    
            var isArray = type.EndsWith("[]", StringComparison.Ordinal);
            var scalarType = isArray ? type.Substring(0, type.Length - 2) : type;
            if (!IsSupportedScalarType(scalarType))
            {
                throw new InvalidDataException("不受支持的配置表字段类型：" + type);
            }
            return "Get" + char.ToUpperInvariant(scalarType[0]) + scalarType.Substring(1) + (isArray ? "Array" : string.Empty);
        }
    
        /// <summary>
        ///   <para>判断是否为支持的类型。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static bool IsSupportedType(string type)
        {
            if (TryGetTupleArrayElementTypes(type, out var tupleElementTypes))
            {
                return IsSupportedScalarType(tupleElementTypes[0]) && IsSupportedScalarType(tupleElementTypes[1]);
            }
            var scalarType = type.EndsWith("[]", StringComparison.Ordinal) ? type.Substring(0, type.Length - 2) : type;
            return IsSupportedScalarType(scalarType);
        }
    
        /// <summary>
        ///   <para>校验支持的类型。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static void ValidateSupportedType(string type)
        {
            if (!IsSupportedType(type))
            {
                throw new InvalidDataException("不受支持的配置表字段类型：" + type);
            }
        }
    
        /// <summary>
        ///   <para>校验元组元素类型。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        /// <param name="elementTypes">输出两个元组元素类型。</param>
        private static void ValidateTupleElementTypes(string type, string[] elementTypes)
        {
            if (!IsSupportedScalarType(elementTypes[0]) || !IsSupportedScalarType(elementTypes[1]))
            {
                throw new InvalidDataException("不受支持的元组元素类型：" + type);
            }
        }
    
        /// <summary>
        ///   <para>尝试解析元组数组元素类型。</para>
        /// </summary>
        /// <param name="type">配置表类型文本。</param>
        /// <param name="elementTypes">输出两个元组元素类型。</param>
        public static bool TryGetTupleArrayElementTypes(string type, out string[] elementTypes)
        {
            elementTypes = null;
            if (string.IsNullOrEmpty(type) || !type.StartsWith("(", StringComparison.Ordinal) ||
                !type.EndsWith(")[]", StringComparison.Ordinal))
            {
                return false;
            }
    
            var inner = type.Substring(1, type.Length - 4);
            var parts = inner.Split(TupleSeparators, StringSplitOptions.None);
            if (parts.Length != 2)
            {
                return false;
            }
    
            elementTypes = new[] { NormalizeScalarType(parts[0]), NormalizeScalarType(parts[1]) };
            return true;
        }
    
        /// <summary>
        ///   <para>判断是否为支持的标量类型。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static bool IsSupportedScalarType(string type) => Array.IndexOf(ScalarTypes, type) >= 0;
    
        /// <summary>
        ///   <para>判断字符串值是否符合指定配置表类型。</para>
        /// </summary>
        /// <param name="type">配置表类型。</param>
        /// <param name="value">待校验的字符串值。</param>
        public static bool IsValidValue(string type, string value)
        {
            if (string.IsNullOrEmpty(type))
            {
                return false;
            }
            if (string.IsNullOrEmpty(value))
            {
                return type == "string" || (IsSupportedType(type) && type.EndsWith("[]", StringComparison.Ordinal));
            }
    
            if (TryGetTupleArrayElementTypes(type, out var tupleElementTypes))
            {
                var tuples = value.Split(ArraySeparators, StringSplitOptions.None);
                for (int i = 0; i < tuples.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(tuples[i]))
                    {
                        return false;
                    }
    
                    var values = SplitTupleValues(tuples[i]);
                    if (values.Length != tupleElementTypes.Length)
                    {
                        return false;
                    }
                    for (int j = 0; j < values.Length; j++)
                    {
                        if (string.IsNullOrEmpty(values[j]) || !IsValidValue(tupleElementTypes[j], values[j]))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
    
            if (type.EndsWith("[]", StringComparison.Ordinal))
            {
                var elementType = type.Substring(0, type.Length - 2);
                var values = value.Split(ArraySeparators, StringSplitOptions.None);
                for (int i = 0; i < values.Length; i++)
                {
                    var element = values[i].Trim();
                    if (string.IsNullOrEmpty(element) || !IsValidValue(elementType, element))
                    {
                        return false;
                    }
                }
                return true;
            }
    
            switch (type)
            {
                case "string":
                    return true;
                case "int":
                    return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
                case "long":
                    return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
                case "float":
                    return TryParseFloat(value, out _);
                case "bool":
                    return TryParseBool(value, out _);
                default:
                    return false;
            }
        }
    
        /// <summary>
        ///   <para>按数组分隔符拆分值。</para>
        /// </summary>
        /// <param name="value">数组文本。</param>
        internal static string[] SplitArrayValues(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Array.Empty<string>();
            }
    
            var values = value.Split(ArraySeparators, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = values[i].Trim();
            }
            return values;
        }
    
        /// <summary>
        ///   <para>按元组分隔符拆分值。</para>
        /// </summary>
        /// <param name="value">元组文本。</param>
        internal static string[] SplitTupleValues(string value)
        {
            value = TrimTupleText(value);
            var values = value.Split(TupleSeparators, StringSplitOptions.None);
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = TrimTupleText(values[i]);
            }
            return values;
        }
    
        /// <summary>
        ///   <para>裁剪元组文本。</para>
        /// </summary>
        /// <param name="value">值。</param>
        private static string TrimTupleText(string value)
        {
            value = (value ?? string.Empty).Trim();
            if (value.Length >= 2 && value[0] == '(' && value[value.Length - 1] == ')')
            {
                return value.Substring(1, value.Length - 2).Trim();
            }
            return value;
        }
    
        /// <summary>
        ///   <para>解析支持 0/1 和 false/true 布尔值。</para>
        /// </summary>
        /// <param name="value">待解析的文本。</param>
        /// <param name="result">输出解析结果。</param>
        public static bool TryParseBool(string value, out bool result)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "1":
                case "true":
                    result = true;
                    return true;
                case "0":
                case "false":
                    result = false;
                    return true;
                default:
                    result = false;
                    return false;
            }
        }
    
        /// <summary>
        ///   <para>按固定区域性解析有限浮点数。</para>
        /// </summary>
        /// <param name="value">待解析的文本。</param>
        /// <param name="result">输出解析结果。</param>
        internal static bool TryParseFloat(string value, out float result)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) &&
                Game.NumberUtility.IsFinite(result);
        }
        
        /// <summary>
        ///   <para>构建支持的类型。</para>
        /// </summary>
        private static string[] BuildSupportedTypes()
        {
            var types = new List<string>(ScalarTypes.Length * (ScalarTypes.Length + 2));
            for (int i = 0; i < ScalarTypes.Length; i++)
            {
                types.Add(ScalarTypes[i]);
            }
            for (int i = 0; i < ScalarTypes.Length; i++)
            {
                types.Add(ScalarTypes[i] + "[]");
            }
            for (int firstIndex = 0; firstIndex < ScalarTypes.Length; firstIndex++)
            {
                for (int secondIndex = 0; secondIndex < ScalarTypes.Length; secondIndex++)
                {
                    types.Add("(" + ScalarTypes[firstIndex] + TupleSeparator + ScalarTypes[secondIndex] + ")[]");
                }
            }
            return types.ToArray();
        }
        
        /// <summary>
        ///   <para>规范化标量类型名称。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static string NormalizeScalarType(string type) => (type ?? string.Empty).Trim().ToLowerInvariant();
    }
}
