using Ruzzie.Common.Types;
using Ruzzie.SimpleReports.Reading;

namespace Ruzzie.SimpleReports;

public static class ReportServiceExtensions
{
    public static Result<Err<CreateParameterErrKind>, IReportParameterValue> CreateDataRangeParameterValue(
        this IReportService  service,
        string               reportId,
        string               parameterId,
        DateRangeInputValues value)
    {
        return service.CreateParameterValue(reportId, parameterId, value);
    }
}