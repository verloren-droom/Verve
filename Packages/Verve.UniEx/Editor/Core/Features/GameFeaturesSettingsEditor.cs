#if UNITY_EDITOR

namespace VerveEditor.UniEx
{
    using System;
    using Verve.UniEx;
    using System.IO;
    using System.Linq;
    using System.Text;
    using UnityEditor;
    using UnityEngine;
    using System.Reflection;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;
    

    [CustomEditor(typeof(GameFeaturesSettings))]
    internal sealed class GameFeaturesSettingsEditor : Editor
    {
        private GameFeaturesSettings m_Settings;
        private SerializedProperty m_ComponentProfileProperty;
        private SerializedProperty m_ModuleProfileProperty;
        
        private readonly TabPagerBox m_TabPagerBox = new TabPagerBox(0);

        private static readonly string[] s_ExcludedFields =
        {
            "m_Script",
            "m_ModuleProfile",
            "m_ComponentProfile",
            "extensionOutputDir",
            "m_Drawers",
        };

        private static class Styles
        {
            public static GUIContent ComponentProfile { get; } = EditorGUIUtility.TrTextContent("Component Profile", "The profile containing all game feature components.");
            public static GUIContent ModuleProfile { get; } = EditorGUIUtility.TrTextContent("Module Profile", "The profile containing all game feature modules.");
            public static GUIContent NewLabel { get; } = EditorGUIUtility.TrTextContent("New", "Create a new component profile.");
            public static GUIContent NewModuleDataLabel { get; } = EditorGUIUtility.TrTextContent("New", "Create a new module profile.");
            public static string NoProfileMessage { get; } = L10n.Tr("Please select or create a new GameFeatures profile to begin applying features to the game.");
            public static string NoModuleDataMessage { get; } = L10n.Tr("Please select or create a new GameFeatures module data to manage game feature modules.");
            public static GUIContent GenerateButton { get; } = EditorGUIUtility.TrTextContent("Generate", "Generate extension methods for all modules.");
            public static GUIContent BrowseButton { get; } = EditorGUIUtility.TrTextContent("Browse", "Select a directory to save generated files.");
            public static GUIContent ExtensionGeneratorTitle { get; } = EditorGUIUtility.TrTextContent("Extension Generator", "Generate extension methods for modules.");
        }

        private void OnEnable()
        {
            m_Settings = target as GameFeaturesSettings;
            if (m_Settings == null || target == null) return;
            
            m_ComponentProfileProperty = serializedObject.FindProperty("m_ComponentProfile");
            m_ModuleProfileProperty = serializedObject.FindProperty("m_ModuleProfile");
        }

