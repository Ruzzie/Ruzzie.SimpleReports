using System;
using System.Collections.Generic;
using Ruzzie.Common.Types;
using Ruzzie.SimpleReports.Types;

namespace Ruzzie.SimpleReports.Reading;

public class FilterParameterDefinition : IReportParameterDefinition
{
    public static readonly FilterParameterDefinition Empty =
        new FilterParameterDefinition("EMPTY"
                                    , ""
                                    , "EMPTY"
                                    , ParameterType.NONE
                                    , ParameterFieldType.None
                                    , Option<IListProvider>.None
                                    , Array.Empty<string>());
    public string                ParameterId        { get; }
    public string                DisplayName        { get; }
    public ParameterType         Type               { get; }
    public string                Name               { get; }
    public ParameterFieldType    ParameterFieldType { get; }
    public IReadOnlyList<string> ParamsArray        { get; }

    public Option<IListProvider> ListProviderType { get; }

    public FilterParameterDefinition(string                parameterId
                                   , string                displayName
                                   , string                name
                                   , ParameterType         type
                                   , ParameterFieldType    parameterFieldType
                                   , Option<IListProvider> listProviderType
                                   , IReadOnlyList<string> paramsArray)
    {
        if (string.IsNullOrWhiteSpace(parameterId))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(parameterId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        ParameterId        = parameterId;
        Name               = name;
        Type               = type;
        ParameterFieldType = parameterFieldType;
        ListProviderType   = listProviderType;
        ParamsArray        = paramsArray;
        DisplayName        = displayName;
    }

    public IReportParameterValue CreateValue<T>(T value)
    {
        //todo: type check .. return error???
        return new FilterParameterValue<T>(ParameterId, value);
    }
}