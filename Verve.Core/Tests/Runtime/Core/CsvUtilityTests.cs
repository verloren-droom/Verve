namespace Verve.Tests.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using NUnit.Framework;

    internal sealed class CsvUtilityTests
    {
        [Test]
        public void Csv_IsAvailableAsAGameTool()
        {
            Assert.That(Game.Csv, Is.SameAs(Game.GetTool<ICsv>()));
            Assert.That(Game.Csv.Parse("a,b")[0], Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void Csv_AppendRowAcceptsReadOnlyFieldListsAndEscapesValues()
        {
            var builder = new StringBuilder();
            IReadOnlyList<string> values = new List<string> { "a", "b,c", "line\n", null };

            Game.Csv.AppendRow(builder, values);

            Assert.That(builder.ToString(), Is.EqualTo("a,\"b,c\",\"line\n\"," + Environment.NewLine));
        }

        [TestCase("\n")]
        [TestCase("\r")]
        [TestCase("\r\n")]
        public void Csv_UsesSameDelimitersForQuotedAndUnquotedValues(string newline)
        {
            var rows = Game.Csv.Parse("a,\"b\",\"\"" + newline + "\"c\",d,");
            Assert.That(rows.Count, Is.EqualTo(2));
            CollectionAssert.AreEqual(new[] { "a", "b", "" }, rows[0]);
            CollectionAssert.AreEqual(new[] { "c", "d", "" }, rows[1]);
            CollectionAssert.AreEqual(new[] { "" }, Game.Csv.Parse("\"\"").Single());
            Assert.That(Game.Csv.Parse(""), Is.Empty);
        }

        [TestCase("a\"b,c")]
        [TestCase("\"a\" b,c")]
        [TestCase("\"a\"\"b,c")]
        public void Csv_RejectsMalformedQuotes(string csv) =>
            Assert.Throws<InvalidDataException>(() => Game.Csv.Parse(csv));

    }
}
