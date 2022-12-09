using System.Collections.Generic;

namespace Ruzzie.SimpleReports.Run;

public readonly struct DataRow : IDataRow
{
    private readonly List<object> _valueList;
    public           int          FieldCount => _valueList.Count;

    private DataRow(List<object> valueList)
    {
        _valueList = valueList;
    }

    public static DataRow Create(int initialFieldCount)
    {
        return new DataRow(new List<object>(initialFieldCount));
    }

    public IReadOnlyList<object> GetValues()
    {
        return _valueList;
    }

    public void AddField(object value)
    {
        _valueList.Add(value);
    }

    public object this[int index] => _valueList[index];
}