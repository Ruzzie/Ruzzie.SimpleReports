using System.Collections.Generic;

namespace Ruzzie.SimpleReports.Run
{
    public interface IDataRow
    {
        IReadOnlyList<object> GetValues();
        object this[int index] { get; }
        int             FieldCount { get; }
        void            AddField(object value);
    }
}