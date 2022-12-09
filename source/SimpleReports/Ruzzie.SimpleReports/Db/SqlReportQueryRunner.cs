using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Threading.Tasks;
using Ruzzie.SimpleReports.Run;

namespace Ruzzie.SimpleReports.Db
{
    public class SqlReportQueryRunner : IReportQueryRunner
    {
        private readonly CreateConnectionForRunFunc _createConnectionForRunFunc;
        private readonly bool                       _usePreparedStatement;

        private readonly ReadOnlyDictionary<Type, ColumnDataType> _columnTypeMapping;

        public SqlReportQueryRunner(CreateConnectionForRunFunc               createConnectionForRunFunc
                                  , Span<KeyValuePair<Type, ColumnDataType>> extraColumnTypeMapping
                                  , bool                                     usePreparedStatement = true)
        {
            _createConnectionForRunFunc = createConnectionForRunFunc;
            _usePreparedStatement       = usePreparedStatement;

            if (extraColumnTypeMapping.IsEmpty)
            {
                _columnTypeMapping = Config.DefaultColumnTypeMapping;
            }
            else
            {
                var columnTypeMapping = new Dictionary<Type, ColumnDataType>(Config.DefaultColumnTypeMapping);

                foreach (var (key, value) in extraColumnTypeMapping)
                {
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract : TrustNo1
                    if (key != null)
                        columnTypeMapping.Add(key, value);
                }

                _columnTypeMapping = new ReadOnlyDictionary<Type, ColumnDataType>(columnTypeMapping);
            }
        }

        ///Runs (executes) a given query. This will create a connection and command (with prepare)
        /// and create parameters for the query and returns an async result such that it can be iterated
        /// with an await foreach.
        /// <remarks>A <see cref="DbConnection"/> is created by calling the <see cref="CreateConnectionForRunFunc"/>
        /// method passed in the <see ref="SqlReportQueryRunner.(CreateConnectionForRunFunc,Span{KeyValuePair{Type, ColumnDataType}})"/> with <paramref name="runParams"/>
        /// </remarks>
        public IAsyncQueryResult Run(ReadOnlySpan<(string Name, string Value)> runParams
                                   , string                                    query
                                   , List<IQueryRunParameter>                  queryParameters)
        {
            //the connection is disposed later when the AsyncEnumerable is done
            //   note: this will throw exceptions when a valid connection cannot be created
            var dbConnection = _createConnectionForRunFunc(runParams);
            dbConnection.Open();

            var dbCommand = dbConnection.CreateCommand();
            dbCommand.CommandText = query;
            dbCommand.Parameters.AddRange(CreateSqlParameters(queryParameters, dbCommand));

            if (_usePreparedStatement)
                dbCommand.Prepare();

            // reader is wrapped in using in the ReadRows method for async reading
            var reader = dbCommand.ExecuteReader();
            reader.Read();

            //Obtain Column information
            var columnSchema = reader.GetColumnSchema();
            var headers      = new IColumn[columnSchema.Count];
            var columnCount  = columnSchema.Count;
            for (var i = 0; i < columnCount; i++)
            {
                var dbColumn = columnSchema[i];
                // ReSharper disable once ConstantNullCoalescingCondition
                // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
                var name = dbColumn.ColumnName ?? "";

                var type       = dbColumn.DataType; // clr type? except datetime in some cases??
                var columnType = type != null ? MapColumnType(type) : ColumnDataType.s;

                // do a type mapping
                headers[i] = new Column(name, columnType);
            }

            if (!reader.HasRows)
            {
                //todo: add test case / handle empty case in writer... // pass columns
                return AsyncQueryResult.Empty(headers);
            }

            var x = ReadRows(reader, dbCommand, dbConnection);

            //the idea is that the columns are read blocking and the rows non-blocking asynchronous
            //  so the AsyncQueryResult is returned as soon as possible to the calling thread
            //  and the reading is done in a separate task, and a consumer reads the async enumerable from the result

            return new AsyncQueryResult(headers, x);
        }

        /// Returns a <see cref="ColumnDataType"/> for a <see cref="Type"/>. When there is no known mapping <see cref="ColumnDataType.s"/> will be returned as a default fallback value.
        private ColumnDataType MapColumnType(Type type)
        {
            _columnTypeMapping.TryGetValue(type, out var columnType);
            return columnType;
        }

        private static DbParameter[] CreateSqlParameters(List<IQueryRunParameter> queryParameters
                                                       , DbCommand                dbCommand)
        {
            var count         = queryParameters.Count;
            var sqlParameters = new DbParameter[count];
            for (var i = 0; i < count; i++)
            {
                var queryParam   = queryParameters[i];
                var sqlParameter = dbCommand.CreateParameter();
                sqlParameter.ParameterName = queryParam.Name;
                sqlParameter.Value         = queryParam.Value;

                //todo: check if we need to set the type explicitly
                //var type = queryParam.Type;
                sqlParameters[i] = sqlParameter;
            }

            return sqlParameters;
        }

        /// Returns an async enumerable to read the rows from a DbDataReader, and disposes the reader and connection when done.
        private static async IAsyncEnumerable<IDataRow> ReadRows(DbDataReader     reader
                                                               , IAsyncDisposable commandToDisposeWhenDone
                                                               , IAsyncDisposable connectionToDisposeWhenDone)
        {
            await using (connectionToDisposeWhenDone.ConfigureAwait(false))
            {
                await using (commandToDisposeWhenDone.ConfigureAwait(false))
                {
                    await using (reader.ConfigureAwait(false))
                    {
                        //Do While loop: since we assume that the Read was called at least once to obtain the column information;
                        do
                        {
                            var fieldCount = reader.FieldCount;
                            var row        = DataRow.Create(fieldCount);

                            for (var i = 0; i < fieldCount; i++)
                            {
                                //Set each field value
                                row.AddField(reader.GetValue(i));
                            }

                            yield return row;
                        } while (await reader.ReadAsync());
                    }
                }
            }
        }
    }
}