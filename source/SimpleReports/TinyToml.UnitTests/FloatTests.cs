using System;
using FluentAssertions;
using NUnit.Framework;
using TinyToml.Scanning;

namespace TinyToml.UnitTests;

[TestFixture]
public class FloatTests
{
    [Test]
    public void Scan_FloatDigits()
    {
        //Arrange
        var toml            = @" key = 3.123\r\nnextkey = 12";
        var sourceScanState = new SourceScanState(toml);

        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey,      "key");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Equal,        "=");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DecDigits,    "3");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DecimalPoint, ".");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DecDigits,    "123");
    }

    [Test]
    public void Scan_FloatDigitsWithExponent()
    {
        //Arrange
        var toml            = @" key = 1e06https://ki.app/nextkey = 12";
        var sourceScanState = new SourceScanState(toml);

        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey,   "key");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Equal,     "=");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DecDigits, "1");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Exponent,  "e");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DecDigits, "06");
    }

    [Test]
    public void Scan_FloatDigitsWithMinusExponent()
    {
        //Arrange
        var toml            = " key = -2E-2nextkey = 12";
        var sourceScanState = new SourceScanState(toml);

        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey,              "key");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Equal,                "=");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Minus,                "-");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DecDigits,            "2");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Exponent,             "E");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.ExponentNegativeSign, "-");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DecDigits,            "2");
    }

    [Test]
    public void Scan_ComplicatedFloat()
    {
        //Arrange
        var toml            = @" key = +33_00_6.6_426e+34\r\nnextkey = 12";
        var sourceScanState = new SourceScanState(toml);

        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey,              "key");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Equal,                "=");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Plus,                 "+");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DecDigits,            "33");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Underscore,           "_");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DecDigits,            "00");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Underscore,           "_");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DecDigits,            "6");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DecimalPoint,         ".");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DecDigits,            "6");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Underscore,           "_");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DecDigits,            "426");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Exponent,             "e");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.ExponentPositiveSign, "+");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DecDigits,            "34");
    }

    [Test]
    public void Parse_SimpleFloat()
    {
        var text = @"                        
                        value = 3.1415                                            
                        ";
        var doc = Toml.Parse(text);

        //Act & Assert
        doc["value"].TryReadTomlFloat(out var value).Should().BeTrue();
        value.Should().Be(3.1415);
    }
    [Test]
    public void Parse_SimpleFloatWithExponent()
    {
        var text = @"                        
                        value = 5e2                                            
                        ";
        var doc = Toml.Parse(text);

        //Act & Assert
        doc["value"].TryReadTomlFloat(out var value).Should().BeTrue();
        value.Should().Be(500);
    }

    [TestCase("value=2_44.12_34", 244.1234)]
    [TestCase("value = 5e+2",     500)]
    [TestCase("value = -5e+2",    -500)]
    [TestCase("value = -5e-2",    -0.05)]
    [TestCase("value = +5E-2",    0.05)]
    [TestCase("value = +0.0",     0.0)]
    [TestCase("value = -0.0",     0.0)]
    [TestCase("value = 0.1e3",    100)]
    public void Parse_Floats(string toml, double expected)
    {
        Toml.Parse(toml)["value"].TryReadTomlFloat(out var value).Should().BeTrue();
        value.Should().Be(expected);
    }

    [Test]
    public void ParseError_When_UsingDoubleDots()
    {
        var text = @"                        
                        value = 0.1.234567                                                   
                        ";
        Action parseDoc = () => Toml.Parse(text);

        //Act & Assert
        parseDoc.Should().Throw<Exception>()
                .WithMessage("Error: Floats cannot have more than 1 '.'. at '234567', Line: 2, Column: 27");
    }

    [Test]
    public void ParseError_When_UsingDoubleExponent()
    {
        var text = @"                        
                        value = 0.1E10E12                                                   
                        ";
        Action parseDoc = () => Toml.Parse(text);

        //Act & Assert
        parseDoc.Should().Throw<Exception>()
                .WithMessage("Error: Floats cannot have more than one Exponent. at '12', Line: 2, Column: 27");
    }

    [Test]
    public void ParseError_When_NoDigitBeforeDot()
    {
        var text = @"                        
                        value = .1                                                  
                        ";
        Action parseDoc = () => Toml.Parse(text);

        //Act & Assert
        parseDoc.Should().Throw<Exception>()
                .WithMessage("Error: Expect expression at '.', Line: 2, Column: 27");
    }

    [Test]
    public void ParseError_When_NoDigitAfterDot()
    {
        var text = @"                        
                        value = 1.                                                  
                        ";
        Action parseDoc = () => Toml.Parse(text);

        //Act & Assert
        parseDoc.Should().Throw<Exception>()
                .WithMessage("Error: Expect digits after '.'. at '', Line: 3, Column: 25");
    }

    [Test]
    public void ParseError_When_ExponentAfterDot()
    {
        var text = @"                        
                        value = 3.e+20                                                  
                        ";
        Action parseDoc = () => Toml.Parse(text);

        //Act & Assert
        parseDoc.Should().Throw<Exception>()
                .WithMessage("Error: Expect digits after '.'. at 'e', Line: 2, Column: 27");
    }

    [Test]
    public void Scan_Infinity()
    {
        //Arrange
        var toml            = @" key = +inf\r\nnextkey = 12";
        var sourceScanState = new SourceScanState(toml);

        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey,  "key");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Equal,    "=");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Plus,     "+");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Infinity, "inf");
    }

    [TestCase("value = inf",  double.PositiveInfinity)]
    [TestCase("value = +inf", double.PositiveInfinity)]
    [TestCase("value = -inf", double.NegativeInfinity)]
    [TestCase("value = nan",  double.NaN)]
    [TestCase("value = +nan", double.NaN)]
    [TestCase("value = -nan", double.NaN)]
    public void Parse_SpecialFloats(string toml, double expected)
    {
        Toml.Parse(toml)["value"].TryReadTomlFloat(out var value).Should().BeTrue();
        value.Should().Be(expected);
    }
}