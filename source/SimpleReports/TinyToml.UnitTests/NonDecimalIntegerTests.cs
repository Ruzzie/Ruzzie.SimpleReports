using System;
using FluentAssertions;
using NUnit.Framework;
using TinyToml.Scanning;

namespace TinyToml.UnitTests
{
    [TestFixture]
    public class NonDecimalIntegerTests
    {
        public class Hex
        {
            [Test]
            public void Scan_HexDigits()
            {
                //Arrange
                var toml = @" key = 0xA_11ab22\r\nnextkey = 12";
                var sourceScanState = new SourceScanState(toml);

                Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey, "key");
                Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Equal, "=");
                Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.HexPrefix, "0x");
                Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.HexDigits, "A");
                Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Underscore, "_");
                Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.HexDigits, "11ab22");
            }

            [Test]
            public void Parse_HexWithPrefix()
            {
                var text = @"                        
                        hex = 0xDEADBEEF                                               
                        ";
                var doc = Toml.Parse(text);

                //Act & Assert
                doc["hex"].TryReadTomlInteger(out var hexString).Should().BeTrue();
                hexString.Should().Be(3735928559);
            }

            [TestCase("value=0xDEAdBEEF", 3735928559)]
            [TestCase("value=0xdeadBeef", 3735928559)]
            [TestCase("value=0xdead_beef", 3735928559)]
            [TestCase("value=0xde_ad_be_ef", 3735928559)]
            [TestCase("value=0x000dead_beef", 3735928559)]
            public void Parse_HexDigits(string toml, long expected)
            {
                Toml.Parse(toml)["value"].TryReadTomlInteger(out var value).Should().BeTrue();
                value.Should().Be(expected);
            }

            [Test]
            public void ParseError_When_UnderscoreBeforeHexadecimal()
            {
                var text = @"                        
                        hex = 0x_DEADBEEF                                               
                        ";
                Action parseDoc = () => Toml.Parse(text);

                //Act & Assert
                parseDoc.Should().Throw<Exception>()
                    .WithMessage("Error: Expect HexDigits after '0x'. at '_', Line: 2, Column: 27");
            }

            [Test]
            public void ParseError_When_UnderscoreAfterHexadecimal()
            {
                var text = @"                        
                        hex = 0xDEADBEEF_                                               
                        ";
                Action parseDoc = () => Toml.Parse(text);

                //Act & Assert
                parseDoc.Should().Throw<Exception>()
                    .WithMessage("Error: Expect HexDigits after '_'. at '', Line: 3, Column: 25");
            }
        }

        public class Octal
        {
            [Test]
            public void Scan_OctalDigits()
            {
                //Arrange
                var toml            = @" key = 0o0123_4567\r\nnextkey = 12";
                var sourceScanState = new SourceScanState(toml);

                Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey,    "key");
                Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Equal,      "=");
                Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.OctPrefix,  "0o");
                Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.OctDigits,  "0123");
                Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Underscore, "_");
                Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.OctDigits,  "4567");
            }
            [Test]
            public void Parse_OctalWithPrefix()
            {
                var text = @"                        
                        oct = 0o01234567                                               
                        ";
                var doc = Toml.Parse(text);

                //Act & Assert
                doc["oct"].TryReadTomlInteger(out var octString).Should().BeTrue();
                octString.Should().Be(342391);
            }

            [TestCase("value=0o1234567", 342391)]
            [TestCase("value=0o00001234567", 342391)]
            [TestCase("value=0o12_34567", 342391)]
            [TestCase("value=0o012_34_567   ", 342391)]
            public void Parse_OctDigits(string toml, long expected)
            {
                Toml.Parse(toml)["value"].TryReadTomlInteger(out var value).Should().BeTrue();
                value.Should().Be(expected);
            }

            [Test]
            public void ParseError_When_UnderScoreBeforeOctal()
            {
                var text = @"                        
                        oct = 0o_01234567                                                   
                        ";
                Action parseDoc = () => Toml.Parse(text);

                //Act & Assert
                parseDoc.Should().Throw<Exception>()
                    .WithMessage("Error: Expect OctDigits after '0o'. at '_', Line: 2, Column: 27");
            }

            [Test]
            public void ParseError_When_UnderScoreAfterOctal()
            {
                var text = @"                        
                        oct = 0o01234567_                                                   
                        ";
                Action parseDoc = () => Toml.Parse(text);

                //Act & Assert
                parseDoc.Should().Throw<Exception>()
                    .WithMessage("Error: Expect OctDigits after '_'. at '', Line: 3, Column: 25");
            }
        }

        public class Binary
        {
            [Test]
            public void Scan_BinDigits()
            {
                //Arrange
                var toml            = @" key = 0b1101_0110\r\nnextkey = 12";
                var sourceScanState = new SourceScanState(toml);

                Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey,    "key");
                Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Equal,      "=");
                Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BinPrefix,  "0b");
                Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BinDigits,  "1101");
                Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Underscore, "_");
                Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BinDigits,  "0110");
            }

            [Test]
            public void Parse_BinaryWithPrefix()
            {
                var text = @"                        
                        bin = 0b11010110                                              
                        ";
                var doc = Toml.Parse(text);

                //Act & Assert
                doc["bin"].TryReadTomlInteger(out var binString).Should().BeTrue();
                binString.Should().Be(214);
            }

            [TestCase("value=0b011010110 ", 214)]
            [TestCase("value=0b110_10110 ", 214)]
            [TestCase("value=0b11_010_110 ", 214)]
            [TestCase("value=0b0001101_01_10 ", 214)]
            public void Parse_BinaryDigits(string toml, long expected)
            {
                Toml.Parse(toml)["value"].TryReadTomlInteger(out var value).Should().BeTrue();
                value.Should().Be(expected);
            }

            [Test]
            public void ParseError_When_UnderScoreBeforeBinary()
            {
                var text = @"                        
                        bin = 0b_011010110                                                   
                        ";
                Action parseDoc = () => Toml.Parse(text);

                //Act & Assert
                parseDoc.Should().Throw<Exception>().WithMessage("Error: Expect BinDigits after '0b'. at '_', Line: 2, Column: 27");
            }

            [Test]
            public void ParseError_When_UnderScoreAfterBinary()
            {
                var text = @"                        
                        bin = 0b011010110_                                                  
                        ";
                Action parseDoc = () => Toml.Parse(text);

                //Act & Assert
                parseDoc.Should().Throw<Exception>()
                    .WithMessage("Error: Expect BinDigits after '_'. at '', Line: 3, Column: 25");
            }

            [Test]
            public void NoParseError_When_ExactMaxDigits()
            {
                var text = @"                        
                        bin = 0b01010101_11111111_01010101_11111111_01010101_11111111_01010101_11111111                                                  
                        ";
                Action parseDoc = () => Toml.Parse(text);

                //Act & Assert
                parseDoc.Should().NotThrow();
            }

            [Test]
            public void ParseError_When_TooManyDigits()
            {
                var text = @"                        
                        bin = 0b01010101_11111111_01010101_11111111_01010101_11111111_01010101_11111111_1                                                  
                        ";
                Action parseDoc = () => Toml.Parse(text);

                //Act & Assert
                parseDoc.Should().Throw<Exception>()
                        .WithMessage("Error: Could not parse number, too many digits*");
            }
        }
    }
}