namespace Ruzzie.SimpleReports.Run;

public record Column (string Name, ColumnDataType Type) : IColumn;