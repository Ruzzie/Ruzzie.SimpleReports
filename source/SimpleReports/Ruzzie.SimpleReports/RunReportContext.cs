using System;
using Ruzzie.SimpleReports.Reading;
using Ruzzie.SimpleReports.Run;
using Ruzzie.SimpleReports.Types;
using Ruzzie.SimpleReports.Writers;

namespace Ruzzie.SimpleReports;

public readonly ref struct RunReportContext
{
    public RunReportContext(string                                    reportId
                          , ReadOnlySpan<(string Name, string Value)> args
                          , ReadOnlySpan<IReportParameterValue>       reportParamValues
                          , IReportDataWriter                         writer
                          , IReportQueryRunner                        queryRunner)
    {
        ReportId          = reportId;
        Args              = args;
        ReportParamValues = reportParamValues;
        Writer            = writer;
        QueryRunner       = queryRunner;
    }

    public readonly string                                    ReportId;
    public readonly ReadOnlySpan<(string Name, string Value)> Args;
    public readonly ReadOnlySpan<IReportParameterValue>       ReportParamValues;
    public readonly IReportDataWriter                         Writer;
    public readonly IReportQueryRunner                        QueryRunner;
}