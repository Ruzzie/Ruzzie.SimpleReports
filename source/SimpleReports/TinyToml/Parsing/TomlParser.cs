using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using TinyToml.Scanning;
using TinyToml.Types;

namespace TinyToml.Parsing
{
    internal static class TomlParser
    {
        private static readonly ParseRule[] AllRules = new ParseRule[(int) TokenType.Eof + 1];

        private static unsafe ParseRule Rule(delegate*<bool, ref ParseState, void> prefix
                                           , delegate*<bool, ref ParseState, void> infix
                                           , Precedence                            precedence)
        {
            return new ParseRule(prefix, infix, precedence);
        }

        // ReSharper disable once InconsistentNaming
        private static readonly unsafe delegate*<bool, ref ParseState, void> NONE_PTR = &NONE;
        // ReSharper disable once InconsistentNaming
        private static void NONE(bool canAssign, ref ParseState parseState)
        {
            throw new NotImplementedException($"{parseState.Previous.TokenType} {parseState.Current.TokenType}");
        }

        static unsafe TomlParser()
        {
            AllRules[(int)TokenType.None]                   = Rule(NONE_PTR,     NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.LeftBracket]            = Rule(&ArrayValues, NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.RightBracket]           = Rule(NONE_PTR,     NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.Comma]                  = Rule(NONE_PTR,     NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.DoubleLeftBracket]      = Rule(NONE_PTR,     NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.DoubleRightBracket]     = Rule(NONE_PTR,     NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.Dot]                    = Rule(NONE_PTR,     &Dot,     Precedence.PREC_ASSIGNMENT);
            AllRules[(int)TokenType.Equal]                  = Rule(NONE_PTR,     NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.Literal]                = Rule(NONE_PTR,     NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.OpenBrace]              = Rule(&InlineTable, NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.CloseBrace]             = Rule(NONE_PTR,     NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.None]                   = Rule(NONE_PTR,     NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.Comment]                = Rule(NONE_PTR,     NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.Plus]                   = Rule(&Number,      NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.Minus]                  = Rule(&Number,      NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.Infinity]               = Rule(&Number,      NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.Nan]                    = Rule(&Number,      NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.BinPrefix]              = Rule(&Number,      NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.OctPrefix]              = Rule(&Number,      NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.HexPrefix]              = Rule(&Number,      NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.DecDigits]              = Rule(&Number,      NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.DateTimeDigits]         = Rule(&DateTime,    NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.Underscore]             = Rule(NONE_PTR,     NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.MultiLineString]        = Rule(&String,      NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.LiteralString]          = Rule(&String,      NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.MultiLineLiteralString] = Rule(&String,      NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.EmptyString]            = Rule(&String,      NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.BasicString]            = Rule(&String,      NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.True]                   = Rule(&Literal,     NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.False]                  = Rule(&Literal,     NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.Error]                  = Rule(NONE_PTR,     NONE_PTR, Precedence.PREC_NONE);
            AllRules[(int)TokenType.Eof]                    = Rule(NONE_PTR,     NONE_PTR, Precedence.PREC_NONE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ParseRule GetParseRule(TokenType tokenType)
        {
            return AllRules[(int) tokenType];
        }

        private static TomlInteger ParseTomlInteger(string key, ReadOnlySpan<char> text, int sign, int radix)
        {
            //Todo: TryParse, error handling
            var number = Convert.ToInt64(text.ToString(), radix) * sign;
            return new TomlInteger(key, number);
        }

        private static TomlFloat ParseTomlFloat(string key, ReadOnlySpan<char> text, int sign)
        {
            var number = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture) * sign;
            return new TomlFloat(key, number);
        }

        private static void DateTime(bool canAssign, ref ParseState parseState)
        {
            var       countDateParts = 0;
            Span<int> dateParts      = stackalloc[] {1, 1, 1, 0, 0, 0, 0, 0, 0, 0}; //set to valid DateTime.MinValue
            var       hasNextPart    = true;
            var       isOffset       = false;

            while (hasNextPart)
            {
                dateParts[countDateParts++] = int.Parse(parseState.Previous.Text);

                switch (countDateParts)
                {
                    case 1:
                    case 2:
                        if (ForwardOnMatch(TokenType.DateTimeDash, ref parseState))
                        {
                            EatOrError(TokenType.DateTimeDigits, ref parseState, "Expect datetime digits after '-'.");
                            break;
                        }

                        hasNextPart = false;
                        break;
                    case 3:
                        if (ForwardOnMatch(TokenType.DateTimeT, ref parseState))
                        {
                            EatOrError(TokenType.DateTimeDigits, ref parseState, "Expect datetime digits after 'T'.");
                            break;
                        }
                        else if (ForwardOnMatch(TokenType.DateTimeDigits, ref parseState))
                        {
                            break;
                        }

                        hasNextPart = false;
                        break;
                    case 4:
                    case 5:
                        if (ForwardOnMatch(TokenType.DateTimeColon, ref parseState))
                        {
                            EatOrError(TokenType.DateTimeDigits, ref parseState, "Expect datetime digits after ':'.");
                            break;
                        }

                        hasNextPart = false;
                        break;
                    case 6:
                        if (ForwardOnMatch(TokenType.DateTimeDot, ref parseState))
                        {
                            EatOrError(TokenType.DateTimeDigits, ref parseState, "Expect datetime digits after '.'.");
                            break;
                        }

                        if (ForwardOnMatch(TokenType.DateTimeZ, ref parseState))
                        {
                            hasNextPart = false;
                            isOffset    = true;
                            break;
                        }

                        if (ForwardOnMatch(TokenType.DateTimeDash, ref parseState))
                        {
                            countDateParts++;
                            EatOrError(TokenType.DateTimeDigits, ref parseState, "Expect datetime digits after '-'.");
                            isOffset = true;
                            break;
                        }

                        hasNextPart = false;
                        break;
                    case 7:
                        if (ForwardOnMatch(TokenType.DateTimeZ, ref parseState))
                        {
                            hasNextPart = false;
                            isOffset    = true;
                            break;
                        }

                        if (ForwardOnMatch(TokenType.DateTimeDash, ref parseState))
                        {
                            EatOrError(TokenType.DateTimeDigits, ref parseState, "Expect datetime digits after '-'.");
                            isOffset = true;
                            break;
                        }

                        hasNextPart = false;
                        break;
                    case 8:
                        if (ForwardOnMatch(TokenType.DateTimeColon, ref parseState))
                        {
                            EatOrError(TokenType.DateTimeDigits, ref parseState, "Expect datetime digits after ':'.");
                            break;
                        }

                        hasNextPart = false;
                        break;
                    default:
                        hasNextPart = false;
                        break;
                }
            }

            // Truncate millis part
            while (dateParts[6] > 999)
            {
                dateParts[6] /= 10;
            }

            DateTimeOffset date;
            if (isOffset)
            {
                date = new DateTimeOffset(new DateTime(dateParts[0], dateParts[1], dateParts[2], dateParts[3],
                                                       dateParts[4], dateParts[5], dateParts[6]),
                                          new TimeSpan(dateParts[7], dateParts[8], dateParts[9]));
            }
            else
            {
                date = new DateTime(dateParts[0], dateParts[1], dateParts[2], dateParts[3],
                                    dateParts[4], dateParts[5], dateParts[6], DateTimeKind.Local);
            }

            var tomlValue = new TomlDateTimeOffset(parseState.CurrentKey, date);
            AddValueToCurrentScope(ref parseState, tomlValue);
        }

        private static void Number(bool canAssign, ref ParseState parseState)
        {
            var radix = 10;
            var sign  = 1;

            switch (parseState.Previous.TokenType)
            {
                case TokenType.Infinity:
                    CreateAndAddInfinityFloat(ref parseState, sign);
                    break;
                case TokenType.Nan:
                    CreateAndAddNaNFloat(ref parseState);
                    break;
                case TokenType.Plus:
                    if (!CreateAndAddWhenSpecialFloats(ref parseState, sign))
                    {
                        EatOrError(TokenType.DecDigits, ref parseState, "Expect digits after '+'.");
                        CollectAll(TokenType.DecDigits, ref parseState, radix, sign);
                    }

                    break;
                case TokenType.Minus:
                    sign = -1;
                    if (!CreateAndAddWhenSpecialFloats(ref parseState, sign))
                    {
                        EatOrError(TokenType.DecDigits, ref parseState, "Expect digits after '-'.");
                        CollectAll(TokenType.DecDigits, ref parseState, radix, sign);
                    }

                    break;
                case TokenType.DecDigits:
                    CollectAll(TokenType.DecDigits, ref parseState, radix, sign);
                    break;
                case TokenType.BinPrefix:
                    radix = 2;
                    EatOrError(TokenType.BinDigits, ref parseState, "Expect BinDigits after '0b'.");
                    CollectAll(TokenType.BinDigits, ref parseState, radix, sign);
                    break;
                case TokenType.OctPrefix:
                    radix = 8;
                    EatOrError(TokenType.OctDigits, ref parseState, "Expect OctDigits after '0o'.");
                    CollectAll(TokenType.OctDigits, ref parseState, radix, sign);
                    break;
                case TokenType.HexPrefix:
                    radix = 16;
                    EatOrError(TokenType.HexDigits, ref parseState, "Expect HexDigits after '0x'.");
                    CollectAll(TokenType.HexDigits, ref parseState, radix, sign);
                    break;
                default:
                    ErrorAtPrevious(ref parseState, "Expected Number.");
                    break;
            }
        }

        private static bool CreateAndAddWhenSpecialFloats(ref ParseState parseState, int sign)
        {
            if (ForwardOnMatch(TokenType.Infinity, ref parseState))
            {
                CreateAndAddInfinityFloat(ref parseState, sign);
                return true;
            }

            if (ForwardOnMatch(TokenType.Nan, ref parseState))
            {
                CreateAndAddNaNFloat(ref parseState);
                return true;
            }

            return false;
        }

        private static void CreateAndAddNaNFloat(ref ParseState parseState)
        {
            var toml = new TomlFloat(parseState.CurrentKey, double.NaN);
            AddValueToCurrentScope(ref parseState, toml);
        }

        private static void CreateAndAddInfinityFloat(ref ParseState parseState, int sign)
        {
            var number = CreateInfNumber(sign);
            var toml   = new TomlFloat(parseState.CurrentKey, number);
            AddValueToCurrentScope(ref parseState, toml);
        }

        private static double CreateInfNumber(int sign)
        {
            return sign > 0 ? double.PositiveInfinity : double.NegativeInfinity;
        }


        //The maximum number of digits is 64 in binary, everything else is smaller
        //note: possible Improvement, dependant on number type we could vary the max digits eg smaller
        // ReSharper disable once InconsistentNaming
        private const int MAX_DIGITS = 64;
        // ReSharper disable once InconsistentNaming
        private static readonly string TOO_MANY_DIGIT_ERR_MSG = FormattableString.Invariant($"Could not parse number, too many digits the maximum number of digits is {MAX_DIGITS}.");

        private static void CollectAll(TokenType digitsTokenType, ref ParseState parseState, int radix, int sign)
        {
            //TODO: Comment this method and clean it up, a bit too long, some duplication, make continue; break; flow more readable
            //

            static void AssertMaxDigits(int currDigitCount, ref ParseState pState)
            {
                if (currDigitCount + pState.Previous.Text.Length > MAX_DIGITS)
                {
                    ErrorAtPrevious(ref pState, TOO_MANY_DIGIT_ERR_MSG);
                }
            }

            Span<char> digits = stackalloc char[MAX_DIGITS];

            var digitCount = parseState.Previous.Text.Length;

            if (digitCount > MAX_DIGITS)
            {
                //ERROR
                ErrorAtPrevious(ref parseState,TOO_MANY_DIGIT_ERR_MSG);
            }

            parseState.Previous.Text.CopyTo(digits);

            while (ForwardOnMatch(TokenType.Underscore, ref parseState))
            {
                EatOrError(digitsTokenType, ref parseState, $"Expect {digitsTokenType} after '_'.");

                AssertMaxDigits(digitCount, ref parseState);

                parseState.Previous.Text.CopyTo(digits.Slice(digitCount));
                digitCount += parseState.Previous.Text.Length;
            }

            if (digitsTokenType != TokenType.DecDigits)
            {
                var tomlInteger = ParseTomlInteger(parseState.CurrentKey,
                                                   digits.Slice(0, digitCount),
                                                   sign,
                                                   radix);
                AddValueToCurrentScope(ref parseState, tomlInteger);
            }
            else if (parseState.Current.TokenType != TokenType.DecimalPoint &&
                     parseState.Current.TokenType != TokenType.Exponent)
            {
                var tomlInteger = ParseTomlInteger(parseState.CurrentKey,
                                                   digits.Slice(0, digitCount),
                                                   sign,
                                                   radix);
                AddValueToCurrentScope(ref parseState, tomlInteger);
            }
            else
            {
                var hasDot      = false;
                var hasExponent = false;
                while (true) // TODO: we now when we need to break, so let's use a variable instead of an infinite loop..?!
                {
                    if (ForwardOnMatch(TokenType.DecimalPoint, ref parseState))
                    {
                        if (hasDot)
                        {
                            ErrorAtCurrent(ref parseState, $"Floats cannot have more than 1 '.'.");
                        }

                        hasDot = true;

                        AssertMaxDigits(digitCount, ref parseState);

                        parseState.Previous.Text.CopyTo(digits.Slice(digitCount));
                        digitCount += parseState.Previous.Text.Length;

                        EatOrError(TokenType.DecDigits, ref parseState, "Expect digits after '.'.");

                        AssertMaxDigits(digitCount, ref parseState);

                        parseState.Previous.Text.CopyTo(digits.Slice(digitCount));
                        digitCount += parseState.Previous.Text.Length;
                        continue;
                    }

                    if (ForwardOnMatch(TokenType.Underscore, ref parseState))
                    {
                        EatOrError(TokenType.DecDigits, ref parseState, "Expect digits after '_'.");

                        AssertMaxDigits(digitCount, ref parseState);

                        parseState.Previous.Text.CopyTo(digits.Slice(digitCount));
                        digitCount += parseState.Previous.Text.Length;
                        continue;
                    }

                    if (ForwardOnMatch(TokenType.Exponent, ref parseState))
                    {
                        if (hasExponent)
                        {
                            ErrorAtCurrent(ref parseState, "Floats cannot have more than one Exponent.");
                        }

                        hasExponent = true;

                        AssertMaxDigits(digitCount, ref parseState);

                        parseState.Previous.Text.CopyTo(digits.Slice(digitCount));
                        digitCount += parseState.Previous.Text.Length;

                        if (ForwardOnMatch(TokenType.ExponentNegativeSign, ref parseState))
                        {
                            AssertMaxDigits(digitCount, ref parseState);
                            parseState.Previous.Text.CopyTo(digits.Slice(digitCount));
                            digitCount += parseState.Previous.Text.Length;
                            EatOrError(TokenType.DecDigits, ref parseState, "Expect digits after '-'.");
                            AssertMaxDigits(digitCount, ref parseState);
                            parseState.Previous.Text.CopyTo(digits.Slice(digitCount));
                            digitCount += parseState.Previous.Text.Length;
                            continue;
                        }

                        if (ForwardOnMatch(TokenType.ExponentPositiveSign, ref parseState))
                        {
                            AssertMaxDigits(digitCount, ref parseState);
                            parseState.Previous.Text.CopyTo(digits.Slice(digitCount));
                            digitCount += parseState.Previous.Text.Length;
                            EatOrError(TokenType.DecDigits, ref parseState, "Expect digits after '+'.");
                            AssertMaxDigits(digitCount, ref parseState);
                            parseState.Previous.Text.CopyTo(digits.Slice(digitCount));
                            digitCount += parseState.Previous.Text.Length;
                            continue;
                        }

                        AssertMaxDigits(digitCount, ref parseState);
                        EatOrError(TokenType.DecDigits, ref parseState, "Expect digits, '+' or '-' after 'e'.");
                        parseState.Previous.Text.CopyTo(digits.Slice(digitCount));
                        digitCount += parseState.Previous.Text.Length;
                        continue;
                    }

                    break;
                }

                var tomlFloat = ParseTomlFloat(parseState.CurrentKey, digits.Slice(0, digitCount), sign);
                AddValueToCurrentScope(ref parseState, tomlFloat);
            }
        }

        private static void AddValueToCurrentScope<T>(ref ParseState parseState, in T tomlValue) where T : ITomlValue
        {
            if (parseState.CurrentScope == ParseState.Scope.Array)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (parseState.CurrentArray != default)
                {
                    parseState.CurrentArray.AddValue(tomlValue);
                }
                else
                {
                    ErrorAtPrevious(ref parseState,
                                    $"Unexpected, {parseState.CurrentKey} current Array scope is not set.");
                }
            }
            else
            {
                parseState.CurrentTable.AddValue(tomlValue);
            }
        }

