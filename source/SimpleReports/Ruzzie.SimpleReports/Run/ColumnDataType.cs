using System.Diagnostics.CodeAnalysis;

namespace Ruzzie.SimpleReports.Run;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum ColumnDataType
{
    /// <summary>
    /// String
    /// this is the default value
    /// </summary>
    s,

    /// <summary>
    /// Boolean, True or false
    /// </summary>
    b,

    /// <summary>
    /// 64 bits signed integer
    /// </summary>
    i,

    /// <summary>
    /// 64 bits unsigned integer
    /// </summary>
    u,

    /// <summary>
    /// 128 bits floating point
    /// </summary>
    f,

    /// <summary>
    /// Date in UnixTimeMillis.
    /// </summary>
    td_ms,

    /// <summary>
    /// DateTime in UnixTimeMillis.
    /// </summary>
    tdt_ms,

    /// <summary>
    /// Date in ISO date string.
    /// </summary>
    td_s,

    /// <summary>
    /// DateTime in ISO dateTime string.
    /// </summary>
    tdt_s
}