using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ruzzie.Common.Types;
using Ruzzie.SimpleReports.Reading;
using Ruzzie.SimpleReports.Types;

namespace Ruzzie.SimpleReports;

public interface IReportService
{
    Result<Err<CreateParameterErrKind>, IReportParameterValue> CreateParameterValue<TValue>(
        string reportId,
        string parameterId,
        TValue value);

    Result<Err<RunReportErrKind, Exception>, Task> RunReport(RunReportContext runContext, Stream streamToWriteTo);
    IReadOnlyCollection<IReportDefinitionInfo>     GetAllReportDefinitionInfos();

    Result<Err<ListParameterValuesErrKind, Exception>, IListParameterValues> ListParameterValues(
        string                                    reportId,
        string                                    parameterId,
        ReadOnlySpan<(string Name, string Value)> args);
}