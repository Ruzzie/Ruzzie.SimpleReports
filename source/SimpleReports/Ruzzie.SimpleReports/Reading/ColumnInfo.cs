namespace Ruzzie.SimpleReports.Reading;

internal class ColumnInfo : IColumnInfo
{
    public string ColumnName { get; }
    public string Type       { get; }

    public ColumnInfo(string columnName, string type)
    {
        ColumnName = columnName;
        Type       = type;
    }
}