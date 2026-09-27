namespace Verve.Tests.Editor
{
    using System;
    using System.Collections;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Verve.Editor;
    using Object = UnityEngine.Object;

    internal class EditorResourceOwnershipTests
    {
        [UnityTest]
        public IEnumerator ReadOnlyDrawer_PreservesOuterDisabledState()
        {
            var target = new GameObject("Drawer ownership");
            var host = ScriptableObject.CreateInstance<ListPopupTestHost>();
            using var serialized = new SerializedObject(target.transform);
            var property = serialized.FindProperty("m_LocalPosition");
            var drawer = new ReadOnlyPropertyDrawer();
            var drawn = false;
            try
            {
                host.Draw = () =>
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        drawer.OnGUI(new Rect(0, 0, 250, 40), property, GUIContent.none);
                        Assert.That(GUI.enabled, Is.False);
                        Assert.That(drawer.GetPropertyHeight(property, GUIContent.none),
                            Is.EqualTo(EditorGUI.GetPropertyHeight(property, GUIContent.none, true)));
                        drawn = true;
                    }
                };
                host.ShowUtility();
                host.SendEvent(new Event { type = EventType.Repaint });
                Assert.That(drawn, Is.True);
            }
            finally
            {
                host.Close();
                Object.DestroyImmediate(target);
            }
            yield return null;
        }

        [Test]
        public void Preview_ReinitializeAndCleanupDestroyInstancesButKeepBorrowedPrefab()
        {
            var name = "__VervePreview_" + Guid.NewGuid().ToString("N");
            var path = "Assets/" + name + ".prefab";
            var original = new GameObject(name, typeof(RectTransform), typeof(UIWidgetComponent));
            var preview = new UIPrefabPreview();
            GameObject FindInstance() => Resources.FindObjectsOfTypeAll<GameObject>()
                .Single(item => item.name == name + " (UI Preview)");
            try
            {
                var asset = PrefabUtility.SaveAsPrefabAsset(original, path);
                preview.Initialize(new Object[] { asset });
                Assert.That(preview.HasPreviewGUI(), Is.True);
                var first = FindInstance();
                preview.Initialize(new Object[] { asset });
                Assert.That(first == null, Is.True);
                var second = FindInstance();
                preview.Cleanup();
                Assert.That(second == null, Is.True);
                Assert.That(preview.HasPreviewGUI(), Is.False);
                Assert.That(asset != null, Is.True);
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(path), Is.SameAs(asset));
            }
            finally
            {
                preview.Cleanup();
                Object.DestroyImmediate(original);
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
