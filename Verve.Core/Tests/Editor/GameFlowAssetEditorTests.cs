#if UNITY_EDITOR

namespace Verve.Tests.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using Verve;
    using Object = UnityEngine.Object;

    [Category("Core")]
    internal sealed class GameFlowAssetEditorTests
    {
        [Test]
        public void GameFlowAsset_UsesGraphicalCustomEditor()
        {
            var asset = ScriptableObject.CreateInstance<GameFlowAsset>();
            var editor = UnityEditor.Editor.CreateEditor(asset);
            try
            {
                Assert.That(editor, Is.Not.Null);
                Assert.That(editor.GetType().Name, Is.EqualTo("GameFlowAssetEditor"));
            }
            finally
            {
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void GameFlowAsset_ProvidesStandaloneFlowEditorWindow()
        {
            var windowType = Type.GetType("Verve.Editor.GameFlowEditorWindow, Verve.Editor");

            Assert.That(windowType, Is.Not.Null);
            Assert.That(typeof(EditorWindow).IsAssignableFrom(windowType), Is.True);
        }

        [Test]
        public void FlowEditorWindow_UsesImmediateModeLayoutAndCanvasZoomState()
        {
            var windowType = Type.GetType("Verve.Editor.GameFlowEditorWindow, Verve.Editor");
            var window = ScriptableObject.CreateInstance(windowType) as EditorWindow;
            try
            {
                Assert.That(windowType.GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic),
                    Is.Not.Null);
                Assert.That(windowType.GetField("m_CanvasZoom",
                    BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
                Assert.That(windowType.GetField("m_DetailsScroll",
                    BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
                Assert.That(windowType.GetField("m_ImGuiContainer",
                    BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void FlowEditorWindow_InitializesTypeOptionsAfterEnable()
        {
            var windowType = Type.GetType("Verve.Editor.GameFlowEditorWindow, Verve.Editor");
            var window = ScriptableObject.CreateInstance(windowType) as EditorWindow;
            try
            {
                Assert.That(windowType.GetField("s_HandlerTypes",
                    BindingFlags.Static | BindingFlags.NonPublic), Is.Null);
                Assert.That(windowType.GetField("s_ConditionTypes",
                    BindingFlags.Static | BindingFlags.NonPublic), Is.Null);
                Assert.That(windowType.GetField("s_ConditionOptions",
                    BindingFlags.Static | BindingFlags.NonPublic), Is.Null);

                var handlerTypes = windowType.GetField("m_HandlerTypes",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(window) as Array;
                var conditionTypes = windowType.GetField("m_ConditionTypes",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(window) as Array;
                var conditionOptions = windowType.GetField("m_ConditionOptions",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(window) as Array;

                Assert.That(handlerTypes, Is.Not.Null);
                Assert.That(conditionTypes, Is.Not.Null);
                Assert.That(conditionOptions, Is.Not.Null);
                Assert.That(conditionOptions.Length, Is.EqualTo(conditionTypes.Length + 1));
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void FlowEditorWindow_ScalesNodeFontSizeWithCanvasZoom()
        {
            var windowType = Type.GetType("Verve.Editor.GameFlowEditorWindow, Verve.Editor");
            var method = windowType.GetMethod("GetScaledFontSize",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            Assert.That(method.Invoke(null, new object[] { 12, 0.5f }), Is.EqualTo(6));
            Assert.That(method.Invoke(null, new object[] { 12, 1f }), Is.EqualTo(12));
            Assert.That(method.Invoke(null, new object[] { 12, 2f }), Is.EqualTo(24));
        }

        [Test]
        public void FlowEditorWindow_KeepsRuntimeBannerInsideNodeBounds()
        {
            var windowType = Type.GetType("Verve.Editor.GameFlowEditorWindow, Verve.Editor");
            var method = windowType.GetMethod("GetRuntimeBannerRect",
                BindingFlags.Static | BindingFlags.NonPublic);
            var node = new Rect(10f, 20f, 230f, 104f);

            Assert.That(method, Is.Not.Null);
            var banner = (Rect)method.Invoke(null, new object[] { node, 1f });

            Assert.That(banner.xMin, Is.EqualTo(node.xMin));
            Assert.That(banner.xMax, Is.EqualTo(node.xMax));
            Assert.That(banner.yMin, Is.GreaterThanOrEqualTo(node.yMin));
            Assert.That(banner.yMax, Is.LessThanOrEqualTo(node.yMax));
            Assert.That(banner.yMax, Is.EqualTo(node.yMax));
        }

        [Test]
        public void FlowEditorWindow_LocksEditingAfterFlowExecution()
        {
            var windowType = Type.GetType("Verve.Editor.GameFlowEditorWindow, Verve.Editor");
            var method = windowType.GetMethod("IsFlowExecutionLocked",
                BindingFlags.Static | BindingFlags.NonPublic);
            var flow = new GameFlow();

            Assert.That(method, Is.Not.Null);
            Assert.That(method.Invoke(null, new object[] { flow, true }), Is.False);

            flow.Add("步骤", _ => default);
            flow.RunAsync().AsTask().GetAwaiter().GetResult();

            Assert.That(flow.State, Is.EqualTo(GameFlowState.Completed));
            Assert.That(method.Invoke(null, new object[] { flow, true }), Is.True);
            Assert.That(method.Invoke(null, new object[] { flow, false }), Is.False);
        }

        [Test]
        public void FlowEditorWindow_ProvidesMultiSelectionAndRuntimeStatePresentation()
        {
            var windowType = Type.GetType("Verve.Editor.GameFlowEditorWindow, Verve.Editor");

            Assert.That(windowType.GetField("m_SelectedIndices",
                BindingFlags.Instance | BindingFlags.NonPublic).FieldType,
                Is.EqualTo(typeof(HashSet<int>)));
            Assert.That(windowType.GetMethod("ApplySelectionBox",
                BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(windowType.GetMethod("RemoveSelectedSteps",
                BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(windowType.GetField("m_IsDraggingNodes",
                BindingFlags.Instance | BindingFlags.NonPublic).FieldType, Is.EqualTo(typeof(bool)));
            Assert.That(windowType.GetField("m_DraggingNode",
                BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
            Assert.That(windowType.GetMethod("TryFindConnection",
                BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(windowType.GetMethod("AutoLayout",
                BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);

            var labelMethod = windowType.GetMethod("GetRuntimeStateLabel",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(labelMethod.Invoke(null, new object[] { GameFlowStepState.Current }),
                Is.EqualTo("当前执行"));
            Assert.That(labelMethod.Invoke(null, new object[] { GameFlowStepState.Executed }),
                Is.EqualTo("已执行"));
            Assert.That(labelMethod.Invoke(null, new object[] { GameFlowStepState.NotExecuted }),
                Is.EqualTo("未执行"));
        }

        [Test]
        public void FlowStepTypes_ExposeEditorDisplayMetadata()
        {
            var metadata = typeof(GameFlowInstallModulesHandler)
                .GetCustomAttribute<GameFlowDisplayNameAttribute>();

            Assert.That(metadata, Is.Not.Null);
            Assert.That(metadata.DisplayName, Is.EqualTo("安装模块"));
        }
    }
}

#endif
