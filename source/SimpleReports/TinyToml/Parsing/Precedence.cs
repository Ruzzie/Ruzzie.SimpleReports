﻿using System.Diagnostics.CodeAnalysis;

namespace TinyToml.Parsing
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    internal enum Precedence
    {
        PREC_NONE,
        PREC_ASSIGNMENT, // =
        PREC_OR,         // or
        PREC_AND,        // and
        PREC_EQUALITY,   // == !=
        PREC_COMPARISON, // < > <= >=
        PREC_TERM,       // + -
        PREC_FACTOR,     // * /
        PREC_UNARY,      // ! -
        PREC_CALL,       // . ()
        PREC_PRIMARY
    }
}