using System.Collections.Generic;
using Ruzzie.SimpleReports.Reading;

namespace Ruzzie.SimpleReports
{
    public interface IListParameterValues
    {
        public string                                   ReportId    { get; }
        public string                                   ParameterId { get; }
        public IReadOnlyList<IReportParameterListValue> Values      { get; }
    }
}