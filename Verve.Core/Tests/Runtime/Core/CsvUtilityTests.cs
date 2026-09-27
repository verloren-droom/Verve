namespace Verve.Tests.Core
{
    using System.IO;
    using System.Linq;
    using NUnit.Framework;

    internal sealed class CsvUtilityTests
    {
        [TestCase("\n")]
        [TestCase("\r")]
        [TestCase("\r\n")]
        public void Csv_UsesSameDelimitersForQuotedAndUnquotedValues(string newline)
        {
            var rows = Game.CsvUtility.Parse("a,\"b\",\"\"" + newline + "\"c\",d,");
            Assert.That(rows.Count, Is.EqualTo(2));
            CollectionAssert.AreEqual(new[] { "a", "b", "" }, rows[0]);
            CollectionAssert.AreEqual(new[] { "c", "d", "" }, rows[1]);
            CollectionAssert.AreEqual(new[] { "" }, Game.CsvUtility.Parse("\"\"").Single());
            Assert.That(Game.CsvUtility.Parse(""), Is.Empty);
        }

        [TestCase("a\"b,c")]
        [TestCase("\"a\" b,c")]
        [TestCase("\"a\"\"b,c")]
        public void Csv_RejectsMalformedQuotes(string csv) =>
            Assert.Throws<InvalidDataException>(() => Game.CsvUtility.Parse(csv));

    }
}