        private static void Dot(bool canAssign, ref ParseState parseState)
        {
            throw new NotImplementedException("dot");
        }

        private static void InlineTable(bool canAssign, ref ParseState parseState)
        {
            //Open brace means it is an inline table
            var previousTableScope = parseState.CurrentTable;
            var tableName = parseState.CurrentScope == ParseState.Scope.Array
                ? $"{parseState.CurrentArray.Key}-{parseState.CurrentArray.Count}"
                : parseState.CurrentKey;

            var tomlTable = new TomlTable(tableName);
            AddValueToCurrentScope(ref parseState, tomlTable);

            //Set the current Table Scope
            parseState.BeginTableScope(tomlTable);

            do
            {
                //Key or dotted key: { myKey = 1, other = 2 }
                //                     ^^^^^
                //                     (Key EQUALS (Expr) COMMA*)
                KeyDeclaration(ref parseState);

                var keyName = parseState.Previous.Text.ToString();

                //Scope the current key
                parseState.CurrentKey = keyName;

                EatOrError(TokenType.Equal, ref parseState, "Expect '=' after key.");

                //Parse value expression
                Expr(ref parseState);
            } while (ForwardOnMatch(TokenType.Comma, ref parseState)); //multiple key values : { myKey = 1, other = 2 }
            //|-------------------------------------------------------------------------------------------^

            EatOrError(TokenType.CloseBrace, ref parseState);

            parseState.EndTableScope(previousTableScope);
        }

