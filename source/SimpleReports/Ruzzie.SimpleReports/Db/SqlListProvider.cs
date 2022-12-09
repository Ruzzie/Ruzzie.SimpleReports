using System;
using System.Collections.Generic;
using Ruzzie.SimpleReports.Reading;

namespace Ruzzie.SimpleReports.Db;

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
///  You can add Sql parameters to the query by providing extra params
///  the value of the parameter will be read from the runParams where the Name should match the given Sql parameter name
/// <code>
/// params        = ["SELECT tagId, name from tags WHERE category=@category", "@category"]
/// </code>
/// <code>
/// runParams = [(Name = "@category", Value ="popular")]
/// </code>
/// </remarks>
/// </summary>
public class SqlListProvider : IListProvider
{
    private readonly CreateConnectionForRunFunc _createConnection;
    private readonly bool                       _usePreparedStatement;

    public SqlListProvider(CreateConnectionForRunFunc createConnection, bool usePreparedStatement = true)
    {
        _createConnection     = createConnection;
        _usePreparedStatement = usePreparedStatement;
    }

    public IReadOnlyList<IReportParameterListValue> ListParameterValues(
        IReportParameterDefinition                parameterDefinition
      , ReadOnlySpan<(string Name, string Value)> runParams)
    {
        using var connection = _createConnection(runParams);
        connection.Open();

        var sql = parameterDefinition.ParamsArray[0];

        //List query
        using var command = connection.CreateCommand();
        command.CommandText = sql;

        //check for extra parameters
        for (var i = 1; i < parameterDefinition.ParamsArray.Count; i++)
        {
            var sqlParamParameterName = parameterDefinition.ParamsArray[i];
            if (RunParamsHelper.GetParameterFromRunParams(runParams, sqlParamParameterName)
                               .TryGetValue(out var sqlParamValue))
            {
                // has a value for a given parameter
                var sqlParam = command.CreateParameter();
                sqlParam.ParameterName = sqlParamParameterName;
                sqlParam.Value         = sqlParamValue;
                command.Parameters.Add(sqlParam);
            }
        }

        if(_usePreparedStatement)
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