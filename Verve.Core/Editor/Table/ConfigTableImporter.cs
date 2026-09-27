#if UNITY_EDITOR

namespace Verve.Editor
{
    using Verve;
    using System;
    using System.IO;
    using System.Text;
    using UnityEditor;
    using UnityEngine;
    using System.Globalization;
    using System.Collections.Generic;
    
    /// <summary>
    ///   <para>配置表导入导出工具。</para>
    /// </summary>
    internal static class ConfigTableImporter
    {
        /// <summary>
        ///   <para>表资源目录。</para>
        /// </summary>
        private const string TableAssetFolder = ConfigTableEditorSettings.ClientTableFolder;
    
        /// <summary>
        ///   <para>已生成代码后缀。</para>
        /// </summary>
        private const string GeneratedCodeSuffix = ".generated.cs";
        /// <summary>
        ///   <para>已生成运行时命名空间。</para>
        /// </summary>
        private const string GeneratedRuntimeNamespace = "Verve";
        /// <summary>
        ///   <para>已生成目标对象参数名称。</para>
        /// </summary>
        private const string GeneratedSelfParameterName = "self";
        /// <summary>
        ///   <para>已生成表类型后缀。</para>
        /// </summary>
        private const string GeneratedTableTypeSuffix = "Table";
        /// <summary>
        ///   <para>已生成行类型后缀。</para>
        /// </summary>
        private const string GeneratedRowTypeSuffix = "Row";
        /// <summary>
        ///   <para>已生成获取方法前缀。</para>
        /// </summary>
        private const string GeneratedGetMethodPrefix = "Get";
        /// <summary>
        ///   <para>生成的 TryGet 方法前缀。</para>
        /// </summary>
        private const string GeneratedTryGetMethodPrefix = "TryGet";
        /// <summary>
        ///   <para>已生成行方法后缀。</para>
        /// </summary>
        private const string GeneratedRowsMethodSuffix = "Rows";
        /// <summary>
        ///   <para>已生成原始方法后缀。</para>
        /// </summary>
        private const string GeneratedRawMethodSuffix = "Raw";
        /// <summary>
        ///   <para>已生成数量方法后缀。</para>
        /// </summary>
        private const string GeneratedCountMethodSuffix = "Count";
        /// <summary>
        ///   <para>已生成表名称常量名称。</para>
        /// </summary>
        private const string GeneratedTableNameConstantName = "Name";
        /// <summary>
        ///   <para>已生成行表名称常量名称。</para>
        /// </summary>
        private const string GeneratedRowTableNameConstantName = "TableName";
        /// <summary>
        ///   <para>已生成原始行属性名称。</para>
        /// </summary>
        private const string GeneratedRawRowPropertyName = "RawRow";
        /// <summary>
        ///   <para>已生成原始行字段名称。</para>
        /// </summary>
        private const string GeneratedRawRowFieldName = "m_Row";
        
        /// <summary>
        ///   <para>已生成配置表字段名称。</para>
        /// </summary>
        private const string GeneratedConfigTablesFieldName = "m_ConfigTables";
        /// <summary>
        ///   <para>已生成工厂字段名称。</para>
        /// </summary>
        private const string GeneratedFactoryFieldName = "s_Create";
        /// <summary>
        ///   <para>已生成引用加载器前缀。</para>
        /// </summary>
        private const string GeneratedReferenceLoaderPrefix = "LoadReference";
        /// <summary>
        ///   <para>已生成引用已加载前缀。</para>
        /// </summary>
        private const string GeneratedReferenceLoadedPrefix = "ReferenceLoaded";
    
        /// <summary>
        ///   <para>CSV 文件扩展名。</para>
        /// </summary>
        private const string CsvExtension = ".csv";
    
        /// <summary>
        ///   <para>编辑器清单文件名称。</para>
        /// </summary>
        private const string EditorManifestFileName = "ConfigTableEditorManifest.json";
    
        /// <summary>
        ///   <para>客户端标记。</para>
        /// </summary>
        private const string ClientTag = "C";
    
        /// <summary>
        ///   <para>服务端标记。</para>
        /// </summary>
        private const string ServerTag = "S";
    
        /// <summary>
        ///   <para>标记分隔符。</para>
        /// </summary>
        private const char TagSeparator = '|';
    
        /// <summary>
        ///   <para>分表分隔符。</para>
        /// </summary>
        private const char PartitionSeparator = '-';
    
        /// <summary>
        ///   <para>不含 BOM 的 UTF-8 编码。</para>
        /// </summary>
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false, true);
    
        /// <summary>
        ///   <para>含 BOM 的 UTF-8 编码。</para>
        /// </summary>
        private static readonly UTF8Encoding Utf8WithBom = new UTF8Encoding(true, true);


        /// <summary>
        ///   <para>标记分隔符。</para>
        /// </summary>
        private static readonly char[] TagSeparators = { TagSeparator };
    
        /// <summary>
        ///   <para>配置表源数据。</para>
        /// </summary>
        private sealed class ConfigTableSource
        {
            /// <summary>
            ///   <para>表。</para>
            /// </summary>
            public ConfigTableAsset table;
            /// <summary>
            ///   <para>标记。</para>
            /// </summary>
            public string[] tags;
        }
    
        /// <summary>
        ///   <para>配置分表。</para>
        /// </summary>
        private sealed class ConfigTablePartition
        {
            /// <summary>
            ///   <para>源。</para>
            /// </summary>
            public ConfigTableSource source;
            /// <summary>
            ///   <para>表名称。</para>
            /// </summary>
            public string tableName;
            /// <summary>
            ///   <para>目标。</para>
            /// </summary>
            public string destination;
            /// <summary>
            ///   <para>标记。</para>
            /// </summary>
            public string tag;
            /// <summary>
            ///   <para>列索引。</para>
            /// </summary>
            public int[] columnIndexes;
        }
    
        /// <summary>
        ///   <para>编辑器配置表清单。</para>
        /// </summary>
        [Serializable]
        private sealed class ConfigTableEditorManifest
        {
            /// <summary>
            ///   <para>源。</para>
            /// </summary>
            public ConfigTableEditorManifestSource[] sources;
        }
    
        /// <summary>
        ///   <para>编辑器源表信息。</para>
        /// </summary>
        [Serializable]
        private sealed class ConfigTableEditorManifestSource
        {
            /// <summary>
            ///   <para>表名称。</para>
            /// </summary>
            public string tableName;
            /// <summary>
            ///   <para>字段。</para>
            /// </summary>
            public string[] fields;
            /// <summary>
            ///   <para>类型。</para>
            /// </summary>
            public string[] types;
            /// <summary>
            ///   <para>注释。</para>
            /// </summary>
            public string[] comments;
            /// <summary>
            ///   <para>引用。</para>
            /// </summary>
            public string[] references;
            /// <summary>
            ///   <para>标记。</para>
            /// </summary>
            public string[] tags;
            /// <summary>
            ///   <para>分表。</para>
            /// </summary>
            public ConfigTableEditorManifestPartition[] partitions;
        }
    
        /// <summary>
        ///   <para>编辑器分表信息。</para>
        /// </summary>
        [Serializable]
        private sealed class ConfigTableEditorManifestPartition
        {
            /// <summary>
            ///   <para>表名称。</para>
            /// </summary>
            public string tableName;
            /// <summary>
            ///   <para>目标。</para>
            /// </summary>
            public string destination;
            /// <summary>
            ///   <para>标记。</para>
            /// </summary>
            public string tag;
            /// <summary>
            ///   <para>列索引。</para>
            /// </summary>
            public int[] columnIndexes;
        }
    
        /// <summary>
        ///   <para>配置表清单。</para>
        /// </summary>
        [Serializable]
        private sealed class ConfigTableTableManifest
        {
            /// <summary>
            ///   <para>表。</para>
            /// </summary>
            public string[] tables;
        }
    
        /// <summary>
        ///   <para>从 Tables 目录中的全部 CSV 重建配置表输出。</para>
        /// </summary>
        public static void BuildAllTables()
        {
            int builtCount;
            try
            {
                if (!TryGetAvailableCsvFiles(out var files))
                {
                    return;
                }
    
                builtCount = BuildAllTablesInternal(files);
            }
            catch (Exception ex)
            {
                Game.LogError("构建配置表失败。\n" + ex.Message);
                return;
            }
    
            AssetDatabase.Refresh();
            Game.Log("已从 CSV 构建 " + builtCount + " 个配置表。");
        }
    
        /// <summary>
        ///   <para>判断配置表是否存在对应的 CSV 源文件。</para>
        /// </summary>
        /// <param name="ctableAssetPath">配置表资产路径。</param>
        internal static bool HasCsvSource(string ctableAssetPath) => TryGetCsvSourcePath(ctableAssetPath, out var csvPath) && File.Exists(csvPath);
    
        /// <summary>
        ///   <para>尝试根据客户端清单获取配置表对应的 CSV 路径。</para>
        /// </summary>
        /// <param name="ctableAssetPath">配置表资产路径。</param>
        /// <param name="csvPath">输出 CSV 绝对路径。</param>
        internal static bool TryGetCsvSourcePath(string ctableAssetPath, out string csvPath)
        {
            csvPath = null;
            if (!TryGetSourceTableName(ctableAssetPath, out var sourceTableName)) return false;
            csvPath = GetCsvPath(sourceTableName);
            return true;
        }
    
        /// <summary>
        ///   <para>判断指定配置表是否属于受清单管理的分表。</para>
        /// </summary>
        /// <param name="ctableAssetPath">配置表资产路径。</param>
        internal static bool IsSplitPartition(string ctableAssetPath)
        {
            if (string.IsNullOrEmpty(ctableAssetPath)) return false;
            var tableName = GetTableNameFromFilePath(ctableAssetPath);
            if (!TryGetManifestSource(ReadManifestIfPresent(TableAssetFolder), tableName, out var source)) return false;
            var editorSource = GetEditorManifestSource(ReadEditorManifest(), source.tableName);
            return !IsCompleteSinglePartition(source, tableName) || !IsCompleteSinglePartition(editorSource, tableName);
        }
    
        /// <summary>
        ///   <para>获取客户端目录下的全部配置表资产路径。</para>
        /// </summary>
        internal static List<string> GetCtableAssetPaths() => GetTableFiles(TableAssetFolder, "*." + ConfigTableModule.FileExtension);
    
        /// <summary>
        ///   <para>获取同一源表的客户端分表资产路径。</para>
        /// </summary>
        /// <param name="ctableAssetPath">任一分表的资产路径。</param>
        internal static string[] GetRelatedClientPartitionAssetPaths(string ctableAssetPath)
        {
            var tableName = GetTableNameFromFilePath(ctableAssetPath);
            if (!TryGetManifestSource(ReadManifestIfPresent(TableAssetFolder), tableName, out var source))
                return Array.Empty<string>();
            var paths = new string[source.partitions.Length];
            for (int i = 0; i < paths.Length; i++)
                paths[i] = Game.PathUtility.Normalize(Path.Combine(TableAssetFolder,
                    source.partitions[i].tableName + "." + ConfigTableModule.FileExtension));
            Array.Sort(paths, StringComparer.OrdinalIgnoreCase);
            return paths;
        }
    
        /// <summary>
        ///   <para>尝试根据客户端清单获取配置表对应的源表名称。</para>
        /// </summary>
        /// <param name="ctableAssetPath">配置表资产路径。</param>
        /// <param name="sourceTableName">输出源表名称。</param>
        internal static bool TryGetSourceTableName(string ctableAssetPath, out string sourceTableName)
        {
            sourceTableName = null;
            if (string.IsNullOrEmpty(ctableAssetPath)) return false;
            var manifest = ReadManifestIfPresent(TableAssetFolder);
            if (!TryGetManifestSource(manifest, GetTableNameFromFilePath(ctableAssetPath), out var source)) return false;
            sourceTableName = source.tableName;
            return true;
        }
    
