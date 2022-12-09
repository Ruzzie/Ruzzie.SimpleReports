using System;
using FluentAssertions;
using NUnit.Framework;
using TinyToml.Scanning;

namespace TinyToml.UnitTests;

[TestFixture]
public class LiteralStrings
{
    [Test]
    public void Scan_LiteralString()
    {
        //Arrange
        var toml            = @" key = 'I need apples'/r/nnextKey = 12";
        var sourceScanState = new SourceScanState(toml);

        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey,       "key");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Equal,         "=");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.LiteralString, "I need apples");
    }
    [TestCase(@"'C:\temp\user\dave'",      "C:\\temp\\user\\dave")]
    [TestCase(@"'\\temp\user$\dave'",      "\\\\temp\\user$\\dave")]
    [TestCase(@"'Dave ""de beer"" Horst'", "Dave \"de beer\" Horst")]
    [TestCase(@"'<\i\c*\s*>'",             "<\\i\\c*\\s*>")]
    public void Parse_EscapedStrings(string escaped, string expectedResult)
    {
        //Arrange
        var toml = $"value = {escaped}";

        //Act
        Toml.Parse(toml)["value"].TryReadTomlString(out var value).Should().BeTrue();

        //AssertParse_MultiLineStringWithoutSpecials
        value.Should().Be(expectedResult);
    }
}

[TestFixture]
public class MultilineLiteralStrings
{
    [Test]
    public void Scan_MultilineLiteralString()
    {
        //Arrange
        var toml            = @" key = '''I need apples'''/r/nnextKey = 12";
        var sourceScanState = new SourceScanState(toml);

        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey,                "key");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Equal,                  "=");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.MultiLineLiteralString, "I need apples");
    }

    [TestCase(@"'''I [dw]on't need \d{2} apples'''", @"I [dw]on't need \d{2} apples")]
    [TestCase(@"'''Here are seven quotation marks: """"""""""""""'''", @"Here are seven quotation marks: """"""""""""""")]
    [TestCase(@"''''That,' she said, 'is still pointless.''''", @"'That,' she said, 'is still pointless.'")]
    [TestCase(@"'''''Dave'''''", @"''Dave''")]
    [TestCase(@"''''''", "")]
    [TestCase(@"''''''''", "''")]
    public void Parse_EscapedStrings(string escaped, string expectedResult)
    {
        //Arrange
        var toml = $"value = {escaped}";

        //Act
        Toml.Parse(toml)["value"].TryReadTomlString(out var value).Should().BeTrue();

        //Assert
        value.Should().Be(expectedResult);
    }

    //IMPORTANT: SET LINE ENDINGS IN YOUR EDITOR TO LF INSTEAD OF CRLF
    [Test]
    public void Parse_MultilineLiteralStringWithWhitespace()
    {
        var toml = @"value = '''
The first newline is
trimmed in raw strings.
   All other whitespace
   is preserved.
'''";
        Toml.Parse(toml)["value"].TryReadTomlString(out var value).Should().BeTrue();

        value.Should().Be($"The first newline is\ntrimmed in raw strings.\n   All other whitespace\n   is preserved.\n");
    }
}