        private static void ArrayValues(bool canAssign, ref ParseState parseState)
        {
            var arrayName = parseState.CurrentScope == ParseState.Scope.Array
                ? $"{parseState.CurrentArray.Key}-{parseState.CurrentArray.Count}"
                : parseState.CurrentKey;

            var tomlArray = new TomlArray(arrayName);

            AddValueToCurrentScope(ref parseState, tomlArray);

            var previousArray = parseState.CurrentArray;
            parseState.BeginArrayScope(tomlArray);

            bool atEndOfArray;
            do
            {
                //Parse the Current Element Value
                Expr(ref parseState);

                // check if it is the TrailingComma
                //  and ignore comments
                if (ForwardOnMatch(TokenType.Comma, ref parseState))
                {
                    if (ForwardOnMatch(TokenType.Comment, ref parseState) ||
                        parseState.Current.TokenType == TokenType.RightBracket)
                    {
                        // [.... 2, #hello
                        // [.... 2, ]
                        // [...,
                        //      2, #hello
                        //]
                        atEndOfArray = true;
                    }
                    else
                    {
                        atEndOfArray = false;
                    }
                }
                else
                {
                    atEndOfArray = true;
                }
            } while (!atEndOfArray); //Next Element

            EatOrError(TokenType.RightBracket, ref parseState, "Expect ']' at end of Array.");

            parseState.EndArrayScope(previousArray);
        }

