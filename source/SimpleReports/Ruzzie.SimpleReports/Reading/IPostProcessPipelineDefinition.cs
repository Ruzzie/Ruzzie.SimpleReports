using System.Collections.Generic;
using Ruzzie.SimpleReports.Pipelines;

namespace Ruzzie.SimpleReports.Reading;

public interface IPostProcessPipelineDefinition
{
    IPostProcessPipeline  Pipeline    { get; }
    IReadOnlyList<string> ParamsArray { get; }
}