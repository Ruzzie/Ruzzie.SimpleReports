using System.Collections.Generic;
using Ruzzie.SimpleReports.Pipelines;

namespace Ruzzie.SimpleReports.Reading;

public record PostProcessPipelineDefinition
    (IPostProcessPipeline Pipeline, IReadOnlyList<string> ParamsArray) : IPostProcessPipelineDefinition;