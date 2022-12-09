using Ruzzie.SimpleReports.Types;

namespace Ruzzie.SimpleReports.Reading;

public record ReportParameterListValue(string Name, object? Value, ParameterFieldType ValueType) : IReportParameterListValue;