using System.Collections.Generic;
using Ruzzie.SimpleReports.Reading;

namespace Ruzzie.SimpleReports;

public record ListParameterValues(string ReportId, string ParameterId, IReadOnlyList<IReportParameterListValue> Values) : IListParameterValues;