namespace Verve.Tests.Editor
{
    using System;
    using System.Collections;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Verve.Editor;
    using Object = UnityEngine.Object;

    internal sealed class GameModuleEditorTests
    {
        [TestCase(typeof(BaseModule), typeof(BaseModuleEditor))]
        [TestCase(typeof(DerivedModule), typeof(BaseModuleEditor))]
        [TestCase(typeof(ExactModule), typeof(ExactModuleEditor))]
        [TestCase(typeof(LeafModule), typeof(ExactModuleEditor))]
        [TestCase(typeof(OnlyExactModule), typeof(OnlyExactModuleEditor))]
        [TestCase(typeof(OnlyExactChild), typeof(GameModuleEditor))]
        [TestCase(typeof(DefaultModule), typeof(GameModuleEditor))]
        public void Selection_UsesExactOrNearestOptedInEditor(Type moduleType, Type expected)
        {
            using var first = GameModuleEditor.Create(moduleType);
            using var second = GameModuleEditor.Create(moduleType);
            Assert.That(first.GetType(), Is.EqualTo(expected));
            Assert.That(second.GetType(), Is.EqualTo(expected));
            Assert.That(first, Is.Not.SameAs(second), "Separate inspectors must own separate editor instances.");
        }

        [TestCase(typeof(DuplicateModule))]
        [TestCase(typeof(InvalidEditorModule))]
        [TestCase(typeof(AbstractEditorModule))]
        [TestCase(typeof(ConstructorModule))]
        public void InvalidRegistration_ReportsErrorInsteadOfDrawingDefaultFields(Type moduleType) =>
            Assert.Throws<InvalidOperationException>(() => GameModuleEditor.Create(moduleType));

        [Test]
        public void Attribute_RejectsNonModuleAndOpenGenericTypes()
        {
            Assert.Throws<ArgumentNullException>(() => new CustomGameModuleEditorAttribute(null));
            Assert.Throws<ArgumentException>(() => new CustomGameModuleEditorAttribute(typeof(string)));
            Assert.Throws<ArgumentException>(() => new CustomGameModuleEditorAttribute(typeof(GenericModule<>)));
        }

        [UnityTest]
        public IEnumerator Manifest_CustomPropertyEditUsesUndoAndPersistsThroughApply()
        {
            ProbeEditor.Reset();
            using var inspector = new ManifestInspector(typeof(EditableModule));
            inspector.Show();
            yield return null;
            inspector.Draw();
            Assert.That(ProbeEditor.Created, Is.EqualTo(1));
            Undo.IncrementCurrentGroup();
            ProbeEditor.NextValue = 37;
            inspector.Draw();
            Assert.That(((EditableModule)inspector.Data.ModuleEntries[0].module).Value, Is.EqualTo(37));
            Assert.That(inspector.Editor.HasModified(), Is.True);
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            inspector.Draw();
            Assert.That(((EditableModule)inspector.Data.ModuleEntries[0].module).Value, Is.EqualTo(3));
            Undo.PerformRedo();
            inspector.Draw();
            Assert.That(((EditableModule)inspector.Data.ModuleEntries[0].module).Value, Is.EqualTo(37));

            inspector.Apply();
            var asset = AssetDatabase.LoadAssetAtPath<GameModuleManifestAsset>(inspector.Path);
            using var modules = new GameModules();
            modules.InstallFromManifest(asset.ToManifest());
            Assert.That(modules.GetModule<EditableModule>().Value, Is.EqualTo(37));
            Assert.That(inspector.Editor.HasModified(), Is.False);
        }

