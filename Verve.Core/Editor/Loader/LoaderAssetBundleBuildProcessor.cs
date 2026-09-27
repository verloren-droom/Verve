#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEngine;
    using UnityEditor.AddressableAssets.Build;
    using UnityEditor.AddressableAssets.Settings;

    /// <summary>
    ///   <para><see cref="UnityEngine.AssetBundle"/> 构建处理器；适配 Unity 2021.2 及以上的 Player 构建回调。</para>
    /// </summary>
    sealed class LoaderAssetBundleBuildProcessor : BuildPlayerProcessor
    {
        /// <inheritdoc />
        public override int callbackOrder => 0;

        /// <inheritdoc />
        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext) => LoaderBuildPipeline.BuildAssetBundles(buildPlayerContext.BuildPlayerOptions.target);
    }

    /// <summary>
    ///   <para>资源构建流程；构建 <see cref="UnityEngine.AssetBundle"/>，<see cref="UnityEngine.AddressableAssets.Addressables"/> 由官方处理器负责。</para>
    /// </summary>
    public static class LoaderBuildPipeline
    {
        /// <summary>
        ///   <para>资源包选项。</para>
        /// </summary>
        private const BuildAssetBundleOptions BundleOptions =
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode;

        /// <summary>
        ///   <para>构建 <see cref="UnityEngine.AssetBundle"/>。</para>
        /// </summary>
        /// <param name="target">目标。</param>
        internal static void BuildAssetBundles(BuildTarget target)
        {
            if (!TryReadConfiguration(out var mode, out var bundleDirectory))
            {
                return;
            }

            if (mode != AssetLoaderMode.AssetBundles)
            {
                return;
            }

            BuildAssetBundles(target, bundleDirectory);
        }

        /// <summary>
        ///   <para>构建 <see cref="UnityEngine.AssetBundle"/>。</para>
        /// </summary>
        /// <param name="target">目标。</param>
        /// <param name="bundleDirectory">资源包目录。</param>
        internal static void BuildAssetBundles(BuildTarget target, string bundleDirectory)
        {
            var outputDirectory = ValidateOutputDirectory(bundleDirectory);
            var bundleNames = AssetDatabase.GetAllAssetBundleNames();
            if (bundleNames == null || bundleNames.Length == 0)
            {
                throw new BuildFailedException(
                    $"{nameof(LoaderModule)} uses {AssetLoaderMode.AssetBundles}, but no AssetBundle names are assigned.");
            }

            Directory.CreateDirectory(outputDirectory);
            var manifest = BuildPipeline.BuildAssetBundles(outputDirectory, BundleOptions, target);
            if (manifest == null)
            {
                throw new BuildFailedException(
                    $"Failed to build AssetBundles. target={target}, output={outputDirectory}");
            }

            var catalog = new AssetBundleCatalog
            {
                bundles = manifest.GetAllAssetBundles().OrderBy(name => name, StringComparer.Ordinal)
                    .Select(name => new AssetBundleCatalog.Bundle
                    {
                        name = name,
                        assets = AssetDatabase.GetAssetPathsFromAssetBundle(name).OrderBy(path => path, StringComparer.Ordinal).ToArray()
                    }).ToArray()
            };
            Game.FileUtility.WriteAllTextAtomically(Path.Combine(outputDirectory, AssetBundleCatalog.FileName),
                JsonUtility.ToJson(catalog), new UTF8Encoding(false));

            Debug.Log($"[Verve.Loader] AssetBundles built. target={target}, output={outputDirectory}");
        }

        /// <summary>
        ///   <para>构建一次内容包；项目负责先执行各业务模块的资源校验。</para>
        /// </summary>
        /// <param name="target">目标。</param>
        /// <remarks>Player 构建由对应处理器执行，无需重复调用。</remarks>
        public static void BuildHotUpdateContent(BuildTarget target)
        {
            if (!TryReadConfiguration(out var mode, out var bundleDirectory))
            {
                throw new BuildFailedException($"No {nameof(LoaderModule)} is configured in a module manifest.");
            }

            if (mode == AssetLoaderMode.AssetBundles)
            {
                BuildAssetBundles(target, bundleDirectory);
                return;
            }

            // Addressables 负责自己的内容版本、Catalog 和 Bundle 依赖图。
            // Player 构建时由 AddressablesPlayerBuildProcessor 按项目设置自动处理。
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (result != null && !string.IsNullOrEmpty(result.Error))
            {
                throw new BuildFailedException("Addressables content build failed: " + result.Error);
            }

            Debug.Log("[Verve.Loader] Addressables content built.");
        }

        /// <summary>
        ///   <para>为 CI 构建热更新内容。</para>
        /// </summary>
        /// <remarks>命令入口：Unity -batchmode -quit -executeMethod <see cref="BuildHotUpdateContentForCI"/>。</remarks>
        public static void BuildHotUpdateContentForCI() => LoaderBuildPipeline.BuildHotUpdateContent(EditorUserBuildSettings.activeBuildTarget);


        /// <summary>
        ///   <para>尝试读取配置。</para>
        /// </summary>
        /// <param name="mode">模式。</param>
        /// <param name="bundleDirectory">资源包目录。</param>
        private static bool TryReadConfiguration(out AssetLoaderMode mode, out string bundleDirectory)
        {
            mode = default;
            bundleDirectory = null;
            string firstManifestPath = null;
            string firstBundleDirectory = null;
            var found = false;

            var manifestGuids = AssetDatabase.FindAssets($"t:{nameof(GameModuleManifestAsset)}");
            for (int manifestIndex = 0; manifestIndex < manifestGuids.Length; manifestIndex++)
            {
                var manifestPath = AssetDatabase.GUIDToAssetPath(manifestGuids[manifestIndex]);
                var manifest = AssetDatabase.LoadAssetAtPath<GameModuleManifestAsset>(manifestPath);
                if (manifest == null)
                {
                    continue;
                }

                var entries = manifest.ManifestData.modules;
                for (int entryIndex = 0; entryIndex < entries.Length; entryIndex++)
                {
                    var entry = entries[entryIndex];
                    if (!GameModuleManifestAsset.TryGetModuleType(entry.type, out var moduleType, out var typeError))
                    {
                        throw new BuildFailedException(
                            $"Invalid module manifest entry. asset={manifestPath}, entry={entryIndex}, error={typeError}");
                    }

                    if (!typeof(LoaderModule).IsAssignableFrom(moduleType))
                    {
                        continue;
                    }

                    using (var module = GameModuleManifestAsset.CreateModuleInstance(moduleType, entry.fields))
                    {
                        if (!(module is LoaderModule loader))
                        {
                            throw new BuildFailedException(
                                $"Module manifest entry is not a {nameof(LoaderModule)}. asset={manifestPath}, entry={entryIndex}");
                        }

                        if (loader.LoaderMode != AssetLoaderMode.Addressables &&
                            loader.LoaderMode != AssetLoaderMode.AssetBundles)
                        {
                            throw new BuildFailedException(
                                $"Unsupported {nameof(LoaderModule)} mode. asset={manifestPath}, entry={entryIndex}, mode={loader.LoaderMode}");
                        }

                        if (!found)
                        {
                            found = true;
                            mode = loader.LoaderMode;
                            firstManifestPath = manifestPath;
                            firstBundleDirectory = loader.BundleDirectory;
                            continue;
                        }

                        if (mode != loader.LoaderMode ||
                            (mode == AssetLoaderMode.AssetBundles &&
                             !PathsEqual(firstBundleDirectory, loader.BundleDirectory)))
                        {
                            throw new BuildFailedException(
                                $"Conflicting {nameof(LoaderModule)} configurations. " +
                                $"first={firstManifestPath}, current={manifestPath}");
                        }
                    }
                }
            }

            bundleDirectory = firstBundleDirectory;
            return found;
        }

        /// <summary>
        ///   <para>校验输出目录。</para>
        /// </summary>
        /// <param name="configuredDirectory">已配置目录。</param>
        private static string ValidateOutputDirectory(string configuredDirectory)
        {
            if (string.IsNullOrWhiteSpace(configuredDirectory))
            {
                throw new BuildFailedException("AssetBundle output directory cannot be empty.");
            }

            var outputDirectory = NormalizePath(configuredDirectory);
            var streamingAssetsDirectory = NormalizePath(Application.streamingAssetsPath);
            var prefix = streamingAssetsDirectory + Path.DirectorySeparatorChar;
            if (!string.Equals(outputDirectory, streamingAssetsDirectory, StringComparison.Ordinal) &&
                !outputDirectory.StartsWith(prefix, StringComparison.Ordinal))
            {
                throw new BuildFailedException(
                    "AssetBundle output must be inside StreamingAssets so Unity includes it in the Player. " +
                    $"output={outputDirectory}, streamingAssets={streamingAssetsDirectory}");
            }

            return outputDirectory;
        }

        /// <summary>
        ///   <para>路径相等。</para>
        /// </summary>
        /// <param name="left">左。</param>
        /// <param name="right">右。</param>
        private static bool PathsEqual(string left, string right) => string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.Ordinal);

        /// <summary>
        ///   <para>规范化路径。</para>
        /// </summary>
        /// <param name="path">路径。</param>
        private static string NormalizePath(string path)
        {
            var fullPath = Path.GetFullPath(path);
            var normalized = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.IsNullOrEmpty(normalized) ? fullPath : normalized;
        }
    }
}

#endif
