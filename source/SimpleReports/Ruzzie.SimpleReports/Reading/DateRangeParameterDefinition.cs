using System;
using System.Collections.Generic;
using Ruzzie.Common.Types;
using Ruzzie.SimpleReports.Types;

namespace Ruzzie.SimpleReports.Reading;

public class DateRangeParameterDefinition : IReportParameterDefinition
{
    public static readonly DateRangeParameterDefinition Empty =
        new DateRangeParameterDefinition(""
                                       , ""
                                       , ""
                                       , ""
                                       , Option<IListProvider>.None
                                       , Array.Empty<string>()
                                       , ParameterType.NONE
                                       , ParameterFieldType.None);
    public string                ParameterId        { get; }
    public string                DisplayName        { get; }
    public ParameterType         Type               { get; }
    public ParameterFieldType    ParameterFieldType { get; }
    public IReadOnlyList<string> ParamsArray        { get; }
    public Option<IListProvider> ListProviderType   { get; }
    public string                From               { get; }
    public string                To                 { get; }
    public DateRange             Default            { get; }


    public DateRangeParameterDefinition(string                parameterId
                                      , string                displayName
                                      , string                @from
                                      , string                to
                                      , Option<IListProvider> listProviderType
                                      , IReadOnlyList<string> paramsArray
                                      , ParameterType         type               = ParameterType.DATE_RANGE
                                      , ParameterFieldType    parameterFieldType = ParameterFieldType.DT
                                      , DateRange             @default           = DateRange.Today
    )
    {
        Type               = type;
        ParameterFieldType = parameterFieldType;
        From               = @from;
        To                 = to;
        ListProviderType   = listProviderType;
        ParamsArray        = paramsArray;
        ParameterId        = parameterId;
        DisplayName        = displayName;
        Default            = @default;
    }


    public IReportParameterValue CreateValue<T>(T value)
    {
        if (value is DateRangeInputValues inputValues)
        {
            return new DateRangeParameterValue(ParameterId, inputValues.FromValue, inputValues.ToValue);
        }

        return new DateRangeParameterValue(ParameterId);
    }
}