        public override void OnInspectorGUI()
        {
            if (m_Settings == null || target == null) return;
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            
            DrawPropertiesExcluding(serializedObject, s_ExcludedFields);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(Styles.ModuleProfile);
                
                using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
                {
                    var newModuleProfile = EditorGUILayout.ObjectField(
                        m_ModuleProfileProperty.objectReferenceValue, 
                        typeof(GameFeatureModuleProfile), 
                        false) as GameFeatureModuleProfile;
            
                    if (changeCheckScope.changed)
                    {
                        m_ModuleProfileProperty.objectReferenceValue = newModuleProfile;
                    }
                }
        
                if (GUILayout.Button(Styles.NewModuleDataLabel, EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    string defaultPath = "Assets/Resources";
                    if (!AssetDatabase.IsValidFolder(defaultPath))
                    {
                        defaultPath = "Assets";
                    }
                    
                    string profilePath = EditorUtility.SaveFilePanelInProject("Save Game Feature Module Profile", "GameFeatureModuleProfile", "asset", "Save the Game Feature Module Profile asset");
                    if (!string.IsNullOrEmpty(profilePath))
                    {
                        var profile = ScriptableObject.CreateInstance<GameFeatureModuleProfile>();
                        profile.name = "Game Feature Module Profile";
                        AssetDatabase.CreateAsset(profile, profilePath);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        
                        m_ModuleProfileProperty.objectReferenceValue = profile;
                    }
                }
            }
            
            if (m_ModuleProfileProperty.objectReferenceValue != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel(Styles.ComponentProfile);
                    
                    using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
                    {
                        var newComponentProfile = EditorGUILayout.ObjectField(
                            m_ComponentProfileProperty.objectReferenceValue, 
                            typeof(GameFeatureComponentProfile), 
                            false) as GameFeatureComponentProfile;
            
                        if (changeCheckScope.changed)
                        {
                            m_ComponentProfileProperty.objectReferenceValue = newComponentProfile;
                        }
                    }
        
                    if (GUILayout.Button(Styles.NewLabel, EditorStyles.miniButton, GUILayout.Width(60)))
                    {
                        string defaultPath = "Assets/Resources";
                        if (!AssetDatabase.IsValidFolder(defaultPath))
                        {
                            defaultPath = "Assets";
                        }
                    
                        string profilePath = EditorUtility.SaveFilePanelInProject("Save Game Feature Component Profile", "GameFeatureComponentProfile", "asset", "Save the Game Feature Component Profile asset");
                        if (!string.IsNullOrEmpty(profilePath))
                        {
                            var profile = ScriptableObject.CreateInstance<GameFeatureComponentProfile>();
                            profile.name = "Game Feature Component Profile";
                            AssetDatabase.CreateAsset(profile, profilePath);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                        
                            m_ComponentProfileProperty.objectReferenceValue = profile;
                        }
                    }
                }

                EditorGUILayout.Space();
                
                if (m_ComponentProfileProperty.objectReferenceValue == null)
                    EditorGUILayout.HelpBox(Styles.NoProfileMessage, MessageType.Info);
                
                EditorGUILayout.Space();
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel(Styles.ExtensionGeneratorTitle);
                    m_Settings.extensionOutputDir = EditorGUILayout.TextField(m_Settings.extensionOutputDir, GUILayout.ExpandWidth(true));
                    if (GUILayout.Button(Styles.BrowseButton, EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        string selectedPath = EditorUtility.SaveFolderPanel("Select Save Directory", m_Settings.extensionOutputDir, "");
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            m_Settings.extensionOutputDir = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                        }
                    }
                    if (GUILayout.Button(Styles.GenerateButton, EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        GenerateAllExtensions();
                    }
                }
                
                DrawModuleEditorSettings();
            }
            else
            {
                EditorGUILayout.HelpBox(Styles.NoModuleDataMessage, MessageType.Warning);
            }

            
            if (EditorGUI.EndChangeCheck())
            {
                GameFeaturesRunner.Instance.SetProfiles(GameFeaturesSettings.instance.ModuleProfile, GameFeaturesSettings.instance.ComponentProfile);
                GameFeaturesRunner.Instance.skipRuntimeDependencyChecks = GameFeaturesSettings.instance.SkipRuntimeDependencyChecks;
                serializedObject.ApplyModifiedProperties();
                GameFeaturesSettings.instance.Save();
            }
        }
        
        /// <summary>
        /// 绘制模块编辑器设置
        /// </summary>
        private void DrawModuleEditorSettings()
        {
            var drawerTypes = TypeCache.GetTypesDerivedFrom<ModuleEditorDrawer>().Where(t => !t.IsAbstract && t.GetCustomAttribute<ModuleEditorDrawerAttribute>() != null);
            foreach (var drawerType in drawerTypes)
            {
                m_Settings.GetOrCreateModuleEditor(drawerType);
            }
            
            if (m_Settings.Drawers == null || m_Settings.Drawers.Count == 0)
            {
                EditorGUILayout.HelpBox("No module editor settings available.", MessageType.Info);
                return;
            }
            
            var tabTitles = new List<string>();
            var drawersList = m_Settings.Drawers.ToList();
            
            var activeModuleTypes = new HashSet<Type>();
            if (m_Settings.ModuleProfile != null && m_Settings.ModuleProfile.Modules != null)
            {
                foreach (var module in m_Settings.ModuleProfile.Modules)
                {
                    if (module != null)
                    {
                        activeModuleTypes.Add(module.GetType());
                    }
                }
            }
            
            var moduleTypeToDrawer = new Dictionary<Type, ModuleEditorDrawer>();
            foreach (var drawer in drawersList)
            {
                if (drawer != null)
                {
                    var attr = drawer.GetType().GetCustomAttribute<ModuleEditorDrawerAttribute>();
                    if (attr != null)
                    {
                        moduleTypeToDrawer[attr.moduleType] = drawer;
                    }
                }
            }
            
            var drawerActivationStatus = new List<bool>();
            foreach (var drawer in drawersList)
            {
                if (drawer != null)
                {
                    var drawerType = drawer.GetType();
                    var attr = drawerType.GetCustomAttribute<ModuleEditorDrawerAttribute>();
                    bool isActive = attr != null && activeModuleTypes.Contains(attr.moduleType);
                    drawerActivationStatus.Add(isActive);
                    
                    var moduleName = ObjectNames.NicifyVariableName(drawerType.Name);
                    const string suffix = "Module";
                    if (moduleName.EndsWith(suffix))
                        moduleName = moduleName.Substring(0, moduleName.Length - suffix.Length);
                        
                    if (moduleName.Length > 20)
                    {
                        moduleName = $"{moduleName.Substring(0, 17)}...{moduleName.Substring(moduleName.Length - 3)}";
                    }

                    tabTitles.Add(moduleName);
                }
                else
                {
                    drawerActivationStatus.Add(false);
                    tabTitles.Add("Unknown");
                }
            }
            
            int selectedIndex = m_TabPagerBox.Begin(tabTitles.ToArray());
            
            if (selectedIndex >= 0 && selectedIndex < drawersList.Count)
            {
                var selectedDrawer = drawersList[selectedIndex];
                bool isDrawerActive = drawerActivationStatus[selectedIndex];
                
                if (selectedDrawer != null)
                {
                    using (new EditorGUI.DisabledGroupScope(!isDrawerActive))
                    {
                        EditorGUI.BeginChangeCheck();
                        selectedDrawer.OnGUI();
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorUtility.SetDirty(m_Settings);
                            serializedObject.ApplyModifiedProperties();
                        }
                    }
                }
            }
        
