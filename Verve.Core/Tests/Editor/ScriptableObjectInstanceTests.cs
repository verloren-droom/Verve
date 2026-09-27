namespace Verve.Tests.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEngine;
    using Verve.Editor;

    public class ScriptableObjectInstanceTests
    {
        private const string DuplicatePath = "Assets/Verve.GlobalSettings.Duplicate.Test.asset";

        [SetUp]
        public void SetUp()
        {
            Assert.That(File.Exists(GlobalSettingsProbe.AssetPath), Is.False);
            Assert.That(File.Exists(DuplicatePath), Is.False);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(GlobalSettingsProbe.AssetPath);
            AssetDatabase.DeleteAsset(DuplicatePath);
        }

        [Test]
        public void MissingResource_IsOptionalOnlyWhenExplicitlyRequested()
        {
            Assert.That(GlobalSettingsProbe.TryGetInstance(out var instance), Is.False);
            Assert.That(instance, Is.Null);
            var error = Assert.Throws<InvalidOperationException>(() => _ = GlobalSettingsProbe.Instance);
            Assert.That(error.Message, Does.Contain(GlobalSettingsProbe.AssetPath));
            Assert.That(File.Exists(GlobalSettingsProbe.AssetPath), Is.False);
        }

        [Test]
        public void Instance_BorrowsThePersistentAssetAndReloadsAfterDeletion()
        {
            var source = CreateAt(GlobalSettingsProbe.AssetPath);
            source.value = 42;
            Assert.That(GlobalSettingsProbe.Instance, Is.SameAs(source));
            Assert.That(GlobalSettingsProbe.Instance.value, Is.EqualTo(42));
            AssetDatabase.DeleteAsset(GlobalSettingsProbe.AssetPath);
            Assert.That(GlobalSettingsProbe.TryGetInstance(out _), Is.False);
            var replacement = CreateAt(GlobalSettingsProbe.AssetPath);
            Assert.That(GlobalSettingsProbe.Instance, Is.SameAs(replacement));
        }

        [Test]
        public void DuplicateOrMisplacedResource_FailsValidationAndBuild()
        {
            CreateAt(DuplicatePath);
            Assert.Throws<InvalidOperationException>(() => GlobalSettingsProbe.TryGetInstance(out _));
            Assert.That(TypeCache.GetTypesDerivedFrom(typeof(ScriptableObjectInstanceBase<>)).Contains(typeof(GlobalSettingsProbe)), Is.True);
            Assert.Throws<BuildFailedException>(() => new ScriptableObjectInstanceBuildProcessor().OnPreprocessBuild(null));
            CreateAt(GlobalSettingsProbe.AssetPath);
            var error = Assert.Throws<InvalidOperationException>(() => GlobalSettingsProbe.LoadAsset());
            Assert.That(error.Message, Does.Contain(DuplicatePath));
        }

        [Test]
        public void Subasset_CannotCreateAnotherGlobalInstance()
        {
            CreateAt(GlobalSettingsProbe.AssetPath);
            var duplicate = ScriptableObject.CreateInstance<GlobalSettingsProbe>();
            AssetDatabase.AddObjectToAsset(duplicate, GlobalSettingsProbe.AssetPath);
            AssetDatabase.SaveAssets();
            Assert.Throws<InvalidOperationException>(() => GlobalSettingsProbe.LoadAsset());
        }

        [Test]
        public void WrongAssetType_IsNotTreatedAsMissing()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GlobalSettingsProbe.AssetPath));
            var asset = new TextAsset("wrong asset type");
            AssetDatabase.CreateAsset(asset, GlobalSettingsProbe.AssetPath);
            Assert.Throws<InvalidOperationException>(() => GlobalSettingsProbe.TryGetInstance(out _));
        }

        private static GlobalSettingsProbe CreateAt(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var asset = ScriptableObject.CreateInstance<GlobalSettingsProbe>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
