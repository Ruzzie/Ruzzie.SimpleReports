using System;
using System.Collections.Generic;

namespace Ruzzie.SimpleReports.Run
{
    public interface IReportQueryRunner
    {
        ///Executes a report with given <paramref name="runParams"/> the runParams can contain context dependant parameters for running this report.
        IAsyncQueryResult Run(ReadOnlySpan<(string Name, string Value)> runParams,
                              string                                    query,
                              List<IQueryRunParameter>               queryParameters);
    }
}