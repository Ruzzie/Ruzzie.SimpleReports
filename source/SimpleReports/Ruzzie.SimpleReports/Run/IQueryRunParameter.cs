using Ruzzie.SimpleReports.Types;

namespace Ruzzie.SimpleReports.Run
{
    public interface IQueryRunParameter
    {
        string             Name  { get; }
        object?            Value { get; }
        ParameterFieldType Type  { get; }
    }

    public interface IQueryRunParameter<out T> : IQueryRunParameter
    {
        new T Value { get; }
    }
}