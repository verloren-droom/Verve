#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    ///   <para>配置表编辑器设置。</para>
    /// </summary>
    [FilePath("ProjectSettings/ConfigTableEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class ConfigTableEditorSettings : ScriptableSingleton<ConfigTableEditorSettings>
    {
        /// <summary>
        ///   <para>客户端配置表目录。</para>
        /// </summary>
        internal const string ClientTableFolder = "Assets/StreamingAssets/" + ConfigTableModule.TablesFolder;
        /// <summary>
        ///   <para>C# 访问代码默认目录。</para>
        /// </summary>
        internal const string DefaultGeneratedCodeFolder = "Assets/Scripts/Tables";
        /// <summary>
        ///   <para>服务端配置表默认目录。</para>
        /// </summary>
        internal const string DefaultServerTableFolder = "Server/" + ConfigTableModule.TablesFolder;
    
        /// <summary>
        ///   <para>已生成代码目录。</para>
        /// </summary>
        [SerializeField]
        private string generatedCodeFolder = DefaultGeneratedCodeFolder;
    
        /// <summary>
        ///   <para>服务端表目录。</para>
        /// </summary>
        [SerializeField]
        private string serverTableFolder = DefaultServerTableFolder;
    
        /// <summary>
        ///   <para>获取 C# 生成目录。</para>
        /// </summary>
        internal string GeneratedCodeFolder => GetGeneratedCodeFolder();
    
        /// <summary>
        ///   <para>获取服务端表目录。</para>
        /// </summary>
        internal string ServerTableFolder => GetServerTableFolder();
    
        /// <summary>
        ///   <para>获取当前 Unity 项目根目录的绝对路径。</para>
        /// </summary>
        internal string ProjectRoot => Directory.GetParent(Application.dataPath)?.FullName;
    
        /// <summary>
        ///   <para>校验并保存 C# 生成目录，值未变化时不写入设置资产。</para>
        /// </summary>
        /// <param name="folder">待设置的目录。</param>
        /// <param name="error">输出校验失败原因。</param>
        internal bool TrySetGeneratedCodeFolder(string folder, out string error)
        {
            if (!TryValidateGeneratedCodeFolder(folder, out var normalizedFolder, out error))
            {
                return false;
            }
    
            if (!string.Equals(generatedCodeFolder, normalizedFolder, StringComparison.OrdinalIgnoreCase))
            {
                generatedCodeFolder = normalizedFolder;
                Save(true);
            }
            error = string.Empty;
            return true;
        }
    
        /// <summary>
        ///   <para>校验并保存服务端表目录，值未变化时不写入设置资产。</para>
        /// </summary>
        /// <param name="folder">待设置的目录。</param>
        /// <param name="error">输出校验失败原因。</param>
        internal bool TrySetServerTableFolder(string folder, out string error)
        {
            if (!TryValidateServerTableFolder(folder, out var normalizedFolder, out error))
            {
                return false;
            }
    
            if (!string.Equals(serverTableFolder, normalizedFolder, StringComparison.OrdinalIgnoreCase))
            {
                serverTableFolder = normalizedFolder;
                Save(true);
            }
            error = string.Empty;
            return true;
        }
    
        /// <summary>
        ///   <para>校验 C# 生成目录是否位于 Assets 内。</para>
        /// </summary>
        /// <param name="folder">待校验的目录。</param>
        /// <param name="normalizedFolder">输出规范化目录。</param>
        /// <param name="error">输出校验失败原因。</param>
        internal bool TryValidateGeneratedCodeFolder(string folder, out string normalizedFolder, out string error)
        {
            if (!TryNormalizeProjectRelativeFolder(folder, out normalizedFolder, out error))
            {
                return false;
            }
            if (!IsAssetsFolder(normalizedFolder))
            {
                error = "C# 生成路径必须位于 Assets 目录内，才能被 Unity 编译。";
                return false;
            }
            return true;
        }
    
        /// <summary>
        ///   <para>校验服务端表目录并阻止其与客户端目录重合。</para>
        /// </summary>
        /// <param name="folder">待校验的目录。</param>
        /// <param name="normalizedFolder">输出规范化目录。</param>
        /// <param name="error">输出校验失败原因。</param>
        internal bool TryValidateServerTableFolder(string folder, out string normalizedFolder, out string error)
        {
            if (!TryNormalizeProjectRelativeFolder(folder, out normalizedFolder, out error))
            {
                return false;
            }
            if (string.Equals(normalizedFolder, ClientTableFolder, StringComparison.OrdinalIgnoreCase))
            {
                error = "服务端配置表路径不能与客户端配置表路径相同。";
                return false;
            }
            return true;
        }
    
        /// <summary>
        ///   <para>将项目相对路径转换为项目内绝对路径。</para>
        /// </summary>
        /// <param name="projectRelativePath">项目相对路径。</param>
        internal string GetProjectAbsolutePath(string projectRelativePath) => Path.GetFullPath(Path.Combine(ProjectRoot, projectRelativePath ?? string.Empty));
    
        /// <summary>
        ///   <para>规范化并校验项目内相对目录。</para>
        /// </summary>
        /// <param name="folder">待规范化的目录。</param>
        /// <param name="normalizedFolder">输出项目相对目录。</param>
        /// <param name="error">输出校验失败原因。</param>
        internal bool TryNormalizeProjectRelativeFolder(string folder, out string normalizedFolder, out string error)
        {
            normalizedFolder = string.Empty;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(folder))
            {
                error = "路径不能为空。";
                return false;
            }
    
            string projectRoot = Path.GetFullPath(ProjectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullPath;
            try
            {
                fullPath = Path.IsPathRooted(folder)
                    ? Path.GetFullPath(folder)
                    : Path.GetFullPath(Path.Combine(projectRoot, folder));
                fullPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch (ArgumentException exception)
            {
                error = "路径格式无效：" + exception.Message;
                return false;
            }
    
            string projectRootPrefix = projectRoot + Path.DirectorySeparatorChar;
            if (string.Equals(fullPath, projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                error = "路径不能为项目根目录。";
                return false;
            }
            if (!fullPath.StartsWith(projectRootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                error = "路径必须位于当前 Unity 项目根目录内。";
                return false;
            }
    
            normalizedFolder = fullPath.Substring(projectRootPrefix.Length).Replace('\\', '/');
            return true;
        }
    
        /// <summary>
        ///   <para>获取已生成代码目录。</para>
        /// </summary>
        private string GetGeneratedCodeFolder()
        {
            if (!TryValidateGeneratedCodeFolder(generatedCodeFolder, out var normalizedFolder, out var error))
                throw new InvalidOperationException(error);
            return normalizedFolder;
        }
    
        /// <summary>
        ///   <para>获取服务端表目录。</para>
        /// </summary>
        private string GetServerTableFolder()
        {
            if (!TryValidateServerTableFolder(serverTableFolder, out var normalizedFolder, out var error))
                throw new InvalidOperationException(error);
            return normalizedFolder;
        }
    
        /// <summary>
        ///   <para>判断是否为资源目录。</para>
        /// </summary>
        /// <param name="projectRelativeFolder">项目相对目录。</param>
        private bool IsAssetsFolder(string projectRelativeFolder)
        {
            return string.Equals(projectRelativeFolder, "Assets", StringComparison.OrdinalIgnoreCase) ||
                projectRelativeFolder.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
        }
    }
    
    /// <summary>
    ///   <para>配置表项目设置；校验输出目录并迁移已有文件。</para>
    /// </summary>
    internal sealed class ConfigTableSettingsProvider : SettingsProvider
    {
        /// <summary>
        ///   <para>项目设置路径。</para>
        /// </summary>
        internal const string SettingsPath = CoreEditorUtility.ModuleSettingsRoot + "/Table";
        /// <summary>
        ///   <para>C# 生成目录草稿。</para>
        /// </summary>
        private string m_CodeFolder;
        /// <summary>
        ///   <para>服务端表目录草稿。</para>
        /// </summary>
        private string m_ServerFolder;

        /// <summary>
        ///   <para>创建设置页面。</para>
        /// </summary>
        private ConfigTableSettingsProvider() : base(SettingsPath, SettingsScope.Project) =>
            keywords = new[] { "Verve", "Table", "配置表", "CSV", "生成代码", "服务端", "路径" };

        /// <summary>
        ///   <para>注册项目设置。</para>
        /// </summary>
        [SettingsProvider]
        private static SettingsProvider CreateProvider() => new ConfigTableSettingsProvider();

        /// <inheritdoc />
        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement) => LoadSettings();

        /// <inheritdoc />
        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("客户端表路径", ConfigTableEditorSettings.ClientTableFolder);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                DrawFolderField("C# 生成路径", ref m_CodeFolder);
                DrawFolderField("服务端表路径", ref m_ServerFolder);
                var settings = ConfigTableEditorSettings.instance;
                var validCode = settings.TryValidateGeneratedCodeFolder(m_CodeFolder, out var code, out var codeError);
                var validServer = settings.TryValidateServerTableFolder(m_ServerFolder, out var server, out var serverError);
                if (!validCode) EditorGUILayout.HelpBox(codeError, MessageType.Error);
                if (!validServer) EditorGUILayout.HelpBox(serverError, MessageType.Error);
                EditorGUILayout.HelpBox("应用目录变更时，将迁移已有的生成代码或服务端配置表。", MessageType.Info);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("恢复默认"))
                    {
                        m_CodeFolder = ConfigTableEditorSettings.DefaultGeneratedCodeFolder;
                        m_ServerFolder = ConfigTableEditorSettings.DefaultServerTableFolder;
                    }
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("还原未应用修改")) LoadSettings();
                    using (new EditorGUI.DisabledScope(!validCode || !validServer ||
                        code == settings.GeneratedCodeFolder && server == settings.ServerTableFolder))
                        if (GUILayout.Button("应用")) ApplySettings();
                }
            }
        }

        /// <summary>
        ///   <para>绘制目录选择；草稿仅在应用后保存。</para>
        /// </summary>
        /// <param name="label">标签。</param>
        /// <param name="folder">目录草稿。</param>
        private static void DrawFolderField(string label, ref string folder)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                folder = EditorGUILayout.TextField(label, folder);
                if (!GUILayout.Button("选择", GUILayout.Width(54f))) return;
                var selected = EditorUtility.OpenFolderPanel(label, ConfigTableEditorSettings.instance.ProjectRoot, string.Empty);
                if (!string.IsNullOrEmpty(selected)) folder = selected;
            }
        }

        /// <summary>
        ///   <para>读取已保存设置。</para>
        /// </summary>
        private void LoadSettings()
        {
            m_CodeFolder = ConfigTableEditorSettings.instance.GeneratedCodeFolder;
            m_ServerFolder = ConfigTableEditorSettings.instance.ServerTableFolder;
        }

        /// <summary>
        ///   <para>迁移输出并保存目录；保留失败项供修正。</para>
        /// </summary>
        private void ApplySettings()
        {
            var settings = ConfigTableEditorSettings.instance;
            if (!settings.TryValidateGeneratedCodeFolder(m_CodeFolder, out var code, out var error) ||
                !settings.TryValidateServerTableFolder(m_ServerFolder, out var server, out error))
                throw new InvalidOperationException(error);
            var moveCode = !string.Equals(settings.GeneratedCodeFolder, code, StringComparison.OrdinalIgnoreCase);
            var moveServer = !string.Equals(settings.ServerTableFolder, server, StringComparison.OrdinalIgnoreCase);
            if (!moveCode && !moveServer) return;
            if (!EditorUtility.DisplayDialog("移动配置表输出", "将已有输出移动到新目录，并保存对应设置。", "移动并应用", "取消")) return;
            if (moveCode)
            {
                if (!ConfigTableImporter.TryMoveGeneratedCodeFiles(settings.GeneratedCodeFolder, code, out error))
                {
                    EditorUtility.DisplayDialog("移动生成代码失败", error, "确定");
                    return;
                }
                if (!settings.TrySetGeneratedCodeFolder(code, out error)) throw new InvalidOperationException(error);
            }
            if (moveServer)
            {
                if (!ConfigTableImporter.TryMoveServerTableFiles(settings.ServerTableFolder, server, out error))
                {
                    EditorUtility.DisplayDialog("移动服务端配置表失败", error, "确定");
                    return;
                }
                if (!settings.TrySetServerTableFolder(server, out error)) throw new InvalidOperationException(error);
            }
            LoadSettings();
        }
    }
}

#endif