using Ruzzie.Common.Types;
using Ruzzie.SimpleReports.Run;

namespace Ruzzie.SimpleReports.Reading;

public readonly struct FilterParameterValue<T> : IReportParameterValue
{
    public string    ParameterId { get; }
    public Option<T> Value       { get; } // maybe nullable?

    public FilterParameterValue(string parameterId, T value)
    {
        ParameterId = parameterId;
        Value       = value;
    }

    public FilterParameterValue(string parameterId)
    {
        ParameterId = parameterId;
        Value       = Option.None<T>();
    }

    public Result<Err<AddParameterErrorCode>, IQueryRunParameter[]> CreateQueryParameters(in IReportQueryCtx ctx)
    {
        var filterDefinition = ctx.TryGetParameterDefinition<FilterParameterDefinition>(ParameterId);

        if (filterDefinition.TryGetValue(out var parameterDefinition, FilterParameterDefinition.Empty))
        {
            if (Value.TryGetValue(out var value))
            {
                var filter = ctx.CreateQueryParameter(
                                                      parameterDefinition.Name,
                                                      parameterDefinition.ParameterFieldType,
                                                      value);
                return new[] {filter};
            }
        }

        return new Err<AddParameterErrorCode>($"Could not add {ParameterId} of type {typeof(FilterParameterValue<T>).FullName} ",
                                              AddParameterErrorCode.ParameterIdNotFoundForParameterValueType);
    }
}