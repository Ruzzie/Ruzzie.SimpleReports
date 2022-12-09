using System;
using System.Collections.Generic;
using Ruzzie.SimpleReports.Run;

namespace Ruzzie.SimpleReports.Pipelines;

public interface IPostProcessPipeline
{
    IAsyncQueryResult Process(IReadOnlyList<string> pipelineArgs, in ReadOnlySpan<(string Name, string Value)> runParams, IAsyncQueryResult queryResult);
}