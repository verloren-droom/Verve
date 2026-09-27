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

    internal class EditorListPopupTests
    {
        [UnityTest]
        public IEnumerator Options_CloseBeforeSingleSelectionCallbackAndStayOpenForMultipleSelection()
        {
            for (var mode = 0; mode < 2; mode++)
            {
                var host = ScriptableObject.CreateInstance<ListPopupTestHost>();
                EditorWindow window = null;
                var called = false;
                var closedDuringCallback = false;
                string selected = null;
                var popup = mode == 0
                    ? new SearchableOptionsPopup("", new[] { "target" }, value =>
                    {
                        called = true;
                        selected = value;
                        closedDuringCallback = window == null;
                    })
                    : new SearchableOptionsPopup(Array.Empty<string>(), new[] { "target" }, values =>
                    {
                        called = true;
                        selected = values.Single();
                        closedDuringCallback = window == null;
                    });
                try
                {
                    host.Draw = () => PopupWindow.Show(new Rect(90 + mode * 40, 90, 1, 1), popup);
                    host.ShowUtility();
                    host.SendEvent(new Event { type = EventType.Repaint });
                    window = popup.editorWindow;
                    Assert.That(window, Is.Not.Null);
                    window.SendEvent(new Event { type = EventType.Layout });
                    window.SendEvent(new Event { type = EventType.Repaint });
                    window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = new Vector2(100, 30) });
                    window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = new Vector2(100, 30) });
                    Assert.That(called, Is.True);
                    Assert.That(selected, Is.EqualTo("target"));
                    Assert.That(closedDuringCallback, Is.EqualTo(mode == 0));
                }
                finally
                {
                    if (window != null) window.Close();
                    host.Close();
                }
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Selection_UsesSnapshotAndClosesBeforeCallbackWithoutDestroyingBorrowedIcon()
        {
            var icon = new Texture2D(1, 1);
            var host = ScriptableObject.CreateInstance<ListPopupTestHost>();
            var items = new[] { "first", "target", "last" };
            var target = new GUIContent("Target", icon, "folder/needle");
            var contentCalls = 0;
            string selected = null;
            EditorWindow window = null;
            var closedBeforeCallback = false;
            var popup = new EditorListPopup<string>(items,
                item => { contentCalls++; return item == "target" ? target : new GUIContent(item); },
                item => { selected = item; closedBeforeCallback = window == null; });
            items[1] = "changed";
            target.tooltip = "changed";
            try
            {
                host.Draw = () => PopupWindow.Show(new Rect(50, 50, 1, 1), popup);
                host.ShowUtility();
                host.SendEvent(new Event { type = EventType.Repaint });
                window = popup.editorWindow;
                Assert.That(window, Is.Not.Null);
                window.SendEvent(new Event { type = EventType.Layout });
                window.SendEvent(new Event { type = EventType.Repaint });
                TypeSearch(window, "FOLDER/NEEDLE");
                Assert.That(contentCalls, Is.EqualTo(3));
                window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = new Vector2(100, 42) });
                Assert.That(selected, Is.EqualTo("target"));
                Assert.That(closedBeforeCallback, Is.True);
                Assert.That(icon != null, Is.True);
            }
            finally
            {
                if (window != null) window.Close();
                host.Close();
                Object.DestroyImmediate(icon);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Search_ReusesMatchesAndRebuildsWhenOptionsChangeWithoutChangingText()
        {
            var host = ScriptableObject.CreateInstance<ListPopupTestHost>();
            var calls = 0;
            var searchChanges = 0;
            var search = new EditorSearchFilter(string.Empty);
            var selected = -1;
            var popup = new EditorListPopup<int>(Enumerable.Range(0, 300).ToArray(),
                item => new GUIContent(item.ToString()), item => selected = item,
                (item, filter) =>
                {
                    Assert.That(filter.RequiresExactLength, Is.EqualTo(search.RequiresExactLength), "Notify before matching.");
                    calls++;
                    return !filter.RequiresExactLength || item % 2 == 1;
                },
                filter => { search = filter; searchChanges++; });
            EditorWindow window = null;
            try
            {
                host.Draw = () => PopupWindow.Show(new Rect(10, 10, 1, 1), popup);
                host.ShowUtility();
                host.SendEvent(new Event { type = EventType.Repaint });
                window = popup.editorWindow;
                Assert.That(window, Is.Not.Null);
                window.SendEvent(new Event { type = EventType.Layout });
                window.SendEvent(new Event { type = EventType.Repaint });
                Assert.That(calls, Is.EqualTo(300));
                window.SendEvent(new Event { type = EventType.Repaint });
                Assert.That(calls, Is.EqualTo(300));
                Assert.That(searchChanges, Is.EqualTo(1));
                Click(window, new Vector2(window.position.width - 40, 14));
                window.SendEvent(new Event { type = EventType.Layout });
                window.SendEvent(new Event { type = EventType.Repaint });
                Assert.That(calls, Is.EqualTo(600));
                Assert.That(searchChanges, Is.EqualTo(2));
                Assert.That(search.Text, Is.Empty);
                Assert.That(search.RequiresExactLength, Is.True);
                window.SendEvent(new Event { type = EventType.ScrollWheel, mousePosition = new Vector2(100, 100), delta = new Vector2(0, 20) });
                window.SendEvent(new Event { type = EventType.Repaint });
                window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = new Vector2(100, 27) });
                Assert.That(selected, Is.EqualTo(-1), "The clipped row must not handle clicks above the list.");
                window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = new Vector2(100, 60) });
                Assert.That(selected, Is.GreaterThan(1));
                Assert.That(selected % 2, Is.EqualTo(1));
            }
            finally
            {
                if (window != null) window.Close();
                host.Close();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Search_ButtonsCombineAndClearingRestoresAllItems()
        {
            var contents = new[]
            {
                new GUIContent("prefixmenu"), new GUIContent("menu"), new GUIContent("prefixMenu"),
                new GUIContent("tooltip", "Menu"), new GUIContent("Menu")
            };
            var cases = new[]
            {
                (exact: false, sensitive: false, clear: false, expected: 0),
                (exact: true, sensitive: false, clear: false, expected: 1),
                (exact: false, sensitive: true, clear: false, expected: 2),
                (exact: true, sensitive: true, clear: false, expected: 3),
                (exact: true, sensitive: true, clear: true, expected: 0)
            };
            for (var i = 0; i < cases.Length; i++)
            {
                var scenario = cases[i];
                var host = ScriptableObject.CreateInstance<ListPopupTestHost>();
                EditorWindow window = null;
                var selected = -1;
                var popup = new EditorListPopup<int>(Enumerable.Range(0, contents.Length).ToArray(),
                    item => contents[item], item => selected = item);
                try
                {
                    host.Draw = () => PopupWindow.Show(new Rect(180 + i * 40, 90, 1, 1), popup);
                    host.ShowUtility();
                    host.SendEvent(new Event { type = EventType.Repaint });
                    window = popup.editorWindow;
                    Assert.That(window, Is.Not.Null);
                    window.SendEvent(new Event { type = EventType.Layout });
                    window.SendEvent(new Event { type = EventType.Repaint });
                    TypeSearch(window, "Menu");
                    if (scenario.exact) Click(window, new Vector2(window.position.width - 40, 14));
                    if (scenario.sensitive) Click(window, new Vector2(window.position.width - 16, 14));
                    if (scenario.clear) Click(window, new Vector2(window.position.width - 60, 14));
                    window.SendEvent(new Event { type = EventType.Repaint });
                    window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = new Vector2(100, 42) });
                    Assert.That(selected, Is.EqualTo(scenario.expected), $"Search scenario {scenario}");
                }
                finally
                {
                    if (window != null) window.Close();
                    host.Close();
                }
                yield return null;
            }
        }

        [TestCase("Menu", false, false, "prefixmenu", true)]
        [TestCase("Menu", true, false, "prefixmenu", false)]
        [TestCase("Menu", true, false, "menu", true)]
        [TestCase("Menu", true, true, "menu", false)]
        [TestCase("Menu", true, true, "Menu", true)]
        [TestCase("Menu", true, false, "Load", false)]
        [TestCase("Menu", false, false, null, false)]
        [TestCase("", true, true, "Menu", true)]
        [TestCase("  ", true, true, "Menu", true)]
        [TestCase(null, true, true, null, true)]
        public void SearchFilter_MatchesText(string text, bool exact, bool sensitive, string value, bool expected) =>
            Assert.That(new EditorSearchFilter(text, exact, sensitive).Matches(value), Is.EqualTo(expected));

        [Test]
        public void TableSearch_ExactLengthMatchesNameAndPathSeparately()
        {
            var table = new ConfigTableAsset { tableName = "Items" };
            const string path = "Assets/Tables/Items.ctable";
            Assert.That(ConfigTableSearch.MatchesTable(table, path, ConfigTableSearch.Parse("Items", true, true)), Is.True);
            Assert.That(ConfigTableSearch.MatchesTable(table, path, ConfigTableSearch.Parse("items", true, true)), Is.False);
            Assert.That(ConfigTableSearch.MatchesTable(table, path, ConfigTableSearch.Parse("items", true)), Is.True);
            Assert.That(ConfigTableSearch.MatchesTable(table, path, ConfigTableSearch.Parse("Item", true)), Is.False);
            Assert.That(ConfigTableSearch.MatchesTable(table, path, ConfigTableSearch.Parse(path, true, true)), Is.True);
        }

        private static void TypeSearch(EditorWindow window, string text)
        {
            Click(window, new Vector2(100, 14));
            foreach (var character in text)
                window.SendEvent(new Event { type = EventType.KeyDown, character = character });
        }

        private static void Click(EditorWindow window, Vector2 position)
        {
            window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = position });
            window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = position });
        }
    }
}
