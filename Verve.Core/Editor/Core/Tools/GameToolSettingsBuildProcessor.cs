#if UNITY_EDITOR
namespace Verve.Editor
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Reflection;
    using System.Collections.Generic;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;

    /// <summary>
    ///   <para>工具构建处理；校验实现选择并生成裁剪保留配置。</para>
    /// </summary>
    internal sealed class GameToolSettingsBuildProcessor : IPreprocessBuildWithReport
    {
        /// <summary>
        ///   <para>链接配置路径。</para>
        /// </summary>
        private const string LinkPath = "Assets/Verve.Generated/Tools.link.xml";
        /// <inheritdoc />
        public int callbackOrder => 0;

        /// <inheritdoc />
        public void OnPreprocessBuild(BuildReport report)
        {
            try
            {
                var settings = GameToolSettings.LoadAsset();
                var selections = (settings == null ? new GameToolConfiguration() : settings.configuration).GetSelections();
                var contracts = GameToolEditorUtility.FindToolContracts();
                var preserve = new List<Type>(contracts);
                foreach (var selection in selections)
                    if (!contracts.Contains(selection.Key) || !GameToolEditorUtility.FindRuntimeImplementations(selection.Key).Contains(selection.Value))
                        throw new InvalidOperationException($"Tool implementation '{selection.Value}' for '{selection.Key}' is unavailable for the current player target.");
                foreach (var contract in contracts)
                {
                    if (!selections.TryGetValue(contract, out var implementation))
                        implementation = contract.GetCustomAttribute<GameToolAttribute>(inherit: false)?.DefaultImplementationType;
                    if (implementation == null) continue;
                    if (!GameToolEditorUtility.FindRuntimeImplementations(contract, includeInternal: true).Contains(implementation))
                        throw new InvalidOperationException($"Invalid default implementation '{implementation}' for '{contract}'.");
                    preserve.Add(implementation);
                }
                CoreEditorUtility.WriteTextAsset(LinkPath, CoreEditorUtility.CreateLinkXml(preserve), new UTF8Encoding(false));
            }
            catch (Exception failure) { throw new BuildFailedException($"Invalid Verve tool configuration: {failure}"); }
        }
    }
}
#endif
