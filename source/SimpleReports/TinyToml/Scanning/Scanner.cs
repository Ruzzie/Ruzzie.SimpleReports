using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TinyToml.Scanning
{
    internal static class Scanner
    {
        public static Token ScanNext(scoped ref SourceScanState scanState)
        {
            var prevLine = scanState.Line;

            AdvanceWhiteSpaces(ref scanState);

            scanState.StartIndex = scanState.CurrentPos;

            if (scanState.IsAtEnd())
            {
                return new Token(TokenType.Eof, ReadOnlySpan<char>.Empty, scanState.Line, scanState.Column);
            }

            if (scanState.Line > prevLine && scanState.PeekNext() == '[')
            {
                //Scope is always reset to key. A newline is end of previous scope
                // and a new scope is always Key
                scanState.ScanScope = ScanScope.Key;
            }

            var current = scanState.Advance();

            if (scanState.ScanScope == ScanScope.Value)
            {
                switch (scanState.NumberScope)
                {
                    case NumberScope.None:

                        if (TryCheckAndSetNewNumberScope(ref scanState, current, out var digitsToken))
                        {
                            return digitsToken;
                        }

                        break;

                    case NumberScope.Dec:

                        if (NumberRule.IsExponent(current))
                        {
                            return CreateToken(TokenType.Exponent, scanState);
                        }

                        return current switch
                        {
                            '.' => CreateToken(TokenType.DecimalPoint,         scanState)
                          , '+' => CreateToken(TokenType.ExponentPositiveSign, scanState)
                          , '-' => CreateToken(TokenType.ExponentNegativeSign, scanState)
                          , _   => ScanNumberBaseDigits(ref scanState, current)
                        };

                    case NumberScope.DateTime:
                        return current switch
                        {
                            '.' => CreateToken(TokenType.DateTimeDot,   scanState)
                          , ':' => CreateToken(TokenType.DateTimeColon, scanState)
                          , '-' => CreateToken(TokenType.DateTimeDash,  scanState)
                          , 'T' => CreateToken(TokenType.DateTimeT,     scanState)
                          , 'Z' => CreateToken(TokenType.DateTimeZ,     scanState)
                          , _   => ScanNumberBaseDigits(ref scanState, current)
                        };

                    default:
                        return ScanNumberBaseDigits(ref scanState, current);
                }
            }

            switch (current)
            {
                case '[':
                    if (scanState.ScanScope == ScanScope.Key && scanState.PeekNext() == '[')
                    {
                        scanState.Advance();
                        return CreateToken(TokenType.DoubleLeftBracket, scanState);
                    }
                    else
                    {
                        return CreateToken(TokenType.LeftBracket, scanState);
                    }
                case ']':
                    if (scanState.ScanScope == ScanScope.Key && scanState.PeekNext() == ']')
                    {
                        scanState.Advance();
                        return CreateToken(TokenType.DoubleRightBracket, scanState);
                    }
                    else
                    {
                        return CreateToken(TokenType.RightBracket, scanState);
                    }
                case '{':
                    scanState.ScanScope = ScanScope.Key;
                    return CreateToken(TokenType.OpenBrace, scanState);
                case '}': return CreateToken(TokenType.CloseBrace, scanState);
                case '=':
                    scanState.ScanScope = ScanScope.Value;
                    return CreateToken(TokenType.Equal, scanState);
                case '+': return CreateToken(TokenType.Plus,  scanState);
                case '-': return CreateToken(TokenType.Minus, scanState);
                case '.': return CreateToken(TokenType.Dot,   scanState);
                case ',':
                    scanState.ScanScope = ScanScope.Value;
                    return CreateToken(TokenType.Comma, scanState);
                case '_':  return CreateToken(TokenType.Underscore, scanState);
                case '#':  return SingleLineComment(ref scanState);
                case '\'': return LiteralString(ref scanState);
                case '"':  return String(ref scanState);
                case 'i':
                    if (scanState.ScanScope == ScanScope.Value &&
                        MatchesString("nf", TokenType.Infinity, ref scanState, out var infToken))
                    {
                        return infToken;
                    }

                    goto default;
                case 'n':
                    if (scanState.ScanScope == ScanScope.Value &&
                        MatchesString("an", TokenType.Nan, ref scanState, out var nanToken))
                    {
                        return nanToken;
                    }

                    goto default;
                case 'f':
                    //false
                    if (scanState.ScanScope == ScanScope.Value &&
                        // ReSharper disable once StringLiteralTypo
                        MatchesString("alse", TokenType.False, ref scanState, out var falseToken))
                    {
                        return falseToken;
                    }

                    goto default;
                case 't':
                    //true
                    if (scanState.ScanScope == ScanScope.Value &&
                        MatchesString("rue", TokenType.True, ref scanState, out var trueToken))
                    {
                        return trueToken;
                    }

                    goto default;
                default:
                    //Scan until termination of key
                    while (scanState.HasNext()
                           && scanState.PeekNextUnsafe() != '='
                           && scanState.PeekNextUnsafe() != '.'
                           && scanState.PeekNextUnsafe() != ']'
                           && scanState.PeekNextUnsafe() != '{'
                           && !char.IsWhiteSpace(scanState.PeekNextUnsafe())
                          )
                    {
                        scanState.Advance();
                    }

                    var peekNext = scanState.PeekNext();
                    if (peekNext == '=' || peekNext == '.' || peekNext == ']' || peekNext == '{' ||
                        char.IsWhiteSpace(peekNext))
                    {
                        //Bare key
                        return CreateToken(TokenType.BareKey, scanState);
                    }

                    //Other literal
                    return CreateToken(TokenType.Literal, scanState);
            }
        }

        private static bool TryCheckAndSetNewNumberScope(scoped ref SourceScanState scanState
                                                       , char                       current
                                                       , out Token                  digitsToken)
        {
            //todo: refactor this method so it only does 1 thing
            // we probably can return the new numberScope and let the caller use that to set it in the sourceState
            //   this makes the control flow a bit more consistent
            if (current == '0')
            {
                var scope = TryParseHexOctIntPrefix(ref scanState, out var prefixToken);
                if (scope != NumberScope.None)
                {
                    scanState.BeginNumberScope(scope);
                    {
                        digitsToken = prefixToken;
                        return true;
                    }
                }

                //JUMP TO CHECK_DIGIT ; We know that 0 is a digit, no need for a double check
                // however we should jump to a method, so refactoring is needed
            }

            if (char.IsDigit(current))
            {
                //CHECK_DIGIT

                // DATE-TIME PART CHECK, and advance when we know the next char is a digit
                // note: we could replace this by peek, don't know what is better and faster.
                var nextChars = scanState.LookAhead(4);
                if (!nextChars.IsEmpty)
                {
                    if (char.IsDigit(nextChars[0])) //second year digit
                    {
                        scanState.Advance();

                        if (char.IsDigit(nextChars[1])) //third year digit
                        {
                            scanState.Advance();

                            if (char.IsDigit(nextChars[2])) //fourth year digit
                            {
                                scanState.Advance();

                                if (nextChars[3] == '-') //peek if date separator as fifth char
                                {
                                    scanState.BeginNumberScope(NumberScope.DateTime);
                                    {
                                        digitsToken = CreateToken(TokenType.DateTimeDigits, scanState);
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }

                //NOT a DATE-TIME
                //  Continue Decimal scope
                scanState.BeginNumberScope(NumberScope.Dec);
                {
                    digitsToken = ScanNumberBaseDigits(ref scanState, current);
                    return true;
                }
            }

            digitsToken = default;
            return false;
        }

        private static Token ScanNumberBaseDigits(scoped ref SourceScanState scanState, char current)
        {
            //todo: let's see if we can extract this if and Underscore token created out of this method?
            if (current == '_')
            {
                return CreateToken(TokenType.Underscore, scanState);
            }

            var currentNumberScope = scanState.NumberScope;
            while (scanState.HasNext() && NumberRule.IsBaseDigit(currentNumberScope, scanState.PeekNextUnsafe()))
            {
                scanState.Advance();
            }

            var nextNonDigitChar = scanState.PeekNext();

            if (!NumberRule.IsDigitsSeparator(currentNumberScope, nextNonDigitChar))
            {
                scanState.EndNumberScope();
            }

            return CreateToken(NumberRule.TokenTypeForNumberScope(currentNumberScope), scanState);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchesString(ReadOnlySpan<char>         valueToMatch
                                        , TokenType                  tokenType
                                        , scoped ref SourceScanState scanState
                                        , out        Token           tokenOnMatch)
        {
            var lenghtToCheck = valueToMatch.Length;

            if (lenghtToCheck == 0)
            {
                tokenOnMatch = default;
                return false;
            }

            var charsAhead = scanState.LookAhead(lenghtToCheck);

            if (charsAhead.IsEmpty)
            {
                tokenOnMatch = default;
                return false;
            }

            if (!charsAhead.StartsWith(valueToMatch, StringComparison.Ordinal))
            {
                tokenOnMatch = default;
                return false;
            }

            scanState.Advance(lenghtToCheck);

            if (scanState.IsAtEnd() || IsTerminationCharForLiteral(scanState.PeekNext()))
            {
                tokenOnMatch = CreateToken(tokenType, scanState);
                return true;
            }

            static bool IsTerminationCharForLiteral(char value)
            {
                return !char.IsLetterOrDigit(value) && value != '-' && value != '_';
            }

            tokenOnMatch = default;
            return false;
        }

        private static Token String(scoped ref SourceScanState scanState)
        {
            if (scanState.PeekNext() == '"')
            {
                scanState.Advance();
                if (scanState.PeekNext() == '"')
                {
                    scanState.Advance();
                    return TomlUnescapeStrings.MultiLineString(ref scanState);
                }

                return EmptyString(scanState);
            }

            //Quoted Key or string value
            return TomlUnescapeStrings.BasicString(ref scanState);
        }

        private static Token EmptyString(SourceScanState scanState)
        {
            scanState.Advance();
            return CreateToken(TokenType.EmptyString, scanState);
        }

        private static NumberScope TryParseHexOctIntPrefix(scoped ref SourceScanState scanState, out Token prefixToken)
        {
            var next = scanState.PeekNext();
            switch (next)
            {
                case 'x':
                    scanState.Advance();
                    prefixToken = CreateToken(TokenType.HexPrefix, scanState);
                    return NumberScope.Hex;
                case 'b':
                    scanState.Advance();
                    prefixToken = CreateToken(TokenType.BinPrefix, scanState);
                    return NumberScope.Bin;
                case 'o':
                    scanState.Advance();
                    prefixToken = CreateToken(TokenType.OctPrefix, scanState);
                    return NumberScope.Oct;
                default:
                    prefixToken = default;
                    return NumberScope.None;
            }
        }

        private static Token LiteralString(scoped ref SourceScanState scanState)
        {
            if (scanState.PeekNext() == '\'')
            {
                scanState.Advance();
                if (scanState.PeekNext() == '\'')
                {
                    scanState.Advance();
                    return MultilineLiteralString(ref scanState);
                }

                return CreateToken(TokenType.EmptyString, scanState);
            }

            while (scanState.HasNext() && scanState.PeekNextUnsafe() != '\'')
            {
                scanState.Advance();
            }

            if (scanState.IsAtEnd())
            {
                return ErrorToken(scanState, "Unterminated literal string");
            }

            scanState.Advance();

            //We want the string without quotes
            return CreateTokenTrimBeforeAndAfter(TokenType.LiteralString, scanState, 1);
        }

        private static Token MultilineLiteralString(scoped ref SourceScanState scanState)
        {
            //todo: see if we can restructure this method, so it is easier to follow the flow
            //  and see it we can reduce the number of lookaheads

            var lookAhead = scanState.LookAhead(2);
            switch (lookAhead[0])
            {
                case '\n':
                    scanState.StartIndex += 1;
                    break;
                case '\r' when lookAhead[1] == '\n':
                    scanState.StartIndex += 2;
                    break;
            }

            if (NextAreMultilineLiteralClosingQuotes(ref scanState))
            {
                //when the literal string (after opening quotes) contains for example '''''
                //  the last ''' are closing quotes                          -----------^^^
                //  the first '' are just literal quotes                    ----------^^
                //                                                                    01234

                //we know that the next 3 characters are quotes
                // so when the 4th char (index 3) is a quote, we are not in a closing sequence
                lookAhead = scanState.LookAhead(4);
                if (lookAhead.IsEmpty || lookAhead[3] != '\'')
                {
                    //the 4th character is not a quote,
                    // so we are a closing sequence at the start of the literal multi-line string
                    //   and we are therefore empty
                    scanState.Advance(3);
                    return CreateToken(TokenType.EmptyString, scanState);
                }

                //continue on
            }

            while (!scanState.IsAtEnd())
            {
                scanState.Advance();
                if (!NextAreMultilineLiteralClosingQuotes(ref scanState))
                {
                    continue;
                }

                scanState.Advance(3);
                if (scanState.PeekNext() != '\'')
                {
                    return CreateTokenTrimBeforeAndAfter(TokenType.MultiLineLiteralString, scanState, 3);
                }

                scanState.Advance();
                if (scanState.PeekNext() != '\'')
                {
                    return CreateTokenTrimBeforeAndAfter(TokenType.MultiLineLiteralString, scanState, 3);
                }

                scanState.Advance();
                if (scanState.PeekNext() != '\'')
                {
                    return CreateTokenTrimBeforeAndAfter(TokenType.MultiLineLiteralString, scanState, 3);
                }

                // 6 ' in a row is not allowed
                scanState.Advance();
                return ErrorToken(scanState, "Invalid usage of single quotes.");
            }

            return ErrorToken(scanState, "Unterminated literal string");
        }

        private static bool NextAreMultilineLiteralClosingQuotes(ref SourceScanState scanState)
        {
            var lookAhead = scanState.LookAhead(3);
            return lookAhead.Length == 3 && lookAhead[0] == '\'' && lookAhead[1] == '\'' && lookAhead[2] == '\'';
        }

        public static Token ErrorToken( /*in*/ SourceScanState state, ReadOnlySpan<char> errorMessage)
        {
            return new Token(TokenType.Error, errorMessage, state.Line, state.Column);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        enum Until
        {
            EOF   = 0
          , LF    = 1
          , CR_LF = 2
           ,
        }

        private static Token SingleLineComment(scoped ref SourceScanState scanState)
        {
            //Scan until newline
            AdvanceUntilBeforeEndOfLine(ref scanState);
            return CreateToken(TokenType.Comment, scanState);
        }

        private static Until AdvanceUntilBeforeEndOfLine(ref SourceScanState scanState)
        {
            while (scanState.HasNext() && scanState.PeekNextUnsafe() != '\n')
            {
                var nextTwoChars = scanState.LookAhead(2);
                if (!nextTwoChars.IsEmpty)
                {
                    switch (nextTwoChars[1])
                    {
                        case '\n' when nextTwoChars[0] == '\r':
                            return Until.CR_LF;
                        case '\n':
                            scanState.Advance();
                            return Until.LF;
                        case '\r':
                            scanState.Advance(); //slow down [?][\r] advance 1
                            break;
                        default:
                            scanState.Advance(2); //be eager, jump to the next 2 chars
                            break;
                    }
                }
                else
                {
                    //Only 1 char available
                    scanState.Advance();
                }
            }

            return scanState.PeekNext() == '\n' ? Until.LF : Until.EOF;
        }

        public static Token CreateToken(TokenType tokenType, /*in*/ SourceScanState state)
        {
            return new Token(tokenType
                           , state.SourceDataSpan[state.StartIndex .. state.CurrentPos]
                           , state.Line
                           , state.Column
                            );
        }

        public static Token CreateTokenTrimBeforeAndAfter(TokenType              tokenType
                                                        , /*in*/ SourceScanState state
                                                        , int                    trimCount)
        {
            return new Token(tokenType
                           , state.SourceDataSpan[(state.StartIndex + trimCount) ..(state.CurrentPos - trimCount)]
                           , state.Line
                           , state.Column
                            );
        }

        public static Token CreateStringToken(TokenType tokenType
                                             ,
                                              /*in*/ SourceScanState state
                                            , ReadOnlySpan<char>     unescapedString)
        {
            return new Token(tokenType
                           , unescapedString
                            ,
                             // state.SourceDataSpan[state.StartIndex .. state.CurrentPos],
                             state.Line
                           , state.Column
                            );
        }

        internal static void AdvanceWhiteSpaces(ref SourceScanState scanState)
        {
            while (true)
            {
                var c = scanState.PeekNext();
                switch (c)
                {
                    case '\r':
                    case ' ':
                    case '\t':
                    case '\v':
                    case '\f':
                        scanState.Column++;
                        scanState.Advance();
                        break;
                    case '\n':
                        scanState.Line++;
                        scanState.Column = 1;
                        scanState.Advance();
                        break;
                    default:
                        return;
                }
            }
        }
    }

    internal enum NumberScope
    {
        None
      , Bin
      , Oct
      , Dec
      , Hex
      , DateTime
      , // must be last element
    }

    internal enum ScanScope
    {
        Key
      , Value
       ,
    }
}