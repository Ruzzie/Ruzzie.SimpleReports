using Ruzzie.Common.Types;
using Ruzzie.SimpleReports.Run;

namespace Ruzzie.SimpleReports.Reading;

public interface IReportParameterValue
{
    string                                                   ParameterId { get; }
    Result<Err<AddParameterErrorCode>, IQueryRunParameter[]> CreateQueryParameters(in IReportQueryCtx ctx);
}