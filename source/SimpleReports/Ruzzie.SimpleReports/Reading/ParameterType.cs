using System.Diagnostics.CodeAnalysis;

namespace Ruzzie.SimpleReports.Reading;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum ParameterType
{
    NONE
   ,

    /// <summary>A parameter that is used as a filter and a list of possible values can be obtained.</summary>
    FILTER_LOOKUP

   ,

    /// <summary>A parameter that represents a date range with a from and to date.</summary>
    DATE_RANGE

   ,

    /// <summary>A parameter that represents a time interval, like 1 hour, 2 days, 1 week, 1 year etc.</summary>
    TIME_INTERVAL


  , /// <summary>A parameter that represents a time zone, possible values van be obtained. ex. "Europe/Amsterdam", "UTC" </summary>
    TIMEZONE
}