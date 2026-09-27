namespace Verve.Tests.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;
    using Verve.Editor;

    internal class GameModuleManifestLinkXmlTests
    {
        [Test]
        public void ManifestWrite_SkipsUnchangedContentAndLeavesNoTemporaryFiles()
        {
            var directory = Path.Combine(Path.GetTempPath(), "verve-manifest-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "modules.manifest");
            var data = new GameModuleManifestAsset.GameModuleManifestData
            {
                modules = new[]
                {
                    new GameModuleManifestAsset.GameModuleEntry
                    {
                        type = typeof(LinkModule).AssemblyQualifiedName,
                        fields = "{}"
                    }
                }
            };
            try
            {
                GameModuleManifestImporter.WriteManifestData(path, data);
                Assert.That(GameModuleManifestImporter.ReadManifestData(path).modules.Length, Is.EqualTo(1));
                File.SetLastWriteTimeUtc(path, new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                var timestamp = File.GetLastWriteTimeUtc(path);

                GameModuleManifestImporter.WriteManifestData(path, data);
                Assert.That(File.GetLastWriteTimeUtc(path), Is.EqualTo(timestamp));
                data.modules = Array.Empty<GameModuleManifestAsset.GameModuleEntry>();
                GameModuleManifestImporter.WriteManifestData(path, data);

                Assert.That(GameModuleManifestImporter.ReadManifestData(path).modules, Is.Empty);
                Assert.That(Directory.GetFiles(directory).Length, Is.EqualTo(1));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void PreserveGraph_TracksConcreteSerializedTypesAndDisposesTemporaryModule()
        {
            LinkModule.DisposeCount = 0;
            var manifest = ScriptableObject.CreateInstance<GameModuleManifestAsset>();
            try
            {
                manifest.SetManifestData(new GameModuleManifestAsset.GameModuleManifestData
                {
                    modules = new[]
                    {
                        new GameModuleManifestAsset.GameModuleEntry
                        {
                            type = typeof(LinkModule).AssemblyQualifiedName,
                            fields = "{}"
                        }
                    }
                });
                var preserved = new List<Type>();
                typeof(GameModuleManifestLinkXmlGenerator)
                    .GetMethod("AppendManifestTypesToPreserve", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { manifest, "test.manifest", preserved, new HashSet<RuntimeTypeHandle>() });

                Assert.That(preserved, Does.Contain(typeof(LinkModule)));
                Assert.That(preserved, Does.Contain(typeof(ConcretePayload)));
                Assert.That(preserved, Does.Contain(typeof(InheritedPayload)));
                Assert.That(LinkModule.DisposeCount, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(manifest); }
        }

        [Serializable]
        private sealed class LinkModule : BaseLinkModule
        {
            public static int DisposeCount;
            [SerializeReference] public Payload Data = new ConcretePayload();
            public LinkModule() { }
            protected override void OnDispose() => DisposeCount++;
        }

        [Serializable]
        private abstract class BaseLinkModule : GameModule
        {
            [SerializeReference] private Payload m_Payload = new InheritedPayload();
        }

        [Serializable] private abstract class Payload { }
        [Serializable] private sealed class ConcretePayload : Payload { public int Value; }
        [Serializable] private sealed class InheritedPayload : Payload { public int Value; }
    }
}
