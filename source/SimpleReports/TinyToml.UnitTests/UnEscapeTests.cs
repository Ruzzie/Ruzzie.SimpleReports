using System;
using FluentAssertions;
using NUnit.Framework;
using TinyToml.Scanning;

namespace TinyToml.UnitTests;

[TestFixture]
public class EscapeTests
{
    public class BasicStrings
    {
        [TestCase('\u00E9',     true)]
        [TestCase('\u0000',     true)]
        [TestCase('\uFFFF',     true)]
        [TestCase('\U00000000', true)]
        public void IsValidUnicodeScalarValue(char unicode, bool expectedResult)
        {
            var result = TomlUnescapeStrings.IsValidUnicodeScalarValue(unicode);

            result.Should().Be(expectedResult);
        }

        [Test]
        public void Exception_NotAllowedCharacter()
        {
            var    stringWithLineFeed = @"key = ""Hello\lWorld""";
            Action parseText          = () => Toml.Parse(stringWithLineFeed);

            parseText.Should().Throw<Exception>()
                     .WithMessage("Escaped character not allowed, \"\\l\".");
        }

        [Test]
        public void Exception_FalseFourDigitUnicodeCharacter()
        {
            var    stringWithLineFeed = @"key = ""Hello\uD9FFWorld""";
            Action parseText          = () => Toml.Parse(stringWithLineFeed);

            parseText.Should().Throw<Exception>()
                     .WithMessage("Unicode character \"\\uD9FF\" is not valid. InValidUnicodeScalarValue.");
        }

        [Test]
        public void Exception_WrongHexDigitUsageForFourDigits()
        {
            var    stringWithLineFeed = @"key = ""Hello\uFTFFWorld""";
            Action parseText          = () => Toml.Parse(stringWithLineFeed);

            parseText.Should().Throw<Exception>()
                     .WithMessage("Unicode character \"\\uFTFF\" is not valid.*");
        }

        [Test]
        public void Exception_FalseEightDigitUnicodeCharacter()
        {
            var    stringWithLineFeed = @"key = ""Hello\U11111111lWorld""";
            Action parseText          = () => Toml.Parse(stringWithLineFeed);

            parseText.Should().Throw<Exception>()
                     .WithMessage("Unicode character \"\\U11111111\" is not valid. A valid UTF32 value is between 0x000000 and 0x10ffff, *");
        }

        [Test]
        public void Exception_WrongHexDigitUsageForEightDigits()
        {
            var    stringWithLineFeed = @"key = ""Hello\U0000000GWorld""";
            Action parseText          = () => Toml.Parse(stringWithLineFeed);

            parseText.Should().Throw<Exception>()
                     .WithMessage("Unicode character \"\\U0000000G\" is not valid.*");
        }

        [Test]
        public void BasicString_Error_UnescapedNewLinesNotAllowed()
        {
            var    input     = "key = \" hello \n \t\"";
            Action parseText = () => Toml.Parse(input);

            parseText.Should().Throw<Exception>()
                     .WithMessage("*Unescaped \\n is not allowed in a basic string*");
        }