        [UnityTest]
        public IEnumerator Editor_IsReusedForSameTypeAndReleasedOnResetAndClose()
        {
            ProbeEditor.Reset();
            using (var inspector = new ManifestInspector(typeof(EditableModule)))
            {
                inspector.Show();
                yield return null;
                inspector.Draw();
                inspector.Draw();
                Assert.That(ProbeEditor.Created, Is.EqualTo(1));
                Assert.That(ProbeEditor.Disposed, Is.Zero);

                using (var serialized = new SerializedObject(inspector.Data))
                {
                    var entry = serialized.FindProperty(GameModuleManifestEditorData.ModuleEntriesPropertyName).GetArrayElementAtIndex(0);
                    GameModuleManifestEditorData.FindModuleProperty(entry).managedReferenceValue = new EditableModule();
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                inspector.Draw();
                Assert.That(ProbeEditor.Created, Is.EqualTo(1));
                Assert.That(ProbeEditor.Disposed, Is.Zero);

                inspector.Reset();
                Assert.That(ProbeEditor.Disposed, Is.EqualTo(1));
                inspector.Draw();
                Assert.That(ProbeEditor.Created, Is.EqualTo(2));
            }
            Assert.That(ProbeEditor.Disposed, Is.EqualTo(ProbeEditor.Created));
        }

        [UnityTest]
        public IEnumerator Cleanup_ContinuesAfterOneCustomEditorThrows()
        {
            ProbeEditor.Reset();
            FailingDisposeEditor.DisposeCount = 0;
            using var inspector = new ManifestInspector(typeof(EditableModule), typeof(FailingDisposeModule));
            inspector.Show();
            yield return null;
            inspector.Draw();
            var failure = Assert.Throws<TargetInvocationException>(() => inspector.ClearEditors());
            Assert.That(failure.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(FailingDisposeEditor.DisposeCount, Is.EqualTo(1));
            Assert.That(ProbeEditor.Disposed, Is.EqualTo(1));
        }

        private sealed class ManifestInspector : IDisposable
        {
            internal readonly string Path = "Assets/VerveModuleEditor-" + Guid.NewGuid().ToString("N") + ".gmm";
            internal readonly GameModuleManifestImporterEditor Editor;
            private readonly ListPopupTestHost m_Host;
            internal GameModuleManifestEditorData Data =>
                Game.ReflectionUtility.GetFieldValue<GameModuleManifestEditorData>(Editor, "m_EditorData");

            internal ManifestInspector(params Type[] types)
            {
                var entries = new GameModuleManifestAsset.GameModuleEntry[types.Length];
                for (var i = 0; i < types.Length; i++)
                    entries[i] = new GameModuleManifestAsset.GameModuleEntry { type = types[i].AssemblyQualifiedName, fields = "{}" };
                GameModuleManifestImporter.WriteManifestData(Path, new GameModuleManifestAsset.GameModuleManifestData { modules = entries });
                AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceSynchronousImport);
                Editor = (GameModuleManifestImporterEditor)UnityEditor.Editor.CreateEditor(AssetImporter.GetAtPath(Path));
                m_Host = ScriptableObject.CreateInstance<ListPopupTestHost>();
                m_Host.position = new Rect(50, 50, 600, 900);
            }

            internal void Show()
            {
                Action draw = null;
                draw = () =>
                {
                    m_Host.Draw = draw;
                    var serialized = Game.ReflectionUtility.GetPropertyValue<SerializedObject>(Editor, "extraDataSerializedObject");
                    var entries = serialized.FindProperty(GameModuleManifestEditorData.ModuleEntriesPropertyName);
                    for (var i = 0; i < entries.arraySize; i++) entries.GetArrayElementAtIndex(i).isExpanded = true;
                    Editor.OnInspectorGUI();
                };
                m_Host.Draw = draw;
                m_Host.ShowUtility();
            }

            internal void Draw()
            {
                m_Host.SendEvent(new Event { type = EventType.Layout });
                m_Host.SendEvent(new Event { type = EventType.Repaint });
            }

            internal void Apply() => Editor.SaveChanges();
            internal void Reset() => Invoke("ResetValues");
            internal void ClearEditors() => Invoke("ClearModuleEntryContexts");
            private void Invoke(string method) => typeof(GameModuleManifestImporterEditor)
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Editor, null);

            public void Dispose()
            {
                m_Host.Close();
                try { Reset(); }
                finally
                {
                    Object.DestroyImmediate(Editor);
                    AssetDatabase.DeleteAsset(Path);
                }
            }
        }

        [Serializable] private class BaseModule : GameModule { }
        [Serializable] private class DerivedModule : BaseModule { }
        [Serializable] private class ExactModule : DerivedModule { }
        [Serializable] private sealed class LeafModule : ExactModule { }
        [Serializable] private class OnlyExactModule : GameModule { }
        [Serializable] private sealed class OnlyExactChild : OnlyExactModule { }
        [Serializable] private sealed class DefaultModule : GameModule { }
        [Serializable] private sealed class DuplicateModule : GameModule { }
        [Serializable] private sealed class InvalidEditorModule : GameModule { }
        [Serializable] private sealed class AbstractEditorModule : GameModule { }
        [Serializable] private sealed class ConstructorModule : GameModule { }
        private sealed class GenericModule<T> : GameModule { }
        [Serializable] private sealed class FailingDisposeModule : GameModule { public FailingDisposeModule() { } }
        [Serializable] private sealed class EditableModule : GameModule
        {
            public int Value = 3;
            public EditableModule() { }
        }

        [CustomGameModuleEditor(typeof(BaseModule), true)]
        private sealed class BaseModuleEditor : GameModuleEditor { }
        [CustomGameModuleEditor(typeof(ExactModule), true)]
        private sealed class ExactModuleEditor : GameModuleEditor { }
        [CustomGameModuleEditor(typeof(OnlyExactModule))]
        private sealed class OnlyExactModuleEditor : GameModuleEditor { }
        [CustomGameModuleEditor(typeof(DuplicateModule))]
        private sealed class DuplicateEditorA : GameModuleEditor { }
        [CustomGameModuleEditor(typeof(DuplicateModule))]
        private sealed class DuplicateEditorB : GameModuleEditor { }
        [CustomGameModuleEditor(typeof(InvalidEditorModule))]
        private sealed class InvalidEditor { }
        [CustomGameModuleEditor(typeof(AbstractEditorModule))]
        private abstract class AbstractEditor : GameModuleEditor { }
        [CustomGameModuleEditor(typeof(ConstructorModule))]
        private sealed class ConstructorEditor : GameModuleEditor { public ConstructorEditor(int value) { } }

        [CustomGameModuleEditor(typeof(FailingDisposeModule))]
        private sealed class FailingDisposeEditor : GameModuleEditor
        {
            internal static int DisposeCount;
            protected override void OnDispose() { DisposeCount++; throw new InvalidOperationException("editor cleanup failure"); }
        }

        [CustomGameModuleEditor(typeof(EditableModule))]
        private sealed class ProbeEditor : GameModuleEditor
        {
            internal static int Created;
            internal static int Disposed;
            internal static int? NextValue;
            public ProbeEditor() => Created++;
            internal static void Reset() { Created = Disposed = 0; NextValue = null; }
            public override void OnInspectorGUI(SerializedProperty module)
            {
                if (NextValue is int value)
                {
                    module.FindPropertyRelative(nameof(EditableModule.Value)).intValue = value;
                    NextValue = null;
                }
                base.OnInspectorGUI(module);
            }
            protected override void OnDispose() => Disposed++;
        }
    }
}
