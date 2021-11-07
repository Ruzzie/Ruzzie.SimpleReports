﻿using System.Collections.Generic;
using Ruzzie.Common.Types;
using Ruzzie.SimpleReports.Types;

namespace Ruzzie.SimpleReports.Reading
{
    public interface IReportParameterDefinition : IReportParameterDefinitionInfo
    {
        IReportParameterValue        CreateValue<T>(T value);
        Option<IListProvider>        ListProviderType { get; }
        public IReadOnlyList<string> ParamsArray      { get; }
    }
}