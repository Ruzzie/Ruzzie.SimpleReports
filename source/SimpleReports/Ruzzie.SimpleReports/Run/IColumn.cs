namespace Ruzzie.SimpleReports.Run
{
    public interface IColumn
    {
        string         Name { get; }
        ColumnDataType Type { get; }
    }
}