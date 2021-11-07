using Ruzzie.SimpleReports.Types;

namespace Ruzzie.SimpleReports.Run
{
    public record QueryRunParameter<T>(string Name, ParameterFieldType Type, T Value) : IQueryRunParameter<T>
    {
        object? IQueryRunParameter.Value => Value;
    }
}