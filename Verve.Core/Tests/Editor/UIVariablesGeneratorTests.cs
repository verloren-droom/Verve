namespace Verve.Editor.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    internal sealed class UIVariablesGeneratorTests
    {
        [Test]
        public void BuildSource_WritesFullyQualifiedVariableTypes()
        {
            var variables = new List<UIVariableDefinition>
            {
                new("v1", typeof(Image)),
            };

            var source = UIVariablesGenerator.BuildSource(typeof(TestView), variables, 42);

            StringAssert.Contains("private UnityEngine.UI.Image v1 => GetVariable<UnityEngine.UI.Image>(0);", source);
            StringAssert.DoesNotContain("public UnityEngine.UI.Image v1", source);
            StringAssert.DoesNotContain("global::", source);
            StringAssert.DoesNotContain("OnBindEvents", source);
        }

        [Test]
        public void BuildEventSource_WritesOptionalControlEventBinding()
        {
            var variables = new List<UIVariableDefinition>
            {
                new("confirm", typeof(UnityEngine.UI.Button), new[] { "onClick" }),
                new("enabled", typeof(UnityEngine.UI.Toggle), new[] { "onValueChanged" }),
                new("volume", typeof(UnityEngine.UI.Slider)),
            };

            var source = UIEventsGenerator.BuildSource(typeof(TestView), variables);

            StringAssert.Contains("confirm.onClick.AddListener(HandleConfirmClick);", source);
            StringAssert.Contains("confirm.onClick.RemoveListener(HandleConfirmClick);", source);
            StringAssert.Contains("enabled.onValueChanged.AddListener(HandleEnabledValueChanged);", source);
            StringAssert.Contains("partial void OnEnabledValueChanged(System.Boolean value0);", source);
            StringAssert.Contains("private void HandleConfirmClick()", source);
            StringAssert.DoesNotContain("volume.onValueChanged", source);
            StringAssert.Contains("protected override void OnBindEvents()", source);
            StringAssert.Contains("protected override void OnUnbindEvents()", source);
            StringAssert.Contains("base.OnBindEvents();", source);
            StringAssert.Contains("base.OnUnbindEvents();", source);
        }

        [Test]
        public void VariableSignature_ExcludesEventSelection()
        {
            var withoutEvent = new List<UIVariableDefinition>
            {
                new("button", typeof(UnityEngine.UI.Button)),
            };
            var withEvent = new List<UIVariableDefinition>
            {
                new("button", typeof(UnityEngine.UI.Button), new[] { "onClick" }),
            };

            Assert.That(
                UIVariablesGenerator.CalculateSignature(withoutEvent),
                Is.EqualTo(UIVariablesGenerator.CalculateSignature(withEvent)));
            Assert.That(
                UIEventsGenerator.CalculateSignature(withoutEvent),
                Is.Not.EqualTo(UIEventsGenerator.CalculateSignature(withEvent)));
        }

        [Test]
        public void BuildEventSource_SupportsMultipleEventsOnOneComponent()
        {
            var variables = new List<UIVariableDefinition>
            {
                new("control", typeof(MultiEventComponent), new[] { "onFinished", "onStarted" }),
            };

            var source = UIEventsGenerator.BuildSource(typeof(TestView), variables);

            StringAssert.Contains("control.onFinished.AddListener(HandleControlFinished);", source);
            StringAssert.Contains("control.onStarted.AddListener(HandleControlStarted);", source);
            StringAssert.Contains("control.onFinished.RemoveListener(HandleControlFinished);", source);
            StringAssert.Contains("control.onStarted.RemoveListener(HandleControlStarted);", source);
            StringAssert.Contains("partial void OnControlFinished(System.Int32 value0);", source);
            StringAssert.Contains("partial void OnControlStarted();", source);
        }

        [Test]
        public void EventSignature_IsIndependentOfSelectionOrder()
        {
            var first = new List<UIVariableDefinition>
            {
                new("control", typeof(MultiEventComponent), new[] { "onStarted", "onFinished" }),
            };
            var second = new List<UIVariableDefinition>
            {
                new("control", typeof(MultiEventComponent), new[] { "onFinished", "onStarted" }),
            };

            Assert.That(
                UIEventsGenerator.CalculateSignature(first),
                Is.EqualTo(UIEventsGenerator.CalculateSignature(second)));
        }

        [TestCase("Image", true)]
        [TestCase("v1", true)]
        [TestCase("class", false)]
        [TestCase("1Image", false)]
        public void IsValidIdentifier_UsesCSharpRules(string value, bool expected)
        {
            Assert.That(UIVariablesGenerator.IsValidIdentifier(value), Is.EqualTo(expected));
        }

        [TestCase(nameof(MemberOwner.LocalMember), true)]
        [TestCase("InheritedMember", true)]
        [TestCase("GeneratedMember", false)]
        public void HasMemberConflict_UsesConcreteTargetType(string memberName, bool expected)
        {
            Assert.That(
                UIComponentEditorUtility.HasMemberConflict(typeof(MemberOwner), memberName),
                Is.EqualTo(expected));
        }

        private sealed class TestView { }

        private abstract class MemberBase
        {
            protected void InheritedMember() { }
        }

        private sealed class MemberOwner : MemberBase
        {
            public int LocalMember;
        }

        private sealed class MultiEventComponent : MonoBehaviour
        {
            [Serializable]
            public sealed class IntEvent : UnityEvent<int> { }

            public UnityEvent onStarted = new();
            public IntEvent onFinished = new();
        }
    }
}
