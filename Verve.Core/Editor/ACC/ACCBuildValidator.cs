#if UNITY_EDITOR

namespace Verve.Editor
{
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;

    /// <summary>
    ///   <para>ACC 构建校验器；阻止无效能力表单进入构建结果。</para>
    /// </summary>
    internal sealed class ACCBuildValidator : IPreprocessBuildWithReport
    {
        /// <summary>
        ///   <para>构建回调顺序。</para>
        /// </summary>
        public int callbackOrder => 0;

        /// <inheritdoc />
        public void OnPreprocessBuild(BuildReport report)
        {
            if (!TagRegistrySettingsProvider.Validate(out var tagError))
                throw new BuildFailedException($"ACC 标签注册表校验失败：{tagError}");

            var validation = ACCAssetValidator.ValidateAll();
            if (validation.IsValid) return;

            var message = $"ACC 能力表单校验失败：发现 {validation.ErrorCount} 个错误。请打开 Verve/ACC/能力表单资源列表修复。";
            throw new BuildFailedException(message);
        }
    }
}

#endif
