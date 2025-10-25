#if UNITY_EDITOR

namespace VerveEditor.UniEx
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEngine;
    using Verve.UniEx;
    using Object = UnityEngine.Object;

    
    /// <summary>
    /// 桥接扩展代码生成器
    /// </summary>
    public static class BridgeExtensionGenerator
    {
        private const string GENERATED_CODE_HEADER = @"// =================================================
// AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
// =================================================

#if UNITY_5_3_OR_NEWER
";

        private const string GENERATED_CODE_FOOTER = @"
#endif";

        /// <summary>
        /// 生成所有桥接扩展方法
        /// </summary>
        public static void GenerateAllBridgeExtensions(GameFeatureModuleProfile moduleProfile, string outputDir)
        {
            if (moduleProfile == null)
            {
                Debug.LogError("Module profile is null. Cannot generate bridge extensions.");
                return;
            }

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string tempFolder = Path.Combine(Application.temporaryCachePath, "BridgeExtensionsTemp");
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
            Directory.CreateDirectory(tempFolder);

            int generatedCount = 0;
            int updatedCount = 0;

            try
            {
                var bridgeInfo = FindBridgeInformation(moduleProfile);
                
                if (bridgeInfo.Count == 0)
                {
                    Debug.Log("No bridge methods found to generate.");
                    return;
                }

                var modulesGrouped = bridgeInfo.GroupBy(info => info.ModuleType);
                
                foreach (var moduleGroup in modulesGrouped)
                {
                    var moduleType = moduleGroup.Key;
                    var moduleBridges = moduleGroup.ToList();
                    
                    string extensionCode = GenerateModuleExtensionClass(moduleType, moduleBridges, moduleProfile);
                    
                    if (!string.IsNullOrEmpty(extensionCode))
                    {
                        string fileName = $"{moduleType.Name}BridgeExtensions.cs";
                        string tempFilePath = Path.Combine(tempFolder, fileName);
                        string targetFilePath = Path.Combine(outputDir, fileName);
                        
                        File.WriteAllText(tempFilePath, extensionCode, Encoding.UTF8);
                        
                        if (ShouldUpdateFile(tempFilePath, targetFilePath))
                        {
                            File.Copy(tempFilePath, targetFilePath, true);
                            updatedCount++;
                        }
                        
                        generatedCount++;
                    }
                }
                
                AssetDatabase.Refresh();
                Debug.Log($"Successfully generated {generatedCount} bridge extension classes, updated {updatedCount} files in {outputDir}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating bridge extensions: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                if (Directory.Exists(tempFolder))
                {
                    try
                    {
                        Directory.Delete(tempFolder, true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to clean up temporary files: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 查找所有桥接信息
        /// </summary>
        private static List<BridgeMethodInfo> FindBridgeInformation(GameFeatureModuleProfile moduleProfile)
        {
            var bridgeInfo = new List<BridgeMethodInfo>();

            foreach (var module in moduleProfile.Modules)
            {
                if (module == null) continue;

                var moduleType = module.GetType();
                
                foreach (var submodule in module.Submodules)
                {
                    if (submodule == null) continue;

                    var submoduleType = submodule.GetType();
                    var methods = submoduleType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    
                    foreach (var method in methods)
                    {
                        var bridgeAttributes = GetBridgeAttributes(method);
                        if (bridgeAttributes.Count > 0)
                        {
                            bridgeInfo.Add(new BridgeMethodInfo
                            {
                                ModuleType = moduleType,
                                SubmoduleType = submoduleType,
                                Method = method,
                                BridgeAttributes = bridgeAttributes
                            });
                        }
                    }
                }
            }

            return bridgeInfo;
        }

        /// <summary>
        /// 获取方法的桥接特性
        /// </summary>
        private static List<BridgeAttributeInfo> GetBridgeAttributes(MethodInfo method)
        {
            var bridgeAttributes = new List<BridgeAttributeInfo>();
            var parameters = method.GetParameters();

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var bridgeAttr = parameter.GetCustomAttribute<ModuleMethodBridgeAttribute>();
                
                if (bridgeAttr != null)
                {
                    bridgeAttributes.Add(new BridgeAttributeInfo
                    {
                        ParameterIndex = i,
                        Parameter = parameter,
                        Attribute = bridgeAttr
                    });
                }
            }

            return bridgeAttributes;
        }

        /// <summary>
        /// 为模块生成扩展类
        /// </summary>
        private static string GenerateModuleExtensionClass(Type moduleType, List<BridgeMethodInfo> bridgeMethods, GameFeatureModuleProfile moduleProfile)
        {
            if (bridgeMethods.Count == 0)
                return null;

            var requiredNamespaces = new HashSet<string>
            {
                "System",
                "UnityEngine"
            };

            var extensionMethodsCode = new StringBuilder();
            var generatedMethods = new HashSet<string>();

            foreach (var bridgeInfo in bridgeMethods)
            {
                string extensionMethod = GenerateBridgeExtensionMethod(bridgeInfo, requiredNamespaces, moduleProfile);
                
                if (!string.IsNullOrEmpty(extensionMethod) && !generatedMethods.Contains(GetMethodSignature(bridgeInfo.Method)))
                {
                    extensionMethodsCode.Append(extensionMethod);
                    generatedMethods.Add(GetMethodSignature(bridgeInfo.Method));
                }
            }

            if (extensionMethodsCode.Length == 0)
                return null;

            var sb = new StringBuilder();
            sb.Append(GENERATED_CODE_HEADER);
            
            var sortedNamespaces = requiredNamespaces
                .Where(ns => !string.IsNullOrEmpty(ns))
                .OrderBy(ns => ns.StartsWith("System") ? 0 : 1)
                .ThenBy(ns => ns)
                .ToList();
                
            foreach (var ns in sortedNamespaces)
            {
                sb.AppendLine($"using {ns};");
            }
            sb.AppendLine();

            string namespaceName = moduleType.Namespace ?? "Verve.UniEx.Generated";
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    public static class {moduleType.Name}Extensions");
            sb.AppendLine($"    {{");
            sb.Append(extensionMethodsCode.ToString());
            sb.AppendLine($"    }}");
            sb.AppendLine("}");
            sb.Append(GENERATED_CODE_FOOTER);

            return sb.ToString();
        }

        /// <summary>
        /// 生成桥接扩展方法
        /// </summary>
        private static string GenerateBridgeExtensionMethod(BridgeMethodInfo bridgeInfo, HashSet<string> requiredNamespaces, GameFeatureModuleProfile moduleProfile)
        {
            var method = bridgeInfo.Method;
            var parameters = method.GetParameters();
            var bridgeAttributes = bridgeInfo.BridgeAttributes;

            if (bridgeAttributes.Count == 0)
                return null;

            var bridgeAttr = bridgeAttributes[0];
            var targetMethodInfo = ResolveTargetMethod(bridgeAttr.Attribute, moduleProfile);
            
            if (targetMethodInfo == null)
            {
                Debug.LogWarning($"Could not resolve target method for bridge: {bridgeAttr.Attribute.MethodName}");
                return null;
            }

            FindTypeNamespaces(method.ReturnType, requiredNamespaces);
            foreach (var param in parameters)
            {
                FindTypeNamespaces(param.ParameterType, requiredNamespaces);
            }
            FindTypeNamespaces(targetMethodInfo.ReturnType, requiredNamespaces);
            foreach (var param in targetMethodInfo.GetParameters())
            {
                FindTypeNamespaces(param.ParameterType, requiredNamespaces);
            }

            var regularParams = new List<ParameterInfo>();
            var bridgeParams = new List<BridgeAttributeInfo>();

            for (int i = 0; i < parameters.Length; i++)
            {
                var currentBridgeAttr = bridgeAttributes.FirstOrDefault(attr => attr.ParameterIndex == i);
                if (currentBridgeAttr != null)
                {
                    bridgeParams.Add(currentBridgeAttr);
                }
                else
                {
                    regularParams.Add(parameters[i]);
                }
            }

            var methodBuilder = new StringBuilder();
            
            var genericParams = method.GetGenericArguments();
            string genericConstraints = GenerateGenericConstraints(method);
            
            string returnType = GetTypeDisplayName(method.ReturnType);
            bool isAsync = IsAsyncMethod(method);
            bool targetIsAsync = IsAsyncMethod(targetMethodInfo);
            
            if (isAsync)
            {
                methodBuilder.Append($"        public static async {returnType} {method.Name}");
            }
            else
            {
                methodBuilder.Append($"        public static {returnType} {method.Name}");
            }
            
            if (genericParams.Length > 0)
            {
                methodBuilder.Append($"<{string.Join(", ", genericParams.Select(p => p.Name))}>");
            }
            
            methodBuilder.Append($"(this {bridgeInfo.SubmoduleType.Name} self");
            
            var targetParameters = targetMethodInfo.GetParameters();
            var extensionParams = new List<string>();
            
            foreach (var targetParam in targetParameters)
            {
                string paramCode = GenerateParameterCode(targetParam);
                extensionParams.Add(paramCode);
                methodBuilder.Append($", {paramCode}");
            }
            
            foreach (var param in regularParams)
            {
                string paramCode = GenerateParameterCode(param);
                extensionParams.Add(paramCode);
                methodBuilder.Append($", {paramCode}");
            }
            
            methodBuilder.AppendLine($"){genericConstraints}");
            methodBuilder.AppendLine("        {");
            
            string bridgeCall = GenerateBridgeCall(targetMethodInfo, targetIsAsync, extensionParams);
            
            if (method.ReturnType != typeof(void))
            {
                if (isAsync)
                {
                    methodBuilder.Append($"            return await self.{method.Name}(");
                }
                else
                {
                    methodBuilder.Append($"            return self.{method.Name}(");
                }
            }
            else
            {
                if (isAsync)
                {
                    methodBuilder.Append($"            await self.{method.Name}(");
                }
                else
                {
                    methodBuilder.Append($"            self.{method.Name}(");
                }
            }
            
            var callParams = new List<string>();
            int paramIndex = 0;
            
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i == bridgeAttr.ParameterIndex)
                {
                    callParams.Add(bridgeCall);
                }
                else
                {
                    callParams.Add(parameters[i].Name);
                }
                paramIndex++;
            }
            
            methodBuilder.AppendLine($"{string.Join(", ", callParams)});");
            methodBuilder.AppendLine("        }");
            methodBuilder.AppendLine();

            return methodBuilder.ToString();
        }

        /// <summary>
        /// 解析目标方法信息
        /// </summary>
        private static MethodInfo ResolveTargetMethod(ModuleMethodBridgeAttribute bridgeAttr, GameFeatureModuleProfile moduleProfile)
        {
            string targetModuleName = bridgeAttr.ModuleName;
            string targetSubmoduleName = bridgeAttr.SubmoduleName;
            string targetMethodName = bridgeAttr.MethodName;

            var targetModule = moduleProfile.Modules
                .FirstOrDefault(m => m != null && m.GetType().Name == targetModuleName);
            
            if (targetModule == null)
            {
                Debug.LogWarning($"Target module not found: {targetModuleName}");
                return null;
            }

            var targetSubmodule = targetModule.Submodules
                .FirstOrDefault(s => s != null && s.GetType().Name == targetSubmoduleName);
            
            if (targetSubmodule == null)
            {
                Debug.LogWarning($"Target submodule not found: {targetSubmoduleName}");
                return null;
            }

            var targetMethod = targetSubmodule.GetType()
                .GetMethod(targetMethodName, BindingFlags.Public | BindingFlags.Instance);
            
            if (targetMethod == null)
            {
                Debug.LogWarning($"Target method not found: {targetMethodName}");
                return null;
            }

            return targetMethod;
        }

        /// <summary>
        /// 生成桥接调用代码
        /// </summary>
        private static string GenerateBridgeCall(MethodInfo targetMethod, bool isAsync, List<string> parameters)
        {
            var targetParams = targetMethod.GetParameters();
            var paramNames = targetParams.Select(p => p.Name).ToList();
            
            string call = $"{nameof(GameFeatures)}.{nameof(GameFeatures.GetSubmodule)}<{targetMethod.DeclaringType.Name}>().{targetMethod.Name}";
            
            var genericParams = targetMethod.GetGenericArguments();
            if (genericParams.Length > 0)
            {
                call += $"<{string.Join(", ", genericParams.Select(p => p.Name))}>";
            }
            
            call += $"({string.Join(", ", paramNames)})";
            
            if (isAsync)
            {
                call = $"await {call}";
            }
            
            return call;
        }

        /// <summary>
        /// 生成参数代码
        /// </summary>
        private static string GenerateParameterCode(ParameterInfo parameter)
        {
            string typeName = GetTypeDisplayName(parameter.ParameterType);
            string paramName = parameter.Name;
            string defaultValue = "";
            
            if (parameter.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0)
            {
                typeName = $"params {typeName.Replace("[]", "")} []";
            }
            
            if (parameter.HasDefaultValue)
            {
                defaultValue = $" = {GetDefaultValueCode(parameter.DefaultValue, parameter.ParameterType)}";
            }
            
            return $"{typeName} {paramName}{defaultValue}";
        }

        /// <summary>
        /// 生成泛型约束
        /// </summary>
        private static string GenerateGenericConstraints(MethodInfo method)
        {
            var constraints = new List<string>();
            var genericParams = method.GetGenericArguments();
            
            foreach (var genericParam in genericParams)
            {
                var constraintBuilder = new List<string>();
                
                // 类型约束
                var typeConstraints = genericParam.GetGenericParameterConstraints();
                foreach (var constraint in typeConstraints)
                {
                    constraintBuilder.Add(GetTypeDisplayName(constraint));
                }
                
                if (genericParam.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
                {
                    constraintBuilder.Add("struct");
                }
                if (genericParam.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
                {
                    constraintBuilder.Add("class");
                }
                if (genericParam.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint))
                {
                    constraintBuilder.Add("new()");
                }
                
                if (constraintBuilder.Count > 0)
                {
                    constraints.Add($"where {genericParam.Name} : {string.Join(", ", constraintBuilder)}");
                }
            }
            
            return constraints.Count > 0 ? " " + string.Join(" ", constraints) : "";
        }

        /// <summary>
        /// 检查文件是否需要更新
        /// </summary>
        private static bool ShouldUpdateFile(string tempFilePath, string targetFilePath)
        {
            if (!File.Exists(targetFilePath))
                return true;

            try
            {
                string tempContent = File.ReadAllText(tempFilePath);
                string targetContent = File.ReadAllText(targetFilePath);
                return tempContent != targetContent;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to compare files, will update: {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// 查找类型所需的命名空间
        /// </summary>
        private static void FindTypeNamespaces(Type type, HashSet<string> namespaces)
        {
            if (type == null || type.Namespace == null) 
                return;

            string ns = type.Namespace;
            if (!ns.StartsWith("System") || 
                ns == "System.Collections" || 
                ns == "System.Collections.Generic" ||
                ns == "System.Threading.Tasks")
            {
                namespaces.Add(ns);
            }

            if (type.IsArray)
            {
                FindTypeNamespaces(type.GetElementType(), namespaces);
                return;
            }

            if (type.IsGenericType)
            {
                foreach (var argType in type.GetGenericArguments())
                {
                    FindTypeNamespaces(argType, namespaces);
                }
            }

            if (typeof(Task).IsAssignableFrom(type) && type.IsGenericType)
            {
                namespaces.Add("System.Threading.Tasks");
            }
        }

        /// <summary>
        /// 获取类型的显示名称
        /// </summary>
        private static string GetTypeDisplayName(Type type)
        {
            if (type == typeof(void)) return "void";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(char)) return "char";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(object)) return "object";
            if (type == typeof(Task)) return "Task";

            if (type.IsArray)
            {
                return $"{GetTypeDisplayName(type.GetElementType())}[]";
            }
            
            if (type.IsGenericType)
            {
                string name = type.Name.Split('`')[0];
                var args = type.GetGenericArguments().Select(GetTypeDisplayName);
                
                if (name == "Nullable")
                {
                    return $"{args.First()}?";
                }
                
                return $"{name}<{string.Join(", ", args)}>";
            }

            return type.Name;
        }

        /// <summary>
        /// 获取默认值的代码表示
        /// </summary>
        private static string GetDefaultValueCode(object defaultValue, Type type)
        {
            if (defaultValue == null)
                return "null";
            
            if (type == typeof(string))
                return $"\"{defaultValue}\"";
            
            if (type == typeof(bool))
                return defaultValue.ToString().ToLower();
            
            if (type == typeof(float))
                return $"{defaultValue}f";
            
            if (type.IsEnum)
                return $"{type.Name}.{defaultValue}";
            
            return defaultValue.ToString();
        }

        /// <summary>
        /// 检查是否为异步方法
        /// </summary>
        private static bool IsAsyncMethod(MethodInfo method)
        {
            return typeof(Task).IsAssignableFrom(method.ReturnType) ||
                   (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
        }

        /// <summary>
        /// 获取方法签名（用于去重）
        /// </summary>
        private static string GetMethodSignature(MethodInfo method)
        {
            var parameters = method.GetParameters().Select(p => $"{p.ParameterType.Name}_{p.Name}");
            return $"{method.Name}_{string.Join("_", parameters)}";
        }

        #region Helper Classes

        private class BridgeMethodInfo
        {
            public Type ModuleType { get; set; }
            public Type SubmoduleType { get; set; }
            public MethodInfo Method { get; set; }
            public List<BridgeAttributeInfo> BridgeAttributes { get; set; }
        }

        private class BridgeAttributeInfo
        {
            public int ParameterIndex { get; set; }
            public ParameterInfo Parameter { get; set; }
            public ModuleMethodBridgeAttribute Attribute { get; set; }
        }

        #endregion
    }
}

#endif