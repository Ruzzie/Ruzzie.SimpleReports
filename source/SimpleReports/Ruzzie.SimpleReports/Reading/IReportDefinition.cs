using System.Collections.Generic;

namespace Ruzzie.SimpleReports.Reading;

public interface IReportDefinition : IReportDefinitionInfo
{
    string                                                  QueryText            { get; }
    IReadOnlyDictionary<string, IReportParameterDefinition> Parameters           { get; }
    IReadOnlyCollection<IPostProcessPipelineDefinition>     PostProcessPipelines { get; }
}