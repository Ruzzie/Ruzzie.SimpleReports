using System.Collections.Generic;
using Ruzzie.Common.Types;

namespace Ruzzie.SimpleReports.Reading
{
    public interface IReportsDefinitionRepository
    {
        /// <summary>
        /// Get a <see cref="IReportDefinition"/>. If the <paramref name="reportId"/> does not exists None will be returned.
        /// </summary>
        /// <param name="reportId">The Id of the report</param>
        /// <returns>Some when found, None otherwise.</returns>
        Option<IReportDefinition> GetReportDefinition(string reportId);

        IReadOnlyCollection<IReportDefinition> GetAllReportDefinitions();
    }
}