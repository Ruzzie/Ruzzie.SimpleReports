using System;
using System.Collections.Generic;
using Ruzzie.SimpleReports.Run;

namespace Ruzzie.SimpleReports.UnitTests;

public class QueryRunnerStub : IReportQueryRunner
{
    public IAsyncQueryResult Run(ReadOnlySpan<(string Name, string Value)> runParams
                               , string                                    query
                               , List<IQueryRunParameter>                  queryParameters)
    {
        return new AsyncQueryResult(new IColumn[]
                                    {
                                        new Column("Test",   ColumnDataType.s)
                                      , new Column("DTTest", ColumnDataType.td_s)
                                    }
                                  , GenerateTestRows());
    }

#pragma warning disable 1998
    private async IAsyncEnumerable<IDataRow> GenerateTestRows()
#pragma warning restore 1998
    {
        for (int i = 0; i < 10; i++)
        {
            var row = DataRow.Create(1);
            row.AddField(Guid.NewGuid().ToString());
            row.AddField(DateTime.UtcNow);
            //row[0] = //TODO: Change api to reflect that you cannot SET the index when Add was not called first
            yield return row;
        }
    }
}