        /// <summary>
        ///   <para>迁移已生成的 C# 文件及其 Unity 元数据。</para>
        /// </summary>
        /// <param name="sourceFolder">原生成目录。</param>
        /// <param name="destinationFolder">目标生成目录。</param>
        /// <param name="error">输出失败原因。</param>
        internal static bool TryMoveGeneratedCodeFiles(string sourceFolder, string destinationFolder, out string error)
        {
            error = string.Empty;
            if (string.Equals(sourceFolder, destinationFolder, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
    
            try
            {
                var manifest = ReadManifestIfPresent(TableAssetFolder);
                var sourceManifests = manifest?.sources ?? Array.Empty<ConfigTableManifestSource>();
                var fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < sourceManifests.Length; i++)
                {
                    var source = sourceManifests[i];
                    if (source != null && !string.IsNullOrEmpty(source.tableName))
                    {
                        fileNames.Add(GetGeneratedCodeFileName(source.tableName));
                    }
                }

                if (fileNames.Count == 0) return true;

                EnsureDirectory(destinationFolder);
                AssetDatabase.Refresh();
                foreach (var fileName in fileNames)
                {
                    string sourcePath = Game.PathUtility.Normalize(Path.Combine(sourceFolder, fileName));
                    string destinationPath = Game.PathUtility.Normalize(Path.Combine(destinationFolder, fileName));
                    if (File.Exists(sourcePath) && (File.Exists(destinationPath) || File.Exists(destinationPath + ".meta")))
                    {
                        error = "目标目录已存在生成文件：" + destinationPath;
                        return false;
                    }
                }
    
                foreach (var fileName in fileNames)
                {
                    string sourcePath = Game.PathUtility.Normalize(Path.Combine(sourceFolder, fileName));
                    if (!File.Exists(sourcePath))
                    {
                        continue;
                    }
    
                    string destinationPath = Game.PathUtility.Normalize(Path.Combine(destinationFolder, fileName));
                    string moveError = AssetDatabase.MoveAsset(sourcePath, destinationPath);
                    if (!string.IsNullOrEmpty(moveError))
                    {
                        error = "移动生成的 C# 文件失败：" + sourcePath + " -> " + destinationPath + "\n" + moveError;
                        return false;
                    }
                }
                return true;
            }
            catch (Exception exception)
            {
                error = "迁移生成的 C# 文件失败：" + sourceFolder + " -> " + destinationFolder + "\n" + exception.Message;
                return false;
            }
        }
    
        /// <summary>
        ///   <para>迁移服务端配置表文件及其清单。</para>
        /// </summary>
        /// <param name="sourceFolder">原服务端目录。</param>
        /// <param name="destinationFolder">目标服务端目录。</param>
        /// <param name="error">输出失败原因。</param>
        internal static bool TryMoveServerTableFiles(string sourceFolder, string destinationFolder, out string error)
        {
            error = string.Empty;
            if (string.Equals(sourceFolder, destinationFolder, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
    
            try
            {
                string sourcePath = ConfigTableEditorSettings.instance.GetProjectAbsolutePath(sourceFolder);
                string destinationPath = ConfigTableEditorSettings.instance.GetProjectAbsolutePath(destinationFolder);
                var manifest = ReadManifestIfPresent(sourcePath);
                var filesToMove = new List<string>();
                var tableNames = manifest?.tables ?? Array.Empty<string>();
                for (int i = 0; i < tableNames.Length; i++)
                {
                    if (!string.IsNullOrEmpty(tableNames[i]))
                    {
                        filesToMove.Add(tableNames[i] + "." + ConfigTableModule.FileExtension);
                    }
                }
                filesToMove.Add(ConfigTableModule.ManifestFileName);
    
                EnsureDirectory(destinationPath);
                for (int i = 0; i < filesToMove.Count; i++)
                {
                    string sourceFilePath = Path.Combine(sourcePath, filesToMove[i]);
                    string destinationFilePath = Path.Combine(destinationPath, filesToMove[i]);
                    if (File.Exists(sourceFilePath) && (File.Exists(destinationFilePath) || File.Exists(destinationFilePath + ".meta")))
                    {
                        error = "目标目录已存在服务端文件：" + destinationFilePath;
                        return false;
                    }
                }
    
                for (int i = 0; i < filesToMove.Count; i++)
                {
                    string sourceFilePath = Path.Combine(sourcePath, filesToMove[i]);
                    if (File.Exists(sourceFilePath))
                    {
                        MoveFileAndMeta(sourceFilePath, Path.Combine(destinationPath, filesToMove[i]));
                    }
                }
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception exception)
            {
                error = "迁移服务端配置表失败：" + sourceFolder + " -> " + destinationFolder + "\n" + exception.Message;
                return false;
            }
        }
    
        /// <summary>
        ///   <para>根据当前配置表关联的 CSV 重建全部受影响输出。</para>
        /// </summary>
        /// <param name="ctableAssetPath">配置表资产路径。</param>
        public static bool BuildTableFromCsv(string ctableAssetPath)
        {
            try
            {
                var tableName = GetTableNameFromFilePath(ctableAssetPath);
                var sourceTableName = GetSourceTableName(tableName);
                var csvPath = GetCsvPath(sourceTableName);
                if (!File.Exists(csvPath))
                {
                    Game.LogError("未找到 CSV 源文件：" + csvPath);
                    return false;
                }
    
                List<string> files;
                if (!TryGetAvailableCsvFiles(out files))
                {
                    return false;
                }
    
                BuildAllTablesInternal(files);
                AssetDatabase.Refresh();
                Game.Log("已从 CSV 构建配置表：" + sourceTableName + "。");
                return true;
            }
            catch (Exception ex)
            {
                Game.LogError("从 CSV 构建配置表失败。\n" + ex.Message);
                return false;
            }
        }
    
        /// <summary>
        ///   <para>合并并导出全部配置表为 CSV。</para>
        /// </summary>
        public static void ExportAllTablesToCsv()
        {
            try
            {
                var manifest = ReadEditorManifest();
                var sourceManifests = manifest.sources ?? Array.Empty<ConfigTableEditorManifestSource>();
                var sources = new List<ConfigTableSource>(sourceManifests.Length);
                var csvPaths = new List<string>(sourceManifests.Length);
                for (int i = 0; i < sourceManifests.Length; i++)
                {
                    var source = MergeSource(sourceManifests[i]);
                    sources.Add(source);
                    csvPaths.Add(GetCsvPath(source.table.tableName));
                }
    
                if (!TryGetAvailableFiles(csvPaths, "CSV 配置表文件正被占用", "以下 CSV 文件正被其他应用程序使用或无法写入。请保存并关闭它们后重试："))
                {
                    return;
                }
    
                for (int i = 0; i < sources.Count; i++)
                {
                    WriteCsvTable(sources[i]);
                }
                Game.Log("已从 ." + ConfigTableModule.FileExtension + " 导出 " + sources.Count + " 个 CSV 配置表。");
            }
            catch (Exception ex)
            {
                Game.LogError("从 ." + ConfigTableModule.FileExtension + " 导出 CSV 配置表失败。\n" + ex.Message);
            }
        }
    
        /// <summary>
        ///   <para>判断指定配置表是否具备导出 CSV 所需的清单数据。</para>
        /// </summary>
        /// <param name="ctableAssetPath">配置表资产路径。</param>
        internal static bool CanExportToCsv(string ctableAssetPath)
        {
            if (!TryGetSourceTableName(ctableAssetPath, out var sourceTableName)) return false;
            if (!File.Exists(GetEditorManifestPath())) return false;
            GetEditorManifestSource(ReadEditorManifest(), sourceTableName);
            return true;
        }
    
        /// <summary>
        ///   <para>合并并导出指定配置表所属源表为 CSV。</para>
        /// </summary>
        /// <param name="ctableAssetPath">配置表资产路径。</param>
        public static bool ExportTableToCsv(string ctableAssetPath)
        {
            try
            {
                var tableName = GetTableNameFromFilePath(ctableAssetPath);
                var sourceTableName = GetSourceTableName(tableName);
                var source = MergeSource(GetEditorManifestSource(ReadEditorManifest(), sourceTableName));
                var csvPaths = new List<string> { GetCsvPath(source.table.tableName) };
                if (!TryGetAvailableFiles(csvPaths, "CSV 配置表文件正被占用", "CSV 文件正被其他应用程序使用或无法写入。请保存并关闭它后重试："))
                {
                    return false;
                }
    
                WriteCsvTable(source);
                Game.Log("已从 ." + ConfigTableModule.FileExtension + " 导出 CSV 配置表：" + source.table.tableName + "。");
                return true;
            }
            catch (Exception ex)
            {
                Game.LogError("从 ." + ConfigTableModule.FileExtension + " 导出 CSV 配置表失败。\n" + ex.Message);
                return false;
            }
        }
    
        /// <summary>
        ///   <para>构建全部表。</para>
        /// </summary>
        /// <param name="files">文件。</param>
        private static int BuildAllTablesInternal(List<string> files)
        {
            var sources = LoadCsvSources(files);
            WriteBuiltTables(sources);
            return sources.Count;
        }
    
        /// <summary>
        ///   <para>加载 CSV 源。</para>
        /// </summary>
        /// <param name="files">文件。</param>
        private static List<ConfigTableSource> LoadCsvSources(List<string> files)
        {
            var sources = new List<ConfigTableSource>(files.Count);
            for (int i = 0; i < files.Count; i++)
            {
                try
                {
                    sources.Add(LoadCsvSource(files[i], ReadCsvText(files[i])));
                }
                catch (Exception exception)
                {
                    throw new InvalidDataException("CSV 配置表读取失败：" + files[i] + "\n" + exception.Message, exception);
                }
            }
            return sources;
        }
    
        /// <summary>
        ///   <para>尝试获取可用 CSV 文件。</para>
        /// </summary>
        /// <param name="files">文件。</param>
        private static bool TryGetAvailableCsvFiles(out List<string> files)
        {
            var csvRoot = GetCsvRoot();
            EnsureDirectory(csvRoot);
            files = GetTableFiles(csvRoot, "*" + CsvExtension);
            if (files.Count == 0)
            {
                Game.LogError("未找到 CSV 配置表：" + csvRoot + "。");
                return false;
            }
            return TryGetAvailableFiles(files, "配置表文件正被占用", "以下 CSV 文件正被其他应用程序使用或无法读取。请保存并关闭它们后重试：");
        }
    
        /// <summary>
        ///   <para>尝试获取可用文件。</para>
        /// </summary>
        /// <param name="files">文件。</param>
        /// <param name="dialogTitle">对话框标题。</param>
        /// <param name="messagePrefix">消息前缀。</param>
        private static bool TryGetAvailableFiles(List<string> files, string dialogTitle, string messagePrefix)
        {
            while (true)
            {
                var unavailableFiles = GetUnavailableFiles(files);
                if (unavailableFiles.Count == 0)
                {
                    return true;
                }
    
                var message = messagePrefix + "\n\n" + FormatFileList(unavailableFiles);
                if (Application.isBatchMode)
                {
                    Game.LogError(dialogTitle + "\n" + message);
                    return false;
                }
                if (!EditorUtility.DisplayDialog(dialogTitle, message, "重试", "取消"))
                {
                    return false;
                }
            }
        }
    
        /// <summary>
        ///   <para>获取不可用文件。</para>
        /// </summary>
        /// <param name="files">文件。</param>
        private static List<string> GetUnavailableFiles(List<string> files)
        {
            var unavailableFiles = new List<string>();
            for (int i = 0; i < files.Count; i++)
            {
                if (!File.Exists(files[i]))
                {
                    continue;
                }
                try
                {
                    using (var stream = new FileStream(files[i], FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                    }
                }
                catch (IOException)
                {
                    unavailableFiles.Add(files[i]);
                }
                catch (UnauthorizedAccessException)
                {
                    unavailableFiles.Add(files[i]);
                }
            }
            return unavailableFiles;
        }
    
        /// <summary>
        ///   <para>格式化文件列表。</para>
        /// </summary>
        /// <param name="files">文件。</param>
        private static string FormatFileList(List<string> files)
        {
            const int maxDisplayedCount = 2;
            var builder = new StringBuilder();
            for (int i = 0; i < files.Count && i < maxDisplayedCount; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(Path.GetFileName(files[i]));
            }
            if (files.Count > maxDisplayedCount)
            {
                builder.Append("... 以及 ");
                builder.Append(files.Count - maxDisplayedCount);
                builder.Append(" 个更多文件");
            }
            return builder.ToString();
        }
    
        /// <summary>
        ///   <para>获取表文件。</para>
        /// </summary>
        /// <param name="folder">目录。</param>
        /// <param name="searchPattern">搜索模式。</param>
        private static List<string> GetTableFiles(string folder, string searchPattern)
        {
            var files = new List<string>();
            if (!Directory.Exists(folder))
            {
                return files;
            }
    
            var filePaths = Directory.GetFiles(folder, searchPattern, SearchOption.TopDirectoryOnly);
            for (int i = 0; i < filePaths.Length; i++)
            {
                files.Add(Game.PathUtility.Normalize(filePaths[i]));
            }
            files.Sort(StringComparer.OrdinalIgnoreCase);
            return files;
        }
    
        /// <summary>
        ///   <para>确保目录。</para>
        /// </summary>
        /// <param name="path">路径。</param>
        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    
        /// <summary>
        ///   <para>构建表代码。</para>
        /// </summary>
        /// <param name="table">表。</param>
        private static string BuildTableCode(ConfigTableAsset table)
        {
            var tableClassName = GetGeneratedTableTypeName(table.tableName);
            var rowClassName = GetGeneratedRowTypeName(table.tableName);
            var getMethodName = GetGeneratedGetMethodName(table.tableName);
            var tryGetMethodName = GetGeneratedTryGetMethodName(table.tableName);
            var rowsMethodName = GetGeneratedRowsMethodName(table.tableName);
            var rawMethodName = GetGeneratedRawMethodName(table.tableName);
            var countMethodName = GetGeneratedCountMethodName(table.tableName);
            var fields = BuildFieldCodeSchemas(table);
            bool hasTableReferences = HasTableReferences(fields);
            var idType = ConfigTableSchema.NormalizeType(table.types[0]);
            if (idType != "int" && idType != "string")
            {
                throw new InvalidDataException(table.tableName + "：ID 字段类型仅支持 int 或 string。");
            }

            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated>");
            builder.AppendLine("// Generated by ConfigTableImporter. Do not edit manually.");
            builder.AppendLine("// </auto-generated>");
            builder.AppendLine("using System;");
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine("using " + GeneratedRuntimeNamespace + ";");
            builder.AppendLine();
            builder.AppendLine("public static class " + tableClassName);
            builder.AppendLine("{");
            builder.AppendLine("\tpublic const string " + GeneratedTableNameConstantName + " = " + ToCSharpStringLiteral(table.tableName) + ";");
            builder.AppendLine();
            builder.AppendLine("\tpublic static " + rowClassName + " " + getMethodName + "(this IConfigTables " + GeneratedSelfParameterName + ", " + idType + " id)");
            builder.AppendLine("\t{");
            builder.AppendLine("\t\tvar table = " + GeneratedSelfParameterName + " == null ? null : " + GeneratedSelfParameterName + ".Get(Name);");
            builder.AppendLine("\t\treturn table == null ? null : Wrap(" + GeneratedSelfParameterName + ", table, table.Get(id));");
            builder.AppendLine("\t}");
            builder.AppendLine();
            builder.AppendLine("\tpublic static bool " + tryGetMethodName + "(this IConfigTables " + GeneratedSelfParameterName + ", " + idType + " id, out " + rowClassName + " row)");
            builder.AppendLine("\t{");
            builder.AppendLine("\t\tvar table = " + GeneratedSelfParameterName + " == null ? null : " + GeneratedSelfParameterName + ".Get(Name);");
            builder.AppendLine("\t\tif (table != null && table.TryGet(id, out var rawRow))");
            builder.AppendLine("\t\t{");
            builder.AppendLine("\t\t\trow = Wrap(" + GeneratedSelfParameterName + ", table, rawRow);");
            builder.AppendLine("\t\t\treturn true;");
            builder.AppendLine("\t\t}");
            builder.AppendLine("\t\trow = null;");
            builder.AppendLine("\t\treturn false;");
            builder.AppendLine("\t}");
            builder.AppendLine();
            builder.AppendLine("\tpublic static IReadOnlyList<" + rowClassName + "> " + rowsMethodName + "(this IConfigTables " + GeneratedSelfParameterName + ")");
            builder.AppendLine("\t{");
            builder.AppendLine("\t\tvar table = " + GeneratedSelfParameterName + " == null ? null : " + GeneratedSelfParameterName + ".Get(Name);");
            builder.AppendLine("\t\treturn table == null ? Array.Empty<" + rowClassName + ">() : table.GetOrCreateRows(" + GeneratedSelfParameterName + ", " + GeneratedFactoryFieldName + ");");
            builder.AppendLine("\t}");
            builder.AppendLine();
            builder.AppendLine("\tpublic static ConfigTable " + rawMethodName + "(this IConfigTables " + GeneratedSelfParameterName + ") => " + GeneratedSelfParameterName + " == null ? null : " + GeneratedSelfParameterName + ".Get(Name);");
            builder.AppendLine("\tpublic static int " + countMethodName + "(this IConfigTables " + GeneratedSelfParameterName + ") => " + rawMethodName + "(" + GeneratedSelfParameterName + ")?.Count ?? 0;");
            builder.AppendLine();
            builder.AppendLine("\tprivate static readonly Func<IConfigTables, ConfigTableRow, " + rowClassName + "> " + GeneratedFactoryFieldName + " = Create;");
            builder.AppendLine("\tprivate static " + rowClassName + " Wrap(IConfigTables " + GeneratedSelfParameterName + ", ConfigTable table, ConfigTableRow rawRow)");
            builder.AppendLine("\t{");
            builder.AppendLine("\t\treturn rawRow == null ? null : table.GetOrCreateRow(" + GeneratedSelfParameterName + ", rawRow, " + GeneratedFactoryFieldName + ");");
            builder.AppendLine("\t}");
            builder.AppendLine();
            builder.AppendLine("\tprivate static " + rowClassName + " Create(IConfigTables " + GeneratedSelfParameterName + ", ConfigTableRow row) => new " + rowClassName + "(" + (hasTableReferences ? GeneratedSelfParameterName + ", " : string.Empty) + "row);");
            builder.AppendLine("}");
            builder.AppendLine();
            builder.AppendLine("public sealed class " + rowClassName);
            builder.AppendLine("{");
            builder.AppendLine("\tpublic const string " + GeneratedRowTableNameConstantName + " = " + ToCSharpStringLiteral(table.tableName) + ";");
            if (idType == "string")
            {
                builder.AppendLine();
                AppendStringIdConstants(builder, table);
            }
            if (hasTableReferences)
            {
                builder.AppendLine();
                builder.AppendLine("\tprivate readonly IConfigTables " + GeneratedConfigTablesFieldName + ";");
            }
            builder.AppendLine("\tprivate readonly ConfigTableRow " + GeneratedRawRowFieldName + ";");
            for (int i = 0; i < fields.Length; i++)
            {
                if (string.IsNullOrEmpty(fields[i].referenceTableClassName))
                {
                    builder.AppendLine("\tprivate readonly " + fields[i].csType + " " + GetGeneratedFieldName(fields[i].memberName) + ";");
                }
                else
                {
                    builder.AppendLine("\tprivate " + fields[i].csType + " " + GetGeneratedFieldName(fields[i].memberName) + ";");
                    builder.AppendLine("\tprivate bool " + GetGeneratedReferenceLoadedFieldName(i) + ";");
                }
            }
            builder.AppendLine();
            builder.AppendLine("\tinternal " + rowClassName + "(" + (hasTableReferences ? "IConfigTables " + GeneratedSelfParameterName + ", " : string.Empty) + "ConfigTableRow row)");
            builder.AppendLine("\t{");
            if (hasTableReferences)
            {
                builder.AppendLine("\t\tthis." + GeneratedConfigTablesFieldName + " = " + GeneratedSelfParameterName + ";");
            }
            builder.AppendLine("\t\tthis." + GeneratedRawRowFieldName + " = row;");
            for (int i = 0; i < fields.Length; i++)
            {
                if (string.IsNullOrEmpty(fields[i].referenceTableClassName))
                {
                    builder.AppendLine();
                    AppendRowValueAssignment(builder, fields[i]);
                }
            }
            builder.AppendLine("\t}");
            builder.AppendLine();
            builder.AppendLine("\tpublic ConfigTableRow " + GeneratedRawRowPropertyName + " => this." + GeneratedRawRowFieldName + ";");
            for (int i = 0; i < fields.Length; i++)
            {
                builder.AppendLine();
                AppendRowProperty(builder, fields[i]);
            }
            builder.AppendLine("}");

            return builder.ToString();
        }
    
        /// <summary>
        ///   <para>追加字符串标识常量。</para>
        /// </summary>
        /// <param name="builder">构建器。</param>
        /// <param name="table">表。</param>
        private static void AppendStringIdConstants(StringBuilder builder, ConfigTableAsset table)
        {
            var usedNames = new HashSet<string>(StringComparer.Ordinal);
            var rows = table.rows ?? Array.Empty<ConfigTableRecord>();
            for (int i = 0; i < rows.Length; i++)
            {
                string memberName = MakeUniqueIdentifier("Id_" + ToMemberIdentifier(rows[i].id), usedNames);
                builder.AppendLine("\tpublic const string " + memberName + " = " + ToCSharpStringLiteral(rows[i].id) + ";");
            }
        }
    
        /// <summary>
        ///   <para>构建字段代码结构。</para>
        /// </summary>
        /// <param name="table">表。</param>
        private static FieldCodeSchema[] BuildFieldCodeSchemas(ConfigTableAsset table)
        {
            var fields = new FieldCodeSchema[table.fields.Length];
            var usedMemberNames = new HashSet<string>(StringComparer.Ordinal)
            {
                GeneratedRawRowPropertyName,
                "Row",
                GeneratedRawRowFieldName,
                GeneratedConfigTablesFieldName
            };
            for (int i = 0; i < table.fields.Length; i++)
            {
                usedMemberNames.Add(GeneratedReferenceLoaderPrefix + i.ToString(CultureInfo.InvariantCulture));
                usedMemberNames.Add(GeneratedReferenceLoadedPrefix + i.ToString(CultureInfo.InvariantCulture));
            }
            for (int i = 0; i < table.fields.Length; i++)
            {
                string normalizedType = ConfigTableSchema.NormalizeType(table.types[i]);
                string memberName = ToMemberIdentifier(table.fields[i]);
                memberName = MakeUniqueIdentifier(memberName, usedMemberNames);
                var field = new FieldCodeSchema
                {
                    index = i,
                    memberName = memberName,
                    comment = i < table.comments.Length ? table.comments[i] : string.Empty,
                    csType = ConfigTableSchema.GetCSharpType(normalizedType),
                    getterName = ConfigTableSchema.GetGetterName(normalizedType)
                };
    
                var reference = i < table.references.Length ? table.references[i] : string.Empty;
                if (!string.IsNullOrEmpty(reference) && ConfigTableSchema.TryGetTupleArrayElementTypes(normalizedType, out var tupleElementTypes) &&
                    ConfigTableSchema.TryGetReferenceTupleIndex(reference, out var tupleReferenceIndex))
                {
                    var tupleTypes = new string[tupleElementTypes.Length];
                    for (int typeIndex = 0; typeIndex < tupleTypes.Length; typeIndex++)
                    {
                        tupleTypes[typeIndex] = ConfigTableSchema.GetCSharpType(tupleElementTypes[typeIndex]);
                    }
    
                    var targetTableName = ConfigTableSchema.GetReferenceTargetName(reference);
                    tupleTypes[tupleReferenceIndex] = GetGeneratedRowTypeName(targetTableName);
                    field.csType = "(" + string.Join(", ", tupleTypes) + ")[]";
                    field.referenceTableClassName = GetGeneratedRowTypeName(targetTableName);
                    field.referenceTableName = GetGeneratedTableTypeName(targetTableName);
                    field.referenceGetMethodName = GetGeneratedGetMethodName(targetTableName);
                    field.referenceTupleIndex = tupleReferenceIndex;
                }
    
                fields[i] = field;
            }
            return fields;
        }

        /// <summary>
        ///   <para>判断是否包含表引用。</para>
        /// </summary>
        /// <param name="fields">字段名称。</param>
        private static bool HasTableReferences(FieldCodeSchema[] fields)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (!string.IsNullOrEmpty(fields[i].referenceTableClassName))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///   <para>追加行属性。</para>
        /// </summary>
        /// <param name="builder">构建器。</param>
        /// <param name="field">字段。</param>
        private static void AppendRowProperty(StringBuilder builder, FieldCodeSchema field)
        {
            AppendXmlSummary(builder, field.comment);
            if (!string.IsNullOrEmpty(field.referenceTableClassName))
            {
                builder.AppendLine("\tpublic " + field.csType + " @" + field.memberName);
                builder.AppendLine("\t{");
                builder.AppendLine("\t\tget");
                builder.AppendLine("\t\t{");
                builder.AppendLine("\t\t\tif (!this." + GetGeneratedReferenceLoadedFieldName(field.index) + ")");
                builder.AppendLine("\t\t\t{");
                builder.AppendLine("\t\t\t\tthis." + GetGeneratedFieldName(field.memberName) + " = this." + GetGeneratedReferenceLoaderMethodName(field.index) + "();");
                builder.AppendLine("\t\t\t\tthis." + GetGeneratedReferenceLoadedFieldName(field.index) + " = true;");
                builder.AppendLine("\t\t\t}");
                builder.AppendLine("\t\t\treturn this." + GetGeneratedFieldName(field.memberName) + ";");
                builder.AppendLine("\t\t}");
                builder.AppendLine("\t}");
                builder.AppendLine();
                AppendReferenceRowLoader(builder, field);
                return;
            }
            builder.AppendLine("\tpublic " + field.csType + " @" + field.memberName + " => this." + GetGeneratedFieldName(field.memberName) + ";");
        }
    
        /// <summary>
        ///   <para>追加行值赋值。</para>
        /// </summary>
        /// <param name="builder">构建器。</param>
        /// <param name="field">字段。</param>
        private static void AppendRowValueAssignment(StringBuilder builder, FieldCodeSchema field)
        {
            string index = field.index.ToString(CultureInfo.InvariantCulture);
            builder.AppendLine("\t\tthis." + GetGeneratedFieldName(field.memberName) + " = row." + field.getterName + "(" + index + ");");
        }

        /// <summary>
        ///   <para>追加引用行加载器。</para>
        /// </summary>
        /// <param name="builder">构建器。</param>
        /// <param name="field">字段。</param>
        private static void AppendReferenceRowLoader(StringBuilder builder, FieldCodeSchema field)
        {
            string index = field.index.ToString(CultureInfo.InvariantCulture);
            builder.AppendLine("\tprivate " + field.csType + " " + GetGeneratedReferenceLoaderMethodName(field.index) + "()");
            builder.AppendLine("\t{");
            builder.AppendLine("\t\tvar values = this." + GeneratedRawRowFieldName + "." + field.getterName + "(" + index + ");");
            builder.AppendLine("\t\tvar result = new " + field.csType.Substring(0, field.csType.Length - 2) + "[values.Length];");
            builder.AppendLine("\t\tfor (var i = 0; i < values.Length; i++)");
            builder.AppendLine("\t\t{");
            builder.AppendLine("\t\t\tvar reference = " + field.referenceTableName + "." + field.referenceGetMethodName + "(this." + GeneratedConfigTablesFieldName + ", values[i].Item" +
                (field.referenceTupleIndex + 1).ToString(CultureInfo.InvariantCulture) + ");");
            if (field.referenceTupleIndex == 0)
            {
                builder.AppendLine("\t\t\tresult[i] = (reference, values[i].Item2);");
            }
            else
            {
                builder.AppendLine("\t\t\tresult[i] = (values[i].Item1, reference);");
            }
            builder.AppendLine("\t\t}");
            builder.AppendLine("\t\treturn result;");
            builder.AppendLine("\t}");
        }
    
        /// <summary>
        ///   <para>追加 XML 摘要。</para>
        /// </summary>
        /// <param name="builder">构建器。</param>
        /// <param name="comment">注释。</param>
        private static void AppendXmlSummary(StringBuilder builder, string comment)
        {
            if (string.IsNullOrEmpty(comment))
            {
                return;
            }
    
            var lines = comment.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            builder.AppendLine("\t/// <summary>");
            for (int i = 0; i < lines.Length; i++)
            {
                builder.AppendLine("\t/// " + EscapeXmlComment(lines[i]));
            }
            builder.AppendLine("\t/// </summary>");
        }
    
        /// <summary>
        ///   <para>转义 XML 注释。</para>
        /// </summary>
        /// <param name="value">值。</param>
        private static string EscapeXmlComment(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
    
            var builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                var character = value[i];
                switch (character)
                {
                    case '&':
                        builder.Append("&amp;");
                        break;
                    case '<':
                        builder.Append("&lt;");
                        break;
                    case '>':
                        builder.Append("&gt;");
                        break;
                    default:
                        if (character > 0x7f)
                        {
                            builder.Append("&#x");
                            builder.Append(((int)character).ToString("x", CultureInfo.InvariantCulture));
                            builder.Append(';');
                        }
                        else
                        {
                            builder.Append(character);
                        }
                        break;
                }
            }
            return builder.ToString();
        }
    
        /// <summary>
        ///   <para>转换为类型标识符。</para>
        /// </summary>
        /// <param name="value">值。</param>
        private static string ToTypeIdentifier(string value) => ToIdentifier(value, true);

        /// <summary>
        ///   <para>获取已生成表类型名称。</para>
        /// </summary>
        /// <param name="tableName">表名称。</param>
        private static string GetGeneratedTableTypeName(string tableName) => ToTypeIdentifier(tableName) + GeneratedTableTypeSuffix;

        /// <summary>
        ///   <para>获取已生成行类型名称。</para>
        /// </summary>
        /// <param name="tableName">表名称。</param>
        private static string GetGeneratedRowTypeName(string tableName) => GetGeneratedTableTypeName(tableName) + GeneratedRowTypeSuffix;

        /// <summary>
        ///   <para>获取已生成获取方法名称。</para>
        /// </summary>
        /// <param name="tableName">表名称。</param>
        private static string GetGeneratedGetMethodName(string tableName) => GeneratedGetMethodPrefix + GetGeneratedTableTypeName(tableName);

        /// <summary>
        ///   <para>获取生成的 TryGet 方法名称。</para>
        /// </summary>
        /// <param name="tableName">表名称。</param>
        private static string GetGeneratedTryGetMethodName(string tableName) => GeneratedTryGetMethodPrefix + GetGeneratedTableTypeName(tableName);

        /// <summary>
        ///   <para>获取已生成行方法名称。</para>
        /// </summary>
        /// <param name="tableName">表名称。</param>
        private static string GetGeneratedRowsMethodName(string tableName) => GetGeneratedGetMethodName(tableName) + GeneratedRowsMethodSuffix;

        /// <summary>
        ///   <para>获取已生成原始方法名称。</para>
        /// </summary>
        /// <param name="tableName">表名称。</param>
        private static string GetGeneratedRawMethodName(string tableName) => GetGeneratedGetMethodName(tableName) + GeneratedRawMethodSuffix;

        /// <summary>
        ///   <para>获取已生成数量方法名称。</para>
        /// </summary>
        /// <param name="tableName">表名称。</param>
        private static string GetGeneratedCountMethodName(string tableName) => GetGeneratedGetMethodName(tableName) + GeneratedCountMethodSuffix;

        /// <summary>
        ///   <para>获取已生成代码文件名称。</para>
        /// </summary>
        /// <param name="tableName">表名称。</param>
        private static string GetGeneratedCodeFileName(string tableName) => GetGeneratedTableTypeName(tableName) + GeneratedCodeSuffix;
    
        /// <summary>
        ///   <para>转换为成员标识符。</para>
        /// </summary>
        /// <param name="value">值。</param>
        private static string ToMemberIdentifier(string value) => ToIdentifier(value, false);

        /// <summary>
        ///   <para>获取已生成字段名称。</para>
        /// </summary>
        /// <param name="memberName">成员名称。</param>
        private static string GetGeneratedFieldName(string memberName) => "m_" + memberName;

        /// <summary>
        ///   <para>获取已生成引用已加载字段名称。</para>
        /// </summary>
        /// <param name="fieldIndex">字段索引。</param>
        private static string GetGeneratedReferenceLoadedFieldName(int fieldIndex) => "m_" + GeneratedReferenceLoadedPrefix + fieldIndex.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        ///   <para>获取已生成引用加载器方法名称。</para>
        /// </summary>
        /// <param name="fieldIndex">字段索引。</param>
        private static string GetGeneratedReferenceLoaderMethodName(int fieldIndex) => GeneratedReferenceLoaderPrefix + fieldIndex.ToString(CultureInfo.InvariantCulture);
    
        /// <summary>
        ///   <para>转换为标识符。</para>
        /// </summary>
        /// <param name="value">值。</param>
        /// <param name="pascalCase">Pascal 大小写。</param>
        private static string ToIdentifier(string value, bool pascalCase)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidDataException("配置表名称和字段名称不能为空。");
            }
    
            var builder = new StringBuilder(value.Length);
            bool upperNext = pascalCase;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (IsAsciiIdentifierPart(c))
                {
                    if (builder.Length == 0 && c >= '0' && c <= '9')
                    {
                        builder.Append('_');
                    }
    
                    if (upperNext)
                    {
                        builder.Append(char.ToUpperInvariant(c));
                        upperNext = false;
                    }
                    else
                    {
                        builder.Append(c);
                    }
                }
                else
                {
                    upperNext = builder.Length > 0;
                }
            }
    
            if (builder.Length == 0) throw new InvalidDataException("无法从名称生成 C# 标识符：" + value);
            return builder.ToString();
        }
    
