namespace TinyToml.Parsing
{
    internal class ParseError
    {
        public static readonly ParseError Empty = new ParseError();
        public                 string     Message { get; set; } = "";
    }
}