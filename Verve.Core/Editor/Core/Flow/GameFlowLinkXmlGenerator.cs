#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Linq;
    using System.Text;
    using UnityEditor;
    using UnityEditor.Build;
    using System.Collections.Generic;
    using UnityEditor.Build.Reporting;

    /// <summary>
    ///   <para>流程处理器链接配置生成器。</para>
    /// </summary>
    sealed class GameFlowLinkXmlGenerator : IPreprocessBuildWithReport
    {
        private const string OutputPath = "Assets/Verve.Generated/GameFlow.link.xml";
        private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);

        public int callbackOrder => 1;

        public void OnPreprocessBuild(BuildReport report)
        {
            var types = new HashSet<Type>();
            var guids = AssetDatabase.FindAssets($"t:{nameof(GameFlowAsset)}");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<GameFlowAsset>(path);
                if (asset == null) continue;
                var steps = asset.Steps;
                for (var stepIndex = 0; stepIndex < steps.Count; stepIndex++)
                {
                    var step = steps[stepIndex];
                    if (step == null)
                        throw new BuildFailedException(
                            $"Flow asset '{path}' contains an empty step at index {stepIndex}.");

                    var handler = step.Handler;
                    if (handler != null) types.Add(handler.GetType());
                    var condition = step.Condition;
                    if (condition != null) types.Add(condition.GetType());
                }
            }

            CoreEditorUtility.WriteTextAsset(OutputPath,
                CoreEditorUtility.CreateLinkXml(types.OrderBy(type => type.FullName, StringComparer.Ordinal)),
                Utf8WithoutBom);
        }
    }
}

#endif