        /// <summary>
        ///   <para>判断是否为 ASCII 标识符字符。</para>
        /// </summary>
        /// <param name="c">字符。</param>
        private static bool IsAsciiIdentifierPart(char c) => c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
    
        /// <summary>
        ///   <para>创建唯一标识符。</para>
        /// </summary>
        /// <param name="identifier">标识符。</param>
        /// <param name="usedIdentifiers">已使用标识符集合。</param>
        private static string MakeUniqueIdentifier(string identifier, HashSet<string> usedIdentifiers)
        {
            var baseIdentifier = identifier;
            int index = 2;
            while (usedIdentifiers.Contains(identifier))
            {
                identifier = baseIdentifier + index.ToString(CultureInfo.InvariantCulture);
                index++;
            }
            usedIdentifiers.Add(identifier);
            return identifier;
        }
    
        /// <summary>
        ///   <para>转换为 C# 字符串字面量。</para>
        /// </summary>
        /// <param name="value">值。</param>
        private static string ToCSharpStringLiteral(string value)
        {
            var builder = new StringBuilder();
            builder.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (c < 0x20 || c > 0x7f)
                        {
                            builder.Append("\\u");
                            builder.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(c);
                        }
                        break;
                }
            }
            builder.Append('"');
            return builder.ToString();
        }
    
        /// <summary>
        ///   <para>加载 CSV 源。</para>
        /// </summary>
        /// <param name="filePath">配置表文件绝对路径。</param>
        /// <param name="text">要压缩的字符串。</param>
        private static ConfigTableSource LoadCsvSource(string filePath, string text)
        {
            var tableName = GetTableNameFromFilePath(filePath);
            var rows = Game.CsvUtility.Parse(text);
            if (rows.Count < 5)
            {
                throw new InvalidDataException("配置表至少需要 5 行元数据：字段、类型、注释、引用、标签。");
            }
    
            var fields = NormalizeSchemaRow(rows[0], rows[0].Length, tableName, "字段");
            var types = NormalizeTypeRow(rows[1], fields.Length, tableName, "类型");
            var comments = NormalizeSchemaRow(rows[2], fields.Length, tableName, "注释");
            var references = NormalizeSchemaRow(rows[3], fields.Length, tableName, "引用");
            var tags = NormalizeTagRow(rows[4], fields.Length, tableName);
            ConfigTableSchema.ValidateSchema(tableName, fields, types, comments, references);
    
            var records = new List<ConfigTableRecord>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 5; i < rows.Count; i++)
            {
                if (IsEmptyRow(rows[i]))
                {
                    continue;
                }
    
                var location = "第 " + (i + 1).ToString(CultureInfo.InvariantCulture) + " 行";
                var values = NormalizeDataRow(rows[i], fields.Length, tableName, location);
                var record = new ConfigTableRecord
                {
                    id = values[0],
                    values = values
                };
                ConfigTableSchema.ValidateRecord(tableName, fields, types, record, ids, location);
                records.Add(record);
            }
    
            return new ConfigTableSource
            {
                table = new ConfigTableAsset
                {
                    tableName = tableName,
                    fields = fields,
                    types = types,
                    comments = comments,
                    references = references,
                    rows = records.ToArray()
                },
                tags = tags
            };
        }
    
        /// <summary>
        ///   <para>规范化标记行。</para>
        /// </summary>
        /// <param name="row">表记录。</param>
        /// <param name="count">数量。</param>
        /// <param name="tableName">表名称。</param>
        private static string[] NormalizeTagRow(string[] row, int count, string tableName)
        {
            var tags = NormalizeSchemaRow(row, count, tableName, "标签");
            for (int i = 0; i < tags.Length; i++)
            {
                tags[i] = NormalizeTagCell(tags[i], tableName, i);
            }
            return tags;
        }
    
        /// <summary>
        ///   <para>规范化标记单元格。</para>
        /// </summary>
        /// <param name="value">值。</param>
        /// <param name="tableName">表名称。</param>
        /// <param name="columnIndex">字段索引。</param>
        private static string NormalizeTagCell(string value, string tableName, int columnIndex)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidDataException(tableName + "：第 " + (columnIndex + 1).ToString(CultureInfo.InvariantCulture) + " 列的标签不能为空。");
            }
    
            var rawTags = value.Split(TagSeparators, StringSplitOptions.None);
            var normalizedTags = new List<string>(rawTags.Length);
            var uniqueTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool hasClient = false;
            bool hasServer = false;
            for (int i = 0; i < rawTags.Length; i++)
            {
                var rawTag = rawTags[i];
                if (string.IsNullOrEmpty(rawTag) || !string.Equals(rawTag, rawTag.Trim(), StringComparison.Ordinal))
                {
                    throw new InvalidDataException(tableName + "：第 " + (columnIndex + 1).ToString(CultureInfo.InvariantCulture) + " 列包含空标签或标签包含首尾空白字符。");
                }
    
                string normalizedTag;
                if (string.Equals(rawTag, ClientTag, StringComparison.OrdinalIgnoreCase))
                {
                    normalizedTag = ClientTag;
                    hasClient = true;
                }
                else if (string.Equals(rawTag, ServerTag, StringComparison.OrdinalIgnoreCase))
                {
                    normalizedTag = ServerTag;
                    hasServer = true;
                }
                else
                {
                    if (!IsValidCustomTag(rawTag))
                    {
                        throw new InvalidDataException(tableName + "：第 " + (columnIndex + 1).ToString(CultureInfo.InvariantCulture) + " 列的自定义标签无效：" + rawTag + "。标签只能包含英文字母、数字和下划线。");
                    }
                    normalizedTag = rawTag;
                }
    
                if (!uniqueTags.Add(normalizedTag))
                {
                    throw new InvalidDataException(tableName + "：第 " + (columnIndex + 1).ToString(CultureInfo.InvariantCulture) + " 列包含重复标签：" + normalizedTag + "。");
                }
                normalizedTags.Add(normalizedTag);
            }
    
            if (!hasClient && !hasServer)
            {
                throw new InvalidDataException(tableName + "：第 " + (columnIndex + 1).ToString(CultureInfo.InvariantCulture) + " 列至少需要包含保留标签 C 或 S。");
            }
            if (columnIndex == 0)
            {
                if (!hasClient || !hasServer || normalizedTags.Count != 2)
                {
                    throw new InvalidDataException(tableName + "：ID 列必须且只能使用 C|S 标签。");
                }
                return ClientTag + TagSeparator + ServerTag;
            }
    
            return string.Join(TagSeparator.ToString(), normalizedTags);
        }
    
        /// <summary>
        ///   <para>判断是否为有效自定义标记。</para>
        /// </summary>
        /// <param name="tag">标记。</param>
        private static bool IsValidCustomTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return false;
            }
            for (int i = 0; i < tag.Length; i++)
            {
                var character = tag[i];
                if (!(character >= 'A' && character <= 'Z') && !(character >= 'a' && character <= 'z') &&
                    !(character >= '0' && character <= '9') && character != '_')
                {
                    return false;
                }
            }
            return true;
        }
    
        /// <summary>
        ///   <para>读取、规范化并校验配置表文件。</para>
        /// </summary>
        /// <param name="filePath">配置表文件绝对路径。</param>
        internal static ConfigTableAsset LoadCtable(string filePath)
        {
            var tableName = GetTableNameFromFilePath(filePath);
            ConfigTableAsset asset;
            try
            {
                asset = JsonUtility.FromJson<ConfigTableAsset>(ReadUtf8Text(filePath));
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("无效的 ." + ConfigTableModule.FileExtension + " 配置表：" + filePath + "\n" + ex.Message);
            }
    
            if (asset == null)
            {
                throw new InvalidDataException("无效的 ." + ConfigTableModule.FileExtension + " 配置表：根对象为空。");
            }
            if (!string.Equals(asset.tableName, tableName, StringComparison.Ordinal))
            {
                throw new InvalidDataException("." + ConfigTableModule.FileExtension + " 配置表名称与文件名不一致：" + asset.tableName + " != " + tableName + "。");
            }
    
            var fields = NormalizeSchemaRow(asset.fields, asset.fields?.Length ?? 0, tableName, "字段");
            var types = NormalizeTypeRow(asset.types, fields.Length, tableName, "类型");
            var comments = NormalizeSchemaRow(asset.comments, fields.Length, tableName, "注释");
            var references = NormalizeSchemaRow(asset.references, fields.Length, tableName, "引用");
            ConfigTableSchema.ValidateSchema(tableName, fields, types, comments, references);
    
            var records = new List<ConfigTableRecord>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sourceRows = asset.rows ?? Array.Empty<ConfigTableRecord>();
            for (int i = 0; i < sourceRows.Length; i++)
            {
                var source = sourceRows[i];
                if (source == null)
                {
                    throw new InvalidDataException(tableName + " 第 " + (i + 1).ToString(CultureInfo.InvariantCulture) + " 行：记录为空。");
                }
    
                var location = "第 " + (i + 1).ToString(CultureInfo.InvariantCulture) + " 行";
                var values = NormalizeDataRow(source.values, fields.Length, tableName, location);
                var record = new ConfigTableRecord
                {
                    id = source.id,
                    values = values
                };
                ConfigTableSchema.ValidateRecord(tableName, fields, types, record, ids, location);
                records.Add(record);
            }
    
            return new ConfigTableAsset
            {
                tableName = tableName,
                fields = fields,
                types = types,
                comments = comments,
                references = references,
                rows = records.ToArray()
            };
        }
    
        /// <summary>
        ///   <para>校验并保存配置表文件，同时维护相关清单和生成代码。</para>
        /// </summary>
        /// <param name="filePath">配置表文件绝对路径。</param>
        /// <param name="table">待保存的表数据。</param>
        internal static void SaveCtable(string filePath, ConfigTableAsset table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }
    
            var expectedTableName = GetTableNameFromFilePath(filePath);
            if (!string.Equals(table.tableName, expectedTableName, StringComparison.Ordinal))
            {
                throw new InvalidDataException("." + ConfigTableModule.FileExtension + " 配置表名称与文件名不一致：" + table.tableName + " != " + expectedTableName + "。");
            }
            ConfigTableSchema.ValidateAsset(table);
            var clientManifest = ReadManifestIfPresent(TableAssetFolder);
            if (clientManifest == null)
            {
                Game.FileUtility.WriteAllTextAtomically(filePath, BuildJsonTable(table), Utf8WithoutBom);
                return;
            }
            if (clientManifest.tables == null || clientManifest.sources == null)
            {
                throw new InvalidDataException("配置表清单缺少分表元数据。请从 CSV 重新构建所有配置表。");
            }
            if (!TryGetManifestSource(clientManifest, expectedTableName, out var clientSource))
            {
                Game.FileUtility.WriteAllTextAtomically(filePath, BuildJsonTable(table), Utf8WithoutBom);
                return;
            }
    
            var editorManifest = ReadEditorManifest();
            var editorSource = GetEditorManifestSource(editorManifest, clientSource.tableName);
            if (IsCompleteSinglePartition(clientSource, expectedTableName) && IsCompleteSinglePartition(editorSource, expectedTableName))
            {
                SaveWholeSourceTable(filePath, table, clientManifest, clientSource, editorManifest, editorSource);
                return;
            }
    
            ValidateManagedPartitionForSave(filePath, table, clientSource);
            Game.FileUtility.WriteAllTextAtomically(filePath, BuildJsonTable(table), Utf8WithoutBom);
        }
    
        /// <summary>
        ///   <para>判断分表是否完整覆盖源表。</para>
        /// </summary>
        /// <param name="source">源。</param>
        /// <param name="tableName">表名称。</param>
        private static bool IsCompleteSinglePartition(ConfigTableManifestSource source, string tableName)
        {
            return source?.fields != null && source.partitions != null && source.partitions.Length == 1 &&
                IsCompleteSinglePartition(source.fields, source.partitions[0]?.tableName, source.partitions[0]?.columnIndexes, tableName);
        }
    
        /// <summary>
        ///   <para>判断分表是否完整覆盖源表。</para>
        /// </summary>
        /// <param name="source">源。</param>
        /// <param name="tableName">表名称。</param>
        private static bool IsCompleteSinglePartition(ConfigTableEditorManifestSource source, string tableName)
        {
            return source?.fields != null && source.partitions != null && source.partitions.Length == 1 &&
                IsCompleteSinglePartition(source.fields, source.partitions[0]?.tableName, source.partitions[0]?.columnIndexes, tableName);
        }
    
        /// <summary>
        ///   <para>判断分表是否完整覆盖源表。</para>
        /// </summary>
        /// <param name="fields">字段名称。</param>
        /// <param name="partitionTableName">分表名称。</param>
        /// <param name="columnIndexes">列索引。</param>
        /// <param name="tableName">表名称。</param>
        private static bool IsCompleteSinglePartition(string[] fields, string partitionTableName, int[] columnIndexes, string tableName)
        {
            if (fields == null || columnIndexes == null || columnIndexes.Length != fields.Length ||
                !string.Equals(partitionTableName, tableName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
    
            for (int columnIndex = 0; columnIndex < columnIndexes.Length; columnIndex++)
            {
                if (columnIndexes[columnIndex] != columnIndex)
                {
                    return false;
                }
            }
            return true;
        }
    
        /// <summary>
        ///   <para>保存完整源表。</para>
        /// </summary>
        /// <param name="filePath">配置表文件绝对路径。</param>
        /// <param name="table">表。</param>
        /// <param name="clientManifest">客户端清单。</param>
        /// <param name="clientSource">客户端源。</param>
        /// <param name="editorManifest">编辑器清单。</param>
        /// <param name="editorSource">编辑器源。</param>
        private static void SaveWholeSourceTable(string filePath, ConfigTableAsset table, ConfigTableManifest clientManifest,
            ConfigTableManifestSource clientSource, ConfigTableEditorManifest editorManifest, ConfigTableEditorManifestSource editorSource)
        {
            if (table.fields.Length < 2)
            {
                throw new InvalidDataException(table.tableName + "：单表至少保留 ID 和一个数据字段。");
            }
    
            var clientPartition = clientSource.partitions[0];
            var editorPartition = editorSource.partitions[0];
            clientSource.fields = table.fields;
            clientSource.types = table.types;
            clientSource.comments = table.comments;
            clientSource.references = table.references;
            editorSource.fields = table.fields;
            editorSource.types = table.types;
            editorSource.comments = table.comments;
            editorSource.references = table.references;
            clientPartition.columnIndexes = CreateIdentityIndexes(table.fields.Length);
            editorPartition.columnIndexes = CreateIdentityIndexes(table.fields.Length);
            editorSource.tags = CreateSingleTableTags(table.fields.Length, editorPartition.tag);
    
            var sourceTable = new ConfigTableAsset
            {
                tableName = clientSource.tableName,
                fields = table.fields,
                types = table.types,
                comments = table.comments,
                references = table.references,
                rows = table.rows
            };
            var clientSourceTables = new List<ConfigTableAsset>(clientManifest.sources.Length);
            for (int i = 0; i < clientManifest.sources.Length; i++)
            {
                var source = clientManifest.sources[i];
                if (source == null)
                {
                    throw new InvalidDataException("配置表清单包含空源表，索引 " +
                        i.ToString(CultureInfo.InvariantCulture) + "。");
                }
    
                clientSourceTables.Add(new ConfigTableAsset
                {
                    tableName = source.tableName,
                    fields = source.fields,
                    types = source.types,
                    comments = source.comments,
                    references = source.references
                });
            }
            ValidateGeneratedTupleReferences(clientSourceTables);

            string generatedCodeFolder = ConfigTableEditorSettings.instance.GeneratedCodeFolder;
            var generatedCodePath = Path.Combine(generatedCodeFolder, GetGeneratedCodeFileName(sourceTable.tableName));
            string generatedCode = BuildTableCode(sourceTable);
            Game.FileUtility.WriteAllTextAtomically(filePath, BuildJsonTable(table), Utf8WithoutBom);
            bool generatedCodeChanged = Game.FileUtility.WriteAllTextAtomically(generatedCodePath, generatedCode, Utf8WithBom);
            Game.FileUtility.WriteAllTextAtomically(Path.Combine(TableAssetFolder, ConfigTableModule.ManifestFileName),
                EscapeNonAscii(JsonUtility.ToJson(clientManifest)), Utf8WithoutBom);
            Game.FileUtility.WriteAllTextAtomically(GetEditorManifestPath(), EscapeNonAscii(JsonUtility.ToJson(editorManifest)), Utf8WithoutBom);
            if (generatedCodeChanged)
            {
                AssetDatabase.ImportAsset(Game.PathUtility.Normalize(generatedCodePath), ImportAssetOptions.ForceUpdate);
            }
        }
    
        /// <summary>
        ///   <para>创建连续列索引。</para>
        /// </summary>
        /// <param name="count">数量。</param>
        private static int[] CreateIdentityIndexes(int count)
        {
            var indexes = new int[count];
            for (int i = 0; i < indexes.Length; i++)
            {
                indexes[i] = i;
            }
            return indexes;
        }
    
        /// <summary>
        ///   <para>创建单个表标记。</para>
        /// </summary>
        /// <param name="columnCount">列数。</param>
        /// <param name="tag">标记。</param>
        private static string[] CreateSingleTableTags(int columnCount, string tag)
        {
            var tags = new string[columnCount];
            if (columnCount == 0)
            {
                return tags;
            }
    
            tags[0] = ClientTag + TagSeparator + ServerTag;
            var valueTag = string.IsNullOrEmpty(tag) ? ClientTag : ClientTag + TagSeparator + tag;
            for (int i = 1; i < tags.Length; i++)
            {
                tags[i] = valueTag;
            }
            return tags;
        }
    
        /// <summary>
        ///   <para>校验待保存分表的托管关系。</para>
        /// </summary>
        /// <param name="filePath">配置表文件绝对路径。</param>
        /// <param name="table">表。</param>
        /// <param name="source">源。</param>
        private static void ValidateManagedPartitionForSave(string filePath, ConfigTableAsset table,
            ConfigTableManifestSource source)
        {
            var tableName = GetTableNameFromFilePath(filePath);
            ConfigTableManifestPartition partition = null;
            for (int i = 0; i < source.partitions.Length; i++)
            {
                var candidate = source.partitions[i];
                if (candidate != null && string.Equals(candidate.tableName, tableName, StringComparison.OrdinalIgnoreCase))
                {
                    partition = candidate;
                    break;
                }
            }
            if (partition == null)
            {
                throw new InvalidDataException("配置表清单中未找到分表：" + tableName + "。");
            }
    
            var sourceTable = new ConfigTableAsset
            {
                tableName = source.tableName,
                fields = source.fields,
                types = source.types,
                comments = source.comments,
                references = source.references
            };
            ConfigTableSchema.ValidatePartitionSchema(sourceTable, partition.tableName, partition.columnIndexes, table);
            var existingTable = LoadCtable(filePath);
            var existingRows = existingTable.rows ?? Array.Empty<ConfigTableRecord>();
            var updatedRows = table.rows ?? Array.Empty<ConfigTableRecord>();
            if (updatedRows.Length != existingRows.Length)
            {
                throw new InvalidDataException(tableName + "：分表编辑仅支持修改数据值，不允许增减行。请从 CSV 修改结构后重新构建。");
            }
            for (int rowIndex = 0; rowIndex < updatedRows.Length; rowIndex++)
            {
                if (!string.Equals(updatedRows[rowIndex].id, existingRows[rowIndex].id, StringComparison.Ordinal))
                {
                    throw new InvalidDataException(tableName + "：分表编辑不允许修改 ID。请从 CSV 修改行结构后重新构建。");
                }
            }
        }
    
        /// <summary>
        ///   <para>校验已生成表名称。</para>
        /// </summary>
        /// <param name="sources">源。</param>
        private static List<ConfigTableAsset> ValidateGeneratedTableNames(List<ConfigTableSource> sources)
        {
            var clientTableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var serverTableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var clientClassNames = new HashSet<string>(StringComparer.Ordinal);
            var clientSourceTables = new List<ConfigTableAsset>(sources.Count);
            for (int i = 0; i < sources.Count; i++)
            {
                var partitions = CreatePartitions(sources[i]);
                for (int j = 0; j < partitions.Count; j++)
                {
                    var partition = partitions[j];
                    var tableNames = partition.destination == ClientTag ? clientTableNames : serverTableNames;
                    if (!tableNames.Add(partition.tableName))
                    {
                        throw new InvalidDataException("多个配置表生成了相同的分表：" + partition.tableName + "。");
                    }
    
                }
    
                var clientSourceTable = CreateClientSourceTable(sources[i]);
                if (clientSourceTable != null)
                {
                    clientSourceTables.Add(clientSourceTable);
                    var className = GetGeneratedTableTypeName(sources[i].table.tableName);
                    if (!clientClassNames.Add(className))
                    {
                        throw new InvalidDataException("配置表名称生成了相同的 C# 类：" + className + "。");
                    }
                }
            }
            ValidateGeneratedTupleReferences(clientSourceTables);
            return clientSourceTables;
        }
    
        /// <summary>
        ///   <para>校验已生成元组引用。</para>
        /// </summary>
        /// <param name="clientSourceTables">客户端源表集合。</param>
        private static void ValidateGeneratedTupleReferences(IList<ConfigTableAsset> clientSourceTables)
        {
            var tablesByName = new Dictionary<string, ConfigTableAsset>(StringComparer.Ordinal);
            for (int tableIndex = 0; tableIndex < clientSourceTables.Count; tableIndex++)
            {
                var table = clientSourceTables[tableIndex];
                if (table == null)
                {
                    throw new InvalidDataException("生成的客户端配置表为空。");
                }
    
                ConfigTableSchema.ValidateSchema(table.tableName, table.fields, table.types, table.comments, table.references);
                if (!tablesByName.TryAdd(table.tableName, table))
                {
                    throw new InvalidDataException("生成的客户端配置表名称重复：" + table.tableName + "。");
                }
            }
    
            for (int tableIndex = 0; tableIndex < clientSourceTables.Count; tableIndex++)
            {
                var table = clientSourceTables[tableIndex];
                for (int fieldIndex = 0; fieldIndex < table.fields.Length; fieldIndex++)
                {
                    string reference = table.references[fieldIndex];
                    string type = ConfigTableSchema.NormalizeType(table.types[fieldIndex]);
                    if (string.IsNullOrEmpty(reference) ||
                        !ConfigTableSchema.TryGetTupleArrayElementTypes(type, out var tupleElementTypes) ||
                        !ConfigTableSchema.TryGetReferenceTupleIndex(reference, out var tupleReferenceIndex))
                    {
                        continue;
                    }
    
                    string targetTableName = ConfigTableSchema.GetReferenceTargetName(reference);
                    if (!tablesByName.TryGetValue(targetTableName, out var targetTable))
                    {
                        throw new InvalidDataException(table.tableName + "：字段 " + table.fields[fieldIndex] +
                            " 的元组引用 " + reference + " 的目标客户端表不存在或大小写不匹配：" +
                            targetTableName + "。");
                    }
    
                    string referencedIdType = tupleElementTypes[tupleReferenceIndex];
                    string targetIdType = ConfigTableSchema.NormalizeType(targetTable.types[0]);
                    if (!string.Equals(referencedIdType, targetIdType, StringComparison.Ordinal))
                    {
                        throw new InvalidDataException(table.tableName + "：字段 " + table.fields[fieldIndex] +
                            " 的元组引用 " + reference + " 的 ID 类型不匹配：引用元素类型为 " +
                            referencedIdType + "，目标表 " + targetTableName + " 的 ID 类型为 " + targetIdType + "。");
                    }
                }
            }
        }
    
        /// <summary>
        ///   <para>创建客户端源表。</para>
        /// </summary>
        /// <param name="source">源。</param>
        private static ConfigTableAsset CreateClientSourceTable(ConfigTableSource source)
        {
            var columnIndexes = GetDestinationColumnIndexes(source, ClientTag);
            return columnIndexes.Length < 2 ? null : CreateTableWithColumns(source.table, source.table.tableName, columnIndexes);
        }
    
        /// <summary>
        ///   <para>获取目标列索引。</para>
        /// </summary>
        /// <param name="source">源。</param>
        /// <param name="destination">目标。</param>
        private static int[] GetDestinationColumnIndexes(ConfigTableSource source, string destination)
        {
            var indexes = new List<int> { 0 };
            for (int columnIndex = 1; columnIndex < source.table.fields.Length; columnIndex++)
            {
                if (ContainsTag(source.tags[columnIndex].Split(TagSeparators, StringSplitOptions.None), destination))
                {
                    indexes.Add(columnIndex);
                }
            }
            return indexes.ToArray();
        }
    
        /// <summary>
        ///   <para>创建分表。</para>
        /// </summary>
        /// <param name="source">源。</param>
        private static List<ConfigTablePartition> CreatePartitions(ConfigTableSource source)
        {
            var partitions = new List<ConfigTablePartition>();
            CreateDestinationPartitions(source, ClientTag, partitions);
            CreateDestinationPartitions(source, ServerTag, partitions);
            return partitions;
        }
    
        /// <summary>
        ///   <para>创建目标分表。</para>
        /// </summary>
        /// <param name="source">源。</param>
        /// <param name="destination">目标。</param>
        /// <param name="partitions">参与合并的分表映射。</param>
        private static void CreateDestinationPartitions(ConfigTableSource source, string destination, List<ConfigTablePartition> partitions)
        {
            var columnsByTag = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            for (int columnIndex = 1; columnIndex < source.table.fields.Length; columnIndex++)
            {
                var tags = source.tags[columnIndex].Split(TagSeparators, StringSplitOptions.None);
                if (!ContainsTag(tags, destination))
                {
                    continue;
                }
    
                var customTags = new List<string>();
                for (int tagIndex = 0; tagIndex < tags.Length; tagIndex++)
                {
                    if (!string.Equals(tags[tagIndex], ClientTag, StringComparison.Ordinal) && !string.Equals(tags[tagIndex], ServerTag, StringComparison.Ordinal))
                    {
                        customTags.Add(tags[tagIndex]);
                    }
                }
    
                if (customTags.Count == 0)
                {
                    AddPartitionColumn(columnsByTag, string.Empty, columnIndex);
                }
                else
                {
                    for (int tagIndex = 0; tagIndex < customTags.Count; tagIndex++)
                    {
                        AddPartitionColumn(columnsByTag, customTags[tagIndex], columnIndex);
                    }
                }
            }
    
            var tagsForDestination = new List<string>(columnsByTag.Keys);
            tagsForDestination.Sort((left, right) =>
            {
                if (left.Length == 0)
                {
                    return right.Length == 0 ? 0 : -1;
                }
                return right.Length == 0 ? 1 : string.CompareOrdinal(left, right);
            });
            for (int i = 0; i < tagsForDestination.Count; i++)
            {
                var tag = tagsForDestination[i];
                var columns = columnsByTag[tag];
                var columnIndexes = new int[columns.Count + 1];
                columnIndexes[0] = 0;
                for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
                {
                    columnIndexes[columnIndex + 1] = columns[columnIndex];
                }
    
                partitions.Add(new ConfigTablePartition
                {
                    source = source,
                    tableName = GetPartitionTableName(source.table.tableName, tag),
                    destination = destination,
                    tag = tag,
                    columnIndexes = columnIndexes
                });
            }
        }
    
        /// <summary>
        ///   <para>包含标记。</para>
        /// </summary>
        /// <param name="tags">标记。</param>
        /// <param name="tag">标记。</param>
        private static bool ContainsTag(string[] tags, string tag)
        {
            for (int i = 0; i < tags.Length; i++)
            {
                if (string.Equals(tags[i], tag, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
    
        /// <summary>
        ///   <para>添加分表列。</para>
        /// </summary>
        /// <param name="columnsByTag">标记到列索引的映射。</param>
        /// <param name="tag">标记。</param>
        /// <param name="columnIndex">字段索引。</param>
        private static void AddPartitionColumn(Dictionary<string, List<int>> columnsByTag, string tag, int columnIndex)
        {
            if (!columnsByTag.TryGetValue(tag, out var columns))
            {
                columns = new List<int>();
                columnsByTag.Add(tag, columns);
            }
            columns.Add(columnIndex);
        }
    
        /// <summary>
        ///   <para>获取分表名称。</para>
        /// </summary>
        /// <param name="sourceTableName">输出源表名称。</param>
        /// <param name="tag">标记。</param>
        private static string GetPartitionTableName(string sourceTableName, string tag) => string.IsNullOrEmpty(tag) ? sourceTableName : sourceTableName + PartitionSeparator + tag;
    
        /// <summary>
        ///   <para>按指定列创建表。</para>
        /// </summary>
        /// <param name="source">源。</param>
        /// <param name="tableName">表名称。</param>
        /// <param name="columnIndexes">列索引。</param>
        private static ConfigTableAsset CreateTableWithColumns(ConfigTableAsset source, string tableName, int[] columnIndexes)
        {
            var rows = source.rows ?? Array.Empty<ConfigTableRecord>();
            var partitionRows = new ConfigTableRecord[rows.Length];
            for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                var values = SelectColumns(rows[rowIndex].values, columnIndexes);
                partitionRows[rowIndex] = new ConfigTableRecord
                {
                    id = values[0],
                    values = values
                };
            }
    
            return new ConfigTableAsset
            {
                tableName = tableName,
                fields = SelectColumns(source.fields, columnIndexes),
                types = SelectColumns(source.types, columnIndexes),
                comments = SelectColumns(source.comments, columnIndexes),
                references = SelectColumns(source.references, columnIndexes),
                rows = partitionRows
            };
        }
    
        /// <summary>
        ///   <para>选择列。</para>
        /// </summary>
        /// <param name="values">值。</param>
        /// <param name="columnIndexes">列索引。</param>
        private static string[] SelectColumns(string[] values, int[] columnIndexes)
        {
            var selected = new string[columnIndexes.Length];
            for (int i = 0; i < columnIndexes.Length; i++)
            {
                selected[i] = values[columnIndexes[i]];
            }
            return selected;
        }
    
        /// <summary>
        ///   <para>写入已构建表。</para>
        /// </summary>
        /// <param name="sources">源。</param>
        private static void WriteBuiltTables(List<ConfigTableSource> sources)
        {
            var clientSourceTables = ValidateGeneratedTableNames(sources);
            string generatedCodeFolder = ConfigTableEditorSettings.instance.GeneratedCodeFolder;
            string serverTableAssetFolder = GetServerTableAssetFolder();
            var previousClientManifest = ReadManifestIfPresent(TableAssetFolder);
            var previousServerManifest = ReadManifestIfPresent(serverTableAssetFolder);
            var clientPartitions = new List<ConfigTablePartition>();
            var serverPartitions = new List<ConfigTablePartition>();
            for (int i = 0; i < sources.Count; i++)
            {
                var partitions = CreatePartitions(sources[i]);
                for (int j = 0; j < partitions.Count; j++)
                {
                    if (partitions[j].destination == ClientTag)
                    {
                        clientPartitions.Add(partitions[j]);
                    }
                    else
                    {
                        serverPartitions.Add(partitions[j]);
                    }
                }
            }
    
            var generatedTableCodes = new string[clientSourceTables.Count];
            for (int i = 0; i < clientSourceTables.Count; i++)
            {
                generatedTableCodes[i] = BuildTableCode(clientSourceTables[i]);
            }
            var sourceManifests = CreateSourceManifests(sources, clientPartitions);
            EnsureDirectory(TableAssetFolder);
            EnsureDirectory(serverTableAssetFolder);
            EnsureDirectory(generatedCodeFolder);
    
            for (int i = 0; i < clientPartitions.Count; i++)
            {
                var partition = clientPartitions[i];
                var table = CreateTableWithColumns(partition.source.table, partition.tableName, partition.columnIndexes);
                Game.FileUtility.WriteAllTextAtomically(Path.Combine(TableAssetFolder, table.tableName + "." + ConfigTableModule.FileExtension), BuildJsonTable(table), Utf8WithoutBom);
            }
            for (int i = 0; i < clientSourceTables.Count; i++)
            {
                var clientSourceTable = clientSourceTables[i];
                Game.FileUtility.WriteAllTextAtomically(Path.Combine(generatedCodeFolder, GetGeneratedCodeFileName(clientSourceTable.tableName)),
                    generatedTableCodes[i], Utf8WithBom);
            }
            for (int i = 0; i < serverPartitions.Count; i++)
            {
                var partition = serverPartitions[i];
                var table = CreateTableWithColumns(partition.source.table, partition.tableName, partition.columnIndexes);
                Game.FileUtility.WriteAllTextAtomically(Path.Combine(serverTableAssetFolder, table.tableName + "." + ConfigTableModule.FileExtension), BuildJsonTable(table), Utf8WithoutBom);
            }
    
            WriteTableManifest(TableAssetFolder, sourceManifests, clientPartitions);
            WriteTableManifest(serverTableAssetFolder, null, serverPartitions);
            WriteEditorManifest(sources, clientPartitions, serverPartitions);
            DeleteStaleManagedTables(TableAssetFolder, previousClientManifest, clientPartitions);
            DeleteStaleManagedTables(serverTableAssetFolder, previousServerManifest, serverPartitions);
            DeleteStaleGeneratedCode(generatedCodeFolder, previousClientManifest, clientSourceTables);
        }
    
        /// <summary>
        ///   <para>删除过期托管表。</para>
        /// </summary>
        /// <param name="folder">目录。</param>
        /// <param name="previousManifest">之前清单。</param>
        /// <param name="partitions">参与合并的分表映射。</param>
        private static void DeleteStaleManagedTables(string folder, ConfigTableManifest previousManifest,
            List<ConfigTablePartition> partitions)
        {
            if (previousManifest?.tables == null)
            {
                return;
            }
    
            var currentTableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < partitions.Count; i++)
            {
                currentTableNames.Add(partitions[i].tableName);
            }
    
            for (int i = 0; i < previousManifest.tables.Length; i++)
            {
                var tableName = previousManifest.tables[i];
                if (!string.IsNullOrEmpty(tableName) && !currentTableNames.Contains(tableName))
                {
                    DeleteFileAndMeta(Path.Combine(folder, tableName + "." + ConfigTableModule.FileExtension));
                }
            }
        }
    
        /// <summary>
        ///   <para>删除过期已生成代码。</para>
        /// </summary>
        /// <param name="generatedCodeFolder">已生成代码目录。</param>
        /// <param name="previousManifest">之前清单。</param>
        /// <param name="clientSourceTables">客户端源表集合。</param>
        private static void DeleteStaleGeneratedCode(string generatedCodeFolder, ConfigTableManifest previousManifest,
            IList<ConfigTableAsset> clientSourceTables)
        {
            if (previousManifest == null)
            {
                return;
            }
    
            var currentGeneratedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < clientSourceTables.Count; i++)
            {
                currentGeneratedFileNames.Add(GetGeneratedCodeFileName(clientSourceTables[i].tableName));
            }
    
            var previousGeneratedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (previousManifest.tables != null)
            {
                for (int i = 0; i < previousManifest.tables.Length; i++)
                {
                    var tableName = previousManifest.tables[i];
                    if (!string.IsNullOrEmpty(tableName))
                    {
                        previousGeneratedFileNames.Add(GetGeneratedCodeFileName(tableName));
                    }
                }
            }
            if (previousManifest.sources != null)
            {
                for (int i = 0; i < previousManifest.sources.Length; i++)
                {
                    var source = previousManifest.sources[i];
                    if (source != null && !string.IsNullOrEmpty(source.tableName))
                    {
                        previousGeneratedFileNames.Add(GetGeneratedCodeFileName(source.tableName));
                    }
                }
            }
    
            foreach (var fileName in previousGeneratedFileNames)
            {
                if (!currentGeneratedFileNames.Contains(fileName))
                {
                    DeleteFileAndMeta(Path.Combine(generatedCodeFolder, fileName));
                }
            }
        }
    
        /// <summary>
        ///   <para>删除文件和元数据。</para>
        /// </summary>
        /// <param name="path">路径。</param>
        private static void DeleteFileAndMeta(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                var metaPath = path + ".meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }
            }
            catch (Exception exception)
            {
                Verve.Game.LogWarning("清理过期配置表输出失败：" + path + "\n" + exception.Message);
            }
        }
    
        /// <summary>
        ///   <para>移动文件和元数据。</para>
        /// </summary>
        /// <param name="sourcePath">源路径。</param>
        /// <param name="destinationPath">目标路径。</param>
        private static void MoveFileAndMeta(string sourcePath, string destinationPath)
        {
            File.Move(sourcePath, destinationPath);
            string sourceMetaPath = sourcePath + ".meta";
            if (File.Exists(sourceMetaPath))
            {
                File.Move(sourceMetaPath, destinationPath + ".meta");
            }
        }
    
        /// <summary>
        ///   <para>创建源清单。</para>
        /// </summary>
        /// <param name="sources">源。</param>
        /// <param name="clientPartitions">客户端分表。</param>
        private static ConfigTableManifestSource[] CreateSourceManifests(List<ConfigTableSource> sources, List<ConfigTablePartition> clientPartitions)
        {
            var sourceManifests = new List<ConfigTableManifestSource>();
            for (int sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
            {
                var source = sources[sourceIndex];
                var sourceColumnIndexes = GetDestinationColumnIndexes(source, ClientTag);
                if (sourceColumnIndexes.Length < 2)
                {
                    continue;
                }
    
                var clientSourceTable = CreateTableWithColumns(source.table, source.table.tableName, sourceColumnIndexes);
                var clientColumnIndexBySourceIndex = new Dictionary<int, int>();
                for (int columnIndex = 0; columnIndex < sourceColumnIndexes.Length; columnIndex++)
                {
                    clientColumnIndexBySourceIndex.Add(sourceColumnIndexes[columnIndex], columnIndex);
                }
    
                var sourcePartitions = new List<ConfigTableManifestPartition>();
                for (int partitionIndex = 0; partitionIndex < clientPartitions.Count; partitionIndex++)
                {
                    var partition = clientPartitions[partitionIndex];
                    if (!ReferenceEquals(partition.source, source))
                    {
                        continue;
                    }
    
                    var columnIndexes = new int[partition.columnIndexes.Length];
                    for (int columnIndex = 0; columnIndex < columnIndexes.Length; columnIndex++)
                    {
                        if (!clientColumnIndexBySourceIndex.TryGetValue(partition.columnIndexes[columnIndex], out columnIndexes[columnIndex]))
                        {
                            throw new InvalidDataException(source.table.tableName + "：客户端分表字段映射无效。");
                        }
                    }
                    sourcePartitions.Add(new ConfigTableManifestPartition
                    {
                        tableName = partition.tableName,
                        columnIndexes = columnIndexes
                    });
                }
    
                sourceManifests.Add(new ConfigTableManifestSource
                {
                    tableName = clientSourceTable.tableName,
                    fields = clientSourceTable.fields,
                    types = clientSourceTable.types,
                    comments = clientSourceTable.comments,
                    references = clientSourceTable.references,
                    partitions = sourcePartitions.ToArray()
                });
            }
            return sourceManifests.ToArray();
        }
    
        /// <summary>
        ///   <para>写入编辑器清单。</para>
        /// </summary>
        /// <param name="sources">源。</param>
        /// <param name="clientPartitions">客户端分表。</param>
        /// <param name="serverPartitions">服务端分表。</param>
        private static void WriteEditorManifest(List<ConfigTableSource> sources, List<ConfigTablePartition> clientPartitions,
            List<ConfigTablePartition> serverPartitions)
        {
            var sourceManifests = new ConfigTableEditorManifestSource[sources.Count];
            for (int sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
            {
                var source = sources[sourceIndex];
                var sourcePartitions = new List<ConfigTableEditorManifestPartition>();
                AddEditorManifestPartitions(sourcePartitions, clientPartitions, source);
                AddEditorManifestPartitions(sourcePartitions, serverPartitions, source);
                sourceManifests[sourceIndex] = new ConfigTableEditorManifestSource
                {
                    tableName = source.table.tableName,
                    fields = source.table.fields,
                    types = source.table.types,
                    comments = source.table.comments,
                    references = source.table.references,
                    tags = source.tags,
                    partitions = sourcePartitions.ToArray()
                };
            }
    
            var manifest = new ConfigTableEditorManifest { sources = sourceManifests };
            Game.FileUtility.WriteAllTextAtomically(GetEditorManifestPath(), EscapeNonAscii(JsonUtility.ToJson(manifest)), Utf8WithoutBom);
        }
    
        /// <summary>
        ///   <para>添加编辑器清单分表。</para>
        /// </summary>
        /// <param name="target">目标。</param>
        /// <param name="partitions">参与合并的分表映射。</param>
        /// <param name="source">源。</param>
        private static void AddEditorManifestPartitions(List<ConfigTableEditorManifestPartition> target,
            List<ConfigTablePartition> partitions, ConfigTableSource source)
        {
            for (int partitionIndex = 0; partitionIndex < partitions.Count; partitionIndex++)
            {
                var partition = partitions[partitionIndex];
                if (!ReferenceEquals(partition.source, source))
                {
                    continue;
                }
    
                target.Add(new ConfigTableEditorManifestPartition
                {
                    tableName = partition.tableName,
                    destination = partition.destination,
                    tag = partition.tag,
                    columnIndexes = (int[])partition.columnIndexes.Clone()
                });
            }
        }
    
        /// <summary>
        ///   <para>写入表清单。</para>
        /// </summary>
        /// <param name="folder">目录。</param>
        /// <param name="sources">源。</param>
        /// <param name="partitions">参与合并的分表映射。</param>
        private static void WriteTableManifest(string folder, ConfigTableManifestSource[] sources, List<ConfigTablePartition> partitions)
        {
            var tableNames = new string[partitions.Count];
            for (int i = 0; i < partitions.Count; i++)
            {
                tableNames[i] = partitions[i].tableName;
            }
    
            if (sources == null)
            {
                var tableManifest = new ConfigTableTableManifest { tables = tableNames };
                Game.FileUtility.WriteAllTextAtomically(Path.Combine(folder, ConfigTableModule.ManifestFileName), EscapeNonAscii(JsonUtility.ToJson(tableManifest)), Utf8WithoutBom);
                return;
            }
    
            var manifest = new ConfigTableManifest
            {
                tables = tableNames,
                sources = sources
            };
            Game.FileUtility.WriteAllTextAtomically(Path.Combine(folder, ConfigTableModule.ManifestFileName), EscapeNonAscii(JsonUtility.ToJson(manifest)), Utf8WithoutBom);
        }
    
        /// <summary>
        ///   <para>构建 JSON 表。</para>
        /// </summary>
        /// <param name="table">表。</param>
        private static string BuildJsonTable(ConfigTableAsset table) => EscapeNonAscii(JsonUtility.ToJson(table));
    
        /// <summary>
        ///   <para>写入 CSV 表。</para>
        /// </summary>
        /// <param name="source">源。</param>
        private static void WriteCsvTable(ConfigTableSource source)
        {
            var builder = new StringBuilder();
    
            Game.CsvUtility.AppendRow(builder, source.table.fields);
            Game.CsvUtility.AppendRow(builder, source.table.types);
            Game.CsvUtility.AppendRow(builder, source.table.comments);
            Game.CsvUtility.AppendRow(builder, source.table.references);
            Game.CsvUtility.AppendRow(builder, source.tags);
            for (int i = 0; i < source.table.rows.Length; i++)
            {
                Game.CsvUtility.AppendRow(builder, source.table.rows[i].values);
            }
    
            string outputPath = GetCsvPath(source.table.tableName);
            Game.FileUtility.WriteAllTextAtomically(outputPath, builder.ToString(), Utf8WithBom);
        }
    
        /// <summary>
        ///   <para>读取客户端清单。</para>
        /// </summary>
        private static ConfigTableManifest ReadClientManifest()
        {
            var manifest = ReadManifest(TableAssetFolder, true);
            if (manifest.tables == null || manifest.sources == null)
            {
                throw new InvalidDataException("配置表清单缺少分表元数据。请从 CSV 重新构建所有配置表。");
            }
            return manifest;
        }
    
        /// <summary>
        ///   <para>读取已有清单；文件不存在时返回空。</para>
        /// </summary>
        /// <param name="folder">目录。</param>
        private static ConfigTableManifest ReadManifestIfPresent(string folder) => File.Exists(Path.Combine(folder, ConfigTableModule.ManifestFileName)) ? ReadManifest(folder, false) : null;
    
        /// <summary>
        ///   <para>读取清单。</para>
        /// </summary>
        /// <param name="folder">目录。</param>
        /// <param name="required">必需的。</param>
        private static ConfigTableManifest ReadManifest(string folder, bool required)
        {
            var path = Path.Combine(folder, ConfigTableModule.ManifestFileName);
            if (!File.Exists(path))
            {
                if (required)
                {
                    throw new InvalidDataException("未找到配置表清单：" + path + "。请从 CSV 重新构建所有配置表。");
                }
                return null;
            }
    
            try
            {
                var manifest = JsonUtility.FromJson<ConfigTableManifest>(ReadUtf8Text(path));
                if (manifest == null)
                {
                    throw new InvalidDataException("配置表清单为空：" + path);
                }
                ValidateManifestTableNames(manifest, path);
                return manifest;
            }
            catch (Exception ex) when (!(ex is InvalidDataException))
            {
                throw new InvalidDataException("配置表清单无效：" + path + "\n" + ex.Message);
            }
        }
    
        /// <summary>
        ///   <para>校验清单表名称。</para>
        /// </summary>
        /// <param name="manifest">清单。</param>
        /// <param name="manifestPath">清单路径。</param>
        private static void ValidateManifestTableNames(ConfigTableManifest manifest, string manifestPath)
        {
            var tableNames = manifest.tables;
            for (int i = 0; tableNames != null && i < tableNames.Length; i++)
            {
                var tableName = tableNames[i];
                if (!string.IsNullOrEmpty(tableName) && !ConfigTableSchema.IsValidTableFileName(tableName))
                {
                    throw new InvalidDataException("配置表清单包含无效表名：" + tableName +
                        "。\n清单路径：" + manifestPath + "\n索引：" +
                        i.ToString(CultureInfo.InvariantCulture));
                }
            }
    
            var sources = manifest.sources;
            if (sources == null)
            {
                return;
            }
            for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
            {
                var source = sources[sourceIndex];
                if (source == null || !ConfigTableSchema.IsValidTableFileName(source.tableName))
                {
                    throw new InvalidDataException("配置表清单包含无效源表名，索引 " +
                        sourceIndex.ToString(CultureInfo.InvariantCulture) + "。\n清单路径：" + manifestPath);
                }
    
                var partitions = source.partitions;
                if (partitions == null)
                {
                    continue;
                }
                for (int partitionIndex = 0; partitionIndex < partitions.Length; partitionIndex++)
                {
                    var partition = partitions[partitionIndex];
                    if (partition == null || !ConfigTableSchema.IsValidTableFileName(partition.tableName))
                    {
                        throw new InvalidDataException("配置表清单包含无效分表名，源表索引 " +
                            sourceIndex.ToString(CultureInfo.InvariantCulture) + "，分表索引 " +
                            partitionIndex.ToString(CultureInfo.InvariantCulture) + "。\n清单路径：" + manifestPath);
                    }
                }
            }
        }
    
        /// <summary>
        ///   <para>获取源表名称。</para>
        /// </summary>
        /// <param name="clientTableName">客户端表名称。</param>
        private static string GetSourceTableName(string clientTableName) => GetManifestSource(ReadClientManifest(), clientTableName).tableName;
    
        /// <summary>
        ///   <para>获取清单源。</para>
        /// </summary>
        /// <param name="manifest">清单。</param>
        /// <param name="clientTableName">客户端表名称。</param>
        private static ConfigTableManifestSource GetManifestSource(ConfigTableManifest manifest, string clientTableName)
        {
            if (TryGetManifestSource(manifest, clientTableName, out var source))
            {
                return source;
            }
    
            throw new InvalidDataException("配置表清单中未找到分表映射：" + clientTableName + "。请从 CSV 重新构建所有配置表。");
        }
    
        /// <summary>
        ///   <para>尝试获取清单源。</para>
        /// </summary>
        /// <param name="manifest">清单。</param>
        /// <param name="clientTableName">客户端表名称。</param>
        /// <param name="source">源。</param>
        private static bool TryGetManifestSource(ConfigTableManifest manifest, string clientTableName,
            out ConfigTableManifestSource source)
        {
            source = null;
            var sources = manifest?.sources;
            if (sources == null)
            {
                return false;
            }
    
            for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
            {
                var candidate = sources[sourceIndex];
                if (candidate?.partitions == null)
                {
                    continue;
                }
                for (int partitionIndex = 0; partitionIndex < candidate.partitions.Length; partitionIndex++)
                {
                    var partition = candidate.partitions[partitionIndex];
                    if (partition != null && string.Equals(partition.tableName, clientTableName, StringComparison.OrdinalIgnoreCase))
                    {
                        source = candidate;
                        return true;
                    }
                }
            }
            return false;
        }
    
        /// <summary>
        ///   <para>读取编辑器清单。</para>
        /// </summary>
        private static ConfigTableEditorManifest ReadEditorManifest()
        {
            var path = GetEditorManifestPath();
            if (!File.Exists(path))
            {
                throw new InvalidDataException("未找到配置表编辑元数据：" + path + "。请先从 CSV 构建所有配置表。");
            }
    
            try
            {
                var manifest = JsonUtility.FromJson<ConfigTableEditorManifest>(ReadUtf8Text(path));
                if (manifest?.sources == null)
                {
                    throw new InvalidDataException("配置表编辑元数据缺少源表信息。");
                }
                ValidateEditorManifestTableNames(manifest, path);
                return manifest;
            }
            catch (Exception ex) when (!(ex is InvalidDataException))
            {
                throw new InvalidDataException("无效的配置表编辑元数据：" + path + "\n" + ex.Message);
            }
        }
    
        /// <summary>
        ///   <para>获取编辑器清单源。</para>
        /// </summary>
        /// <param name="manifest">清单。</param>
        /// <param name="sourceTableName">输出源表名称。</param>
        private static ConfigTableEditorManifestSource GetEditorManifestSource(ConfigTableEditorManifest manifest, string sourceTableName)
        {
            for (int sourceIndex = 0; sourceIndex < manifest.sources.Length; sourceIndex++)
            {
                var source = manifest.sources[sourceIndex];
                if (source != null && string.Equals(source.tableName, sourceTableName, StringComparison.OrdinalIgnoreCase))
                {
                    return source;
                }
            }
    
            throw new InvalidDataException("配置表编辑元数据中未找到源表：" + sourceTableName + "。请从 CSV 重新构建所有配置表。");
        }
    
        /// <summary>
        ///   <para>合并源。</para>
        /// </summary>
        /// <param name="sourceManifest">源清单。</param>
        private static ConfigTableSource MergeSource(ConfigTableEditorManifestSource sourceManifest)
        {
            if (sourceManifest == null || string.IsNullOrEmpty(sourceManifest.tableName))
            {
                throw new InvalidDataException("配置表编辑元数据中的源表无效。");
            }
    
            var fields = NormalizeSchemaRow(sourceManifest.fields, sourceManifest.fields?.Length ?? 0, sourceManifest.tableName, "字段");
            var types = NormalizeTypeRow(sourceManifest.types, fields.Length, sourceManifest.tableName, "类型");
            var comments = NormalizeSchemaRow(sourceManifest.comments, fields.Length, sourceManifest.tableName, "注释");
            var references = NormalizeSchemaRow(sourceManifest.references, fields.Length, sourceManifest.tableName, "引用");
            var tags = NormalizeTagRow(sourceManifest.tags, fields.Length, sourceManifest.tableName);
            if (sourceManifest.partitions == null || sourceManifest.partitions.Length == 0)
            {
                throw new InvalidDataException(sourceManifest.tableName + "：没有可合并的分表。");
            }
    
            var sourceTable = new ConfigTableAsset
            {
                tableName = sourceManifest.tableName,
                fields = fields,
                types = types,
                comments = comments,
                references = references
            };
            var partitions = new ConfigTableMergePartition[sourceManifest.partitions.Length];
            for (int partitionIndex = 0; partitionIndex < sourceManifest.partitions.Length; partitionIndex++)
            {
                var partition = sourceManifest.partitions[partitionIndex];
                ValidateEditorManifestPartition(sourceManifest, partition);
                var folder = partition.destination == ServerTag ? GetServerTableAssetFolder() : TableAssetFolder;
                var table = LoadCtable(Path.Combine(folder,
                    partition.tableName + "." + ConfigTableModule.FileExtension));
                partitions[partitionIndex] = new ConfigTableMergePartition(
                    partition.tableName, table, partition.columnIndexes);
            }
    
            var mergedTable = ConfigTableSchema.MergePartitions(sourceTable, partitions);
            return new ConfigTableSource { table = mergedTable, tags = tags };
        }
    
        /// <summary>
        ///   <para>校验编辑器清单分表。</para>
        /// </summary>
        /// <param name="source">源。</param>
        /// <param name="partition">分表数据。</param>
        private static void ValidateEditorManifestPartition(ConfigTableEditorManifestSource source,
            ConfigTableEditorManifestPartition partition)
        {
            if (partition == null || (partition.destination != ClientTag && partition.destination != ServerTag) ||
                !ConfigTableSchema.IsValidTableFileName(partition.tableName))
            {
                throw new InvalidDataException(source.tableName + "：分表元数据无效。");
            }
            var expectedTableName = GetPartitionTableName(source.tableName, partition.tag ?? string.Empty);
            if (!string.Equals(partition.tableName, expectedTableName, StringComparison.Ordinal))
            {
                throw new InvalidDataException(source.tableName + "：分表名称无效：" + partition.tableName + "。");
            }
        }
    
        /// <summary>
        ///   <para>校验编辑器清单表名称。</para>
        /// </summary>
        /// <param name="manifest">清单。</param>
        /// <param name="manifestPath">清单路径。</param>
        private static void ValidateEditorManifestTableNames(ConfigTableEditorManifest manifest, string manifestPath)
        {
            for (int sourceIndex = 0; sourceIndex < manifest.sources.Length; sourceIndex++)
            {
                var source = manifest.sources[sourceIndex];
                if (source == null || !ConfigTableSchema.IsValidTableFileName(source.tableName))
                {
                    throw new InvalidDataException("配置表编辑元数据包含无效源表名，索引 " +
                        sourceIndex.ToString(CultureInfo.InvariantCulture) + "。\n元数据路径：" + manifestPath);
                }
    
                var partitions = source.partitions;
                if (partitions == null)
                {
                    continue;
                }
                for (int partitionIndex = 0; partitionIndex < partitions.Length; partitionIndex++)
                {
                    var partition = partitions[partitionIndex];
                    if (partition == null || !ConfigTableSchema.IsValidTableFileName(partition.tableName))
                    {
                        throw new InvalidDataException("配置表编辑元数据包含无效分表名，源表索引 " +
                            sourceIndex.ToString(CultureInfo.InvariantCulture) + "，分表索引 " +
                            partitionIndex.ToString(CultureInfo.InvariantCulture) + "。\n元数据路径：" + manifestPath);
                    }
                }
            }
        }
    

    

    

    
        /// <summary>
        ///   <para>读取 CSV 文本。</para>
        /// </summary>
        /// <param name="filePath">配置表文件绝对路径。</param>
        private static string ReadCsvText(string filePath) => DecodeCsvText(File.ReadAllBytes(filePath));
    
        /// <summary>
        ///   <para>读取 UTF8 文本。</para>
        /// </summary>
        /// <param name="filePath">配置表文件绝对路径。</param>
        private static string ReadUtf8Text(string filePath)
        {
            try
            {
                return DecodeUtf8(File.ReadAllBytes(filePath));
            }
            catch (DecoderFallbackException)
            {
                throw new InvalidDataException("文件必须使用 UTF-8 编码（可带或不带 BOM）：" + filePath);
            }
        }
    
        /// <summary>
        ///   <para>解码 CSV 文本。</para>
        /// </summary>
        /// <param name="bytes">字节。</param>
        private static string DecodeCsvText(byte[] bytes)
        {
            if (bytes.Length >= 2 && bytes[0] == 0xff && bytes[1] == 0xfe)
            {
                return new UnicodeEncoding(false, true, true).GetString(bytes, 2, bytes.Length - 2);
            }
            if (bytes.Length >= 2 && bytes[0] == 0xfe && bytes[1] == 0xff)
            {
                return new UnicodeEncoding(true, true, true).GetString(bytes, 2, bytes.Length - 2);
            }
            return DecodeUtf8(bytes);
        }
    
        /// <summary>
        ///   <para>解码 UTF8。</para>
        /// </summary>
        /// <param name="bytes">字节。</param>
        private static string DecodeUtf8(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf)
            {
                return Utf8WithoutBom.GetString(bytes, 3, bytes.Length - 3);
            }
            return Utf8WithoutBom.GetString(bytes);
        }
    

    
        /// <summary>
        ///   <para>从文件路径读取表名。</para>
        /// </summary>
        /// <param name="filePath">配置表文件绝对路径。</param>
        private static string GetTableNameFromFilePath(string filePath)
        {
            var tableName = Path.GetFileNameWithoutExtension(filePath);
            if (string.IsNullOrEmpty(tableName) || tableName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new InvalidDataException("无效的配置表文件名：" + filePath);
            }
            return tableName;
        }
    
        /// <summary>
        ///   <para>转义非 ASCII。</para>
        /// </summary>
        /// <param name="text">要压缩的字符串。</param>
        private static string EscapeNonAscii(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            int startIndex = text[0] == '\ufeff' ? 1 : 0;
            var builder = new StringBuilder(text.Length - startIndex);
            for (int i = startIndex; i < text.Length; i++)
            {
                char c = text[i];
                if (c > 0x7f)
                {
                    builder.Append("\\u");
                    builder.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                }
                else
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }
    

    

    
        /// <summary>
        ///   <para>规范化结构行。</para>
        /// </summary>
        /// <param name="row">表记录。</param>
        /// <param name="count">数量。</param>
        /// <param name="tableName">表名称。</param>
        /// <param name="rowName">行名称。</param>
        private static string[] NormalizeSchemaRow(string[] row, int count, string tableName, string rowName)
        {
            row = TrimExcelTrailingEmptyColumns(row, count);
            ValidateRowColumnCount(row, count, tableName, rowName);
            var result = new string[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = TrimSchemaValue(row[i]);
            }
            return result;
        }
    
        /// <summary>
        ///   <para>裁剪结构值。</para>
        /// </summary>
        /// <param name="value">值。</param>
        private static string TrimSchemaValue(string value) => (value ?? string.Empty).Trim().Trim('\ufeff', '\u200b', '\u00a0').Trim();
    
        /// <summary>
        ///   <para>规范化类型行。</para>
        /// </summary>
        /// <param name="row">表记录。</param>
        /// <param name="count">数量。</param>
        /// <param name="tableName">表名称。</param>
        /// <param name="rowName">行名称。</param>
        private static string[] NormalizeTypeRow(string[] row, int count, string tableName, string rowName)
        {
            var types = NormalizeSchemaRow(row, count, tableName, rowName);
            for (int i = 0; i < types.Length; i++)
            {
                types[i] = ConfigTableSchema.NormalizeType(types[i]);
            }
            return types;
        }
    
        /// <summary>
        ///   <para>规范化数据行。</para>
        /// </summary>
        /// <param name="row">表记录。</param>
        /// <param name="count">数量。</param>
        /// <param name="tableName">表名称。</param>
        /// <param name="location">记录位置描述。</param>
        private static string[] NormalizeDataRow(string[] row, int count, string tableName, string location)
        {
            row = TrimExcelTrailingEmptyColumns(row, count);
            ValidateRowColumnCount(row, count, tableName, location);
            var result = new string[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = row[i] ?? string.Empty;
            }
            return result;
        }
    
        /// <summary>
        ///   <para>裁剪 Excel 末尾空列。</para>
        /// </summary>
        /// <param name="row">表记录。</param>
        /// <param name="expectedCount">预期数量。</param>
        private static string[] TrimExcelTrailingEmptyColumns(string[] row, int expectedCount)
        {
            if (row == null || row.Length <= expectedCount)
            {
                return row;
            }
    
            var trimmedCount = row.Length;
            while (trimmedCount > expectedCount && string.IsNullOrWhiteSpace(row[trimmedCount - 1]))
            {
                trimmedCount--;
            }
            if (trimmedCount == row.Length)
            {
                return row;
            }
    
            var trimmedRow = new string[trimmedCount];
            Array.Copy(row, trimmedRow, trimmedCount);
            return trimmedRow;
        }
    
        /// <summary>
        ///   <para>校验行列数。</para>
        /// </summary>
        /// <param name="row">表记录。</param>
        /// <param name="expectedCount">预期数量。</param>
        /// <param name="tableName">表名称。</param>
        /// <param name="location">记录位置描述。</param>
        private static void ValidateRowColumnCount(string[] row, int expectedCount, string tableName, string location)
        {
            var actualCount = row?.Length ?? 0;
            if (actualCount != expectedCount)
            {
                throw new InvalidDataException(tableName + " " + location + "：应有 " + expectedCount.ToString(CultureInfo.InvariantCulture) + " 列，但实际找到 " + actualCount.ToString(CultureInfo.InvariantCulture) + " 列。");
            }
        }
    
        /// <summary>
        ///   <para>判断是否为空行。</para>
        /// </summary>
        /// <param name="row">表记录。</param>
        private static bool IsEmptyRow(string[] row)
        {
            for (int i = 0; i < row.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(row[i]))
                {
                    return false;
                }
            }
            return true;
        }
    
        /// <summary>
        ///   <para>获取 CSV 根节点。</para>
        /// </summary>
        private static string GetCsvRoot() => ConfigTableEditorSettings.instance.GetProjectAbsolutePath(ConfigTableModule.TablesFolder);
    
        /// <summary>
        ///   <para>获取服务端表资源目录。</para>
        /// </summary>
        private static string GetServerTableAssetFolder() => ConfigTableEditorSettings.instance.GetProjectAbsolutePath(ConfigTableEditorSettings.instance.ServerTableFolder);
    
        /// <summary>
        ///   <para>获取 CSV 路径。</para>
        /// </summary>
        /// <param name="tableName">表名称。</param>
        private static string GetCsvPath(string tableName) => Path.Combine(GetCsvRoot(), tableName + CsvExtension);
    
        /// <summary>
        ///   <para>获取编辑器清单路径。</para>
        /// </summary>
        private static string GetEditorManifestPath() => Path.Combine(GetCsvRoot(), EditorManifestFileName);
    
        /// <summary>
        ///   <para>字段代码结构。</para>
        /// </summary>
        private sealed class FieldCodeSchema
        {
            /// <summary>
            ///   <para>索引。</para>
            /// </summary>
            public int index;
    
            /// <summary>
            ///   <para>成员名称。</para>
            /// </summary>
            public string memberName;
    
            /// <summary>
            ///   <para>注释。</para>
            /// </summary>
            public string comment;
    
            /// <summary>
            ///   <para>C#类型。</para>
            /// </summary>
            public string csType;
    
            /// <summary>
            ///   <para>取值器名称。</para>
            /// </summary>
            public string getterName;
    
            /// <summary>
            ///   <para>引用表类名称。</para>
            /// </summary>
            public string referenceTableClassName;

            /// <summary>
            ///   <para>引用表名称。</para>
            /// </summary>
            public string referenceTableName;

            /// <summary>
            ///   <para>引用获取方法名称。</para>
            /// </summary>
            public string referenceGetMethodName;

            /// <summary>
            ///   <para>引用元组索引。</para>
            /// </summary>
            public int referenceTupleIndex = -1;
        }
    }
}

#endif