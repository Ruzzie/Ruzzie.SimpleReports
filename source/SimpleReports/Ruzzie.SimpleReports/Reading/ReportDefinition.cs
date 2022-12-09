using System;
using System.Collections.Generic;

namespace Ruzzie.SimpleReports.Reading;

internal class ReportDefinition : IReportDefinition
{
    private Dictionary<string, IReportParameterDefinition> _parameters =
        new Dictionary<string, IReportParameterDefinition>();

    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Feature { get; set; } = string.Empty;
    public bool WithTotals { get; set; }
    public string QueryText { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, IColumnInfo> Columns { get; set; } = new Dictionary<string, IColumnInfo>();
    public IReadOnlyCollection<IReportParameterDefinitionInfo> ParameterInfos => _parameters.Values;

    public IReadOnlyDictionary<string, IReportParameterDefinition> Parameters
    {
        get => _parameters;
        set => _parameters = new Dictionary<string, IReportParameterDefinition>(value);
    }

    public IReadOnlyCollection<IPostProcessPipelineDefinition> PostProcessPipelines { get; set; } =
        Array.Empty<IPostProcessPipelineDefinition>();
}