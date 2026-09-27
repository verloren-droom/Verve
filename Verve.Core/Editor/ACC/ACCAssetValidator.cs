#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;

    /// <summary>
    ///   <para>ACC 资产校验器；检查能力表单能否安全转换为运行时对象。</para>
    /// </summary>
    internal static class ACCAssetValidator
    {
        /// <summary>
        ///   <para>校验所有能力表单资产；结果供编辑器和构建流程复用。</para>
        /// </summary>
        /// <returns>校验结果。</returns>
        public static ACCValidationReport ValidateAll()
        {
            var report = new ACCValidationReport();
            var guids = AssetDatabase.FindAssets("t:CapabilitySheetAsset");
            Array.Sort(guids, StringComparer.Ordinal);

            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<CapabilitySheetAsset>(path);
                report.AssetCount++;
                if (asset == null)
                {
                    report.AddError(path, "无法加载能力表单资产。");
                    continue;
                }

                Validate(asset, path, report, new HashSet<CapabilitySheetAsset>());
            }

            return report;
        }

        /// <summary>
        ///   <para>校验单个能力表单及其递归子表单。</para>
        /// </summary>
        /// <param name="asset">待校验资产。</param>
        /// <param name="path">资产路径。</param>
        /// <param name="report">接收校验结果。</param>
        /// <param name="visited">当前递归路径中的资产。</param>
        private static void Validate(
            CapabilitySheetAsset asset,
            string path,
            ACCValidationReport report,
            HashSet<CapabilitySheetAsset> visited)
        {
            if (!visited.Add(asset))
            {
                report.AddError(path, "检测到能力表单循环引用。");
                return;
            }

            var capabilityNames = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < asset.CapabilityTypeEntries.Count; i++)
            {
                var entry = asset.CapabilityTypeEntries[i];
                if (entry == null)
                {
                    report.AddError(path, $"能力条目 {i + 1} 为空。");
                    continue;
                }

                var type = ResolveType(entry.AssemblyQualifiedName, path, report, $"能力条目 {i + 1}");
                if (type == null) continue;
                if (!typeof(Capability).IsAssignableFrom(type))
                {
                    report.AddError(path, $"能力类型 {type.FullName} 未继承 Capability。");
                    continue;
                }

                if (type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null)
                    report.AddError(path, $"能力类型 {type.FullName} 必须是非抽象且拥有公开无参构造函数的类型。");
                AddDuplicateError(capabilityNames, type, "能力", path, report);
            }

            var componentNames = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < asset.ComponentTypeEntries.Count; i++)
            {
                var entry = asset.ComponentTypeEntries[i];
                if (entry == null)
                {
                    report.AddError(path, $"组件条目 {i + 1} 为空。");
                    continue;
                }

                var type = ResolveType(entry.AssemblyQualifiedName, path, report, $"组件条目 {i + 1}");
                if (type == null) continue;
                if (!typeof(IComponent).IsAssignableFrom(type) || !type.IsValueType)
                {
                    report.AddError(path, $"组件类型 {type.FullName} 必须是实现 IComponent 的结构体。");
                    continue;
                }

                AddDuplicateError(componentNames, type, "组件", path, report);
            }

            var childNames = new HashSet<CapabilitySheetAsset>();
            for (int i = 0; i < asset.SubSheets.Count; i++)
            {
                var child = asset.SubSheets[i];
                if (child == null)
                {
                    report.AddError(path, $"子表单条目 {i + 1} 为空。");
                    continue;
                }

                if (!childNames.Add(child))
                    report.AddError(path, $"子表单 {child.name} 被重复引用。");

                var childPath = AssetDatabase.GetAssetPath(child);
                if (visited.Contains(child))
                {
                    report.AddError(path, $"子表单 {child.name} 会形成循环引用。");
                    continue;
                }

                Validate(child, childPath, report, visited);
            }

            visited.Remove(asset);
        }

        /// <summary>
        ///   <para>解析表单中保存的程序集限定类型名。</para>
        /// </summary>
        /// <param name="assemblyQualifiedName">程序集限定类型名。</param>
        /// <param name="path">资产路径。</param>
        /// <param name="report">接收校验结果。</param>
        /// <param name="entryName">条目名称。</param>
        /// <returns>解析到的类型；失败时返回 <see langword="null"/>。</returns>
        private static Type ResolveType(
            string assemblyQualifiedName,
            string path,
            ACCValidationReport report,
            string entryName)
        {
            if (string.IsNullOrWhiteSpace(assemblyQualifiedName))
            {
                report.AddError(path, $"{entryName}没有保存类型信息。");
                return null;
            }

            Type type;
            try
            {
                type = Type.GetType(assemblyQualifiedName, false);
            }
            catch (ArgumentException exception)
            {
                report.AddError(path, $"{entryName}的类型名称格式无效：{exception.Message}");
                return null;
            }

            if (type == null)
                report.AddError(path, $"{entryName}无法解析类型 {assemblyQualifiedName}。");
            return type;
        }

        /// <summary>
        ///   <para>登记重复类型错误。</para>
        /// </summary>
        /// <param name="names">已登记类型名称。</param>
        /// <param name="type">待登记类型。</param>
        /// <param name="kind">类型类别。</param>
        /// <param name="path">资产路径。</param>
        /// <param name="report">接收校验结果。</param>
        private static void AddDuplicateError(
            HashSet<string> names,
            Type type,
            string kind,
            string path,
            ACCValidationReport report)
        {
            if (!names.Add(type.AssemblyQualifiedName))
                report.AddError(path, $"{kind}类型 {type.FullName} 被重复添加。");
        }
    }

    /// <summary>
    ///   <para>ACC 校验结果；保存资产数量和可定位的问题。</para>
    /// </summary>
    internal sealed class ACCValidationReport
    {
        /// <summary>
        ///   <para>校验的资产数量。</para>
        /// </summary>
        public int AssetCount { get; internal set; }
        /// <summary>
        ///   <para>错误数量。</para>
        /// </summary>
        public int ErrorCount { get; private set; }
        /// <summary>
        ///   <para>问题列表。</para>
        /// </summary>
        public List<ACCValidationIssue> Issues { get; } = new();
        /// <summary>
        ///   <para>是否通过校验。</para>
        /// </summary>
        public bool IsValid => ErrorCount == 0;

        /// <summary>
        ///   <para>添加错误问题。</para>
        /// </summary>
        /// <param name="path">资产路径。</param>
        /// <param name="message">错误信息。</param>
        public void AddError(string path, string message)
        {
            ErrorCount++;
            Issues.Add(new ACCValidationIssue(path, message));
        }
    }

    /// <summary>
    ///   <para>ACC 校验问题；关联资产路径和错误信息。</para>
    /// </summary>
    internal readonly struct ACCValidationIssue
    {
        /// <summary>
        ///   <para>资产路径。</para>
        /// </summary>
        public readonly string path;
        /// <summary>
        ///   <para>问题信息。</para>
        /// </summary>
        public readonly string message;

        /// <summary>
        ///   <para>创建校验问题。</para>
        /// </summary>
        /// <param name="path">资产路径。</param>
        /// <param name="message">问题信息。</param>
        public ACCValidationIssue(string path, string message)
        {
            this.path = path;
            this.message = message;
        }
    }
}

#endif
