#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.IO;
    using System.Text;
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using UnityEditor.AssetImporters;
    
    /// <summary>
    ///   <para>游戏模块清单导入器。</para>
    /// </summary>
    [ScriptedImporter(GameModuleManifestAsset.VERSION, k_Extension)]
    sealed class GameModuleManifestImporter : ScriptedImporter
    {
        /// <summary>
        ///   <para>清单文件扩展名。</para>
        /// </summary>
        private const string k_Extension = "gmm";
        /// <summary>
        ///   <para>默认文件名。</para>
        /// </summary>
        private const string k_DefaultFileName = "New Module Manifest";
        /// <summary>
        ///   <para>对象引用属性名称。</para>
        /// </summary>
        private const string k_ObjectRefPropertyName = "$objectReference";

        /// <summary>
        ///   <para>文件编码。</para>
        /// </summary>
        private static readonly UTF8Encoding s_Utf8WithoutBom = new(false);
        /// <summary>
        ///   <para>文本资源图标。</para>
        /// </summary>
        private static Texture2D s_TextAssetIcon;

        /// <summary>
        ///   <para>新建文件空内容。</para>
        /// </summary>
        private static string DefaultManifestJson =>
            FormatManifestJson(GameModuleManifestAsset.CreateEmptyManifestData());

        /// <summary>
        ///   <para>导入器内部使用的对象引用声明。</para>
        /// </summary>
        [Serializable]
        private struct ObjectRefData
        {
            /// <summary>
            ///   <para>路径。</para>
            /// </summary>
            public string path;
            /// <summary>
            ///   <para>GUID。</para>
            /// </summary>
            public string guid;
            /// <summary>
            ///   <para>文件标识。</para>
            /// </summary>
            public long fileId;
        }
        
        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            GameModuleManifestAsset asset = null;
            try
            {
                var sourceData = ReadManifestData(ctx.assetPath);
                var manifestData = CreateRuntimeManifestData(sourceData, out var importedModules);
                asset = ScriptableObject.CreateInstance<GameModuleManifestAsset>();
                asset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
                asset.SetImportedManifestData(manifestData, importedModules);

                var icon = GetIconTexture();
                ApplyIcon(asset, icon);
                ctx.AddObjectToAsset("<root>", asset, icon);
                ctx.SetMainObject(asset);
            }
            catch (Exception ex)
            {
                if (asset != null)
                {
                    DestroyImmediate(asset);
                }

                ctx.LogImportError($"Could not import manifest file '{ctx.assetPath}' ({FormatExceptionMessage(ex)})");
            }
        }

        /// <summary>
        ///   <para>创建新的游戏模块清单文件。</para>
        /// </summary>
        [MenuItem("Assets/Create/Verve/Game Module Manifest", priority = 0)]
        private static void CreateManifestAsset()
        {
            ProjectWindowUtil.CreateAssetWithContent(
                $"{k_DefaultFileName}.{k_Extension}",
                DefaultManifestJson,
                GetIconTexture());
        }

        /// <summary>
        ///   <para>获取图标纹理。</para>
        /// </summary>
        internal static Texture2D GetIconTexture() => s_TextAssetIcon ??= EditorGUIUtility.IconContent("TextAsset Icon").image as Texture2D;

        /// <summary>
        ///   <para>应用图标。</para>
        /// </summary>
        /// <param name="target">目标。</param>
        /// <param name="icon">图标。</param>
        internal static void ApplyIcon(UnityEngine.Object target, Texture2D icon = null)
        {
            if (target == null)
            {
                return;
            }

            icon ??= GetIconTexture();
            if (icon != null)
            {
                EditorGUIUtility.SetIconForObject(target, icon);
            }
        }

        /// <summary>
        ///   <para>读取游戏模块清单文件。</para>
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
        internal static GameModuleManifestAsset.GameModuleManifestData ReadManifestData(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new ArgumentException($"{nameof(assetPath)} can not be null or empty", nameof(assetPath));
            }

            var json = File.ReadAllText(assetPath, s_Utf8WithoutBom);
            if (string.IsNullOrWhiteSpace(json))
            {
                return GameModuleManifestAsset.CreateEmptyManifestData();
            }

            try
            {
                return ParseManifestData(json);
            }
            catch (Exception error)
            {
                throw new InvalidOperationException("Manifest JSON format is invalid.", error);
            }
        }

        /// <summary>
        ///   <para>写入游戏模块清单文件。</para>
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
        /// <param name="manifestData">清单数据。</param>
        internal static void WriteManifestData(string assetPath, GameModuleManifestAsset.GameModuleManifestData manifestData)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new ArgumentException($"{nameof(assetPath)} can not be null or empty", nameof(assetPath));
            }

            Game.FileUtility.WriteAllTextAtomically(assetPath, FormatManifestJson(manifestData), s_Utf8WithoutBom);
        }

        /// <summary>
        ///   <para>读取当前清单格式。</para>
        /// </summary>
        /// <param name="json">JSON。</param>
        private static GameModuleManifestAsset.GameModuleManifestData ParseManifestData(string json)
        {
            if (JToken.Parse(json) is not JObject manifestJson)
            {
                throw new InvalidOperationException("Manifest root must be a JSON object.");
            }

            return GameModuleManifestAsset.NormalizeManifestData(new GameModuleManifestAsset.GameModuleManifestData
            {
                version = ParseManifestVersion(manifestJson[nameof(GameModuleManifestAsset.GameModuleManifestData.version)]),
                installOrder = ParseInstallOrder(manifestJson[nameof(GameModuleManifestAsset.GameModuleManifestData.installOrder)]),
                modules = ParseModuleEntries(manifestJson[nameof(GameModuleManifestAsset.GameModuleManifestData.modules)])
            });
        }

        /// <summary>
        ///   <para>把清单数据序列化为外部 JSON 文件内容。</para>
        /// </summary>
        /// <param name="manifestData">清单数据。</param>
        private static string FormatManifestJson(GameModuleManifestAsset.GameModuleManifestData manifestData)
        {
            var manifestJson = CreateManifestJson(GameModuleManifestAsset.NormalizeManifestData(manifestData));
            return manifestJson.ToString(Formatting.Indented);
        }

        /// <summary>
        ///   <para>创建清单 JSON。</para>
        /// </summary>
        /// <param name="manifestData">清单数据。</param>
        private static JObject CreateManifestJson(GameModuleManifestAsset.GameModuleManifestData manifestData)
        {
            var entries = manifestData.modules;
            var modulesJson = new JArray();
            for (int i = 0; i < entries.Length; i++)
            {
                modulesJson.Add(new JObject
                {
                    [nameof(GameModuleManifestAsset.GameModuleEntry.type)] = new JValue(entries[i].type ?? string.Empty),
                    [nameof(GameModuleManifestAsset.GameModuleEntry.fields)] = CreateFieldsObject(entries[i].fields)
                });
            }

            return new JObject
            {
                [nameof(GameModuleManifestAsset.GameModuleManifestData.version)] = new JValue(manifestData.version),
                [nameof(GameModuleManifestAsset.GameModuleManifestData.installOrder)] = new JValue(manifestData.installOrder.ToString()),
                [nameof(GameModuleManifestAsset.GameModuleManifestData.modules)] = modulesJson
            };
        }

        /// <summary>
        ///   <para>把内部保存的字段 JSON 转换为外部对象字段。</para>
        /// </summary>
        /// <param name="fields">序列化字段 JSON。</param>
        private static JObject CreateFieldsObject(string fields) => JObject.Parse(GameModuleManifestAsset.NormalizeFields(fields));

        /// <summary>
        ///   <para>解析清单版本。</para>
        /// </summary>
        /// <param name="versionJson">版本 JSON。</param>
        private static uint ParseManifestVersion(JToken versionJson)
        {
            if (versionJson == null || versionJson.Type == JTokenType.Null)
            {
                return GameModuleManifestAsset.VERSION;
            }

            if (versionJson.Type != JTokenType.Integer ||
                !uint.TryParse(versionJson.ToString(), out var version))
            {
                throw new InvalidOperationException("Manifest version must be an unsigned integer.");
            }

            if (version != GameModuleManifestAsset.VERSION)
            {
                throw new InvalidOperationException(
                    $"Unsupported manifest version {version}. Expected {GameModuleManifestAsset.VERSION}.");
            }

            return version;
        }

        /// <summary>
        ///   <para>解析安装顺序。</para>
        /// </summary>
        /// <param name="installOrderJson">安装顺序 JSON。</param>
        private static GameModuleInstallOrder ParseInstallOrder(JToken installOrderJson)
        {
            if (installOrderJson == null || installOrderJson.Type == JTokenType.Null)
            {
                return GameModuleInstallOrder.Dependency;
            }

            if (installOrderJson.Type == JTokenType.String &&
                Enum.TryParse(installOrderJson.ToString(), true, out GameModuleInstallOrder parsed))
            {
                return parsed;
            }

            throw new InvalidOperationException("Unsupported manifest installOrder.");
        }

        /// <summary>
        ///   <para>解析模块条目。</para>
        /// </summary>
        /// <param name="modulesJson">模块 JSON。</param>
        private static GameModuleManifestAsset.GameModuleEntry[] ParseModuleEntries(JToken modulesJson)
        {
            if (modulesJson == null || modulesJson.Type == JTokenType.Null)
            {
                return Array.Empty<GameModuleManifestAsset.GameModuleEntry>();
            }

            if (modulesJson is not JArray modules)
            {
                throw new InvalidOperationException("Manifest modules must be a JSON array.");
            }

            if (modules.Count == 0)
            {
                return Array.Empty<GameModuleManifestAsset.GameModuleEntry>();
            }

            var moduleEntries = new GameModuleManifestAsset.GameModuleEntry[modules.Count];
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i] is not JObject module)
                {
                    throw new InvalidOperationException($"Manifest module entry must be a JSON object. index={i}");
                }

                moduleEntries[i] = ParseModuleEntry(module, i);
            }

            return moduleEntries;
        }

        /// <summary>
        ///   <para>解析模块条目。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="index">索引。</param>
        private static GameModuleManifestAsset.GameModuleEntry ParseModuleEntry(JObject module, int index)
        {
            var typeJson = module[nameof(GameModuleManifestAsset.GameModuleEntry.type)];
            if (typeJson != null && typeJson.Type != JTokenType.String && typeJson.Type != JTokenType.Null)
            {
                throw new InvalidOperationException($"Manifest module type must be a string. index={index}");
            }

            return new GameModuleManifestAsset.GameModuleEntry
            {
                type = typeJson?.ToString() ?? string.Empty,
                fields = ParseModuleFields(module[nameof(GameModuleManifestAsset.GameModuleEntry.fields)], index)
            };
        }

        /// <summary>
        ///   <para>解析模块字段。</para>
        /// </summary>
        /// <param name="fields">字段名称。</param>
        /// <param name="index">索引。</param>
        private static string ParseModuleFields(JToken fields, int index)
        {
            if (fields == null || fields.Type == JTokenType.Null)
            {
                return GameModuleManifestAsset.EmptyFields;
            }

            if (fields is not JObject)
            {
                throw new InvalidOperationException($"Manifest module fields must be a JSON object. index={index}");
            }

            return fields.ToString(Formatting.None);
        }

        /// <summary>
        ///   <para>创建清单条目。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        internal static GameModuleManifestAsset.GameModuleEntry CreateManifestEntry(GameModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));

            var moduleType = module.GetType();
            var error = GameModuleManifestAsset.GetManifestModuleTypeError(moduleType);
            if (error != null)
            {
                throw new ArgumentException(error, nameof(module));
            }

            return new GameModuleManifestAsset.GameModuleEntry
            {
                type = GameModuleUtility.GetStableTypeName(moduleType),
                fields = SerializeManifestFields(module)
            };
        }

        /// <summary>
        ///   <para>尝试创建模块实例。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="fields">序列化字段 JSON。</param>
        /// <param name="module">模块。</param>
        /// <param name="error">错误。</param>
        internal static bool TryCreateModuleInstance(
            Type moduleType,
            string fields,
            out GameModule module,
            out string error)
        {
            module = null;
            error = null;

            try
            {
                var importedModuleData = CreateImportedModule(fields, -1, out var runtimeFields);
                module = GameModuleManifestAsset.CreateModuleInstance(moduleType, runtimeFields);
                if (!GameModuleManifestObjectRefUtility.TryAssignObjectRefs(
                        module,
                        importedModuleData.objectRefs,
                        out error))
                {
                    DisposeModuleSafely(ref module, "disposing module after object reference restoration failed", ref error);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = FormatExceptionMessage(ex);
                DisposeModuleSafely(ref module, "disposing module after manifest restoration failed", ref error);
                return false;
            }
        }

        /// <summary>
        ///   <para>释放预览模块并报告清理错误。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="operation">操作。</param>
        /// <param name="error">错误。</param>
        private static void DisposeModuleSafely(ref GameModule module, string operation, ref string error)
        {
            if (module == null)
            {
                return;
            }

            var disposeFailure = GameModuleUtility.DisposeModuleAndCreateFailure(module, operation);
            if (disposeFailure != null)
            {
                var disposeError = FormatExceptionMessage(disposeFailure);
                error = string.IsNullOrWhiteSpace(error)
                    ? disposeError
                    : $"{error} {disposeError}";
            }

            module = null;
        }

        /// <summary>
        ///   <para>格式化异常信息。</para>
        /// </summary>
        /// <param name="exception">异常。</param>
        private static string FormatExceptionMessage(Exception exception)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            string previousMessage = null;
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (string.IsNullOrWhiteSpace(current.Message) ||
                    string.Equals(previousMessage, current.Message, StringComparison.Ordinal))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(current.Message);
                previousMessage = current.Message;
            }

            return builder.Length == 0 ? exception.GetType().Name : builder.ToString();
        }

        /// <summary>
        ///   <para>创建运行时清单数据。</para>
        /// </summary>
        /// <param name="manifestData">清单数据。</param>
        /// <param name="importedModules">已导入模块。</param>
        internal static GameModuleManifestAsset.GameModuleManifestData CreateRuntimeManifestData(
            GameModuleManifestAsset.GameModuleManifestData manifestData,
            out GameModuleManifestAsset.ImportedModuleData[] importedModules)
        {
            var runtimeData = GameModuleManifestAsset.NormalizeManifestData(manifestData);
            var modules = runtimeData.modules;
            if (modules.Length == 0)
            {
                importedModules = Array.Empty<GameModuleManifestAsset.ImportedModuleData>();
                return runtimeData;
            }

            importedModules = new GameModuleManifestAsset.ImportedModuleData[modules.Length];
            for (int i = 0; i < modules.Length; i++)
            {
                var entry = modules[i];
                importedModules[i] = CreateImportedModule(entry.fields, i, out entry.fields);
                modules[i] = entry;
            }

            ValidateRuntimeManifestData(runtimeData, importedModules);
            return runtimeData;
        }

        /// <summary>
        ///   <para>校验运行时清单数据。</para>
        /// </summary>
        /// <param name="manifestData">清单数据。</param>
        /// <param name="importedModules">已导入模块。</param>
        private static void ValidateRuntimeManifestData(
            GameModuleManifestAsset.GameModuleManifestData manifestData,
            GameModuleManifestAsset.ImportedModuleData[] importedModules)
        {
            var modules = manifestData.modules;
            for (int i = 0; i < modules.Length; i++)
            {
                var entry = modules[i];
                if (!GameModuleManifestAsset.TryGetModuleType(entry.type, out var moduleType, out var typeError))
                {
                    throw new InvalidOperationException(
                        $"Manifest module type is invalid. index={i}, type={entry.type ?? "<null>"}, error={typeError}");
                }

                GameModule module = null;
                Exception failure = null;
                try
                {
                    module = GameModuleManifestAsset.CreateModuleInstance(moduleType, entry.fields);
                    if (!GameModuleManifestObjectRefUtility.TryAssignObjectRefs(
                            module,
                            importedModules[i].objectRefs,
                            out var objectRefError))
                    {
                        throw new InvalidOperationException(objectRefError);
                    }
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
                finally
                {
                    failure = ExceptionUtility.Combine(
                        failure,
                        GameModuleUtility.DisposeModuleAndCreateFailure(
                            module,
                            "disposing module after manifest import validation"));
                }

                if (failure != null)
                {
                    throw new InvalidOperationException(
                        $"Manifest module runtime restoration failed. index={i}, type={entry.type ?? "<null>"}, error={FormatExceptionMessage(failure)}",
                        failure);
                }
            }
        }

        /// <summary>
        ///   <para>序列化清单字段。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        private static string SerializeManifestFields(GameModule module)
        {
            var rawJson = GameModuleManifestAsset.NormalizeFields(JsonUtility.ToJson(module, false));
            var clonedModule = GameModuleManifestAsset.CreateModuleInstance(module.GetType(), rawJson);
            ModuleBox moduleBox = null;
            string result = null;
            Exception failure = null;

            try
            {
                moduleBox = ScriptableObject.CreateInstance<ModuleBox>();
                moduleBox.hideFlags = HideFlags.HideAndDontSave;
                moduleBox.module = clonedModule;

                using var serializedObject = new SerializedObject(moduleBox);
                var moduleProperty = serializedObject.FindProperty(nameof(ModuleBox.module));
                if (moduleProperty == null)
                {
                    throw new InvalidOperationException("Failed to create temporary module serialization box.");
                }

                var objectRefs = ReadObjectRefsAndClearFields(
                    serializedObject,
                    moduleProperty,
                    out var objectRefPaths);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                var fields = GameModuleManifestAsset.NormalizeFields(JsonUtility.ToJson(moduleBox.module, false));
                result = WriteObjectRefsToFields(fields, objectRefs, objectRefPaths);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                failure = ExceptionUtility.Combine(
                    failure,
                    GameModuleUtility.DisposeModuleAndCreateFailure(
                        clonedModule,
                        "disposing temporary module after manifest serialization"));

                if (moduleBox != null)
                {
                    DestroyImmediate(moduleBox);
                }
            }

            if (failure != null)
            {
                throw new InvalidOperationException(
                    $"Failed to serialize manifest module fields. type={module.GetType().FullName}, error={FormatExceptionMessage(failure)}",
                    failure);
            }

            return result;
        }

        /// <summary>
        ///   <para>创建已导入模块。</para>
        /// </summary>
        /// <param name="fields">序列化字段 JSON。</param>
        /// <param name="moduleIndex">模块索引。</param>
        /// <param name="runtimeFields">运行时字段。</param>
        private static GameModuleManifestAsset.ImportedModuleData CreateImportedModule(
            string fields,
            int moduleIndex,
            out string runtimeFields)
        {
            var fieldsObject = CreateFieldsObject(fields);
            var objectRefs = ReadObjectRefsFromFields(fieldsObject, moduleIndex);
            if (!TryLoadObjectRefs(objectRefs, out var importedRefs, out var error))
            {
                throw new InvalidOperationException(
                    moduleIndex >= 0
                        ? $"Failed to load object references for manifest entry index {moduleIndex}. {error}"
                        : error);
            }

            runtimeFields = GameModuleManifestAsset.NormalizeFields(fieldsObject.ToString(Formatting.None));
            return new GameModuleManifestAsset.ImportedModuleData
            {
                objectRefs = importedRefs
            };
        }

        /// <summary>
        ///   <para>从字段中读取对象引用。</para>
        /// </summary>
        /// <param name="fields">字段名称。</param>
        /// <param name="moduleIndex">模块索引。</param>
        private static ObjectRefData[] ReadObjectRefsFromFields(
            JToken fields,
            int moduleIndex)
        {
            var objectRefs = new List<ObjectRefData>();
            ReadObjectRefsFromFields(fields, string.Empty, objectRefs, moduleIndex);
            return objectRefs.Count == 0
                ? Array.Empty<ObjectRefData>()
                : objectRefs.ToArray();
        }

        /// <summary>
        ///   <para>从字段中读取对象引用。</para>
        /// </summary>
        /// <param name="jsonValue">JSON 值。</param>
        /// <param name="propertyPath">属性路径。</param>
        /// <param name="objectRefs">对象引用。</param>
        /// <param name="moduleIndex">模块索引。</param>
        private static bool ReadObjectRefsFromFields(
            JToken jsonValue,
            string propertyPath,
            List<ObjectRefData> objectRefs,
            int moduleIndex)
        {
            if (jsonValue is JObject jsonObject)
            {
                if (TryReadObjectRef(jsonObject, propertyPath, moduleIndex, out var objectRef))
                {
                    objectRefs.Add(objectRef);
                    return true;
                }

                var properties = new List<JProperty>(jsonObject.Properties());
                for (int i = 0; i < properties.Count; i++)
                {
                    var property = properties[i];
                    if (ReadObjectRefsFromFields(
                            property.Value,
                            AppendPropertyFieldPath(propertyPath, property.Name),
                            objectRefs,
                            moduleIndex))
                    {
                        property.Value = JValue.CreateNull();
                    }
                }

                return false;
            }

            if (jsonValue is JArray jsonArray)
            {
                for (int i = 0; i < jsonArray.Count; i++)
                {
                    if (ReadObjectRefsFromFields(
                            jsonArray[i],
                            AppendPropertyArrayElementPath(propertyPath, i),
                            objectRefs,
                            moduleIndex))
                    {
                        jsonArray[i] = JValue.CreateNull();
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>尝试读取对象引用。</para>
        /// </summary>
        /// <param name="jsonObject">JSON 对象。</param>
        /// <param name="propertyPath">属性路径。</param>
        /// <param name="moduleIndex">模块索引。</param>
        /// <param name="objectRef">对象引用。</param>
        private static bool TryReadObjectRef(
            JObject jsonObject,
            string propertyPath,
            int moduleIndex,
            out ObjectRefData objectRef)
        {
            objectRef = default;
            var objectRefProperty = jsonObject.Property(k_ObjectRefPropertyName);
            if (objectRefProperty == null)
            {
                return false;
            }

            if (jsonObject.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Manifest object reference field cannot contain sibling JSON fields. moduleIndex={moduleIndex}, path={propertyPath}");
            }

            if (string.IsNullOrWhiteSpace(propertyPath))
            {
                throw new InvalidOperationException(
                    $"Manifest object reference cannot be declared at fields root. moduleIndex={moduleIndex}");
            }

            if (objectRefProperty.Value is not JObject locationJson)
            {
                throw new InvalidOperationException(
                    $"Manifest object reference location must be a JSON object. moduleIndex={moduleIndex}, path={propertyPath}");
            }

            var guidJson = locationJson[nameof(ObjectRefData.guid)];
            var guid = guidJson?.ToString();
            if (guidJson == null || guidJson.Type != JTokenType.String || string.IsNullOrWhiteSpace(guid))
            {
                throw new InvalidOperationException(
                    $"Manifest object reference guid must be a string. moduleIndex={moduleIndex}, path={propertyPath}");
            }

            var fileIdJson = locationJson[nameof(ObjectRefData.fileId)];
            if (fileIdJson == null ||
                fileIdJson.Type != JTokenType.Integer ||
                !long.TryParse(fileIdJson.ToString(), out var fileId))
            {
                throw new InvalidOperationException(
                    $"Manifest object reference fileId must be an integer. moduleIndex={moduleIndex}, path={propertyPath}");
            }

            objectRef = new ObjectRefData
            {
                path = propertyPath,
                guid = guid,
                fileId = fileId
            };
            return true;
        }

        /// <summary>
        ///   <para>将对象引用写入字段。</para>
        /// </summary>
        /// <param name="fields">序列化字段 JSON。</param>
        /// <param name="objectRefs">对象引用。</param>
        /// <param name="objectRefPaths">对象引用路径。</param>
        private static string WriteObjectRefsToFields(
            string fields,
            ObjectRefData[] objectRefs,
            string[] objectRefPaths)
        {
            var fieldsObject = CreateFieldsObject(fields);
            if (objectRefPaths != null)
            {
                for (int i = 0; i < objectRefPaths.Length; i++)
                {
                    var path = objectRefPaths[i];
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        TrySetJsonByPropertyPath(fieldsObject, path, JValue.CreateNull(), allowMissingLeaf: true, out _);
                    }
                }
            }

            var normalizedRefs = NormalizeObjectRefs(objectRefs);
            for (int i = 0; i < normalizedRefs.Length; i++)
            {
                var objectRef = normalizedRefs[i];
                if (string.IsNullOrWhiteSpace(objectRef.path))
                {
                    continue;
                }

                if (!TrySetJsonByPropertyPath(
                        fieldsObject,
                        objectRef.path,
                        CreateObjectRefJson(objectRef),
                        allowMissingLeaf: false,
                        out var error))
                {
                    throw new InvalidOperationException(
                        $"Failed to embed object reference field '{objectRef.path}'. {error}");
                }
            }

            return GameModuleManifestAsset.NormalizeFields(fieldsObject.ToString(Formatting.None));
        }

        /// <summary>
        ///   <para>创建对象引用 JSON。</para>
        /// </summary>
        /// <param name="objectRef">对象引用。</param>
        private static JObject CreateObjectRefJson(ObjectRefData objectRef)
        {
            return new JObject
            {
                [k_ObjectRefPropertyName] = new JObject
                {
                    [nameof(ObjectRefData.guid)] = objectRef.guid ?? string.Empty,
                    [nameof(ObjectRefData.fileId)] = objectRef.fileId
                }
            };
        }

        /// <summary>
        ///   <para>规范化对象引用。</para>
        /// </summary>
        /// <param name="objectRefs">对象引用。</param>
        private static ObjectRefData[] NormalizeObjectRefs(ObjectRefData[] objectRefs)
        {
            if (objectRefs == null || objectRefs.Length == 0)
            {
                return Array.Empty<ObjectRefData>();
            }

            var normalized = new ObjectRefData[objectRefs.Length];
            for (int i = 0; i < objectRefs.Length; i++)
            {
                normalized[i] = new ObjectRefData
                {
                    path = string.IsNullOrWhiteSpace(objectRefs[i].path)
                        ? string.Empty
                        : objectRefs[i].path.Trim(),
                    guid = string.IsNullOrWhiteSpace(objectRefs[i].guid)
                        ? string.Empty
                        : objectRefs[i].guid.Trim(),
                    fileId = objectRefs[i].fileId
                };
            }

            return normalized;
        }

        /// <summary>
        ///   <para>追加属性字段路径。</para>
        /// </summary>
        /// <param name="parentPath">父级路径。</param>
        /// <param name="fieldName">字段名称。</param>
        private static string AppendPropertyFieldPath(string parentPath, string fieldName)
        {
            return string.IsNullOrEmpty(parentPath)
                ? fieldName ?? string.Empty
                : $"{parentPath}.{fieldName}";
        }

        /// <summary>
        ///   <para>追加属性数组元素路径。</para>
        /// </summary>
        /// <param name="parentPath">父级路径。</param>
        /// <param name="index">索引。</param>
        private static string AppendPropertyArrayElementPath(string parentPath, int index)
        {
            return string.IsNullOrEmpty(parentPath)
                ? $"Array.data[{index}]"
                : $"{parentPath}.Array.data[{index}]";
        }

        /// <summary>
        ///   <para>尝试加载对象引用。</para>
        /// </summary>
        /// <param name="objectRefs">对象引用。</param>
        /// <param name="importedRefs">已导入引用。</param>
        /// <param name="error">错误。</param>
        private static bool TryLoadObjectRefs(
            ObjectRefData[] objectRefs,
            out GameModuleManifestAsset.ImportedObjectRef[] importedRefs,
            out string error)
        {
            importedRefs = Array.Empty<GameModuleManifestAsset.ImportedObjectRef>();
            error = null;

            var normalizedRefs = NormalizeObjectRefs(objectRefs);
            if (normalizedRefs.Length == 0)
            {
                return true;
            }

            importedRefs = new GameModuleManifestAsset.ImportedObjectRef[normalizedRefs.Length];
            for (int i = 0; i < normalizedRefs.Length; i++)
            {
                var objectRef = normalizedRefs[i];
                if (string.IsNullOrWhiteSpace(objectRef.path))
                {
                    error = $"Object reference path cannot be empty. index={i}";
                    importedRefs = Array.Empty<GameModuleManifestAsset.ImportedObjectRef>();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(objectRef.guid))
                {
                    error = $"Object reference guid cannot be empty. path={objectRef.path}";
                    importedRefs = Array.Empty<GameModuleManifestAsset.ImportedObjectRef>();
                    return false;
                }

                var assetPath = AssetDatabase.GUIDToAssetPath(objectRef.guid);
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    error = $"Object reference asset was not found. path={objectRef.path}, guid={objectRef.guid}";
                    importedRefs = Array.Empty<GameModuleManifestAsset.ImportedObjectRef>();
                    return false;
                }

                var asset = FindPersistentObject(assetPath, objectRef.fileId);
                if (asset == null)
                {
                    error =
                        $"Object reference target was not found in asset. path={objectRef.path}, guid={objectRef.guid}, fileId={objectRef.fileId}";
                    importedRefs = Array.Empty<GameModuleManifestAsset.ImportedObjectRef>();
                    return false;
                }

                importedRefs[i] = new GameModuleManifestAsset.ImportedObjectRef
                {
                    path = objectRef.path,
                    asset = asset
                };
            }

            return true;
        }

        /// <summary>
        ///   <para>读取对象引用并清空对应字段。</para>
        /// </summary>
        /// <param name="serializedObject">序列化对象。</param>
        /// <param name="moduleProperty">模块属性。</param>
        /// <param name="objectRefPaths">对象引用路径。</param>
        private static ObjectRefData[] ReadObjectRefsAndClearFields(
            SerializedObject serializedObject,
            SerializedProperty moduleProperty,
            out string[] objectRefPaths)
        {
            if (serializedObject == null) throw new ArgumentNullException(nameof(serializedObject));
            if (moduleProperty == null) throw new ArgumentNullException(nameof(moduleProperty));

            var objectRefs = new List<ObjectRefData>();
            var objectRefPathList = new List<string>();
            var iterator = moduleProperty.Copy();
            var endProperty = iterator.GetEndProperty();
            bool enterChildren = true;

            while (iterator.Next(enterChildren) &&
                   !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;
                if (iterator.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                var relativePath = GetRelativePropertyPath(moduleProperty.propertyPath, iterator.propertyPath);
                objectRefPathList.Add(relativePath);

                var asset = iterator.objectReferenceValue;
                if (asset == null)
                {
                    continue;
                }

                if (!EditorUtility.IsPersistent(asset) ||
                    !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long fileId) ||
                    string.IsNullOrWhiteSpace(guid))
                {
                    throw new InvalidOperationException(
                        $"Manifest only supports persistent asset references. field={relativePath}, object={asset.name}");
                }

                objectRefs.Add(new ObjectRefData
                {
                    path = relativePath,
                    guid = guid,
                    fileId = fileId
                });
            }

            for (int i = 0; i < objectRefPathList.Count; i++)
            {
                var property = serializedObject.FindProperty(
                    string.IsNullOrEmpty(moduleProperty.propertyPath)
                        ? objectRefPathList[i]
                        : $"{moduleProperty.propertyPath}.{objectRefPathList[i]}");
                if (property != null)
                {
                    property.objectReferenceValue = null;
                }
            }

            objectRefPaths = objectRefPathList.Count == 0
                ? Array.Empty<string>()
                : objectRefPathList.ToArray();
            return objectRefs.Count == 0
                ? Array.Empty<ObjectRefData>()
                : objectRefs.ToArray();
        }

        /// <summary>
        ///   <para>按属性路径尝试设置 JSON。</para>
        /// </summary>
        /// <param name="root">JSON 根节点。</param>
        /// <param name="propertyPath">属性路径。</param>
        /// <param name="value">值。</param>
        /// <param name="allowMissingLeaf">是否允许末级路径缺失。</param>
        /// <param name="error">错误。</param>
        private static bool TrySetJsonByPropertyPath(
            JToken root,
            string propertyPath,
            JToken value,
            bool allowMissingLeaf,
            out string error)
        {
            error = null;
            var segments = GameModuleManifestObjectRefUtility.ParseSerializedPropertyPath(propertyPath, out error);
            if (segments == null || segments.Count == 0)
            {
                return false;
            }

            var current = root;
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                bool isLast = i == segments.Count - 1;

                if (segment.kind == GameModuleManifestObjectRefUtility.SerializedPropertyPathElementKind.Field)
                {
                    if (current is not JObject currentObject)
                    {
                        error = $"Property path '{propertyPath}' expected a JSON object at '{segment.fieldName}'.";
                        return false;
                    }

                    var property = currentObject.Property(segment.fieldName);
                    if (property == null)
                    {
                        if (isLast && allowMissingLeaf)
                        {
                            return true;
                        }

                        error = $"Property path '{propertyPath}' could not find JSON field '{segment.fieldName}'.";
                        return false;
                    }

                    if (isLast)
                    {
                        property.Value = value;
                        return true;
                    }

                    current = property.Value;
                    continue;
                }

                if (current is not JArray currentArray)
                {
                    error = $"Property path '{propertyPath}' expected a JSON array at index {segment.index}.";
                    return false;
                }

                if (segment.index < 0 || segment.index >= currentArray.Count)
                {
                    if (isLast && allowMissingLeaf)
                    {
                        return true;
                    }

                    error = $"Property path '{propertyPath}' array index is out of range. index={segment.index}";
                    return false;
                }

                if (isLast)
                {
                    currentArray[segment.index] = value;
                    return true;
                }

                current = currentArray[segment.index];
            }

            error = $"Property path '{propertyPath}' ended unexpectedly.";
            return false;
        }

        /// <summary>
        ///   <para>获取相对属性路径。</para>
        /// </summary>
        /// <param name="rootPath">根节点路径。</param>
        /// <param name="propertyPath">属性路径。</param>
        private static string GetRelativePropertyPath(string rootPath, string propertyPath)
        {
            if (string.IsNullOrWhiteSpace(propertyPath))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(rootPath))
            {
                return propertyPath;
            }

            if (string.Equals(propertyPath, rootPath, StringComparison.Ordinal))
            {
                return string.Empty;
            }

            var prefix = rootPath + ".";
            return propertyPath.StartsWith(prefix, StringComparison.Ordinal)
                ? propertyPath.Substring(prefix.Length)
                : propertyPath;
        }

        /// <summary>
        ///   <para>查找持久化对象。</para>
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
        /// <param name="fileId">文件标识。</param>
        private static UnityEngine.Object FindPersistentObject(string assetPath, long fileId)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                var asset = assets[i];
                if (asset == null)
                {
                    continue;
                }

                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out _, out long candidateFileId) &&
                    candidateFileId == fileId)
                {
                    return asset;
                }
            }

            return null;
        }

        /// <summary>
        ///   <para>模块显示区域。</para>
        /// </summary>
        [Serializable]
        private sealed class ModuleBox : ScriptableObject
        {
            /// <summary>
            ///   <para>模块。</para>
            /// </summary>
            [SerializeReference] public GameModule module;
        }
    }
}

#endif