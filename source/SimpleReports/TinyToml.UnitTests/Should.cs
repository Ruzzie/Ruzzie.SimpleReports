using FluentAssertions;
using TinyToml.Scanning;

namespace TinyToml.UnitTests;

internal static class Should
{
    internal static void Expect(Token actual, TokenType expectedTokenType, string expectedText)
    {
        actual.TokenType.Should().Be(expectedTokenType, actual.Text.ToString());
        actual.Text.ToString().Should().Be(expectedText);
    }
}