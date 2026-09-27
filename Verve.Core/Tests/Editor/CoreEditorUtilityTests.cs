namespace Verve.Tests.Editor
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Text;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Verve.Editor;

    internal class CoreEditorUtilityTests
    {
        [UnityTest]
        public IEnumerator HeaderDrawing_PreservesBorrowedTransactionsUntilTheHostCommits()
        {
            var target = new GameObject("original");
            var other = new GameObject("other");
            var host = ScriptableObject.CreateInstance<ListPopupTestHost>();
            using var serialized = new SerializedObject(target);
            using var otherSerialized = new SerializedObject(other);
            try
            {
                host.ShowUtility();
                var name = serialized.FindProperty("m_Name");
                // 同一对象、另一个对象和无开关，均不得刷新或提交借用的序列化数据。
                for (var mode = 0; mode < 3; mode++)
                {
                    var active = mode == 2 ? null : (mode == 0 ? serialized : otherSerialized).FindProperty("m_IsActive");
                    for (var i = 0; i < 128; i++)
                    {
                        name.stringValue = $"pending-{mode}-{i}";
                        if (active != null) active.boolValue = false;
                        var drawn = false;
                        host.Draw = () =>
                        {
                            CoreEditorUtility.DrawHeaderToggle(new GUIContent("Header"), name, active);
                            drawn = true;
                        };
                        host.SendEvent(new Event { type = EventType.Layout });

                        Assert.That(drawn, Is.True);
                        Assert.That(name.stringValue, Is.EqualTo($"pending-{mode}-{i}"));
                        Assert.That(serialized.hasModifiedProperties, Is.True);
                        Assert.That(target.name, Is.EqualTo("original"));
                        Assert.That(target.activeSelf && other.activeSelf, Is.True);
                        if (active != null) Assert.That(active.boolValue, Is.False);
                    }
                }

                Undo.IncrementCurrentGroup();
                serialized.ApplyModifiedProperties();
                otherSerialized.ApplyModifiedProperties();
                Assert.That(target.name, Is.EqualTo("pending-2-127"));
                Assert.That(target.activeSelf || other.activeSelf, Is.False);
                Undo.PerformUndo();
                Assert.That(target.name, Is.EqualTo("original"));
                Assert.That(target.activeSelf && other.activeSelf, Is.True);
                Undo.PerformRedo();
                Assert.That(target.name, Is.EqualTo("pending-2-127"));
                Assert.That(target.activeSelf || other.activeSelf, Is.False);
            }
            finally
            {
                host.Close();
                Undo.ClearUndo(target);
                Undo.ClearUndo(other);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(other);
            }
            yield return null;
        }

        [Test]
        public void MarkDirtyAndSave_OnlySavesTheTargetAsset()
        {
            var prefix = "Assets/__VerveSave_" + Guid.NewGuid().ToString("N");
            var targetPath = prefix + ".anim";
            var otherPath = prefix + "_other.anim";
            try
            {
                var target = new AnimationClip { frameRate = 24 };
                var other = new AnimationClip { frameRate = 24 };
                AssetDatabase.CreateAsset(target, targetPath);
                AssetDatabase.CreateAsset(other, otherPath);
                var targetBytes = File.ReadAllBytes(targetPath);
                var otherBytes = File.ReadAllBytes(otherPath);
                target.frameRate = other.frameRate = 60;
                EditorUtility.SetDirty(other);

                CoreEditorUtility.MarkDirtyAndSave(target);

                Assert.That(File.ReadAllBytes(targetPath), Is.Not.EqualTo(targetBytes));
                Assert.That(EditorUtility.IsDirty(target), Is.False);
                Assert.That(File.ReadAllBytes(otherPath), Is.EqualTo(otherBytes));
                Assert.That(EditorUtility.IsDirty(other), Is.True);
            }
            finally
            {
                AssetDatabase.DeleteAsset(targetPath);
                AssetDatabase.DeleteAsset(otherPath);
            }
        }

        [Test]
        public void TextAssetWrite_SkipsUnchangedContentAndPreservesAssetIdentity()
        {
            var path = "Assets/__VerveUtility_" + Guid.NewGuid().ToString("N") + ".txt";
            var encoding = new UTF8Encoding(false);
            try
            {
                Assert.That(CoreEditorUtility.WriteTextAsset(path, "first", encoding), Is.True);
                var guid = AssetDatabase.AssetPathToGUID(path);
                Assert.That(guid, Is.Not.Empty);
                File.SetLastWriteTimeUtc(path, new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                var timestamp = File.GetLastWriteTimeUtc(path);

                Assert.That(CoreEditorUtility.WriteTextAsset(path, "first", encoding), Is.False);
                Assert.That(File.GetLastWriteTimeUtc(path), Is.EqualTo(timestamp));
                Assert.That(CoreEditorUtility.WriteTextAsset(path, "second", encoding), Is.True);

                Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
                Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(path).text, Is.EqualTo("second"));
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }

        [Test]
        public void ScriptLookup_MatchesTheTypeAndHonorsPathFilter()
        {
            var type = typeof(GameModuleManifestImporter);
            var script = CoreEditorUtility.FindMonoScriptForType(type);

            Assert.That(script, Is.Not.Null);
            Assert.That(script.GetClass(), Is.EqualTo(type));
            var path = AssetDatabase.GetAssetPath(script);
            Assert.That(CoreEditorUtility.FindMonoScriptForType(type, candidate => candidate == path), Is.EqualTo(script));
            Assert.That(CoreEditorUtility.FindMonoScriptForType(type, _ => false), Is.Null);
        }

        [Test]
        public void CommandContext_PrefersTheExplicitObjectOrComponent()
        {
            var previous = Selection.activeGameObject;
            var selected = new GameObject("selected");
            var context = new GameObject("context");
            try
            {
                Selection.activeGameObject = selected;
                Assert.That(CoreEditorUtility.GetCommandGameObject(null), Is.SameAs(selected));
                Assert.That(CoreEditorUtility.GetCommandGameObject(new MenuCommand(context)), Is.SameAs(context));
                Assert.That(CoreEditorUtility.GetCommandGameObject(new MenuCommand(context.transform)), Is.SameAs(context));
            }
            finally
            {
                Selection.activeGameObject = previous;
                UnityEngine.Object.DestroyImmediate(context);
                UnityEngine.Object.DestroyImmediate(selected);
            }
        }

        [Test]
        public void SourcePrefabLookup_BorrowsTheAssetForASceneInstance()
        {
            var path = "Assets/__VerveUtility_" + Guid.NewGuid().ToString("N") + ".prefab";
            var original = new GameObject("source");
            GameObject instance = null;
            try
            {
                var asset = PrefabUtility.SaveAsPrefabAsset(original, path);
                instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);

                Assert.That(CoreEditorUtility.GetSourcePrefabAsset(original.transform), Is.Null);
                Assert.That(CoreEditorUtility.GetSourcePrefabAsset(asset.transform), Is.Null);
                Assert.That(CoreEditorUtility.GetSourcePrefabAsset(instance.transform), Is.EqualTo(asset));
                Assert.That(instance, Is.Not.Null);
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                UnityEngine.Object.DestroyImmediate(original);
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
