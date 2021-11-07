using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Ruzzie.SimpleReports.Reading;

namespace Ruzzie.SimpleReports.UnitTests
{
    [TestFixture]
    public class ToStringIdTests
    {
        [FsCheck.NUnit.Property]
        public bool NoExceptionsSmokeTest(string input)
        {
            var stringId = StringIdExtensions.ToStringId(input);

            if (stringId.Length > 0)
            {
                return stringId.All(c => !char.IsWhiteSpace(c) &&
                                         (char.IsDigit(c) || char.IsUpper(c) && char.IsLetter(c) || c == '-'));
            }

            return true;
        }

        [TestCase("hello world",        "HELLO-WORLD")]
        [TestCase("hello world ",        "HELLO-WORLD-")]
        [TestCase("\012 Qs",            "12-QS")]
        [TestCase("   hello world",     "HELLO-WORLD")]
        [TestCase("hello{}(*&*& world", "HELLO-WORLD")]
        public void StringStringTests(string input, string expected)
        {
            StringIdExtensions.ToStringId(input).Should().Be(expected);
        }
    }
}