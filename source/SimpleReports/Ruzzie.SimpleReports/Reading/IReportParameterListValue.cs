using Ruzzie.SimpleReports.Types;

namespace Ruzzie.SimpleReports.Reading
{
    public interface IReportParameterListValue
    {
        string             Name      { get; }
        object?            Value     { get; }
        ParameterFieldType ValueType { get; }
    }
}