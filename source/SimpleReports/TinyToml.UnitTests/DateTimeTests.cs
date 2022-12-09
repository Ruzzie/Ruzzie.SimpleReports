using System;
using FluentAssertions;
using NUnit.Framework;
using TinyToml.Scanning;

namespace TinyToml.UnitTests;

[TestFixture]
public class DateTimeTests
{
    [Test]
    public void Scan_DateTime()
    {
        //Arrange
        var toml            = @"key = 1979-05-27T07:32:00.999999\r\nNextKey = 12";
        var sourceScanState = new SourceScanState(toml);

        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey,        "key");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Equal,          "=");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDigits, "1979");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDash,   "-");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDigits, "05");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDash,   "-");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDigits, "27");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeT,      "T");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDigits, "07");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeColon,  ":");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDigits, "32");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeColon,  ":");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDigits, "00");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDot,    ".");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDigits, "999999");
    }

    [Test]
    public void Scan_DateTimeWithSpace()
    {
        //Arrange
        var toml            = @"key = 1979-05-27 07:32:00-07:00\r\nNextKey = 12";
        var sourceScanState = new SourceScanState(toml);

        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.BareKey,        "key");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.Equal,          "=");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDigits, "1979");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDash,   "-");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDigits, "05");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDash,   "-");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDigits, "27");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDigits, "07");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeColon,  ":");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDigits, "32");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeColon,  ":");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDigits, "00");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDash,   "-");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDigits, "07");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeColon,  ":");
        Should.Expect(Scanner.ScanNext(ref sourceScanState), TokenType.DateTimeDigits, "00");
    }

    [TestCase("value = 1979-05-27",                 1979, 5, 27, 0, 0,  0, 0)]
    [TestCase("value = 1979-05-27T00:32:00",        1979, 5, 27, 0, 32, 0, 0)]
    [TestCase("value = 1979-05-27T01",              1979, 5, 27, 1, 0,  0, 0)]
    [TestCase("value = 1979-05-27 00:32:00.999999", 1979, 5, 27, 0, 32, 0, 999)]
    [TestCase("value = 1979-05-27 00:32:00.1123",   1979, 5, 27, 0, 32, 0, 112)]
    public void Parse_DateTimes(string toml,
                                int    year,
                                int    month,
                                int    day,
                                int    hour,
                                int    minute,
                                int    second,
                                int    milliSecond)
    {
        Toml.Parse(toml)["value"].TryReadTomlDateTimeOffset(out var value).Should().BeTrue();
        value.Should().Be(new DateTime(year, month, day, hour, minute, second, milliSecond));
    }

    [TestCase("1979-05-27T00:32:00.999999", 999)]
    [TestCase("1979-05-27T00:32:00.999998", 999)]
    [TestCase("1979-05-27T00:32:00.123",    123)]
    [TestCase("1979-05-27T00:32:00.1123",   112)]
    [TestCase("1979-05-27T00:32:00.1129",   112)]
    public void Parse_TruncateMilliseconds(string dateTimeString, int expectedMilliSeconds)
    {
        //Arrange
        var toml = $"value = {dateTimeString}";

        //Act
        Toml.Parse(toml)["value"].TryReadTomlDateTimeOffset(out var value).Should().BeTrue();

        //Assert
        value.Millisecond.Should().Be(expectedMilliSeconds);
    }

    [Test]
    public void Parse_IncompleteDateTimeFormat()
    {
        //note: I like that the parser is robust enough to parse partial dates and doesn't give an error.
        //      Either way would be fine.

        //Arrange
        var toml = @"value = 1979-05";
        //Act
        var tomlDoc = Toml.Parse(toml);
        //Assert
        tomlDoc["value"].TryReadTomlDateTimeOffset(out var result).Should().BeTrue();
        result.Should().Be(new DateTime(1979, 5, 1));
    }

    [TestCase("value = 1979-05-27T00:32:00Z",             1979, 5, 27, 0, 32, 0, 0,   0, 0)]
    [TestCase("value = 1979-05-27T00:32:00-07:11",        1979, 5, 27, 0, 32, 0, 0,   7, 11)]
    [TestCase("value = 1979-05-27T00:32:00.999999-07:00", 1979, 5, 27, 0, 32, 0, 999, 7, 0)]
    public void Parse_DateTimesWithOffset(string toml,
                                          int    year,
                                          int    month,
                                          int    day,
                                          int    hour,
                                          int    minute,
                                          int    second,
                                          int    milliSeconds,
                                          int    offsetHours,
                                          int    offsetMinutes)
    {
        Toml.Parse(toml)["value"].TryReadTomlDateTimeOffset(out var value).Should().BeTrue();
        value.Should().Be(new DateTimeOffset(new DateTime(year, month, day, hour, minute, second, milliSeconds),
                                             new TimeSpan(offsetHours, offsetMinutes, 0)));
    }

    [TestCase(@"value = 1979-",                   "Error: Expect datetime digits after '-'. at '', Line: 1, Column: 2")]
    [TestCase(@"value = 1979-05-",                "Error: Expect datetime digits after '-'. at '', Line: 1, Column: 2")]
    [TestCase(@"value = 1979-05-02T",             "Error: Expect datetime digits after 'T'. at '', Line: 1, Column: 2")]
    [TestCase(@"value = 1979-05-02T01:",          "Error: Expect datetime digits after ':'. at '', Line: 1, Column: 2")]
    [TestCase(@"value = 1979-05-02T01:02:",       "Error: Expect datetime digits after ':'. at '', Line: 1, Column: 2")]
    [TestCase(@"value = 1979-05-02T01:02:03.",    "Error: Expect datetime digits after '.'. at '', Line: 1, Column: 2")]
    [TestCase(@"value = 1979-05-02T01:02:03-",    "Error: Expect datetime digits after '-'. at '', Line: 1, Column: 2")]
    [TestCase(@"value = 1979-05-02T01:02:03-07:", "Error: Expect datetime digits after ':'. at '', Line: 1, Column: 2")]
    public void ParseError_When_NoDatetimeDigitsAfterDatetimeDash(string toml, string expectedError)
    {

        Action parseDoc = () => Toml.Parse(toml);

        //Act & Assert
        parseDoc.Should().Throw<Exception>()
                .WithMessage(expectedError);
    }
}