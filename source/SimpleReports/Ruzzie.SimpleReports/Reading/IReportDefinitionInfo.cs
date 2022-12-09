using System.Collections.Generic;

namespace Ruzzie.SimpleReports.Reading;

public interface IReportDefinitionInfo
{
    string                                              Id             { get; }
    string                                              Title          { get; }
    string                                              Description    { get; }
    string                                              Category       { get; }
    string                                              Feature        { get; }
    bool                                                WithTotals     { get; }
    IReadOnlyDictionary<string, IColumnInfo>            Columns        { get; }
    IReadOnlyCollection<IReportParameterDefinitionInfo> ParameterInfos { get; }
}