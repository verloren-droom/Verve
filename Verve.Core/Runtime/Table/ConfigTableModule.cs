namespace Verve
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;
    
    /// <summary>
    ///   <para>配置表模块；加载并查询运行时表数据。</para>
    /// </summary>
    [Serializable, GameModule("运行时配置表模块")]
    public sealed class ConfigTableModule : GameModule, IConfigTables
    {
        /// <summary>
        ///   <para>StreamingAssets 下的配置表目录名。</para>
        /// </summary>
        public const string TablesFolder = "Tables";

        /// <summary>
        ///   <para>配置表文件扩展名。</para>
        /// </summary>
        public const string FileExtension = "ctable";

        /// <summary>
        ///   <para>运行时配置表清单文件名。</para>
        /// </summary>
        public const string ManifestFileName = "ConfigTableManifest.json";

        /// <summary>
        ///   <para>表。</para>
        /// </summary>
        private Dictionary<string, ConfigTable> m_Tables;
        /// <summary>
        ///   <para>生命周期取消源。</para>
        /// </summary>
        private CancellationTokenSource m_Lifetime;
        /// <summary>
        ///   <para>重新加载任务。</para>
        /// </summary>
        private Task m_ReloadTask = Task.CompletedTask;

        /// <inheritdoc />
        public ConfigTable Get(string tableName)
        {
            Game.ThrowIfNotOnMainThread(nameof(Get));
            if (!IsInstalled || m_Tables == null)
                throw new InvalidOperationException("Config tables are not loaded. Await module installation or ReloadAsync first.");
            return m_Tables[tableName];
        }

        /// <inheritdoc />
        public Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            Game.ThrowIfNotOnMainThread(nameof(ReloadAsync));
            if (!IsInstalled) throw new InvalidOperationException("ConfigTableModule is not installed.");
            if (!m_ReloadTask.IsCompleted) throw new InvalidOperationException("A config table reload is already in progress.");
            return m_ReloadTask = ReloadImplAsync(cancellationToken);
        }

        /// <inheritdoc />
        protected override async ValueTask OnInstall(GameModuleContext context, CancellationToken ct)
        {
            m_Lifetime = new CancellationTokenSource();
            await ReloadImplAsync(ct);
        }

        /// <inheritdoc />
        protected override async ValueTask OnUninstall(GameModuleContext context, CancellationToken ct)
        {
            // 取消回调可能同步完成重新加载，先记录需要等待的任务。
            var pending = m_ReloadTask.IsCompleted ? Task.CompletedTask : m_ReloadTask;
            Exception failure = null;
            try { m_Lifetime.Cancel(); }
            catch (Exception exception) { failure = exception; }
            try
            {
                await pending;
            }
            catch (OperationCanceledException) when (pending.IsCanceled && m_Lifetime.IsCancellationRequested)
            {
                // 卸载取消自己拥有的加载操作，调用方仍收到取消异常。
            }
            catch (Exception exception)
            {
                failure = ExceptionUtility.Combine(failure, pending.Exception ?? exception);
            }
            finally
            {
                ReleaseTables();
            }
            ExceptionUtility.Rethrow(failure);
        }

        /// <inheritdoc />
        protected override void OnDispose()
        {
            try { m_Lifetime?.Cancel(); }
            finally { ReleaseTables(); }
        }

        /// <summary>
        ///   <para>释放表。</para>
        /// </summary>
        private void ReleaseTables()
        {
            m_Lifetime?.Dispose();
            m_Lifetime = null;
            m_Tables = null;
            m_ReloadTask = Task.CompletedTask;
        }

        /// <summary>
        ///   <para>异步重新加载。</para>
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        private async Task ReloadImplAsync(CancellationToken cancellationToken)
        {
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_Lifetime.Token);
            var ct = cancellation.Token;
            ct.ThrowIfCancellationRequested();
            m_Tables = null;
            var manifest = DeserializeManifest(await LoadTextAsync(ManifestFileName, ct));
            ValidateManifestTables(manifest.tables);
            var partitions = new Dictionary<string, ConfigTableAsset>(StringComparer.OrdinalIgnoreCase);
            foreach (var tableName in manifest.tables)
            {
                var text = await LoadTextAsync(tableName + "." + FileExtension, ct);
                partitions.Add(tableName, DeserializeTable(tableName, text));
            }
            var tables = BuildSourceTables(manifest.sources, partitions);
            ct.ThrowIfCancellationRequested();
            m_Tables = tables;
        }

        /// <summary>
        ///   <para>反序列化清单。</para>
        /// </summary>
        /// <param name="text">清单 JSON。</param>
        private static ConfigTableManifest DeserializeManifest(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new InvalidOperationException("清单文件不可用。");
            }

            try
            {
                var manifest = JsonUtility.FromJson<ConfigTableManifest>(text);
                if (manifest?.tables == null || manifest.sources == null)
                {
                    throw new InvalidOperationException("清单文件缺少分表元数据。请从 CSV 重新构建所有配置表。");
                }
                return manifest;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("清单文件无效。\n" + exception.Message, exception);
            }
        }

        /// <summary>
        ///   <para>校验清单表。</para>
        /// </summary>
        /// <param name="tableNames">表名称。</param>
        private static void ValidateManifestTables(string[] tableNames)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < tableNames.Length; i++)
            {
                string tableName = tableNames[i];
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    throw new InvalidOperationException("清单包含空表名，索引 " + i + "。");
                }
                if (!ConfigTableSchema.IsValidTableFileName(tableName))
                {
                    throw new InvalidOperationException("清单包含无效表名：" + tableName + "。");
                }
                if (!names.Add(tableName))
                {
                    throw new InvalidOperationException("清单包含重复表名：" + tableName + "。");
                }
            }
        }

        /// <summary>
        ///   <para>反序列化表。</para>
        /// </summary>
        /// <param name="tableName">表名称。</param>
        /// <param name="text">要压缩的字符串。</param>
        private static ConfigTableAsset DeserializeTable(string tableName, string text)
        {
            try
            {
                var asset = JsonUtility.FromJson<ConfigTableAsset>(text);
                if (asset == null || !string.Equals(asset.tableName, tableName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("配置表名称与文件名不一致。");
                }

                ConfigTableSchema.ValidateAsset(asset);
                return asset;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("加载配置表失败：" + tableName + "\n" + exception.Message, exception);
            }
        }

        /// <summary>
        ///   <para>构建源表。</para>
        /// </summary>
        /// <param name="sources">源。</param>
        /// <param name="partitions">参与合并的分表映射。</param>
        private static Dictionary<string, ConfigTable> BuildSourceTables(
            ConfigTableManifestSource[] sources,
            Dictionary<string, ConfigTableAsset> partitions)
        {
            var tables = new Dictionary<string, ConfigTable>(StringComparer.OrdinalIgnoreCase);
            for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
            {
                var source = sources[sourceIndex];
                try
                {
                    var mergedAsset = MergeSourceTable(source, partitions);
                    tables.Add(mergedAsset.tableName, new ConfigTable(mergedAsset));
                }
                catch (Exception exception)
                {
                    string tableName = source?.tableName ?? "未知源表";
                    throw new InvalidOperationException("合并配置表分表失败：" + tableName + "\n" + exception.Message, exception);
                }
            }
            return tables;
        }

        /// <summary>
        ///   <para>合并源表。</para>
        /// </summary>
        /// <param name="source">源。</param>
        /// <param name="partitions">参与合并的分表映射。</param>
        private static ConfigTableAsset MergeSourceTable(
            ConfigTableManifestSource source,
            Dictionary<string, ConfigTableAsset> partitions)
        {
            if (source == null)
            {
                throw new InvalidOperationException("源表元数据无效。");
            }
            if (source.partitions == null || source.partitions.Length == 0)
            {
                throw new InvalidOperationException("源表缺少分表映射。");
            }

            var sourceTable = new ConfigTableAsset
            {
                tableName = source.tableName,
                fields = source.fields,
                types = source.types,
                comments = source.comments,
                references = source.references
            };
            var mergePartitions = new ConfigTableMergePartition[source.partitions.Length];
            for (int partitionIndex = 0; partitionIndex < source.partitions.Length; partitionIndex++)
            {
                var partition = source.partitions[partitionIndex];
                if (partition == null || string.IsNullOrWhiteSpace(partition.tableName) ||
                    !string.Equals(partition.tableName, partition.tableName.Trim(), StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("分表元数据无效。");
                }
                if (!partitions.TryGetValue(partition.tableName, out var partitionAsset))
                {
                    throw new InvalidOperationException("缺少分表：" + partition.tableName + "。");
                }
                mergePartitions[partitionIndex] = new ConfigTableMergePartition(
                    partition.tableName, partitionAsset, partition.columnIndexes);
            }

            return ConfigTableSchema.MergePartitions(sourceTable, mergePartitions);
        }

        /// <summary>
        ///   <para>异步加载文本。</para>
        /// </summary>
        /// <param name="fileName">文件名称。</param>
        /// <param name="ct">取消令牌。</param>
        private static Task<string> LoadTextAsync(string fileName, CancellationToken ct)
        {
            var root = Game.PathUtility.Normalize(Application.streamingAssetsPath).TrimEnd('/');
            var path = root + "/" + TablesFolder + "/" + (root.Contains("://") ? Uri.EscapeDataString(fileName) : fileName);
            return Game.HttpUtility.Get(Game.PathUtility.ToRequestUrl(path), cancellationToken: ct);
        }
    }
}