        private static void Literal(bool canAssign, ref ParseState parseState)
        {
            switch (parseState.Previous.TokenType)
            {
                case TokenType.True:
                    AddValueToCurrentScope(ref parseState, new TomlBoolean(parseState.CurrentKey, true));
                    break;
                case TokenType.False:
                    AddValueToCurrentScope(ref parseState, new TomlBoolean(parseState.CurrentKey, false));
                    break;
                default:
                    ErrorAtPrevious(ref parseState, $"Expected literal type but was {parseState.Previous.TokenType}");
                    return;
            }
        }

        private static void String(bool canAssign, ref ParseState parseState)
        {
            switch (parseState.Previous.TokenType)
            {
                case TokenType.EmptyString:
                    AddValueToCurrentScope(ref parseState, new TomlString(parseState.CurrentKey, string.Empty));
                    break;
                case TokenType.MultiLineString:
                case TokenType.BasicString:
                    var stringValue =
                        new TomlString(parseState.CurrentKey,
                                       parseState.Previous.Text.ToString());
                    AddValueToCurrentScope(ref parseState, stringValue);
                    break;
                case TokenType.LiteralString:
                    var literalStringValue =
                        new TomlString(parseState.CurrentKey, parseState.Previous.Text.ToString());
                    AddValueToCurrentScope(ref parseState, literalStringValue);
                    break;
                case TokenType.MultiLineLiteralString:
                    var multiLineLiteralStringValue =
                        new TomlString(parseState.CurrentKey,
                                       parseState.Previous.Text.ToString());
                    AddValueToCurrentScope(ref parseState, multiLineLiteralStringValue);
                    break;
                default:
                    ErrorAtPrevious(ref parseState, $"Expected string type but was {parseState.Previous.TokenType}");
                    return;
            }
        }

