using Ruzzie.Common.Types;
using Ruzzie.SimpleReports.Run;
using Ruzzie.SimpleReports.Types;

namespace Ruzzie.SimpleReports.Reading;

public readonly struct ReportQueryCtx : IReportQueryCtx
{
    private readonly IReportDefinition _reportDefinition;

    public ReportQueryCtx(IReportDefinition reportDefinition)
    {
        _reportDefinition = reportDefinition;
    }

    // todo: review design of the ctx, especially the createParameter and tryGetParameter definition. is this the proper place and abstraction?
    public IQueryRunParameter<TValue> CreateQueryParameter<TValue>(string name, ParameterFieldType type, TValue value)
    {
        return new QueryRunParameter<TValue>(name, type, value);
    }

    public Option<T> TryGetParameterDefinition<T>(string parameterId) where T : IReportParameterDefinition
    {
        var found = _reportDefinition.Parameters.TryGetValue(parameterId, out var r);
        if (found)
        {
            return r is T definition ? Option.Some(definition) : Option<T>.None;
        }

        return Option<T>.None;
    }
}