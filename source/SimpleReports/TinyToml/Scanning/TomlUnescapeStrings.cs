using System;
using System.Globalization;

namespace TinyToml.Scanning
{
    internal static class TomlUnescapeStrings
    {
        public static Token BasicString(scoped ref SourceScanState scanState)
        {
            Span<char> output   = new char[scanState.SourceDataLength - scanState.CurrentPos + 1];
            var        writePos = 0;

            while (scanState.HasNext())
            {
                var current = scanState.Advance();

                switch (current)
                {
                    case '\n'
                        : //TODO: for extra strictness, check that no unescaped control characters other than \t are allowed
                        scanState.Line++;
                        return Scanner.ErrorToken(scanState, "Unescaped \\n is not allowed in a basic string");
                    case '"':
                        //Closing quote
                        return Scanner.CreateStringToken(TokenType.BasicString, scanState, output[..writePos]);
                    case '\\':
                        if (scanState.HasNext())
                        {
                            current  = scanState.Advance();
                            writePos = UnescapeCharacter(ref scanState, current, output, writePos);
                        }

                        break;
                    default:
                        output[writePos++] = current;
                        break;
                }
            }

            return Scanner.ErrorToken(scanState, "Unterminated basic string");
        }

        public static Token MultiLineString(scoped ref SourceScanState scanState)
        {
            Span<char> output   = new char[scanState.SourceDataLength - scanState.CurrentPos + 4];
            var        writePos = 0;

            //Skip/Ignore starting line-break after opening quotes
            switch (scanState.PeekNext())
            {
                case '\n':
                    //Starting NewLine: skip this
                    scanState.Advance();
                    break;
                case '\r':
                    scanState.Advance();
                    if (scanState.PeekNext() == '\n')
                    {
                        //Starting NewLine: skip this
                        scanState.Advance();
                    }
                    else
                    {
                        //Not a starting NewLine: add the \r to the string
                        output[writePos++] = '\r';
                    }

                    break;
            }

            while (scanState.HasNext())
            {
                var current = scanState.Advance();

                switch (current)
                {
                    case '"':
                        if (IsPartOfClosingQuotes(ref scanState, output, writePos, out var token))
                        {
                            return token;
                        }

                        break;
                    case '\r':
                        output[writePos++] = current;
                        continue;
                    case '\n':
                        output[writePos++] = current;
                        scanState.Line++;
                        continue;
                    case '\\': // can be a line ending slash or an escaped character
                        var next = scanState.PeekNext();

                        //When the next character is a whitespace character
                        //  it should be treated as a line-ending \ in a multi-line string
                        if (IsWhiteSpaceCharacter(next))
                        {
                            var currentLine = scanState.Line;

                            Scanner.AdvanceWhiteSpaces(ref scanState);
                            if (scanState.Line <= currentLine)
                            {
                                // no lines advanced, so error
                                return Scanner.ErrorToken(scanState
                                                        , "Line-ending backslashes must be the last non-whitespace character on the line");
                            }

                            if (scanState.IsAtEnd())
                            {
                                return Scanner.ErrorToken(scanState, "Unterminated multi-line string after '\'");
                            }

                            continue;
                        }
                        // an escaped character like : \\ or \t or \u0123
                        else
                        {
                            //Should be an escaped character sequence
                            current  = scanState.Advance();
                            writePos = UnescapeCharacter(ref scanState, current, output, writePos);
                            continue;
                        }
                }

                output[writePos++] = scanState.SourceDataSpan[scanState.CurrentPos - 1];
            }

            return Scanner.ErrorToken(scanState, "Unterminated multi-line string at EOF");
        }

        private static bool IsPartOfClosingQuotes(scoped ref SourceScanState scanState
                                                , Span<char>                 output
                                                , int                        writePos
                                                , out Token                  token)
        {
            //Closing quotes """
            var lookAhead = scanState.LookAhead(2);
            if (lookAhead.IsEmpty)
            {
                token = Scanner.ErrorToken(scanState, "Unterminated multi-line string");
                return true;
            }

            if (lookAhead[0] == '"' && lookAhead[1] == '"')
            {
                scanState.Advance(2);
                if (scanState.PeekNext() == '"')
                {
                    scanState.Advance();
                    output[writePos++] = '"';
                    if (scanState.PeekNext() == '"') // 5 " in a row is allowed.
                    {
                        scanState.Advance();
                        output[writePos++] = '"';
                        if (scanState.PeekNext() == '"') // 6 " in a row is not allowed.
                        {
                            scanState.Advance();
                            token = Scanner.ErrorToken(scanState, "Invalid usage of double quotes");
                        }
                    }
                }

                if (writePos == 0)
                {
                    token = Scanner.CreateToken(TokenType.EmptyString, scanState);
                    return true;
                }

                token = Scanner.CreateStringToken(TokenType.MultiLineString, scanState, output[..writePos]);
                return true;
            }

            token = default;
            return false;
        }

