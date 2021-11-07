using System.Diagnostics.CodeAnalysis;

namespace Ruzzie.SimpleReports.Reading
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ParameterType
    {
        NONE,
        FILTER_LOOKUP,
        DATE_RANGE
    }
}