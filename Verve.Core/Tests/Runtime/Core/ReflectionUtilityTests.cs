namespace Verve.Tests.Core
{
    using System;
    using System.Reflection;
    using System.Linq;
    using NUnit.Framework;

    [Category("Core")]
    internal class ReflectionUtilityTests
    {
        private class BaseProbe
        {
            private int m_Number = 1;
            public int Shared;
            private string Secret { get; set; } = "base";
            public int Number => m_Number;
            public string ReadSecret() => Secret;
            public string Name { get; set; } = "base name";
            public static int StaticValue = 3;
        }

        private sealed class DerivedProbe : BaseProbe
        {
            public new int Shared;
            public new string Name => "derived name";
            public string Failure => throw new InvalidOperationException("getter failed");
            public string this[int index] => index.ToString();
            public string this[string index] => index;
        }

        private struct ValueProbe
        {
            public int Number;
            public string Name { get; set; }
        }

        [Test]
        public void InstanceAccess_ReadsAndWritesPrivateBaseMembers()
        {
            var target = new DerivedProbe();

            Game.ReflectionUtility.SetFieldValue(target, "m_Number", 42);
            Game.ReflectionUtility.SetPropertyValue(target, "Secret", "changed");

            Assert.That(target.Number, Is.EqualTo(42));
            Assert.That(target.ReadSecret(), Is.EqualTo("changed"));
            Assert.That(Game.ReflectionUtility.GetFieldValue<int>(target, "m_Number"), Is.EqualTo(42));
            Assert.That(Game.ReflectionUtility.GetPropertyValue<string>(target, "Secret"), Is.EqualTo("changed"));
        }

        [Test]
        public void Lookup_RespectsVisibilityDeclaredOnlyAndExplicitStaticMembers()
        {
            var type = typeof(DerivedProbe);
            const BindingFlags publicInstance = BindingFlags.Public | BindingFlags.Instance;
            const BindingFlags privateDeclared = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            Assert.That(Game.ReflectionUtility.FindField(type, "m_Number", publicInstance), Is.Null);
            Assert.That(Game.ReflectionUtility.FindProperty(type, "Secret", publicInstance), Is.Null);
            Assert.That(Game.ReflectionUtility.FindField(type, "m_Number", privateDeclared), Is.Null);
            Assert.That(Game.ReflectionUtility.FindProperty(type, "Secret", privateDeclared), Is.Null);
            Assert.That(Game.ReflectionUtility.FindField(typeof(BaseProbe), "m_Number", privateDeclared), Is.Not.Null);
            Assert.That(Game.ReflectionUtility.FindProperty(typeof(BaseProbe), "Secret", privateDeclared), Is.Not.Null);
            Assert.That(Game.ReflectionUtility.FindField(type, "StaticValue"), Is.Null);
            var field = Game.ReflectionUtility.FindField(type, "StaticValue", BindingFlags.Public | BindingFlags.Static);
            Assert.That(field.GetValue(null), Is.EqualTo(3));
        }

        [Test]
        public void FieldEnumeration_PreservesHiddenDeclarationsAndPrivateBaseFields()
        {
            var type = typeof(DerivedProbe);
            var fields = Game.ReflectionUtility.EnumerateFields(type).ToArray();
            var shared = fields.Where(field => field.Name == "Shared").ToArray();

            Assert.That(shared.Select(field => field.DeclaringType), Is.EqualTo(new[] { type, typeof(BaseProbe) }));
            Assert.That(fields.Any(field => field.Name == "m_Number"), Is.True);
            Assert.That(fields.Any(field => field.IsStatic), Is.False);
            Assert.That(Game.ReflectionUtility.FindField(type, "Shared").DeclaringType, Is.EqualTo(type));
            var declared = Game.ReflectionUtility.EnumerateFields(type,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToArray();
            Assert.That(declared.Select(field => field.Name), Is.EqualTo(new[] { "Shared" }));
        }

        [Test]
        public void HiddenReadOnlyProperty_DoesNotWriteTheBaseProperty()
        {
            var target = new DerivedProbe();

            Assert.That(Game.ReflectionUtility.GetPropertyValue<string>(target, "Name"), Is.EqualTo("derived name"));
            Assert.Throws<ArgumentException>(() => Game.ReflectionUtility.SetPropertyValue(target, "Name", "changed"));
            Assert.That(((BaseProbe)target).Name, Is.EqualTo("base name"));
        }

        [Test]
        public void InvalidTargetsAndMissingMembers_ReportSpecificErrors()
        {
            var target = new DerivedProbe();

            Assert.That(Assert.Throws<ArgumentNullException>(() =>
                Game.ReflectionUtility.SetPropertyValue(null, "Name", "value")).ParamName, Is.EqualTo("target"));
            Assert.Throws<ArgumentNullException>(() => Game.ReflectionUtility.GetFieldValue<int>(null, "Number"));
            Assert.Throws<ArgumentNullException>(() => Game.ReflectionUtility.FindField(null, "Number"));
            Assert.Throws<ArgumentNullException>(() => Game.ReflectionUtility.FindProperty(null, "Name"));
            Assert.That(Game.ReflectionUtility.FindField(typeof(DerivedProbe), "Missing"), Is.Null);
            Assert.That(Game.ReflectionUtility.FindProperty(typeof(DerivedProbe), "Missing"), Is.Null);
            Assert.Throws<MissingFieldException>(() => Game.ReflectionUtility.GetFieldValue<int>(target, "Missing"));
            Assert.Throws<MissingFieldException>(() => Game.ReflectionUtility.SetFieldValue(target, "Missing", 1));
            Assert.Throws<MissingMemberException>(() => Game.ReflectionUtility.GetPropertyValue<string>(target, "Missing"));
            Assert.Throws<MissingMemberException>(() => Game.ReflectionUtility.SetPropertyValue(target, "Missing", "value"));
        }

        [Test]
        public void TypeMismatchAmbiguityAndGetterFailure_AreNotHidden()
        {
            var target = new DerivedProbe();

            Assert.Throws<InvalidCastException>(() => Game.ReflectionUtility.GetFieldValue<string>(target, "m_Number"));
            Assert.Throws<ArgumentException>(() => Game.ReflectionUtility.SetFieldValue(target, "m_Number", new object()));
            Assert.Throws<AmbiguousMatchException>(() => Game.ReflectionUtility.FindProperty(typeof(DerivedProbe), "Item"));
            var error = Assert.Throws<TargetInvocationException>(() =>
                Game.ReflectionUtility.GetPropertyValue<string>(target, "Failure"));
            Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(error.InnerException.Message, Is.EqualTo("getter failed"));
        }

        [Test]
        public void ValueTypeSetters_UpdateTheSuppliedBox()
        {
            object target = new ValueProbe { Number = 1, Name = "before" };

            Game.ReflectionUtility.SetFieldValue(target, "Number", 2);
            Game.ReflectionUtility.SetPropertyValue(target, "Name", "after");

            Assert.That(((ValueProbe)target).Number, Is.EqualTo(2));
            Assert.That(((ValueProbe)target).Name, Is.EqualTo("after"));
        }
    }
}
