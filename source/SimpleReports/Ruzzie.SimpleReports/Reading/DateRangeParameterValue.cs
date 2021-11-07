using System;
using Ruzzie.Common.Types;
using Ruzzie.SimpleReports.Run;

namespace Ruzzie.SimpleReports.Reading
{
    public readonly struct DateRangeParameterValue : IReportParameterValue
    {
        public string         ParameterId { get; }
        public DateTimeOffset FromValue   { get; }
        public DateTimeOffset ToValue     { get; }


        public DateRangeParameterValue(string parameterId, DateTime fromValue, DateTime toValue)
        {
            ParameterId = parameterId;
            FromValue   = fromValue;
            ToValue     = toValue;
        }

        public DateRangeParameterValue(string parameterId)
        {
            ParameterId = parameterId;
            FromValue   = default;
            ToValue     = default;
        }

        public Result<Err<AddParameterErrorCode>, IQueryRunParameter[]> CreateQueryParameters(in IReportQueryCtx ctx)
        {
            var filterDefinition = ctx.TryGetParameterDefinition<DateRangeParameterDefinition>(ParameterId);

            if (filterDefinition.TryGetValue(out var parameterDefinition, DateRangeParameterDefinition.Empty))
            {
                var fromParam = ctx.CreateQueryParameter(
                                                         parameterDefinition.From,
                                                         parameterDefinition.ParameterFieldType,
                                                         FromValue);
                var toParam = ctx.CreateQueryParameter(
                                                       parameterDefinition.To,
                                                       parameterDefinition.ParameterFieldType,
                                                       ToValue);
                return new[] {fromParam, toParam};
            }

            return new Err<AddParameterErrorCode>($"Could not add {ParameterId} of type {typeof(DateRangeParameterValue).FullName} ",
                                                  AddParameterErrorCode.ParameterIdNotFoundForParameterValueType);
        }
    }
}