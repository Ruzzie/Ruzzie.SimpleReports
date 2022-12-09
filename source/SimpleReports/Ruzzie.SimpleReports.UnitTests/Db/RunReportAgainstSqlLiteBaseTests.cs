using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using Ruzzie.Common.Types;
using Ruzzie.SimpleReports.Db;
using Ruzzie.SimpleReports.Pipelines;
using Ruzzie.SimpleReports.Reading;
using Ruzzie.SimpleReports.Run;
using Ruzzie.SimpleReports.Writers;

namespace Ruzzie.SimpleReports.UnitTests.Db;

[TestFixture]
public class WithUsePreparedStatementsAgainstSqlLiteTestBase : RunReportAgainstSqlLiteTestBase
{
    public WithUsePreparedStatementsAgainstSqlLiteTestBase() :
        base(nameof(WithUsePreparedStatementsAgainstSqlLiteTestBase) + ".db")
    {
    }

    protected override SqlReportQueryRunner CreateQueryRunner(CreateConnectionForRunFunc createConnectionFunc)
    {
        return new SqlReportQueryRunner(createConnectionFunc
                                      , Span<KeyValuePair<Type, ColumnDataType>>.Empty
                                      , true);
    }

    protected override SqlListProvider CreateSqlListProvider(CreateConnectionForRunFunc createConnectionFunc)
    {
        return new SqlListProvider(createConnectionFunc, true);
    }
}

[TestFixture]
public class WithoutUsePreparedStatementsAgainstSqlLiteTestBase : RunReportAgainstSqlLiteTestBase
{
    public WithoutUsePreparedStatementsAgainstSqlLiteTestBase() :
        base(nameof(WithoutUsePreparedStatementsAgainstSqlLiteTestBase) + ".db")
    {
    }

    protected override SqlReportQueryRunner CreateQueryRunner(CreateConnectionForRunFunc createConnectionFunc)
    {
        return new SqlReportQueryRunner(createConnectionFunc
                                      , Span<KeyValuePair<Type, ColumnDataType>>.Empty
                                      , false);
    }

    protected override SqlListProvider CreateSqlListProvider(CreateConnectionForRunFunc createConnectionFunc)
    {
        return new SqlListProvider(createConnectionFunc, false);
    }
}

public abstract class RunReportAgainstSqlLiteTestBase
{
    private IReportService _reportService;

    private readonly SqlReportQueryRunner _sqlReportQueryRunner;
    private readonly SqlListProvider      _sqlListProvider;

    private static readonly CsvTypedReportWriter CsvTypedReportWriter = new CsvTypedReportWriter();