        public static TomlDoc Parse(string toml)
        {
            var scanState   = new SourceScanState(toml);
            var tomlDocRoot = new TomlTable("--root--");

            var parseState = new ParseState(ref scanState, tomlDocRoot);

            //Document is a table
            parseState.BeginTableScope(tomlDocRoot);

            //Initialize
            NextToken(ref parseState);

            while (!ForwardOnMatch(TokenType.Eof, ref parseState))
            {
                //todo think of good error reporting strategy
                if (parseState.Current.TokenType == TokenType.Error)
                {
                    throw new Exception($"Error: {parseState.Current.Text.ToString()}");
                }

                // Skip all Comment tokens
                if (!ForwardOnMatch(TokenType.Comment, ref parseState))
                {
                    //Declarations
                    Declaration(ref parseState);
                }
            }

            parseState.EndTableScope(tomlDocRoot);

            return new TomlDoc(tomlDocRoot);
        }

        private static bool IsValidKeyNameType(in Token token)
        {
            return token.TokenType == TokenType.BareKey ||
                   token.TokenType == TokenType.LiteralString ||
                   token.TokenType == TokenType.BasicString;
        }

        private static void KeyDeclaration(ref ParseState parseState)
        {
            if (!IsValidKeyNameType(parseState.Current))
            {
                ErrorAtCurrent(ref parseState,
                               $"Key or table name expected. But was '{parseState.Current.TokenType}'.");
                return;
            }

            parseState.CurrentKey = parseState.Current.Text.ToString();

            NextToken(ref parseState);

            while (ForwardOnMatch(TokenType.Dot, ref parseState)
                   && IsValidKeyNameType(parseState.Current))
            {
                var nextTableName = parseState.Current.Text.ToString();

                if (parseState.CurrentTable.TryGetValue(parseState.CurrentKey, out var tomlValue))
                {
                    switch (tomlValue)
                    {
                        case TomlTable nextTable:
                            parseState.CurrentTable = nextTable;
                            break;
                        case TomlArray {CurrentElement: TomlTable currentElement}:
                            //when the type is an array the behavior should be: the sub-table should be added to the current element
                            parseState.CurrentTable = currentElement;
                            break;
                        case TomlArray array:
                            ErrorAtCurrent(ref parseState,
                                           $"The current element of the Array {array.Key} is not a table, '{parseState.CurrentKey}' was already assigned a value of type {array.CurrentElement.TomlType}. It cannot be used as a Table.");
                            return;
                        default:

                            ErrorAtCurrent(ref parseState,
                                           $"Key '{parseState.CurrentKey}' was already assigned a value of type {tomlValue.TomlType}. It cannot be redefined as a Table.");
                            return;
                    }
                }
                else
                {
                    var nextTable = new TomlTable(parseState.CurrentKey);
                    parseState.CurrentTable.AddValue(nextTable);
                    parseState.CurrentTable = nextTable;
                }

                parseState.CurrentKey = nextTableName;

                NextToken(ref parseState);
            }
        }

