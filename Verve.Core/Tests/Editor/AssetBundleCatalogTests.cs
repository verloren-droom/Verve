namespace Verve.Tests.Editor
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Verve.Editor;

    internal class AssetBundleCatalogTests
    {
        [UnityTest]
        public IEnumerator BuiltCatalog_LoadsFromFileAndUrlWithoutScanningContentBundles()
        {
            var suffix = Guid.NewGuid().ToString("N");
            var source = "Assets/__VerveBundle_" + suffix + ".txt";
            var output = "Assets/StreamingAssets/__VerveBundles_" + suffix;
            var bundleName = "verve-test-" + suffix;
            try
            {
                File.WriteAllText(source, "bundle contents");
                AssetDatabase.ImportAsset(source);
                var importer = AssetImporter.GetAtPath(source);
                importer.assetBundleName = bundleName;
                importer.SaveAndReimport();
                LoaderBuildPipeline.BuildAssetBundles(EditorUserBuildSettings.activeBuildTarget, output);
                var catalog = JsonUtility.FromJson<AssetBundleCatalog>(File.ReadAllText(Path.Combine(output, AssetBundleCatalog.FileName)));
                Assert.That(catalog.bundles.Single(bundle => bundle.name == bundleName).assets, Does.Contain(source));

                foreach (var directory in new[] { Path.GetFullPath(output), new Uri(Path.GetFullPath(output)).AbsoluteUri })
                {
                    using var modules = new GameModules();
                    var installation = modules.InstallAsync(() => new LoaderModule
                    {
                        LoaderMode = AssetLoaderMode.AssetBundles,
                        BundleDirectory = directory
                    }).AsTask();
                    yield return installation.AsIEnumerator();
                    Assert.That(AssetBundle.GetAllLoadedAssetBundles().Any(bundle => bundle.name == bundleName), Is.False);
                    var loader = modules.GetModule<LoaderModule>();
                    var loading = loader.LoadAssetAsync<TextAsset>(source);
                    yield return loading.AsIEnumerator();
                    using var handle = loading.GetAwaiter().GetResult();
                    var asset = handle.Result;
                    Assert.That(asset.text, Is.EqualTo("bundle contents"));
                    handle.Dispose();
                    Assert.That(asset == null, Is.True);
                    Assert.That(AssetBundle.GetAllLoadedAssetBundles().Any(bundle => bundle.name == bundleName), Is.False);
                }
            }
            finally
            {
                AssetDatabase.DeleteAsset(source);
                AssetDatabase.DeleteAsset(output);
                if (Directory.Exists(output)) Directory.Delete(output, true);
                AssetDatabase.RemoveUnusedAssetBundleNames();
            }
        }
    }
}
