#if UNITY_EDITOR
namespace Verve.Tests.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using Verve.Editor;

    public class GameToolSettingsTests
    {
        [Test]
        public void Settings_RejectMisplacedOrDuplicateAssetsBeforeBuild()
        {
            const string path = "Assets/Verve.Tools.Duplicate.Test.asset";
            var original = GameToolSettings.LoadAsset();
            var duplicate = ScriptableObject.CreateInstance<GameToolSettings>();
            Assert.That(File.Exists(path), Is.False);
            try
            {
                AssetDatabase.CreateAsset(duplicate, path);
                var error = Assert.Throws<InvalidOperationException>(() => GameToolSettings.LoadAsset());
                Assert.That(error.Message, Does.Contain(path));
                Assert.That(error.Message, Does.Contain(GameToolSettings.AssetPath));
                Assert.Throws<UnityEditor.Build.BuildFailedException>(() =>
                    new GameToolSettingsBuildProcessor().OnPreprocessBuild(null));
            }
            finally
            {
                if (!AssetDatabase.DeleteAsset(path)) UnityEngine.Object.DestroyImmediate(duplicate);
            }
            Assert.That(GameToolSettings.LoadAsset(), Is.SameAs(original));
        }

        [Test]
        public void Settings_RoundTripSelectionsAndReportMissingTypes()
        {
            const string path = "Assets/Verve.Tools.Test.asset";
            Assert.That(File.Exists(path), Is.False);
            var source = ScriptableObject.CreateInstance<GameToolSettings>();
            try
            {
                Assert.That(source.configuration.GetSelections(), Is.Empty);
                source.configuration.Set<ICompression, EditorCompression>();
                AssetDatabase.CreateAsset(source, path);
                AssetDatabase.SaveAssets();
                var loaded = AssetDatabase.LoadAssetAtPath<GameToolSettings>(path);
                Assert.That(loaded.configuration.GetSelections()[typeof(ICompression)], Is.EqualTo(typeof(EditorCompression)));
                loaded.configuration.selections[0].implementation = "Removed.ProjectCompression, Removed.ProjectAssembly";
                Assert.Catch(() => loaded.configuration.GetSelections());
                loaded.configuration.Set(typeof(ICompression), null);
                Assert.That(loaded.configuration.GetSelections(), Is.Empty);
            }
            finally
            {
                if (!AssetDatabase.DeleteAsset(path)) UnityEngine.Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void ImplementationSearch_ExcludesEditorTestAndUnityObjectTypes()
        {
            Assert.That(GameToolConfiguration.IsSupported(typeof(EditorCompression), typeof(ICompression)), Is.True);
            Assert.That(GameToolEditorUtility.FindRuntimeImplementations(typeof(ICompression)).Contains(typeof(EditorCompression)), Is.False);
            Assert.That(GameToolConfiguration.IsSupported(typeof(ComponentCompression), typeof(ICompression)), Is.False);
            Assert.That(GameToolConfiguration.IsSupported(typeof(AbstractCompression), typeof(ICompression)), Is.False);
            Assert.That(GameToolConfiguration.IsSupported(typeof(ConstructorCompression), typeof(ICompression)), Is.False);
        }

        [Test]
        public void LinkXml_PreservesSelectedNestedTypesOnceAndIsOrderIndependent()
        {
            var type = typeof(EditorCompression);
            var first = CoreEditorUtility.CreateLinkXml(new[] { type, null, type, typeof(GameToolSettings) });
            var second = CoreEditorUtility.CreateLinkXml(new[] { typeof(GameToolSettings), type });
            Assert.That(first, Is.EqualTo(second));
            var document = XDocument.Parse(first);
            var entries = document.Descendants("type").ToArray();
            Assert.That(entries, Has.Length.EqualTo(2));
            Assert.That(entries.Any(entry => (string)entry.Attribute("fullname") == type.FullName.Replace('+', '/')
                && (string)entry.Attribute("preserve") == "all"), Is.True);
        }

        public class EditorCompression : ICompression
        {
            public byte[] Compress(byte[] data) => data;
            public byte[] Decompress(byte[] compressedData) => compressedData;
        }

        public abstract class AbstractCompression : EditorCompression { }
        public class ConstructorCompression : EditorCompression
        {
            public ConstructorCompression(int value) { }
        }

        public class ComponentCompression : MonoBehaviour, ICompression
        {
            public byte[] Compress(byte[] data) => data;
            public byte[] Decompress(byte[] compressedData) => compressedData;
        }
    }
}
#endif