        [Test]
        public void Scan_BasicStringWithEscapedText()
        {
            //Arrange
            var toml            = @"key = ""Hello\bWorld""\r\nnextKey = 12";
            var sourceScanState = new SourceScanState(toml);

            Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey,     "key");
            Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Equal,       "=");
            Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BasicString, "Hello\bWorld");
        }

        [Test]
        public void CheckUnescapedAndOriginalText()
        {
            //Arrange
            var toml            = @"key = ""Hello\bWorld""";
            var sourceScanState = new SourceScanState(toml);

            Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey,     "key");
            Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Equal,       "=");
            Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BasicString, "Hello\bWorld");
        }

        [TestCase(@"""Hello\bWorld""",     "Hello\bWorld")]
        [TestCase(@"""Hello\tWorld""",     "Hello\tWorld")]
        [TestCase(@"""Hello\nWorld""",     "Hello\nWorld")]
        [TestCase(@"""Hello\fWorld""",     "Hello\fWorld")]
        [TestCase(@"""Hello\rWorld""",     "Hello\rWorld")]
        [TestCase(@"""Hello\\World""",     "Hello\\World")]
        [TestCase(@"""Hello\""World""",    "Hello\"World")]
        [TestCase(@"""Sch\u00f6nen""",     "Schönen")]
        [TestCase(@"""Sch\U000000f6nen""", "Schönen")]
        [TestCase(@"""Sch\U0010ffffnen""", "Sch􏿿nen")]
        [TestCase(@"""I'm a string. \""You can quote me\"". Name\tJos\u00E9\nLocation\tSF.""",
                  "I'm a string. \"You can quote me\". Name\tJosé\nLocation\tSF.")]
        public void Parse_EscapedStrings(string escaped, string expectedResult)
        {
            //Arrange
            var toml = $"value = {escaped}";

            //Act
            Toml.Parse(toml)["value"].TryReadTomlString(out var value).Should().BeTrue();

            //Assert
            value.Should().Be(expectedResult);
        }

        [TestCase(@"""Hello\bWorld""",     "Hello\bWorld")]
        [TestCase(@"""Hello\tWorld""",     "Hello\tWorld")]
        [TestCase(@"""Hello\nWorld""",     "Hello\nWorld")]
        [TestCase(@"""Hello\fWorld""",     "Hello\fWorld")]
        [TestCase(@"""Hello\rWorld""",     "Hello\rWorld")]
        [TestCase(@"""Hello\\World""",     "Hello\\World")]
        [TestCase(@"""Hello\""World""",    "Hello\"World")]
        [TestCase(@"""Sch\u00f6nen""",     "Schönen")]
        [TestCase(@"""Sch\U000000f6nen""", "Schönen")]
        [TestCase(@"""Sch\U0010ffffnen""", "Sch􏿿nen")]
        [TestCase(@"""I'm a string. \""You can quote me\"". Name\tJos\u00E9\nLocation\tSF.""",
                  "I'm a string. \"You can quote me\". Name\tJosé\nLocation\tSF.")]
        public void Scan_StringsOriginalAndUnescapedText(string escaped, string expectedResult)
        {
            //Arrange
            var toml            = $"value = {escaped}";
            var sourceScanState = new SourceScanState(toml);
            Scanner.ScanNext(ref sourceScanState);
            Scanner.ScanNext(ref sourceScanState);
            //Act
            var token = Scanner.ScanNext(ref sourceScanState);

            //Assert
            token.TokenType.Should().Be(TokenType.BasicString);
            token.Text.ToString().Should().Be(expectedResult);
        }
    }

    public class MultiLineStrings
    {
        [Test]
        public void Parse_MultiLineStringWithoutSpecials()
        {
            var toml = @"value = """"""Roses are red\r\nViolets are blue""""""";
            Toml.Parse(toml)["value"].TryReadTomlString(out var value).Should().BeTrue();

            value.Should().Be("Roses are red\r\nViolets are blue");
        }

        [Test]
        public void Parse_MultiLineStringWithoutExtraneousWhiteSpace()
        {
            var toml = @"value = """"""Roses are red \

                                violets are blue""""""";
            Toml.Parse(toml)["value"].TryReadTomlString(out var value).Should().BeTrue();

            value.Should().Be("Roses are red violets are blue");
        }

        [TestCase(@"""""""R1 \
 l \
 s \
 t""""""", "R1 l s t")]

        [TestCase(@"""""""R2 \
 l \
 s\
 \nt""""""", "R2 l s\nt")]

        [TestCase(@"""""""R3 \
 l \
 s\
 """"\""t""""""", "R3 l s\"\"\"t")]

        [TestCase("\"\"\"\nR4 \\\n l \\\n s\\\n \"\"\\\"t\"\"\"", "R4 l s\"\"\"t")]
        [TestCase("\"\"\"R5 \\\n     l \\   \n   s \\\n t\"\"\"", "R5 l s t")]
        [TestCase("\"\"\"\r\nR t\"\"\"",                          "R t")]
        [TestCase("\"\"\"R t\\ \n \"\"\"",                        "R t")]
        [TestCase("\"\"\"R t\\ \n     \t\"\"\"",                  "R t")]
        [TestCase(@"""""""""""Dave""""""""""",                    "\"\"Dave\"\"")]
        public void Parse_MultilineStrings(string multilineString, string expectedResult)
        {
            //Arrange
            var toml = $"value = {multilineString}";

            //Act
            Toml.Parse(toml)["value"].TryReadTomlString(out var value).Should().BeTrue();

            //Assert
            value.Should().Be(expectedResult);
        }

        [TestCase("\"\"\" 1\\\\2 \"\"\"", @" 1\2 ")]
        [TestCase("\"\"\" Here are three quotation marks: \"\"\\\". \"\"\"",              @" Here are three quotation marks: """""". ")]
        [TestCase("\"\"\" 3 \\\" 4 \"\"\"",                   @" 3 "" 4 ")]
        [TestCase("\"\"\"2R \\\n     l \\   \n   s \\\n t\"\"\"", "2R l s t")]
        [TestCase(@"""""""3R \\ l \\ s\\ \nt""""""",             "3R \\ l \\ s\\ \nt")]
        [TestCase("\"\"\" 4R \\\n l \\\n s\\\n \"\"\\\\\"t \"\"\"", " 4R l s\"\"\\\"t ")]
        [TestCase("\"\"\"\n5R \\\n l \\\n s\\\n \"\"\\\n\"t\"\"\"",   "5R l s\"\"\"t")]
        [TestCase("\"\"\"\r\n6R t\"\"\"",                     "6R t")]
        [TestCase("\"\"\"7R t\\ \n \"\"\"",                   "7R t")]
        [TestCase("\"\"\"8R t\\ \n     \t\"\"\"",             "8R t")]
        [TestCase(@"""""""""""Dave""""""""""",                "\"\"Dave\"\"")]
        public void Scan_MultilineStrings(string escaped, string expectedResult)
        {
            //Arrange
            var toml            = $"value = {escaped}/r/nNextkey = 12";
            var sourceScanState = new SourceScanState(toml);
            Scanner.ScanNext(ref sourceScanState);
            Scanner.ScanNext(ref sourceScanState);
            //Act
            var token = Scanner.ScanNext(ref sourceScanState);

            //Assert
            token.TokenType.Should().Be(TokenType.MultiLineString, token.Text.ToString());
            token.Text.ToString().Should().Be(expectedResult);
        }

        [Test]
        public void Parse_ComplicatedMultiLineString()
        {
            var toml = @"value = """""" Roses \
        are \
red. \U0010ffff\u00f6. \t5 quotation marks """"\"""""".""""""";
            Toml.Parse(toml)["value"].TryReadTomlString(out var value).Should().BeTrue();

            value.Should().Be(" Roses are red. 􏿿ö. \t5 quotation marks \"\"\"\"\".");
        }

        [Test]
        public void Parse_Error_WhenNoLineEndingAfterBackslash()
        {
            //Error while parsing string: line-ending backslashes must be the last non-whitespace character on the line (error occurred at line 1, column 24)
            var toml = @"value = """""" Roses \        are \
red. \U0010ffff\u00f6. \t5 quotation marks """"\\"""""".""""""";

            Action act = ()=>  Toml.Parse(toml);

            //https://toml-parser.com/
            act.Should().Throw<Exception>()
               .WithMessage("*line-ending backslashes must be the last non-whitespace character*");
        }
    }
}