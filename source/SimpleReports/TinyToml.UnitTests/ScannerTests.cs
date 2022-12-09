using System.IO;
using FluentAssertions;
using NUnit.Framework;
using TinyToml.Scanning;

#nullable enable
namespace TinyToml.UnitTests;

[TestFixture]
public class ScannerTests
{
    [Test]
    public void Empty()
    {
        //Arrange
        var toml            = "";
        var sourceScanState = new SourceScanState(toml);

        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.Eof, token.Text.ToString());
    }

    [Test]
    public void Error_Unexpected_Char()
    {
        //Arrange
        var toml            = "*";
        var sourceScanState = new SourceScanState(toml);

        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.Literal, token.Text.ToString());
    }

    [Test]
    public void BareKey()
    {
        //Arrange
        var toml            = "    key = 1";
        var sourceScanState = new SourceScanState(toml);

        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.BareKey, token.Text.ToString());
        token.Text.ToString().Should().Be("key");
    }

    [Test]
    public void BareKeyOnlyDigits()
    {
        //Arrange
        var toml            = "001 = 1";
        var sourceScanState = new SourceScanState(toml);

        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.BareKey, token.Text.ToString());
        token.Text.ToString().Should().Be("001");
    }

    [TestCase("inf")]
    [TestCase("nan")]
    [TestCase("true")]
    [TestCase("false")]
    public void BareKeyValueKeywords(string keyword)
    {
        //Arrange
        var toml            = $"{keyword} = 1";
        var sourceScanState = new SourceScanState(toml);

        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.BareKey, token.Text.ToString());
        token.Text.ToString().Should().Be(keyword);
    }

    [Test]
    public void QuotedKey()
    {
        //Arrange
        var toml            = "\"key\" = 1";
        var sourceScanState = new SourceScanState(toml);

        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.BasicString, token.Text.ToString());
        token.Text.ToString().Should().Be("key");
    }
    [Test]
    public void QuotedDottedKey()
    {
        //Arrange
        var toml            = "k.\"key\" = 1";
        var sourceScanState = new SourceScanState(toml);

        //1. Assert: BareKey
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey, "k");

        //2. Assert: Dot
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Dot, ".");

        //3. Assert:  Basic string
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BasicString, "key");
    }

    [Test]
    public void DigitsLeadingZeroError()
    {
        //Arrange
        var toml            = "value = +00001";
        var sourceScanState = new SourceScanState(toml);

        //1. Assert: BareKey
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey, "value");

        //2. Assert: =
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Equal, "=");

        //3. Assert: +
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Plus, "+");

        //4. Assert: ERROR| We allow leading zero's
        //Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Error, "Expected x, b, or o. Numbers with leading zero's are not allowed.");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DecDigits, "00001");
    }

    [Test]
    public void MultipleDotsInKey()
    {
        //Arrange
        var toml            = "k.\"key\".'a'.b.c = 1";
        var sourceScanState = new SourceScanState(toml);

        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey,       "k");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Dot,           ".");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BasicString,   "key");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Dot,           ".");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.LiteralString, "a");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Dot,           ".");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey,       "b");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Dot,           ".");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey,       "c");
    }

    [Test]
    public void Equals()
    {
        //Arrange
        var toml            = " key = 1";
        var sourceScanState = new SourceScanState(toml);
        Scanner.ScanNext(ref sourceScanState);

        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.Equal, token.Text.ToString());
        token.Text.ToString().Should().Be("=");
    }

    [Test]
    public void Digits()
    {
        //Arrange
        var toml            = " key = 1";
        var sourceScanState = new SourceScanState(toml);
        Scanner.ScanNext(ref sourceScanState);
        Scanner.ScanNext(ref sourceScanState);
        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.DecDigits, token.Text.ToString());
        token.Text.ToString().Should().Be("1");
    }

    [Test]
    public void True()
    {
        //Arrange
        var toml            = " key = true";
        var sourceScanState = new SourceScanState(toml);
        Scanner.ScanNext(ref sourceScanState);
        Scanner.ScanNext(ref sourceScanState);
        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.True, token.Text.ToString());
        token.Text.ToString().Should().Be("true");
    }

    [TestCase("truekey = true")]
    [TestCase("true-key = true")]
    [TestCase("true_key = true")]
    public void TrueInKeyNameShouldReturnKey(string toml)
    {
        //Arrange
        var sourceScanState = new SourceScanState(toml);

        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.BareKey, token.Text.ToString());
    }

    [Test]
    public void False()
    {
        //Arrange
        var toml            = " key = false ";
        var sourceScanState = new SourceScanState(toml);
        Scanner.ScanNext(ref sourceScanState);
        Scanner.ScanNext(ref sourceScanState);
        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.False, token.Text.ToString());
        token.Text.ToString().Should().Be("false");
    }

    [Test]
    public void LiteralStringValue()
    {
        //Arrange
        var toml            = "key = '1'";
        var sourceScanState = new SourceScanState(toml);
        Scanner.ScanNext(ref sourceScanState);
        Scanner.ScanNext(ref sourceScanState);
        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.LiteralString, token.Text.ToString());
        token.Text.ToString().Should().Be("1");
    }

    [Test]
    public void EmptyLiteralStringValue()
    {
        //Arrange
        var toml            = "key = ''";
        var sourceScanState = new SourceScanState(toml);
        Scanner.ScanNext(ref sourceScanState);
        Scanner.ScanNext(ref sourceScanState);
        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.EmptyString, token.Text.ToString());
        token.Text.ToString().Should().Be("''");

        var postCondition = Scanner.ScanNext(ref sourceScanState);
        postCondition.TokenType.Should().Be(TokenType.Eof, postCondition.Text.ToString());
    }

    [Test]
    public void EmptyBasicStringValue()
    {
        //Arrange
        var toml            = "key = \"\"";
        var sourceScanState = new SourceScanState(toml);
        Scanner.ScanNext(ref sourceScanState);
        Scanner.ScanNext(ref sourceScanState);
        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.EmptyString, token.Text.ToString());
        token.Text.ToString().Should().Be("\"\"");

        var postCondition = Scanner.ScanNext(ref sourceScanState);
        postCondition.TokenType.Should().Be(TokenType.Eof, postCondition.Text.ToString());
    }

    [Test]
    public void UnterminatedLiteralStringValue()
    {
        //Arrange
        var toml            = "key = '1";
        var sourceScanState = new SourceScanState(toml);
        Scanner.ScanNext(ref sourceScanState);
        Scanner.ScanNext(ref sourceScanState);

        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.Error, token.Text.ToString());
        token.Text.ToString().Should().Be("Unterminated literal string");
    }

    [Test]
    public void BasicStringValue()
    {
        //Arrange
        var toml            = "key = \"1\"";
        var sourceScanState = new SourceScanState(toml);
        Scanner.ScanNext(ref sourceScanState);
        Scanner.ScanNext(ref sourceScanState);

        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.BasicString, token.Text.ToString());
        token.Text.ToString().Should().Be("1");
    }

    [Test]
    public void UnterminatedBasicStringValue()
    {
        //Arrange
        var toml            = "key = \"1";
        var sourceScanState = new SourceScanState(toml);
        Scanner.ScanNext(ref sourceScanState);
        Scanner.ScanNext(ref sourceScanState);

        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.Error, token.Text.ToString());
        token.Text.ToString().Should().Be("Unterminated basic string");
    }

    [Test]
    public void MultiLineStringValue()
    {
        //Arrange
        var toml            = "key = \"\"\"\r\n\t1\n\"\"\"";
        var sourceScanState = new SourceScanState(toml);
        Scanner.ScanNext(ref sourceScanState);
        Scanner.ScanNext(ref sourceScanState);

        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.MultiLineString, token.Text.ToString());
        token.Text.ToString().Should().Be("\t1\n");
    }

    [Test]
    public void EmptyMultiLineStringValue()
    {
        //Arrange
        var toml            = "key = \"\"\"\"\"\"";
        var sourceScanState = new SourceScanState(toml);
        Scanner.ScanNext(ref sourceScanState);
        Scanner.ScanNext(ref sourceScanState);

        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.EmptyString, token.Text.ToString());
    }

    [Test]
    public void UnterminatedMultiLineStringValue()
    {
        //Arrange
        var toml            = "key = \"\"\"\r\n\t1\n\"\"";
        var sourceScanState = new SourceScanState(toml);
        Scanner.ScanNext(ref sourceScanState);
        Scanner.ScanNext(ref sourceScanState);

        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.Error, token.Text.ToString());
        token.Text.ToString().Should().Be("Unterminated multi-line string");
    }

    [Test]
    public void UnterminatedMultiLineAtEofStringValue()
    {
        //Arrange
        var toml            = "key = \"\"\"\r\n\t1\n";
        var sourceScanState = new SourceScanState(toml);
        Scanner.ScanNext(ref sourceScanState);
        Scanner.ScanNext(ref sourceScanState);

        //Act
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.Error, token.Text.ToString());
        token.Text.ToString().Should().Be("Unterminated multi-line string at EOF");
    }

    [Test]
    public void Table()
    {
        //Arrange
        var toml            = "[table]";
        var sourceScanState = new SourceScanState(toml);

        //Act & Assert
        Scanner.ScanNext(ref sourceScanState).TokenType.Should().Be(TokenType.LeftBracket);
        Scanner.ScanNext(ref sourceScanState).TokenType.Should().Be(TokenType.BareKey);
        Scanner.ScanNext(ref sourceScanState).TokenType.Should().Be(TokenType.RightBracket);
    }

    [Test]
    public void ArrayOfTables()
    {
        //Arrange
        var toml            = "[[table]]";
        var sourceScanState = new SourceScanState(toml);

        //Act & Assert
        Scanner.ScanNext(ref sourceScanState).TokenType.Should().Be(TokenType.DoubleLeftBracket);
        Scanner.ScanNext(ref sourceScanState).TokenType.Should().Be(TokenType.BareKey);
        Scanner.ScanNext(ref sourceScanState).TokenType.Should().Be(TokenType.DoubleRightBracket);
    }

    [TestCase("[table] #my comment\r\n")]
    [TestCase("[table] #my comment\n")]
    [TestCase("[table] \n #my comment\r\n")]
    [TestCase("[table] \n #my comment\n")]
    public void Comment(string toml)
    {
        //Arrange
        var sourceScanState = new SourceScanState(toml);
        Scanner.ScanNext(ref sourceScanState).TokenType.Should().Be(TokenType.LeftBracket);
        Scanner.ScanNext(ref sourceScanState).TokenType.Should().Be(TokenType.BareKey);
        Scanner.ScanNext(ref sourceScanState).TokenType.Should().Be(TokenType.RightBracket);

        //Act & Assert
        var token = Scanner.ScanNext(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.Comment, token.Text.ToString());
        token.Text.ToString().Should().Be("#my comment");
    }

    [Test]
    public void NoErrorsInValidDocument()
    {
        //Arrange
        var toml =
            File.ReadAllText(Path.Join(TestContext.CurrentContext.TestDirectory, "customer_reports.toml"));

        //Act
        var sourceScanState = new SourceScanState(toml);

        //Assert
        Token token;
        while ((token = Scanner.ScanNext(ref sourceScanState)).TokenType != TokenType.Eof)
        {
            token.TokenType.Should().NotBe(TokenType.Error, token.Text.ToString());
        }
    }
}