#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Collections.Generic;
    using System.Runtime.ExceptionServices;


    /// <summary>
    ///   <para>游戏模块清单资源</para>
    /// </summary>
    [Serializable]
    public sealed class GameModuleManifestAsset : ScriptableObject
    {
        /// <summary>
        ///   <para>游戏模块清单格式版本号</para>
        /// </summary>
        public const int VERSION = 1;

        private const string k_EmptyFields = "{}";

        /// <summary>
        ///   <para>空字段 JSON 对象文本</para>
        /// </summary>
        public static string EmptyFields => k_EmptyFields;

        /// <summary>
        ///   <para>游戏模块清单数据</para>
        /// </summary>
        [Serializable]
        public sealed class GameModuleManifestData
        {
            /// <summary>
            ///   <para>清单格式版本号</para>
            /// </summary>
            public uint version = VERSION;
            /// <summary>
            ///   <para>模块安装顺序策略</para>
            /// </summary>
            public GameModuleInstallOrder installOrder = GameModuleInstallOrder.Dependency;
            /// <summary>
            ///   <para>模块条目列表</para>
            /// </summary>
            public GameModuleEntry[] modules = Array.Empty<GameModuleEntry>();
        }

        /// <summary>
        ///   <para>单个模块条目</para>
        /// </summary>
        [Serializable]
        public struct GameModuleEntry
        {
            /// <summary>
            ///   <para>模块类型名，优先使用 <see cref="Type.AssemblyQualifiedName"/></para>
            /// </summary>
            public string type;
            /// <summary>
            ///   <para>模块序列化字段</para>
            /// </summary>
            public string fields;
        }

        /// <summary>
        ///   <para>单个稳定对象引用在导入后对应的实际资源对象</para>
        /// </summary>
        [Serializable]
        internal struct ImportedObjectRef
        {
            public string path;
            public UnityEngine.Object asset;
        }

        /// <summary>
        ///   <para>单个模块条目导入后可直接用于运行时恢复的数据</para>
        /// </summary>
        [Serializable]
        internal struct ImportedModuleData
        {
            public ImportedObjectRef[] objectRefs;
        }

        private static readonly object s_CacheLock = new();
        private static readonly Dictionary<string, Type> s_TypeLookupCache = new(StringComparer.Ordinal);

        /// <summary>
        ///   <para>游戏模块清单结构数据</para>
        /// </summary>
        [SerializeField] private GameModuleManifestData m_ManifestData = new();

        /// <summary>
        ///   <para>每个模块条目导入后可直接用于运行时恢复的数据</para>
        /// </summary>
        [SerializeField] private ImportedModuleData[] m_ImportedModules = Array.Empty<ImportedModuleData>();

        /// <summary>
        ///   <para>游戏模块清单结构数据</para>
        /// </summary>
        public GameModuleManifestData ManifestData => NormalizeManifestData(m_ManifestData);

        /// <summary>
        ///   <para>设置清单数据</para>
        /// </summary>
        public void SetManifestData(GameModuleManifestData manifestData)
        {
            SetImportedManifestData(manifestData, null);
        }

        /// <summary>
        ///   <para>原子设置清单数据和导入后可直接用于运行时恢复的数据</para>
        /// </summary>
        internal void SetImportedManifestData(
            GameModuleManifestData manifestData,
            ImportedModuleData[] importedModules)
        {
            m_ManifestData = NormalizeManifestData(manifestData);
            m_ImportedModules = NormalizeImportedModules(
                importedModules,
                m_ManifestData.modules.Length);
        }

        /// <summary>
        ///   <para>创建一个空的清单数据对象</para>
        /// </summary>
        public static GameModuleManifestData CreateEmptyManifestData()
        {
            return new GameModuleManifestData
            {
                version = VERSION,
                installOrder = GameModuleInstallOrder.Dependency,
                modules = Array.Empty<GameModuleEntry>()
            };
        }

        /// <summary>
        ///   <para>将清单资源转换为运行时清单</para>
        /// </summary>
        public GameModuleManifest ToManifest()
        {
            var manifestData = ManifestData;
            var manifest = new GameModuleManifest(manifestData.installOrder);
            var modules = manifestData.modules;

            if (modules.Length == 0)
            {
                return manifest;
            }

            for (int i = 0; i < modules.Length; i++)
            {
                manifest.Add(CreateDescriptor(modules[i], i));
            }

            return manifest;
        }

        /// <summary>
        ///   <para>规范化清单数据，保证数组非空且结构稳定</para>
        /// </summary>
        public static GameModuleManifestData NormalizeManifestData(GameModuleManifestData manifestData)
        {
            manifestData ??= new GameModuleManifestData();

            var modules = manifestData.modules;
            if (modules == null || modules.Length == 0)
            {
                return new GameModuleManifestData
                {
                    version = VERSION,
                    installOrder = NormalizeInstallOrder(manifestData.installOrder),
                    modules = Array.Empty<GameModuleEntry>()
                };
            }

            var copiedModules = new GameModuleEntry[modules.Length];
            for (int i = 0; i < modules.Length; i++)
            {
                copiedModules[i] = NormalizeModuleEntry(modules[i]);
            }

            return new GameModuleManifestData
            {
                version = VERSION,
                installOrder = NormalizeInstallOrder(manifestData.installOrder),
                modules = copiedModules
            };
        }

        private static GameModuleInstallOrder NormalizeInstallOrder(GameModuleInstallOrder installOrder)
        {
            return installOrder == GameModuleInstallOrder.Declared
                ? GameModuleInstallOrder.Declared
                : GameModuleInstallOrder.Dependency;
        }

        private static ImportedModuleData[] NormalizeImportedModules(
            ImportedModuleData[] importedModules,
            int moduleCount)
        {
            if (moduleCount <= 0)
            {
                return Array.Empty<ImportedModuleData>();
            }

            var normalized = new ImportedModuleData[moduleCount];
            for (int i = 0; i < moduleCount; i++)
            {
                if (importedModules != null && i < importedModules.Length)
                {
                    normalized[i].objectRefs = GameModuleManifestObjectRefUtility.NormalizeObjectRefs(
                        importedModules[i].objectRefs);
                }
                else
                {
                    normalized[i].objectRefs = Array.Empty<ImportedObjectRef>();
                }
            }

            return normalized;
        }

        /// <summary>
        ///   <para>根据类型名查找模块类型</para>
        /// </summary>
        public static bool TryGetModuleType(string typeName, out Type moduleType)
        {
            return TryGetModuleType(typeName, out moduleType, out _);
        }

        /// <summary>
        ///   <para>根据类型名查找模块类型，并返回清晰的清单校验错误</para>
        /// </summary>
        public static bool TryGetModuleType(string typeName, out Type moduleType, out string error)
        {
            error = null;
            if (!TryFindType(typeName, out moduleType))
            {
                error = $"Failed to find module type: {typeName ?? "<null>"}";
                return false;
            }

            error = GetManifestModuleTypeError(moduleType);
            if (error == null)
            {
                return true;
            }

            moduleType = null;
            return false;
        }

        /// <summary>
        ///   <para>根据类型名和字段 JSON 创建模块实例</para>
        /// </summary>
        public static GameModule CreateModuleInstance(string typeName, string fields)
        {
            if (!TryGetModuleType(typeName, out var moduleType, out var error))
            {
                throw new InvalidOperationException(error);
            }

            return CreateModuleInstance(moduleType, fields);
        }

        /// <summary>
        ///   <para>根据模块类型和字段 JSON 创建模块实例</para>
        /// </summary>
        public static GameModule CreateModuleInstance(Type moduleType, string fields)
        {
            ValidateModuleType(moduleType);

            GameModule module = null;
            try
            {
                module = Activator.CreateInstance(moduleType) as GameModule;
                if (module == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to create module instance. type={moduleType.FullName}, requires parameterless constructor.");
                }

                ApplyFields(module, fields);
                var result = module;
                module = null;
                return result;
            }
            catch (Exception ex)
            {
                var failure = GameModuleUtility.CombineErrors(
                    ex,
                    GameModuleUtility.DisposeModuleAndCreateFailure(module, "disposing module after creation failed"));
                ExceptionDispatchInfo.Capture(failure).Throw();
                throw;
            }
        }

        private GameModule CreateManifestModuleInstance(Type moduleType, string fields, int entryIndex)
        {
            GameModule module = null;
            try
            {
                module = CreateModuleInstance(moduleType, fields);
                ApplyImportedObjectRefs(module, entryIndex);
                var result = module;
                module = null;
                return result;
            }
            catch (Exception ex)
            {
                var failure = GameModuleUtility.CombineErrors(
                    ex,
                    GameModuleUtility.DisposeModuleAndCreateFailure(module, "disposing module after manifest entry restoration failed"));
                ExceptionDispatchInfo.Capture(failure).Throw();
                throw;
            }
        }

        /// <summary>
        ///   <para>获取模块用于清单序列化/编辑时的校验错误；合法时返回空</para>
        /// </summary>
        public static string GetManifestModuleTypeError(Type moduleType)
        {
            if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));

            var error = GameModuleUtility.GetModuleTypeError(moduleType);
            if (error != null)
            {
                return error;
            }

            if (!moduleType.IsDefined(typeof(SerializableAttribute), false))
            {
                return
                    $"{moduleType.FullName} must declare [Serializable] to use Unity native manifest serialization and inspector editing.";
            }

            if (moduleType.GetConstructor(Type.EmptyTypes) == null)
            {
                return
                    $"{moduleType.FullName} must declare a public parameterless constructor to be created from manifest data.";
            }

            return null;
        }

        /// <summary>
        ///   <para>规范化字段 JSON 文本</para>
        /// </summary>
        public static string NormalizeFields(string fields)
        {
            if (string.IsNullOrWhiteSpace(fields))
            {
                return k_EmptyFields;
            }

            fields = fields.Trim();
            return string.Equals(fields, "null", StringComparison.Ordinal) ? k_EmptyFields : fields;
        }

        private void ApplyImportedObjectRefs(
            GameModule module,
            int entryIndex)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (entryIndex < 0 ||
                m_ImportedModules == null ||
                entryIndex >= m_ImportedModules.Length)
            {
                return;
            }

            var objectRefs = m_ImportedModules[entryIndex].objectRefs;
            if (objectRefs == null || objectRefs.Length == 0) return;

            if (!GameModuleManifestObjectRefUtility.TryAssignObjectRefs(module, objectRefs, out var error))
            {
                throw new InvalidOperationException(
                    $"Failed to restore imported object references for module {module.GetType().FullName}. " +
                    $"manifestEntryIndex={entryIndex}, error={error}");
            }
        }

        /// <summary>
        ///   <para>根据类型名查找类型</para>
        /// </summary>
        private static bool TryFindType(string typeName, out Type foundType)
        {
            foundType = null;

            if (string.IsNullOrWhiteSpace(typeName))
            {
                return false;
            }

            typeName = typeName.Trim();
            lock (s_CacheLock)
            {
                if (s_TypeLookupCache.TryGetValue(typeName, out foundType))
                {
                    return true;
                }
            }

            foundType = Type.GetType(typeName, false);
            if (foundType == null)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    var assembly = assemblies[i];
                    if (assembly == null) continue;

                    foundType = assembly.GetType(typeName, false);
                    if (foundType != null)
                    {
                        break;
                    }
                }
            }

            lock (s_CacheLock)
            {
                if (foundType != null)
                {
                    s_TypeLookupCache[typeName] = foundType;
                }
            }

            return foundType != null;
        }

        /// <summary>
        ///   <para>把字段 JSON 应用到模块实例</para>
        /// </summary>
        private static void ApplyFields(GameModule module, string fields)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));

            fields = NormalizeFields(fields);
            if (string.Equals(fields, k_EmptyFields, StringComparison.Ordinal))
            {
                return;
            }

            JsonUtility.FromJsonOverwrite(fields, module);
        }

        /// <summary>
        ///   <para>规范化模块条目</para>
        /// </summary>
        private static GameModuleEntry NormalizeModuleEntry(GameModuleEntry entry)
        {
            entry.type = string.IsNullOrWhiteSpace(entry.type) ? string.Empty : entry.type.Trim();
            entry.fields = NormalizeFields(entry.fields);
            return entry;
        }

        /// <summary>
        ///   <para>验证模块类型</para>
        /// </summary>
        private static void ValidateModuleType(Type moduleType)
        {
            if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));

            var error = GetManifestModuleTypeError(moduleType);
            if (error != null)
            {
                throw new ArgumentException(error, nameof(moduleType));
            }
        }

        /// <summary>
        ///   <para>把单个资源条目转换为运行时描述符</para>
        /// </summary>
        private GameModuleDescriptor CreateDescriptor(GameModuleEntry entry, int entryIndex)
        {
            var normalizedEntry = NormalizeModuleEntry(entry);
            if (!TryGetModuleType(normalizedEntry.type, out var moduleType, out var error))
            {
                throw new InvalidOperationException(
                    error ?? $"Failed to find module type in manifest. index={entryIndex}, type={normalizedEntry.type ?? "<null>"}");
            }

            return new GameModuleDescriptor(
                moduleType,
                () => CreateManifestModuleInstance(moduleType, normalizedEntry.fields, entryIndex));
        }
    }
}

#endif