        private static void Declaration(ref ParseState parseState)
        {
            if (IsValidKeyNameType(parseState.Current))
            {
                //Save current scope
                var previousTableScope = parseState.CurrentTable;

                KeyDeclaration(ref parseState);
                EatOrError(TokenType.Equal, ref parseState, "Expect '=' after key.");
                Expr(ref parseState);

                //Restore scope
                parseState.CurrentTable = previousTableScope;
            }
            else if (ForwardOnMatch(TokenType.LeftBracket, ref parseState))
            {
                //Table : [name]
                //Reset scope to root
                parseState.CurrentTable = parseState.DocRoot;

                KeyDeclaration(ref parseState);

                var       tableName = parseState.Previous.Text.ToString();
                TomlTable tomlTable;

                if (parseState.CurrentTable.TryGetValue(tableName, out var tomlValue))
                {
                    if (!tomlValue.TryReadTomlTable(out tomlTable))
                    {
                        ErrorAtPrevious(ref parseState,
                                        $"Cannot redefine '{tableName}' as a Table. It was already declared as '{tomlValue.TomlType}'.");
                        return;
                    }
                }
                else
                {
                    tomlTable = new TomlTable(tableName);
                    parseState.CurrentTable.AddValue(tomlTable);
                }

                parseState.CurrentTable = tomlTable;
                parseState.CurrentKey   = tableName;

                EatOrError(TokenType.RightBracket, ref parseState, "Expected ']' after table name.");
            }
            else if (ForwardOnMatch(TokenType.DoubleLeftBracket, ref parseState))
            {
                //Array (of tables) : [[
                //Reset scope to root

                parseState.CurrentTable = parseState.DocRoot;

                KeyDeclaration(ref parseState);

                var       arrayName = parseState.Previous.Text.ToString();
                TomlArray array;

                if (parseState.CurrentTable.TryGetValue(arrayName, out var tomlValue))
                {
                    if (!tomlValue.TryReadTomlArray(out array))
                    {
                        ErrorAtPrevious(ref parseState,
                                        $"Cannot redefine '{arrayName}' as an Array. It was already declared as '{tomlValue.TomlType}'.");
                        return;
                    }
                }
                else
                {
                    array = new TomlArray(arrayName);
                    parseState.CurrentTable.AddValue(array);
                }

                parseState.CurrentKey = arrayName;

                var tableEntry = new TomlTable($"{arrayName}-{array.Count}"); // new element
                array.AddValue(tableEntry);
                parseState.CurrentTable = tableEntry;

                EatOrError(TokenType.DoubleRightBracket, ref parseState, "Expected ']]' after table array name.");
            }
            else
            {
                if (parseState.ScanState.ScanScope == ScanScope.Value)
                {
                    ErrorAtCurrent(ref parseState,
                                   $"Unknown value type in {parseState.Current.TokenType} token");
                }
                else
                {
                    ErrorAtCurrent(ref parseState,
                                   $"Panic in {nameof(Declaration)}. Unexpected token {parseState.Current.TokenType}");
                }
            }
        }

