#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;


    /// <summary>
    ///   <para>模块清单导入器的编辑条目数据</para>
    /// </summary>
    [Serializable]
    internal sealed class GameModuleManifestEditorData : ScriptableObject
    {
        internal const string InstallOrderPropertyName = nameof(m_InstallOrder);
        internal const string ModuleEntriesPropertyName = nameof(m_ModuleEntries);

        /// <summary>
        ///   <para>单个模块的编辑条目</para>
        /// </summary>
        [Serializable]
        internal sealed class ModuleEntry
        {
            internal const string TypePropertyName = nameof(type);
            internal const string FieldsPropertyName = nameof(fields);
            internal const string ModulePropertyName = nameof(module);
            internal const string ErrorPropertyName = nameof(error);
            internal const string UsesFallbackModuleInstancePropertyName = nameof(usesFallbackModuleInstance);

            /// <summary>
            ///   <para>模块类型名</para>
            /// </summary>
            public string type;
            /// <summary>
            ///   <para>原始字段条目</para>
            /// </summary>
            public string fields = GameModuleManifestAsset.EmptyFields;
            /// <summary>
            ///   <para>Inspector 当前编辑的模块实例</para>
            /// </summary>
            [SerializeReference] public GameModule module;
            /// <summary>
            ///   <para>恢复或校验错误信息</para>
            /// </summary>
            [HideInInspector] public string error;
            /// <summary>
            ///   <para>当前模块实例是否为字段数据无法恢复时创建的兜底实例</para>
            /// </summary>
            [HideInInspector] public bool usesFallbackModuleInstance;
        }

        [SerializeField] private GameModuleInstallOrder m_InstallOrder = GameModuleInstallOrder.Dependency;
        [SerializeField, HideInInspector] private bool m_HasPendingChanges;
        [SerializeField] private ModuleEntry[] m_ModuleEntries = Array.Empty<ModuleEntry>();

        /// <summary>
        ///   <para>当前编辑中的模块条目列表</para>
        /// </summary>
        public ModuleEntry[] ModuleEntries => m_ModuleEntries ?? Array.Empty<ModuleEntry>();

        /// <summary>
        ///   <para>当前编辑条目是否存在尚未写回清单文件的修改</para>
        /// </summary>
        public bool HasPendingChanges
        {
            get => m_HasPendingChanges;
            set => m_HasPendingChanges = value;
        }

        /// <summary>
        ///   <para>加载清单结构和编辑条目</para>
        /// </summary>
        public void LoadModuleEntries(GameModuleManifestAsset.GameModuleManifestData manifestData, ModuleEntry[] moduleEntries)
        {
            manifestData = GameModuleManifestAsset.NormalizeManifestData(manifestData);
            m_InstallOrder = manifestData.installOrder;
            m_HasPendingChanges = false;
            m_ModuleEntries = NormalizeModuleEntries(moduleEntries);
        }

        /// <summary>
        ///   <para>把当前编辑条目构建为可写回 JSON 的清单数据</para>
        /// </summary>
        public GameModuleManifestAsset.GameModuleManifestData CreateManifestData()
        {
            return CreateManifestData(ModuleEntries, m_InstallOrder);
        }

        /// <summary>
        ///   <para>把给定编辑条目列表构建为清单数据</para>
        /// </summary>
        internal static GameModuleManifestAsset.GameModuleManifestData CreateManifestData(
            ModuleEntry[] moduleEntries,
            GameModuleInstallOrder installOrder = GameModuleInstallOrder.Dependency)
        {
            moduleEntries ??= Array.Empty<ModuleEntry>();
            var modules = new GameModuleManifestAsset.GameModuleEntry[moduleEntries.Length];

            for (int i = 0; i < moduleEntries.Length; i++)
            {
                modules[i] = CreateManifestEntry(moduleEntries[i]);
            }

            return GameModuleManifestAsset.NormalizeManifestData(new GameModuleManifestAsset.GameModuleManifestData
            {
                version = GameModuleManifestAsset.VERSION,
                installOrder = installOrder,
                modules = modules
            });
        }

        /// <summary>
        ///   <para>把单个编辑条目转换为清单条目</para>
        /// </summary>
        internal static GameModuleManifestAsset.GameModuleEntry CreateManifestEntry(ModuleEntry moduleEntry)
        {
            moduleEntry = NormalizeModuleEntry(moduleEntry);

            if (moduleEntry.module != null && !moduleEntry.usesFallbackModuleInstance)
            {
                return GameModuleManifestImporter.CreateManifestEntry(moduleEntry.module);
            }

            return new GameModuleManifestAsset.GameModuleEntry
            {
                type = moduleEntry.type,
                fields = moduleEntry.fields
            };
        }

        /// <summary>
        ///   <para>从清单条目创建编辑条目</para>
        /// </summary>
        internal static ModuleEntry CreateEntryFromManifestEntry(GameModuleManifestAsset.GameModuleEntry entry)
        {
            var moduleEntry = new ModuleEntry
            {
                type = entry.type ?? string.Empty,
                fields = GameModuleManifestAsset.NormalizeFields(entry.fields)
            };

            if (!GameModuleManifestAsset.TryGetModuleType(moduleEntry.type, out var moduleType, out var typeError))
            {
                moduleEntry.error = typeError ?? $"Failed to find module type: {moduleEntry.type ?? "<null>"}";
                return moduleEntry;
            }

            moduleEntry.type = GameModuleUtility.GetStableTypeName(moduleType);

            try
            {
                if (!GameModuleManifestImporter.TryCreateModuleInstance(
                        moduleType,
                        moduleEntry.fields,
                        out var module,
                        out var restoreError))
                {
                    moduleEntry.error = restoreError;
                    return moduleEntry;
                }

                moduleEntry.module = module;
                moduleEntry.fields = GameModuleManifestImporter.CreateManifestEntry(moduleEntry.module).fields;
            }
            catch (Exception ex)
            {
                moduleEntry.error = ex.Message;
                try
                {
                    moduleEntry.module = GameModuleManifestAsset.CreateModuleInstance(moduleType, GameModuleManifestAsset.EmptyFields);
                    moduleEntry.usesFallbackModuleInstance = true;
                }
                catch (Exception fallbackEx)
                {
                    moduleEntry.error =
                        $"{moduleEntry.error} Fallback module instance could not be created: {fallbackEx.Message}";
                }
            }

            return moduleEntry;
        }

        /// <summary>
        ///   <para>从模块实例创建编辑条目</para>
        /// </summary>
        internal static ModuleEntry CreateEntryFromModule(GameModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));

            var entry = GameModuleManifestImporter.CreateManifestEntry(module);
            return new ModuleEntry
            {
                type = entry.type,
                fields = entry.fields,
                module = module
            };
        }

        /// <summary>
        ///   <para>从模块类型和字段 JSON 创建编辑条目</para>
        /// </summary>
        internal static ModuleEntry CreateEntryFromType(Type moduleType, string fields)
        {
            if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));
            return CreateEntryFromManifestEntry(new GameModuleManifestAsset.GameModuleEntry
            {
                type = GameModuleUtility.GetStableTypeName(moduleType),
                fields = fields
            });
        }

        /// <summary>
        ///   <para>从清单数据创建编辑条目</para>
        /// </summary>
        internal static ModuleEntry[] CreateModuleEntriesFromManifestData(
            GameModuleManifestAsset.GameModuleManifestData manifestData)
        {
            var normalizedData = GameModuleManifestAsset.NormalizeManifestData(manifestData);
            var moduleEntries = new ModuleEntry[normalizedData.modules.Length];

            for (int i = 0; i < normalizedData.modules.Length; i++)
            {
                moduleEntries[i] = CreateEntryFromManifestEntry(normalizedData.modules[i]);
            }

            return moduleEntries;
        }

        /// <summary>
        ///   <para>从序列化属性读取模块条目</para>
        /// </summary>
        internal static ModuleEntry ReadModuleEntry(SerializedProperty property)
        {
            return new ModuleEntry
            {
                type = ReadModuleType(property),
                fields = GameModuleManifestAsset.NormalizeFields(
                    property?.FindPropertyRelative(ModuleEntry.FieldsPropertyName)?.stringValue),
                module = FindModuleProperty(property)?.managedReferenceValue as GameModule,
                error = property?.FindPropertyRelative(ModuleEntry.ErrorPropertyName)?.stringValue,
                usesFallbackModuleInstance = property?.FindPropertyRelative(ModuleEntry.UsesFallbackModuleInstancePropertyName)?.boolValue ?? false
            };
        }

        /// <summary>
        ///   <para>写入模块条目到序列化属性</para>
        /// </summary>
        internal static void WriteModuleEntry(SerializedProperty property, ModuleEntry moduleEntry)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            property.FindPropertyRelative(ModuleEntry.TypePropertyName).stringValue = moduleEntry?.type ?? string.Empty;
            property.FindPropertyRelative(ModuleEntry.FieldsPropertyName).stringValue =
                GameModuleManifestAsset.NormalizeFields(moduleEntry?.fields);
            property.FindPropertyRelative(ModuleEntry.ModulePropertyName).managedReferenceValue = moduleEntry?.module;
            property.FindPropertyRelative(ModuleEntry.ErrorPropertyName).stringValue = moduleEntry?.error ?? string.Empty;
            property.FindPropertyRelative(ModuleEntry.UsesFallbackModuleInstancePropertyName).boolValue =
                moduleEntry?.usesFallbackModuleInstance ?? false;
        }

        /// <summary>
        ///   <para>读取模块条目中的声明类型名</para>
        /// </summary>
        internal static string ReadModuleType(SerializedProperty property)
        {
            return property?.FindPropertyRelative(ModuleEntry.TypePropertyName)?.stringValue ?? string.Empty;
        }

        /// <summary>
        ///   <para>获取模块条目中的模块引用属性</para>
        /// </summary>
        internal static SerializedProperty FindModuleProperty(SerializedProperty property)
        {
            return property?.FindPropertyRelative(ModuleEntry.ModulePropertyName);
        }

        private static ModuleEntry[] NormalizeModuleEntries(ModuleEntry[] moduleEntries)
        {
            if (moduleEntries == null || moduleEntries.Length == 0)
            {
                return Array.Empty<ModuleEntry>();
            }

            var normalizedEntries = new ModuleEntry[moduleEntries.Length];
            for (int i = 0; i < moduleEntries.Length; i++)
            {
                normalizedEntries[i] = NormalizeModuleEntry(moduleEntries[i]);
            }

            return normalizedEntries;
        }

        private static ModuleEntry NormalizeModuleEntry(ModuleEntry moduleEntry)
        {
            if (moduleEntry == null)
            {
                return new ModuleEntry();
            }

            return new ModuleEntry
            {
                type = string.IsNullOrWhiteSpace(moduleEntry.type) ? string.Empty : moduleEntry.type.Trim(),
                fields = GameModuleManifestAsset.NormalizeFields(moduleEntry.fields),
                module = moduleEntry.module,
                error = moduleEntry.error,
                usesFallbackModuleInstance = moduleEntry.usesFallbackModuleInstance
            };
        }
    }
}

#endif