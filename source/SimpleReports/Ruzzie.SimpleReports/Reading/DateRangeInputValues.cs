using System;

namespace Ruzzie.SimpleReports.Reading
{
    public readonly struct DateRangeInputValues
    {
        public DateTime FromValue { get; }
        public DateTime ToValue   { get; }

        public DateRangeInputValues(DateTime fromValue, DateTime toValue)
        {
            FromValue = fromValue;
            ToValue   = toValue;
        }
    }
}