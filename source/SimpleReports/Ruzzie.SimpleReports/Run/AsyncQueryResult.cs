using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ruzzie.SimpleReports.Run;

public class AsyncQueryResult : IAsyncQueryResult
{
    public static AsyncQueryResult Empty(IReadOnlyList<IColumn> columns) =>
        new AsyncQueryResult(columns, EmptyAsyncEnumerable());

    private static async IAsyncEnumerable<IDataRow> EmptyAsyncEnumerable()
    {
        foreach (var none in Array.Empty<object[]>())
        {
            yield return default!;
        }

        await Task.FromResult(1);
    }

    public AsyncQueryResult(IReadOnlyList<IColumn> columns, IAsyncEnumerable<IDataRow> rows)
    {
        Columns = columns;
        Rows    = rows;
    }

    public IReadOnlyList<IColumn>     Columns { get; }
    public IAsyncEnumerable<IDataRow> Rows    { get; }
}