    protected RunReportAgainstSqlLiteTestBase(string dbFilename)
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        _sqlReportQueryRunner = CreateQueryRunner(_ => CreateAndOpenConnection());
        // ReSharper disable once VirtualMemberCallInConstructor
        _sqlListProvider = CreateSqlListProvider(_ => CreateAndOpenConnection());
        DbFilename       = dbFilename;
    }

    protected readonly string DbFilename;

    protected abstract SqlReportQueryRunner CreateQueryRunner(CreateConnectionForRunFunc     createConnectionFunc);
    protected abstract SqlListProvider      CreateSqlListProvider(CreateConnectionForRunFunc createConnectionFunc);


    [OneTimeSetUp]
    public void Setup()
    {
        // Clean start
        if (File.Exists(DbFilename))
        {
            File.Delete(DbFilename);
        }

        using var connection = CreateAndOpenConnection();

        //setup test data
        CreateAndSeed(connection);


        //load the sample report
        var listProviders = TypeResolver.Create(new IListProvider[]
                                                {
                                                    _sqlListProvider
                                                });
        var postProcessingPipelines = TypeResolver.Create(new IPostProcessPipeline[] { });

        var reportsDefinitionTomlRepository =
            new ReportsDefinitionTomlRepository($"Db{Path.DirectorySeparatorChar}sample_reports.toml"
                                              , listProviders
                                              , postProcessingPipelines);


        _reportService = new ReportService(reportsDefinitionTomlRepository);

        //Assert precondition: report can be loaded
        _reportService.GetAllReportDefinitionInfos();
    }

    private SqliteConnection CreateAndOpenConnection()
    {
        var c = new SqliteConnection($"Data Source={DbFilename}");
        c.Open();
        return c;
    }

    [Test]
    public void ListValuesForSingleArtistReport_AndThenExecuteWithAParameter()
    {
        //Arrange
        var reportDef = _reportService.GetAllReportDefinitionInfos().First(x => x.Id == "SINGLE-ARTIST");

        //Act
        var reportParameterDefinitionInfo = reportDef.ParameterInfos.ToArray()[0];
        var parameterId                   = reportParameterDefinitionInfo.ParameterId;
        var list = _reportService.ListParameterValues(reportDef.Id
                                                    , parameterId
                                                    , ReadOnlySpan<(string Name, string Value)>.Empty)
                                 .Unwrap();


        //Assert 1
        list.Values.Count.Should().Be(4); // see seed data


        //Act 2
        var parameterValue = _reportService
                             .CreateParameterValue(reportDef.Id, parameterId, list.Values.First().Value)
                             .Unwrap();

        using Stream stream = new MemoryStream();

        //Act
        _reportService.RunReport(new RunReportContext(reportDef.Id
                                                    , ReadOnlySpan<(string Name, string Value)>.Empty
                                                    , new[] { parameterValue }
                                                    , CsvTypedReportWriter
                                                    , _sqlReportQueryRunner)
                               , stream)
                      .Unwrap()
                      .Wait();


        //Assert should be valid csv
        stream.Seek(0, SeekOrigin.Begin);
        using var reader    = new StreamReader(stream, Encoding.UTF8);
        using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture, false);

        csvReader.Read().Should().BeTrue();
        csvReader.ReadHeader().Should().BeTrue();

        var numRows = 0;

        while (csvReader.Read())
        {
            Console.WriteLine(csvReader.Parser.RawRecord);
            numRows++;
        }

        numRows.Should().Be(1);
    }

    [Test]
    public void TestParameterTypes()
    {
        using Stream stream = new MemoryStream();

        var paramValues = new[]
                          {
                              _reportService
                                  .CreateDataRangeParameterValue("PARAM-TYPES-TEST"
                                                               , "range"
                                                               , new DateRangeInputValues(DateTime.Today
                                                                                        , DateTime.Now))
                                  .Unwrap()
                            , _reportService
                              .CreateParameterValue("PARAM-TYPES-TEST", "timezone", "Europe/Amsterdam")
                              .Unwrap()
                            , _reportService
                              .CreateParameterValue("PARAM-TYPES-TEST", "interval", "1 day")
                              .Unwrap()
                          };
        //Act
        _reportService.RunReport(new RunReportContext("PARAM-TYPES-TEST"
                                                    , ReadOnlySpan<(string Name, string Value)>.Empty
                                                    , paramValues
                                                    , CsvTypedReportWriter
                                                    , _sqlReportQueryRunner)
                               , stream)
                      .Unwrap()
                      .Wait();
    }


    [Test]
    public void SqlListProviderTest()
    {
        //Act
        var list = _reportService.ListParameterValues("SINGLE-ARTIST"
                                                    , "artist"
                                                    , ReadOnlySpan<(string Name, string Value)>.Empty)
                                 .Unwrap();

        //Assert
        list.Values.Should().HaveCountGreaterThan(0);
    }

    [Test]
    public void SqlListProviderWithSqlParametersTest()
    {
        //Act
        var list = _reportService.ListParameterValues("SINGLE-ARTIST-LIST-WITH-PARAM"
                                                    , "artist"
                                                    , new[] { (Name: "@minArtistId", Value: "2") })
                                 .Unwrap();

        //Assert
        list.Values.Should().HaveCountGreaterThan(0);
    }

    [Test]
    public void ExecuteSql()
    {
        //Arrange
        using var conn    = CreateAndOpenConnection();
        using var command = conn.CreateCommand();
        command.CommandText = "SELECT * FROM Artist";

        //Act
        using var reader = command.ExecuteReader();

        //Assert
        while (reader.Read())
        {
        }
    }

    private static void CreateAndSeed(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = File.ReadAllText($"Db{Path.DirectorySeparatorChar}integrationtest.sql");
        Console.WriteLine(command.ExecuteNonQuery());
    }

    [Test]
    public void RunNonExistingReportShouldGiveProperError()
    {
        using Stream stream = new MemoryStream();

        //Act
        _reportService.RunReport(new RunReportContext("IDONOTEXIST"
                                                    , ReadOnlySpan<(string Name, string Value)>.Empty
                                                    , ReadOnlySpan<IReportParameterValue>.Empty
                                                    , CsvTypedReportWriter
                                                    , _sqlReportQueryRunner)
                               , stream)
                      .UnwrapError()
                      .ErrorKind.Should()
                      .Be(RunReportErrKind.ReportIdDoesNotExist);
    }
}