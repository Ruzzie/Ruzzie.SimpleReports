using System;
using System.Collections.Generic;

namespace Ruzzie.SimpleReports.Reading
{
    public interface IListProvider
    {
        IReadOnlyList<IReportParameterListValue> ListParameterValues(IReportParameterDefinition forParameterDefinition
                                                                   , ReadOnlySpan<(string Name, string Value)> runParams);

    }

    public class EmptyListProvider : IListProvider
    {
        private static readonly IReadOnlyList<IReportParameterListValue> EmptyReportParameterListValues =
            new List<IReportParameterListValue>(0).AsReadOnly();

        public IReadOnlyList<IReportParameterListValue> ListParameterValues(IReportParameterDefinition forParameterDefinition, ReadOnlySpan<(string Name, string Value)> runParams)
        {
            return EmptyReportParameterListValues;
        }
    }
}