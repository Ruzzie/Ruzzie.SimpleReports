using System.Collections.Generic;
using Ruzzie.SimpleReports.Pipelines;

namespace Ruzzie.SimpleReports.Reading;

public class PostProcessPipelineDefinition : IPostProcessPipelineDefinition
{
    public IPostProcessPipeline  Pipeline    { get; }
    public IReadOnlyList<string> ParamsArray { get; }

    public PostProcessPipelineDefinition(IPostProcessPipeline postProcessPipeline, IReadOnlyList<string> paramsArray)
    {
        Pipeline    = postProcessPipeline;
        ParamsArray = paramsArray;
    }
}