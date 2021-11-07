namespace TinyToml.Scanning
{
    public enum TokenType
    {
        None,

        Error,

        /// .
        Dot,

        /// =
        Equal,

        /// ,
        Comma,

        /// [
        LeftBracket,

        /// ]
        RightBracket,

        /// {
        OpenBrace,

        /// }
        CloseBrace,

        Plus,
        Minus,

        Underscore,

        Infinity, // inf
        Nan,      // nan

        HexPrefix, // 0x
        OctPrefix, // 0o
        BinPrefix, // 0b

        DecimalPoint,          // .

        ExponentPositiveSign,          // +
        ExponentNegativeSign,          // -
        Exponent,                      // e or E

        DecDigits, // 0-9
        BinDigits, // 0-1
        OctDigits, // 0-7
        HexDigits, // 0-A

        DateTimeDigits,
        DateTimeDash,
        DateTimeColon,
        DateTimeT,
        DateTimeZ,
        DateTimeDot,

        BareKey,

        /// There are four ways to express strings: basic, multi-line basic, literal, and multi-line literal.
        /// All strings must contain only valid UTF-8 characters.
        ///     - Basic strings are surrounded by quotation marks (").
        ///       Any Unicode character may be used except those that must be escaped:
        ///        quotation mark, backslash, and the control characters other than tab (U+0000 to U+0008, U+000A to U+001F, U+007F).
        LiteralString, // 'text'
        BasicString,   // "hello\r\n"
        EmptyString,   // "" or '' or """""" or ''''''
        MultiLineString,
        MultiLineLiteralString,
        Literal,

        /// # until EOL
        /// note : Control characters other than tab (U+0000 to U+0008, U+000A to U+001F, U+007F) are not permitted in comments.
        Comment,

        True,
        False,
        DoubleLeftBracket,
        DoubleRightBracket,

        ///End of file
        // Keep this as the last token, order matters
        Eof
    }
}