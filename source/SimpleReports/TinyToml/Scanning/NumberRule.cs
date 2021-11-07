using System;

namespace TinyToml.Scanning
{
    internal static class NumberRule
    {
        public static bool IsBaseDigit(NumberScope scope, char value)
        {
            switch (scope)
            {
                case NumberScope.Dec:
                case NumberScope.DateTime:
                    return char.IsDigit(value);
                case NumberScope.Hex:
                    return Uri.IsHexDigit(value);
                case NumberScope.Bin:
                    return IsBinDigit(value);
                case NumberScope.Oct:
                    return IsOctDigit(value);
                // ReSharper disable once RedundantCaseLabel
                case NumberScope.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope)
                                                        , scope
                                                        , "no IsBaseDigit function for NumberScope found");
            }
        }

        public static bool IsDigitsSeparator(NumberScope scope, char value)
        {
            switch (scope)
            {
                case NumberScope.Dec:
                    return IsDecSeparator(value);
                case NumberScope.Hex:
                case NumberScope.Bin:
                case NumberScope.Oct:
                    return IsIntSeparator(value);
                case NumberScope.DateTime:
                    return IsDateTimeSeparator(value);
                // ReSharper disable once RedundantCaseLabel
                case NumberScope.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope)
                                                        , scope
                                                        , "no IsDigitsSeparator function for NumberScope found");
            }
        }

        public static TokenType TokenTypeForNumberScope(NumberScope scope)
        {
            switch (scope)
            {
                case NumberScope.Dec:
                    return TokenType.DecDigits;
                case NumberScope.Hex:
                    return TokenType.HexDigits;
                case NumberScope.Bin:
                    return TokenType.BinDigits;
                case NumberScope.Oct:
                    return TokenType.OctDigits;
                case NumberScope.DateTime:
                    return TokenType.DateTimeDigits;
                case NumberScope.None:
                    return TokenType.None;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), scope, "no TokenType for NumberScope found");
            }
        }

        public static bool IsIntSeparator(char value)
        {
            return value == '_';
        }

        public static bool IsDecSeparator(char value)
        {
            return value == '_' || IsExponent(value) || value == '.';
        }

        public static bool IsDateTimeSeparator(char value)
        {
            return value == '-' || value == ':' || value == '.' || value == 'T' || value == 'Z' || value == ' ';
        }

        public static bool IsBinDigit(char value)
        {
            return value == '0' || value == '1';
        }

        public static bool IsOctDigit(char character)
        {
            return (uint)(character - '0') <= '7' - '0';
        }

        public static bool IsExponent(char character)
        {
            return character == 'e' || character == 'E';
        }
    }
}