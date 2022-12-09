using Ruzzie.SimpleReports.Types;

namespace Ruzzie.SimpleReports.Reading;

public interface IReportParameterDefinitionInfo
{
    public string      ParameterId        { get; }
    public string      DisplayName        { get; }
    ParameterType      Type               { get; }
    ParameterFieldType ParameterFieldType { get; }
}