#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using VisualElement = UnityEngine.UIElements.VisualElement;

    /// <summary>
    ///   <para>ACC 标签设置页面；编辑项目标签并保持标识稳定。</para>
    /// </summary>
    internal sealed class TagRegistrySettingsProvider : SettingsProvider
    {
        /// <summary>
        ///   <para>项目设置路径。</para>
        /// </summary>
        internal const string SettingsPath = CoreEditorUtility.ModuleSettingsRoot + "/ACC/Tag Registry";

        /// <summary>
        ///   <para>当前标签设置资源。</para>
        /// </summary>
        private TagRegistrySettings m_Settings;
        /// <summary>
        ///   <para>当前设置资源的加载错误。</para>
        /// </summary>
        private string m_LoadError;

        /// <summary>
        ///   <para>创建 ACC 标签设置页面。</para>
        /// </summary>
        private TagRegistrySettingsProvider() : base(SettingsPath, SettingsScope.Project)
        {
            keywords = new[] { "Verve", "ACC", "Tag", "标签", "能力" };
        }

        /// <summary>
        ///   <para>注册 ACC 标签设置页面。</para>
        /// </summary>
        [SettingsProvider]
        private static SettingsProvider CreateProvider() => new TagRegistrySettingsProvider();

        /// <inheritdoc />
        public override void OnActivate(string searchContext, VisualElement rootElement) => LoadSettings();

        /// <inheritdoc />
        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.Space();
            if (!string.IsNullOrEmpty(m_LoadError))
            {
                EditorGUILayout.HelpBox(m_LoadError, MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox(
                "标签 ID 在创建时分配并永久保留，删除标签不会复用旧 ID。运行时会从 ACC 标签资源读取固定标签；未配置的标签仍可由代码按需注册。",
                MessageType.Info);

            if (m_Settings == null)
            {
                EditorGUILayout.HelpBox("项目尚未创建 ACC 标签注册表。添加第一个标签时会自动创建唯一资源。", MessageType.Info);
                if (GUILayout.Button("创建标签注册表"))
                    CreateSettings();
                return;
            }

            var entries = m_Settings.entries;
            if (entries == null)
            {
                EditorGUILayout.HelpBox("标签注册表资源中的条目列表为空，请重新创建资源。", MessageType.Error);
                return;
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                DrawEntries(entries);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("添加标签", GUILayout.Width(100f)))
                        AddEntry(entries);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField($"共 {entries.Count} 个标签", EditorStyles.miniLabel, GUILayout.Width(100f));
                }
            }

            if (!TryValidate(m_Settings, out var error))
                EditorGUILayout.HelpBox(error, MessageType.Error);
        }

        /// <summary>
        ///   <para>绘制标签条目。</para>
        /// </summary>
        /// <param name="entries">标签条目列表。</param>
        private void DrawEntries(List<TagRegistrySettings.Entry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null) continue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.IntField("ID", entry.id, GUILayout.Width(100f));

                    EditorGUI.BeginChangeCheck();
                    var name = EditorGUILayout.DelayedTextField(entry.name ?? string.Empty);
                    if (EditorGUI.EndChangeCheck() && !string.Equals(name, entry.name, StringComparison.Ordinal))
                    {
                        Undo.RecordObject(m_Settings, "Rename ACC Tag");
                        entry.name = name;
                        SaveSettings();
                    }

                    if (GUILayout.Button("删除", GUILayout.Width(48f)) &&
                        EditorUtility.DisplayDialog("删除 ACC 标签", $"确定删除标签“{entry.name}”吗？旧 ID {entry.id} 不会再次使用。", "删除", "取消"))
                    {
                        Undo.RecordObject(m_Settings, "Remove ACC Tag");
                        entries.RemoveAt(i--);
                        SaveSettings();
                    }
                }
            }
        }

        /// <summary>
        ///   <para>读取唯一标签设置资源。</para>
        /// </summary>
        private void LoadSettings()
        {
            m_LoadError = null;
            try
            {
                m_Settings = TagRegistrySettings.TryGetInstance(out var settings) ? settings : null;
            }
            catch (InvalidOperationException exception)
            {
                m_Settings = null;
                m_LoadError = exception.Message;
            }
        }

        /// <summary>
        ///   <para>创建唯一标签设置资源。</para>
        /// </summary>
        private void CreateSettings()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TagRegistrySettings.AssetPath));
            var settings = ScriptableObject.CreateInstance<TagRegistrySettings>();
            try
            {
                AssetDatabase.CreateAsset(settings, TagRegistrySettings.AssetPath);
                AssetDatabase.SaveAssets();
                m_Settings = settings;
                TagRegistry.Reset();
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(settings);
                throw;
            }
        }

        /// <summary>
        ///   <para>添加并分配新的稳定标签 ID。</para>
        /// </summary>
        /// <param name="entries">标签条目列表。</param>
        private void AddEntry(List<TagRegistrySettings.Entry> entries)
        {
            var nextId = m_Settings.nextId;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null && entry.id >= nextId)
                    nextId = entry.id == int.MaxValue ? int.MaxValue : entry.id + 1;
            }
            if (nextId <= 0 || nextId == int.MaxValue)
                throw new InvalidOperationException("ACC 标签 ID 已耗尽，无法创建新标签。");

            Undo.RecordObject(m_Settings, "Add ACC Tag");
            entries.Add(new TagRegistrySettings.Entry { name = GetNewTagName(entries), id = nextId });
            m_Settings.nextId = nextId + 1;
            SaveSettings();
        }

        /// <summary>
        ///   <para>生成当前列表中未使用的标签名称。</para>
        /// </summary>
        /// <param name="entries">标签条目列表。</param>
        /// <returns>可用名称。</returns>
        private static string GetNewTagName(List<TagRegistrySettings.Entry> entries)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null) names.Add(entries[i].name);

            var index = 1;
            string name;
            do name = "NewTag" + index++; while (names.Contains(name));
            return name;
        }

        /// <summary>
        ///   <para>校验项目标签注册表。</para>
        /// </summary>
        /// <param name="settings">标签设置资源。</param>
        /// <param name="error">校验错误。</param>
        /// <returns>是否通过校验。</returns>
        private static bool TryValidate(TagRegistrySettings settings, out string error)
        {
            if (settings == null)
            {
                error = null;
                return true;
            }
            if (settings.nextId <= 0)
            {
                error = "标签注册表的下一个 ID 必须大于零。";
                return false;
            }
            return TryValidateEntries(settings.entries, out error);
        }

        /// <summary>
        ///   <para>校验标签名称和标识唯一性。</para>
        /// </summary>
        /// <param name="entries">标签条目列表。</param>
        /// <param name="error">校验错误。</param>
        /// <returns>是否通过校验。</returns>
        private static bool TryValidateEntries(List<TagRegistrySettings.Entry> entries, out string error)
        {
            if (entries == null)
            {
                error = "标签注册表的条目列表为空。";
                return false;
            }
            var names = new HashSet<string>(StringComparer.Ordinal);
            var ids = new HashSet<int>();
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    error = $"标签条目 {i + 1} 为空。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.name))
                {
                    error = $"标签条目 {i + 1} 的名称不能为空。";
                    return false;
                }
                if (entry.id <= 0)
                {
                    error = $"标签“{entry.name}”的 ID 必须大于零。";
                    return false;
                }
                if (!names.Add(entry.name))
                {
                    error = $"标签名称“{entry.name}”重复。";
                    return false;
                }
                if (!ids.Add(entry.id))
                {
                    error = $"标签 ID {entry.id} 重复。";
                    return false;
                }
            }

            error = null;
            return true;
        }

        /// <summary>
        ///   <para>校验项目标签资源；供构建前校验复用。</para>
        /// </summary>
        /// <param name="error">校验错误。</param>
        /// <returns>是否通过校验。</returns>
        internal static bool Validate(out string error)
        {
            error = null;
            try
            {
                return !TagRegistrySettings.TryGetInstance(out var settings) || TryValidate(settings, out error);
            }
            catch (InvalidOperationException exception)
            {
                error = exception.Message;
                return false;
            }
        }

        /// <summary>
        ///   <para>标记并保存标签设置资源。</para>
        /// </summary>
        private void SaveSettings()
        {
            CoreEditorUtility.MarkDirtyAndSave(m_Settings);
            TagRegistry.Reset();
        }
    }
}

#endif
