namespace Verve
{
    using System;
    using UnityEngine;
    
    /// <summary>
    ///   <para>配置表数据资产。</para>
    /// </summary>
    public sealed class ConfigTableDataAsset : ScriptableObject
    {
        /// <summary>
        ///   <para>表名称。</para>
        /// </summary>
        [SerializeField] private string tableName;
        /// <summary>
        ///   <para>字段。</para>
        /// </summary>
        [SerializeField] private string[] fields;
        /// <summary>
        ///   <para>类型。</para>
        /// </summary>
        [SerializeField] private string[] types;
        /// <summary>
        ///   <para>注释。</para>
        /// </summary>
        [SerializeField] private string[] comments;
        /// <summary>
        ///   <para>引用。</para>
        /// </summary>
        [SerializeField] private string[] references;
        /// <summary>
        ///   <para>行。</para>
        /// </summary>
        [SerializeField] private ConfigTableRecord[] rows;
    
        /// <summary>
        ///   <para>创建与资产数据相互独立的配置表数据副本。</para>
        /// </summary>
        public ConfigTableAsset CreateTableData()
        {
            return ConfigTableAsset.Clone(new ConfigTableAsset
            {
                tableName = tableName,
                fields = fields,
                types = types,
                comments = comments,
                references = references,
                rows = rows
            });
        }
    
#if UNITY_EDITOR
        /// <summary>
        ///   <para>使用配置表数据更新资产内容。</para>
        /// </summary>
        /// <param name="source">要写入资产的数据。</param>
        public void SetTableData(ConfigTableAsset source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
    
            var copy = ConfigTableAsset.Clone(source);
            tableName = copy.tableName;
            fields = copy.fields;
            types = copy.types;
            comments = copy.comments;
            references = copy.references;
            rows = copy.rows;
        }
#endif
    }
}