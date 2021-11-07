namespace TinyToml.Parsing
{
    internal unsafe readonly struct ParseRule
    {
        // public delegate void ParseFunc(bool canAssign, ref ParseState parseState);

        /// <summary>
        /// Prefix. ParseFunc(bool canAssign, ref ParseState parseState);
        /// </summary>
        public readonly delegate*<bool, ref ParseState, void> Prefix;

        /// <summary>
        /// Infix. ParseFunc(bool canAssign, ref ParseState parseState);
        /// </summary>
        public readonly delegate*<bool, ref ParseState, void> Infix;
        public readonly Precedence                            Precedence;

        public ParseRule(delegate*<bool, ref ParseState, void> prefix
                       , delegate*<bool, ref ParseState, void> infix
                       , Precedence                            precedence)
        {
            Prefix     = prefix;
            Infix      = infix;
            Precedence = precedence;
        }
    }
}