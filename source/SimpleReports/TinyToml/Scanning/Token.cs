using System;

namespace TinyToml.Scanning
{
    internal readonly ref struct Token
    {
        public readonly TokenType TokenType;
        public readonly int       Line;
        public readonly int       Column;

        /// <summary>
        /// This is the raw value of the scanned token EXCEPT for <see cref="F:TokenType.BasicString"/> and <see cref="F:TokenType.MultiLineString"/>,
        /// for those the unescaped value without quotes are stored.
        /// </summary>
        public readonly ReadOnlySpan<char> Text;

        public Token(TokenType          tokenType,
                     ReadOnlySpan<char> text,
                     int                line,
                     int                column
        )
        {
            TokenType = tokenType;
            Text      = text;
            Line      = line;
            Column    = column;
        }
    }
}