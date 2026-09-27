namespace Verve
{
    using System;
    
    /// <summary>
    ///   <para>配置表的序列化数据。</para>
    /// </summary>
    [Serializable]
    public class ConfigTableAsset
    {
        /// <summary>
        ///   <para>配置表名称。</para>
        /// </summary>
        public string tableName;
    
        /// <summary>
        ///   <para>字段名称，第一列必须是 id。</para>
        /// </summary>
        public string[] fields;
    
        /// <summary>
        ///   <para>字段类型定义。</para>
        /// </summary>
        public string[] types;
    
        /// <summary>
        ///   <para>字段注释。</para>
        /// </summary>
        public string[] comments;
    
        /// <summary>
        ///   <para>字段引用的目标表定义。</para>
        /// </summary>
        public string[] references;
    
        /// <summary>
        ///   <para>配置表记录。</para>
        /// </summary>
        public ConfigTableRecord[] rows;
        
        /// <summary>
        ///   <para>深拷贝数据。</para>
        /// </summary>
        /// <param name="source">待复制的配置表数据。</param>
        internal static ConfigTableAsset Clone(ConfigTableAsset source)
        {
            if (source == null)
            {
                return null;
            }
    
            var rows = source.rows;
            var clonedRows = rows == null ? null : new ConfigTableRecord[rows.Length];
            if (rows != null)
            {
                for (int i = 0; i < rows.Length; i++)
                {
                    var row = rows[i];
                    clonedRows[i] = row == null
                        ? null
                        : new ConfigTableRecord
                        {
                            id = row.id,
                            values = (string[])row.values?.Clone()
                        };
                }
            }
    
            return new ConfigTableAsset
            {
                tableName = source.tableName,
                fields = (string[])source.fields?.Clone(),
                types = (string[])source.types?.Clone(),
                comments = (string[])source.comments?.Clone(),
                references = (string[])source.references?.Clone(),
                rows = clonedRows
            };
        }
    }
    
    /// <summary>
    ///   <para>配置表中的序列化记录。</para>
    /// </summary>
    [Serializable]
    public class ConfigTableRecord
    {
        /// <summary>
        ///   <para>记录 ID。</para>
        /// </summary>
        public string id;
    
        /// <summary>
        ///   <para>按字段顺序保存的原始字符串值。</para>
        /// </summary>
        public string[] values;
    }
    
    /// <summary>
    ///   <para>客户端运行时使用的配置表清单。</para>
    /// </summary>
    [Serializable]
    public class ConfigTableManifest
    {
        /// <summary>
        ///   <para>运行时加载的分表名称。</para>
        /// </summary>
        public string[] tables;
    
        /// <summary>
        ///   <para>源表与分表之间的映射。</para>
        /// </summary>
        public ConfigTableManifestSource[] sources;
    }
    
    /// <summary>
    ///   <para>源表的字段定义及分表映射。</para>
    /// </summary>
    [Serializable]
    public class ConfigTableManifestSource
    {
        /// <summary>
        ///   <para>源表名称。</para>
        /// </summary>
        public string tableName;
    
        /// <summary>
        ///   <para>源表字段名称。</para>
        /// </summary>
        public string[] fields;
    
        /// <summary>
        ///   <para>源表字段类型。</para>
        /// </summary>
        public string[] types;
    
        /// <summary>
        ///   <para>源表字段注释。</para>
        /// </summary>
        public string[] comments;
    
        /// <summary>
        ///   <para>源表字段引用。</para>
        /// </summary>
        public string[] references;
    
        /// <summary>
        ///   <para>源表包含的分表及列映射。</para>
        /// </summary>
        public ConfigTableManifestPartition[] partitions;
    }
    
    /// <summary>
    ///   <para>源表到单个分表的列索引映射。</para>
    /// </summary>
    [Serializable]
    public class ConfigTableManifestPartition
    {
        /// <summary>
        ///   <para>分表名称。</para>
        /// </summary>
        public string tableName;
    
        /// <summary>
        ///   <para>分表列对应的源表列索引。</para>
        /// </summary>
        public int[] columnIndexes;
    }
}