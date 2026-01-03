#if UNITY_EDITOR

namespace Verve.Editor
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

    
    /// <summary>
    ///   <para>桥接扩展代码生成器</para>
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
        ///   <para>生成所有桥接扩展方法</para>
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

                // 按模块分组生成
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
                Debug.Log($"✅ Successfully generated {generatedCount} bridge extension classes, updated {updatedCount} files in {outputDir}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error generating bridge extensions: {ex.Message}\n{ex.StackTrace}");
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
        ///   <para>查找所有桥接信息</para>
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
                        var bridgeAttributes = method.GetCustomAttributes<ModuleMethodBridgeAttribute>();
                        foreach (var bridgeAttr in bridgeAttributes)
                        {
                            bridgeInfo.Add(new BridgeMethodInfo
                            {
                                ModuleType = moduleType,
                                SubmoduleType = submoduleType,
                                Method = method,
                                BridgeAttribute = bridgeAttr
                            });
                        }
                    }
                }
            }

            return bridgeInfo;
        }

        /// <summary>
        ///   <para>为模块生成扩展类</para>
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
            var generatedMethodSignatures = new HashSet<string>();

            foreach (var bridgeInfo in bridgeMethods)
            {
                string extensionMethod = GenerateBridgeExtensionMethod(bridgeInfo, requiredNamespaces, moduleProfile);
                
                if (!string.IsNullOrEmpty(extensionMethod) && 
                    !generatedMethodSignatures.Contains(GetMethodSignature(bridgeInfo, bridgeInfo.BridgeAttribute.ExtensionSuffix)))
                {
                    extensionMethodsCode.Append(extensionMethod);
                    generatedMethodSignatures.Add(GetMethodSignature(bridgeInfo, bridgeInfo.BridgeAttribute.ExtensionSuffix));
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
            sb.AppendLine($"    public static class {moduleType.Name}BridgeExtensions");
            sb.AppendLine($"    {{");
            sb.Append(extensionMethodsCode.ToString());
            sb.AppendLine($"    }}");
            sb.AppendLine("}");
            sb.Append(GENERATED_CODE_FOOTER);

            return sb.ToString();
        }

        /// <summary>
        ///   <para>生成桥接扩展方法</para>
        /// </summary>
        private static string GenerateBridgeExtensionMethod(BridgeMethodInfo bridgeInfo, HashSet<string> requiredNamespaces, GameFeatureModuleProfile moduleProfile)
        {
            var method = bridgeInfo.Method;
            var bridgeAttr = bridgeInfo.BridgeAttribute;
            
            // 解析目标方法
            var targetMethodInfo = ResolveTargetMethod(bridgeAttr, moduleProfile);
            if (targetMethodInfo == null)
            {
                Debug.LogWarning($"Could not resolve target method: {bridgeAttr.TargetModule}.{bridgeAttr.TargetSubmodule}.{bridgeAttr.TargetMethod}");
                return null;
            }

            // 收集命名空间
            CollectRequiredNamespaces(method, targetMethodInfo, requiredNamespaces);

            // 确定桥接参数
            var bridgeParameter = FindBridgeParameter(method);
            if (bridgeParameter == null)
            {
                Debug.LogWarning($"No bridge parameter found in method: {method.Name}");
                return null;
            }

            // 生成扩展方法
            return GenerateExtensionMethodCode(method, targetMethodInfo, bridgeAttr, bridgeParameter, bridgeInfo.SubmoduleType);
        }

        /// <summary>
        ///   <para>解析目标方法信息</para>
        /// </summary>
        private static MethodInfo ResolveTargetMethod(ModuleMethodBridgeAttribute bridgeAttr, GameFeatureModuleProfile moduleProfile)
        {
            var targetModule = moduleProfile.Modules
                .FirstOrDefault(m => m != null && m.GetType().Name == bridgeAttr.TargetModule);
            
            if (targetModule == null) 
            {
                Debug.LogWarning($"Target module not found: {bridgeAttr.TargetModule}");
                return null;
            }

            var targetSubmodule = targetModule.Submodules
                .FirstOrDefault(s => s != null && s.GetType().Name == bridgeAttr.TargetSubmodule);
            
            if (targetSubmodule == null)
            {
                Debug.LogWarning($"Target submodule not found: {bridgeAttr.TargetSubmodule}");
                return null;
            }

            var targetType = targetSubmodule.GetType();
            var targetMethod = targetType.GetMethod(bridgeAttr.TargetMethod, 
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            
            if (targetMethod == null)
            {
                Debug.LogWarning($"Target method not found: {bridgeAttr.TargetMethod} in {targetType.Name}");
                return null;
            }

            return targetMethod;
        }

        /// <summary>
        ///   <para>查找桥接参数</para>
        /// </summary>
        private static ParameterInfo FindBridgeParameter(MethodInfo method)
        {
            var parameters = method.GetParameters();
            
            // 查找标记了 BridgeParameterAttribute 的参数
            foreach (var param in parameters)
            {
                var bridgeParamAttr = param.GetCustomAttribute<BridgeParameterAttribute>();
                if (bridgeParamAttr != null)
                {
                    return param;
                }
            }
            
            // 如果没有标记的参数，返回第一个参数
            return parameters.Length > 0 ? parameters[0] : null;
        }

        /// <summary>
        ///   <para>生成扩展方法代码</para>
        /// </summary>
        private static string GenerateExtensionMethodCode(MethodInfo sourceMethod, MethodInfo targetMethod, 
            ModuleMethodBridgeAttribute bridgeAttr, ParameterInfo bridgeParameter, Type submoduleType)
        {
            var methodBuilder = new StringBuilder();
            
            // 方法签名
            string extensionMethodName = $"{sourceMethod.Name}With{bridgeAttr.ExtensionSuffix}";
            string returnType = GetTypeDisplayName(sourceMethod.ReturnType);
            bool isSourceAsync = IsAsyncMethod(sourceMethod);
            bool isTargetAsync = IsAsyncMethod(targetMethod);
            
            // 泛型参数
            var genericParams = sourceMethod.GetGenericArguments();
            string genericDeclaration = genericParams.Length > 0 ? 
                $"<{string.Join(", ", genericParams.Select(p => p.Name))}>" : "";
            
            // 泛型约束
            string genericConstraints = GenerateGenericConstraints(sourceMethod);
            
            // 开始构建方法
            if (isSourceAsync)
            {
                methodBuilder.Append($"        public static async {returnType} {extensionMethodName}{genericDeclaration}");
            }
            else
            {
                methodBuilder.Append($"        public static {returnType} {extensionMethodName}{genericDeclaration}");
            }
            
            methodBuilder.Append($"(this {GetTypeDisplayName(submoduleType)} self");
            
            // 目标方法参数
            var targetParameters = targetMethod.GetParameters()
                .Select(p => GenerateParameterCode(p)).ToList();
            
            // 源方法参数（排除桥接参数）
            var sourceParameters = sourceMethod.GetParameters()
                .Where(p => p != bridgeParameter)
                .Select(p => GenerateParameterCode(p)).ToList();
            
            // 合并参数
            var allParameters = targetParameters.Concat(sourceParameters);
            foreach (var param in allParameters)
            {
                methodBuilder.Append($", {param}");
            }
            
            methodBuilder.AppendLine($"){genericConstraints}");
            methodBuilder.AppendLine("        {");
            
            // 生成方法调用 - 直接内联，不声明额外变量
            string methodCall = GenerateDirectMethodCall(sourceMethod, targetMethod, bridgeParameter, isSourceAsync, isTargetAsync);
            methodBuilder.AppendLine($"            {methodCall}");
            
            methodBuilder.AppendLine("        }");
            methodBuilder.AppendLine();

            return methodBuilder.ToString();
        }

        /// <summary>
        ///   <para>生成直接方法调用（不声明额外变量）</para>
        /// </summary>
        private static string GenerateDirectMethodCall(MethodInfo sourceMethod, MethodInfo targetMethod, 
            ParameterInfo bridgeParameter, bool isSourceAsync, bool isTargetAsync)
        {
            // 构建目标方法调用
            string targetCall = GenerateTargetMethodCallExpression(targetMethod, bridgeParameter.ParameterType, isTargetAsync);
            
            // 构建源方法调用参数
            var callParams = new List<string>();
            foreach (var param in sourceMethod.GetParameters())
            {
                if (param == bridgeParameter)
                {
                    // 桥接参数替换为目标方法调用
                    callParams.Add(targetCall);
                }
                else
                {
                    callParams.Add(param.Name);
                }
            }
            
            // 构建源方法调用
            string sourceCall = $"self.{sourceMethod.Name}";
            
            // 处理源方法的泛型参数
            var sourceGenericParams = sourceMethod.GetGenericArguments();
            if (sourceGenericParams.Length > 0)
            {
                sourceCall += $"<{string.Join(", ", sourceGenericParams.Select(GetTypeDisplayName))}>";
            }
            
            sourceCall += $"({string.Join(", ", callParams)})";
            
            // 处理返回值
            if (sourceMethod.ReturnType == typeof(void))
            {
                return isSourceAsync ? $"await {sourceCall};" : $"{sourceCall};";
            }
            else
            {
                return isSourceAsync ? $"return await {sourceCall};" : $"return {sourceCall};";
            }
        }

        /// <summary>
        ///   <para>生成目标方法调用表达式</para>
        /// </summary>
        private static string GenerateTargetMethodCallExpression(MethodInfo targetMethod, Type bridgeParameterType, bool isTargetAsync)
        {
            var targetParams = targetMethod.GetParameters();
            var paramNames = targetParams.Select(p => p.Name).ToList();
            
            string call = $"{nameof(GameFeatures)}.{nameof(GameFeatures.GetSubmodule)}<{GetTypeDisplayName(targetMethod.DeclaringType)}>().{targetMethod.Name}";
            
            // 处理目标方法的泛型参数 - 关键修复：使用桥接参数的实际类型
            var targetGenericParams = targetMethod.GetGenericArguments();
            if (targetGenericParams.Length > 0)
            {
                // 关键修复：使用桥接参数的实际类型，而不是泛型参数名
                string actualTypeName = GetTypeDisplayName(bridgeParameterType);
                call += $"<{actualTypeName}>";
            }
            
            call += $"({string.Join(", ", paramNames)})";
            
            // 处理异步调用
            if (isTargetAsync)
            {
                call = $"await {call}";
            }
            
            return call;
        }

        /// <summary>
        ///   <para>生成泛型约束</para>
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
                    if (constraint != typeof(ValueType) && constraint != typeof(Enum))
                    {
                        constraintBuilder.Add(GetTypeDisplayName(constraint));
                    }
                }
                
                // 特殊约束
                var attributes = genericParam.GenericParameterAttributes;
                if (attributes.HasFlag(System.Reflection.GenericParameterAttributes.NotNullableValueTypeConstraint))
                {
                    constraintBuilder.Add("struct");
                }
                else if (attributes.HasFlag(System.Reflection.GenericParameterAttributes.ReferenceTypeConstraint))
                {
                    constraintBuilder.Add("class");
                }
                
                if (attributes.HasFlag(System.Reflection.GenericParameterAttributes.DefaultConstructorConstraint) &&
                    !attributes.HasFlag(System.Reflection.GenericParameterAttributes.NotNullableValueTypeConstraint))
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
        ///   <para>收集所需的命名空间</para>
        /// </summary>
        private static void CollectRequiredNamespaces(MethodInfo sourceMethod, MethodInfo targetMethod, HashSet<string> namespaces)
        {
            void AddNamespace(Type type)
            {
                if (type == null) return;
                
                if (type.Namespace != null && !type.Namespace.StartsWith("System") && type.Namespace != "System")
                {
                    namespaces.Add(type.Namespace);
                }
                
                if (type.IsArray)
                {
                    AddNamespace(type.GetElementType());
                    return;
                }
                
                if (type.IsGenericType)
                {
                    foreach (var arg in type.GetGenericArguments())
                    {
                        AddNamespace(arg);
                    }
                }
            }

            AddNamespace(sourceMethod.ReturnType);
            foreach (var param in sourceMethod.GetParameters())
            {
                AddNamespace(param.ParameterType);
            }
            
            AddNamespace(targetMethod.ReturnType);
            foreach (var param in targetMethod.GetParameters())
            {
                AddNamespace(param.ParameterType);
            }
            
            if (IsAsyncMethod(sourceMethod) || IsAsyncMethod(targetMethod))
            {
                namespaces.Add("System.Threading.Tasks");
            }
        }

        /// <summary>
        ///   <para>生成参数代码</para>
        /// </summary>
        private static string GenerateParameterCode(ParameterInfo parameter)
        {
            string typeName = GetTypeDisplayName(parameter.ParameterType);
            string paramName = parameter.Name;
            
            if (parameter.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0)
            {
                typeName = $"params {typeName.Replace("[]", "")}[]";
            }
            
            string defaultValue = "";
            if (parameter.HasDefaultValue)
            {
                if (parameter.DefaultValue != null && parameter.DefaultValue.GetType() != typeof(DBNull))
                {
                    defaultValue = $" = {GetDefaultValueCode(parameter.DefaultValue, parameter.ParameterType)}";
                }
                else if (parameter.ParameterType.IsValueType)
                {
                    defaultValue = $" = default({GetTypeDisplayName(parameter.ParameterType)})";
                }
                else
                {
                    defaultValue = " = null";
                }
            }
            
            return $"{typeName} {paramName}{defaultValue}";
        }

        /// <summary>
        ///   <para>获取类型的显示名称</para>
        /// </summary>
        private static string GetTypeDisplayName(Type type)
        {
            if (type == null) return "object";
            
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
                
                if (name == "Task" && type.GetGenericArguments().Length == 1)
                {
                    return $"Task<{args.First()}>";
                }
                
                // 处理 Action<T> 等委托类型
                if (name.StartsWith("Action") || name.StartsWith("Func"))
                {
                    return $"{name}<{string.Join(", ", args)}>";
                }
                
                return $"{name}<{string.Join(", ", args)}>";
            }
            
            return type.Name;
        }

        /// <summary>
        ///   <para>获取默认值的代码表示</para>
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
            
            if (type == typeof(char))
                return $"'{defaultValue}'";
            
            if (type.IsEnum)
                return $"{GetTypeDisplayName(type)}.{defaultValue}";
            
            return defaultValue.ToString();
        }

        /// <summary>
        ///   <para>检查是否为异步方法</para>
        /// </summary>
        private static bool IsAsyncMethod(MethodInfo method)
        {
            return typeof(Task).IsAssignableFrom(method.ReturnType) ||
                   (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
        }

        /// <summary>
        ///   <para>获取方法签名（用于去重）</para>
        /// </summary>
        private static string GetMethodSignature(BridgeMethodInfo bridgeInfo, string suffix)
        {
            var parameters = bridgeInfo.Method.GetParameters()
                .Where(p => p.GetCustomAttribute<BridgeParameterAttribute>() == null)
                .Select(p => $"{GetTypeDisplayName(p.ParameterType)}_{p.Name}");
                
            return $"{bridgeInfo.Method.Name}With{suffix}_{string.Join("_", parameters)}";
        }

        /// <summary>
        ///   <para>检查文件是否需要更新</para>
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

        #region Helper Classes

        private class BridgeMethodInfo
        {
            public Type ModuleType { get; set; }
            public Type SubmoduleType { get; set; }
            public MethodInfo Method { get; set; }
            public ModuleMethodBridgeAttribute BridgeAttribute { get; set; }
        }

        #endregion
    }
}

#endif