            m_TabPagerBox.End();
        }

        /// <summary>
        /// 生成所有扩展方法
        /// </summary>
        private void GenerateAllExtensions()
        {
            if (!Directory.Exists(m_Settings.extensionOutputDir))
            {
                Directory.CreateDirectory(m_Settings.extensionOutputDir);
            }

            string tempFolder = Path.Combine(Application.temporaryCachePath, "GameFeaturesExtensionsTemp");
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
            Directory.CreateDirectory(tempFolder);

            int generatedCount = 0;
            int updatedCount = 0;
            var moduleProfile = m_ModuleProfileProperty.objectReferenceValue as GameFeatureModuleProfile;
            
            try
            {
                foreach (var module in moduleProfile.Modules)
                {
                    if (module == null) continue;
                    
                    var moduleType = module.GetType();
                    var attr = moduleType.GetCustomAttribute<GameFeatureAttribute>();
                    
                    if (attr != null && attr.Dependencies.Length > 0)
                    {
                        if (module is GameFeatureModule m)
                        {
                            string extensionCode = GenerateExtensionClass(m);
                            
                            if (!string.IsNullOrEmpty(extensionCode))
                            {
                                string fileName = $"{moduleType.Name}Extensions.cs";
                                string tempFilePath = Path.Combine(tempFolder, fileName);
                                string targetFilePath = Path.Combine(m_Settings.extensionOutputDir, fileName);
                                
                                File.WriteAllText(tempFilePath, extensionCode);
                                
                                if (ShouldUpdateFile(tempFilePath, targetFilePath))
                                {
                                    File.Copy(tempFilePath, targetFilePath, true);
                                    updatedCount++;
                                }
                                
                                generatedCount++;
                            }
                        }
                    }
                }
                
                AssetDatabase.Refresh();
                Debug.Log($"Generated {generatedCount} extension classes, updated {updatedCount} files in {m_Settings.extensionOutputDir}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating extension methods: {ex.Message}");
            }
            finally
            {
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
            }
        }
        
        /// <summary>
        /// 检查文件是否需要更新
        /// </summary>
        private bool ShouldUpdateFile(string tempFilePath, string targetFilePath)
        {
            if (!File.Exists(targetFilePath))
            {
                return true;
            }
            
            string tempContent = File.ReadAllText(tempFilePath);
            string targetContent = File.ReadAllText(targetFilePath);
            
            return tempContent != targetContent;
        }

        /// <summary>
        /// 为模块生成扩展类
        /// </summary>
        private string GenerateExtensionClass(GameFeatureModule module)
        {
            var moduleType = module.GetType();
            var moduleAttr = moduleType.GetCustomAttribute<GameFeatureAttribute>();
            var namespaceName = moduleType.Namespace ?? "Verve.UniEx";
            
            var requiredNamespaces = new HashSet<string>
            {
                "System",
                "Verve.UniEx"
            };
            
            var extensionMethodsCode = new StringBuilder();
            bool hasMethods = false;
            
            foreach (var submodule in module.Submodules)
            {
                if (submodule == null) continue;
                
                var submoduleType = submodule.GetType();
                var methods = submoduleType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                
                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    
                    var extensionAttributes = new List<(ModuleMethodBridgeAttribute attr, int paramIndex)>();
                    
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        var extensionAttr = param.GetCustomAttribute<ModuleMethodBridgeAttribute>();
                        
                        if (extensionAttr != null)
                        {
                            extensionAttributes.Add((extensionAttr, i));
                        }
                    }
                    
                    if (extensionAttributes.Count > 0)
                    {
                        string extensionMethod = GenerateExtensionMethodWithDelegateReplacement(
                            submoduleType, method, extensionAttributes, requiredNamespaces);
                        
                        if (!string.IsNullOrEmpty(extensionMethod))
                        {
                            extensionMethodsCode.AppendLine(extensionMethod);
                            hasMethods = true;
                        }
                    }
                }
            }
            
            if (!hasMethods)
            {
                extensionMethodsCode.AppendLine("        // No extension methods generated");
            }
            
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("// =================================================");
            sb.AppendLine("// AUTO-GENERATED FILE - DO NOT EDIT MANUALLY");
            sb.AppendLine("// Generated by Game Features Extension");
            sb.AppendLine($"// Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("// =================================================");
            sb.AppendLine();
            
            var sortedNamespaces = requiredNamespaces.OrderBy(ns => ns).ToList();
            foreach (var ns in sortedNamespaces)
            {
                sb.AppendLine($"using {ns};");
            }
            sb.AppendLine();
            
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            
            sb.AppendLine($"    public static class {moduleType.Name}Extensions");
            sb.AppendLine("    {");
            
            sb.Append(extensionMethodsCode.ToString());
            
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return hasMethods ? sb.ToString() : null;
        }

        /// <summary>
        /// 生成扩展方法，将委托参数替换为从GameFeatures获取的子模块方法
        /// </summary>
        private string GenerateExtensionMethodWithDelegateReplacement(
            Type submoduleType,
            MethodInfo method, 
            List<(ModuleMethodBridgeAttribute attr, int paramIndex)> extensionAttributes,
            HashSet<string> requiredNamespaces)
        {
            var parameters = method.GetParameters();
            
            foreach (var param in parameters)
            {
                GetTypeNamespaces(param.ParameterType, requiredNamespaces);
            }
            
            GetTypeNamespaces(method.ReturnType, requiredNamespaces);
            
            var regularParameters = new List<ParameterInfo>();
            var bridgeParameters = new List<(ModuleMethodBridgeAttribute attr, ParameterInfo param)>();
            
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var bridgeAttr = extensionAttributes.FirstOrDefault(ea => ea.paramIndex == i);
                
                if (bridgeAttr.attr != null)
                {
                    bridgeParameters.Add((bridgeAttr.attr, param));
                }
                else
                {
                    regularParameters.Add(param);
                }
            }
            
            StringBuilder paramBuilder = new StringBuilder();
            var regularParamNames = new List<string>();
            
            foreach (var param in regularParameters)
            {
                if (paramBuilder.Length > 0)
                {
                    paramBuilder.Append(", ");
                }
                
                paramBuilder.Append($"{GetTypeName(param.ParameterType)} {param.Name}");
                regularParamNames.Add(param.Name);
            }
            
            var callParams = new List<string>();
            
            for (int i = 0; i < parameters.Length; i++)
            {
                var bridgeAttr = extensionAttributes.FirstOrDefault(ea => ea.paramIndex == i);
                
                if (bridgeAttr.attr != null)
                {
                    string targetSubmoduleType = GetTargetSubmoduleType(bridgeAttr.attr);
                    string targetMethodName = GetMethodTargetName(bridgeAttr.attr);
                    callParams.Add($"{nameof(GameFeatures)}.{nameof(GameFeatures.GetSubmodule)}<{targetSubmoduleType}>().{targetMethodName}");
                    
                    var moduleProfile = m_ModuleProfileProperty.objectReferenceValue as GameFeatureModuleProfile;
                    if (moduleProfile != null)
                    {
                        GetTargetTypeNamespaces(bridgeAttr.attr, moduleProfile, requiredNamespaces);
                    }
                }
                else
                {
                    callParams.Add(parameters[i].Name);
                }
            }
            
            string returnType = GetTypeName(method.ReturnType);
            bool hasReturnValue = method.ReturnType != typeof(void);
            
            StringBuilder methodBuilder = new StringBuilder();
            
            methodBuilder.Append($"        public static {returnType} {method.Name}(this {submoduleType.Name} self");
            
            if (paramBuilder.Length > 0)
            {
                methodBuilder.Append($", {paramBuilder}");
            }
            
            methodBuilder.AppendLine(")");
            methodBuilder.AppendLine("        {");
            
            if (hasReturnValue)
            {
                methodBuilder.Append($"            return ");
            }
            else
            {
                methodBuilder.Append($"            ");
            }
            
            methodBuilder.AppendLine($"self.{method.Name}({string.Join(", ", callParams)});");
            methodBuilder.AppendLine("        }");
            
            return methodBuilder.ToString();
        }

        /// <summary>
        /// 获取MethodBridgeAttribute的目标子模块类型名
        /// </summary>
        private string GetTargetSubmoduleType(ModuleMethodBridgeAttribute attr)
        {
            string[] pathParts = attr.TargetMethodPath.Split('.');
            if (pathParts.Length == 3)
            {
                return pathParts[1];
            }
            return "UnknownSubmodule";
        }

        /// <summary>
        /// 获取MethodBridgeAttribute的目标方法名
        /// </summary>
        private string GetMethodTargetName(ModuleMethodBridgeAttribute attr)
        {
            string[] pathParts = attr.TargetMethodPath.Split('.');
            if (pathParts.Length == 3)
            {
                return pathParts[2];
            }
            return "UnknownMethod";
        }

        /// <summary>
        /// 获取MethodBridge目标类型的命名空间
        /// </summary>
        private void GetTargetTypeNamespaces(ModuleMethodBridgeAttribute attr, GameFeatureModuleProfile moduleProfile, HashSet<string> namespaces)
        {
            string[] pathParts = attr.TargetMethodPath.Split('.');
            if (pathParts.Length != 3) return;
            
            string targetModuleName = pathParts[0];
            string targetSubmoduleName = pathParts[1];
            
            var targetModule = moduleProfile.Modules
                .FirstOrDefault(m => m != null && m.GetType().Name == targetModuleName);
            
            if (targetModule == null) return;
            
            var targetSubmodule = targetModule.Submodules
                .FirstOrDefault(s => s != null && s.GetType().Name == targetSubmoduleName);
            
            if (targetSubmodule == null) return;
            
            var targetSubmoduleType = targetSubmodule.GetType();
            GetTypeNamespaces(targetSubmoduleType, namespaces);
        }

        /// <summary>
        /// 获取类型所需的命名空间
        /// </summary>
        private void GetTypeNamespaces(Type type, HashSet<string> namespaces)
        {
            if (type == null) return;
            
            if (!string.IsNullOrEmpty(type.Namespace))
            {
                namespaces.Add(type.Namespace);
            }
            
            if (type.IsArray)
            {
                GetTypeNamespaces(type.GetElementType(), namespaces);
                return;
            }
            
            if (type.IsGenericType)
            {
                foreach (var argType in type.GetGenericArguments())
                {
                    GetTypeNamespaces(argType, namespaces);
                }
            }
        }

        /// <summary>
        /// 获取类型的可读名称
        /// </summary>
        private string GetTypeName(Type type)
        {
            // if (type == typeof(void)) return "void";
            // if (type == typeof(int)) return "int";
            // if (type == typeof(float)) return "float";
            // if (type == typeof(double)) return "double";
            // if (type == typeof(string)) return "string";
            // if (type == typeof(bool)) return "bool";
            // if (type == typeof(char)) return "char";
            // if (type == typeof(byte)) return "byte";
            // if (type == typeof(sbyte)) return "sbyte";
            // if (type == typeof(short)) return "short";
            // if (type == typeof(ushort)) return "ushort";
            // if (type == typeof(uint)) return "uint";
            // if (type == typeof(long)) return "long";
            // if (type == typeof(ulong)) return "ulong";
            // if (type == typeof(decimal)) return "decimal";
            // if (type == typeof(object)) return "object";

            if (type.IsArray)
            {
                return $"{GetTypeName(type.GetElementType())}[]";
            }
            
            if (type.IsGenericType)
            {
                string name = type.Name.Substring(0, type.Name.IndexOf('`'));
                string[] args = type.GetGenericArguments().Select(GetTypeName).ToArray();
                
                return $"{name}<{string.Join(", ", args)}>";
            }
            
            return type.Name;
        }
        
        // static GameFeaturesSettingsEditor()
        // {
        //     EditorApplication.update += OnEditorUpdate;
        // }
        //
        // private static void OnEditorUpdate()
        // {
        //     if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isUpdating) return;
        //
        //     if (GameFeaturesSettings.GetOrCreateSettings().ModuleProfile != null)
        //     {
        //         GameFeaturesRunner.Instance.SetProfiles(GameFeaturesSettings.GetOrCreateSettings().ModuleProfile, GameFeaturesSettings.GetOrCreateSettings().ComponentProfile);
        //         GameFeaturesRunner.Instance.skipRuntimeDependencyChecks = GameFeaturesSettings.GetOrCreateSettings().SkipRuntimeDependencyChecks;
        //     }
        // }
    }
}

#endif