        private static void Expr(ref ParseState parseState)
        {
            ParsePrecedence(Precedence.PREC_ASSIGNMENT, ref parseState);
        }

        private static unsafe void ParsePrecedence(Precedence precedence, ref ParseState parseState)
        {
            NextToken(ref parseState);

            // PREFIX
            var prefixRule = GetParseRule(parseState.Previous.TokenType).Prefix;

#pragma warning disable 8909
            // We know the Prefix rule is instantiated with the same static Pointer: NONE_PTR when the NONE method
            //  needs to be references.
            if (prefixRule == NONE_PTR)
            {
                ErrorAtPrevious(ref parseState, "Expect expression");
                return;
            }
#pragma warning restore 8909

            bool canAssign = precedence <= Precedence.PREC_ASSIGNMENT;
            prefixRule(canAssign, ref parseState);

            //PRECEDENCE
            while (precedence <= GetParseRule(parseState.Current.TokenType).Precedence)
            {
                NextToken(ref parseState);
                //INFIX
                var infixRule = GetParseRule(parseState.Previous.TokenType).Infix;

                infixRule(canAssign, ref parseState);
            }

            if (canAssign && ForwardOnMatch(TokenType.Equal, ref parseState))
            {
                ErrorAtPrevious(ref parseState, "Invalid assignment target.");
            }
        }

