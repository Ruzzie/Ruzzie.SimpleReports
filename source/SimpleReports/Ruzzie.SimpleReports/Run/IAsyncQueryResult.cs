using System.Collections.Generic;

namespace Ruzzie.SimpleReports.Run
{
    public interface IAsyncQueryResult
    {
        IReadOnlyList<IColumn>     Columns { get; }
        IAsyncEnumerable<IDataRow> Rows    { get; }
    }
}