        private static bool IsWhiteSpaceCharacter(char current)
        {
            /*
            uint tab   = '\t'; // 9           1001
            uint lf    = '\n'; // 10          1010
            uint vt    = '\v'; // 11          1011
            uint ff    = '\f'; // 12          1100
            uint cr    = '\r'; // 13          1101
            uint space = ' ';  // 32    0010  0000
            */

            return (current >= 9 && current <= 13) || current == ' ';
        }

        private static int UnescapeCharacter(ref SourceScanState scanState
                                           , char                current
                                           , Span<char>          outputBuffer
                                           , int                 writePos)
        {
            if (TryParseCompactEscapeSequence(current, out var unescapedChar))
            {
                outputBuffer[writePos++] = unescapedChar;
                return writePos;
            }

            switch (current)
            {
                case 'u':
                    outputBuffer[writePos++] = CreateFourDigitUnicodeChar(ref scanState);
                    return writePos;
                case 'U':
                    writePos = CreateEightDigitUnicodeChar(ref scanState, outputBuffer, writePos);
                    return writePos;
                default:
                    throw new Exception($"Escaped character not allowed, \"\\{current}\".");
            }
        }

        private static bool TryParseCompactEscapeSequence(char current, out char unEscapedChar)
        {
            // \v ...?
            switch (current)
            {
                case 'b':
                    unEscapedChar = '\b';
                    break;
                case 'f':
                    unEscapedChar = '\f';
                    break;
                case 'n':
                    unEscapedChar = '\n';
                    break;
                case 'r':
                    unEscapedChar = '\r';
                    break;
                case 't':
                    unEscapedChar = '\t';
                    break;
                case '"':
                    unEscapedChar = '\"';
                    break;
                case '\\':
                    unEscapedChar = '\\';
                    break;
                default:
                    unEscapedChar = default;
                    return false;
            }

            return true;
        }

        private static int CreateEightDigitUnicodeChar(ref SourceScanState scanState, Span<char> output, int writePos)
        {
            var nextEightChars = scanState.LookAhead(8);
            if (nextEightChars.IsEmpty)
            {
                throw new Exception("Wrong usage of \\U");
            }

            scanState.Advance(8);
            var unescapedUnicodeString = UnEscapeEightDigitUnicodeChar(nextEightChars);
            output[writePos++] = unescapedUnicodeString[0];
            if (unescapedUnicodeString.Length == 2)
            {
                output[writePos++] = unescapedUnicodeString[1];
            }

            return writePos;
        }

        private static char CreateFourDigitUnicodeChar(ref SourceScanState scanState)
        {
            var nextFourChars = scanState.LookAhead(4);
            if (nextFourChars.IsEmpty)
            {
                throw new Exception("Wrong usage of \\u");
            }

            scanState.Advance(4);

            return UnEscapeFourDigitUnicodeChar(nextFourChars);
        }

        private static string UnEscapeEightDigitUnicodeChar(ReadOnlySpan<char> unicode)
        {
            try
            {
                var decoded = int.Parse(unicode, NumberStyles.HexNumber);
                return char.ConvertFromUtf32(decoded);
            }
            catch (Exception e)
            {
                throw new
                    ArgumentException($"Unicode character \"\\U{unicode.ToString()}\" is not valid. {e.Message}", e);
            }
        }

        private static char UnEscapeFourDigitUnicodeChar(ReadOnlySpan<char> unicode)
        {
            try
            {
                var decoded = ushort.Parse(unicode, NumberStyles.HexNumber);
                if (IsValidUnicodeScalarValue(decoded))
                {
                    return (char)decoded;
                }

                throw new ArgumentException("InValidUnicodeScalarValue.");
            }
            catch (Exception e)
            {
                throw new
                    ArgumentException($"Unicode character \"\\u{unicode.ToString()}\" is not valid. {e.Message}", e);
            }
        }

        public static bool IsValidUnicodeScalarValue(ushort c)
        {
            return c <= 0xD7FF || c >= 0xE000;
        }
    }
}