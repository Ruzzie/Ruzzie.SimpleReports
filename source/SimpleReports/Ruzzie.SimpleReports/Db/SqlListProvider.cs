using System;
using System.Collections.Generic;
using Ruzzie.SimpleReports.Reading;

namespace Ruzzie.SimpleReports.Db
{
    /// <summary>
    /// a simple list provider based on a sq1l query this expects the id, value (string) as the sql query's output
    /// <remarks>
    /// <code>
    /// [MY-REPORT-ID.parameters.tag]
    ///  display_name  = "Tag filter"
    ///  name          = "@tagId"
    ///  type          = "FILTER_LOOKUP"
    ///  field_type    = "u8"
    ///  list_provider = "Ruzzie.SimpleReports.SqlListProvider"
    ///  params        = ["SELECT tagId, name from tags"]
    /// </code>
    /// </remarks>
    /// </summary>
    public class SqlListProvider : IListProvider
    {
        private readonly CreateConnectionForRunFunc _createConnection;

        public SqlListProvider(CreateConnectionForRunFunc createConnection)
        {
            _createConnection = createConnection;
        }

        public IReadOnlyList<IReportParameterListValue> ListParameterValues(
            IReportParameterDefinition                parameterDefinition,
            ReadOnlySpan<(string Name, string Value)> runParams)
        {
            using var connection = _createConnection(runParams);
            connection.Open();

            var sql = parameterDefinition.ParamsArray[0];

            //List query
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            command.Prepare();

            using var reader = command.ExecuteReader();

            //Will read the result-set and assume that the first column is the id and the second column is the value
            var resultList = new List<IReportParameterListValue>();

            while (reader.Read())
            {
                resultList.Add(new ReportParameterListValue(reader.GetString(1)
                                                            , reader[0]
                                                            , parameterDefinition.ParameterFieldType));
            }

            return resultList;
        }
    }
}