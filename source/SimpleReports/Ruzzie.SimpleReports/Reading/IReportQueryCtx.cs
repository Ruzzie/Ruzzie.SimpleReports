using Ruzzie.Common.Types;
using Ruzzie.SimpleReports.Run;
using Ruzzie.SimpleReports.Types;

namespace Ruzzie.SimpleReports.Reading;

public interface IReportQueryCtx
{
    IQueryRunParameter<TValue> CreateQueryParameter<TValue>(string name, ParameterFieldType type, TValue value);

    public Option<T> TryGetParameterDefinition<T>(string parameterId) where T : IReportParameterDefinition;
}