        private static void ErrorAtPrevious(ref ParseState parseState, string errMsg)
        {
            //parseState.LastError = new ParseError {Message = errMsg, OnToken = parseState.Previous};
            throw new
                Exception(
                          $"Error: {errMsg} at '{parseState.Previous.Text.ToString()}', Line: {parseState.Previous.Line}, Column: {parseState.Previous.Column}");
        }

        private static void ErrorAtCurrent(ref ParseState parseState, string errMsg)
        {
            //parseState.LastError = new ParseError {Message = errMsg, OnToken = parseState.Current};
            throw new
                Exception(
                          $"Error: {errMsg} at '{parseState.Current.Text.ToString()}', Line: {parseState.Current.Line}, Column: {parseState.Current.Column}");
        }

        /// advance
        private static void NextToken(ref ParseState parseState)
        {
            parseState.Previous = parseState.Current;
            parseState.Current  = Scanner.ScanNext(ref parseState.ScanState);

            if (parseState.Current.TokenType == TokenType.Error)
            {
                ErrorAtCurrent(ref parseState, "Syntax error");
            }
        }

        /// consume : if current = tokenType => NextToken else Error
        private static void EatOrError(TokenType tokenType, ref ParseState parseState, string errorMsg = "")
        {
            if (parseState.Current.TokenType == tokenType)
            {
                NextToken(ref parseState);
            }
            else
            {
                ErrorAtCurrent(ref parseState, errorMsg);
            }
        }

        /// match : if current = tokenType => NextToken return true else return false
        private static bool ForwardOnMatch(TokenType tokenType, ref ParseState parseState)
        {
            var matchCurrent = parseState.Current.TokenType == tokenType;

            if (matchCurrent)
            {
                NextToken(ref parseState);
            }

            return matchCurrent;
        }
    }
}