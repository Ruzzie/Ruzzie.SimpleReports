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

namespace Ruzzie.SimpleReports.UnitTests.Db
{
    [TestFixture]
    public class RunReportAgainstSqlLiteIntegrationTest
    {
        private IReportService _reportService;

        private static readonly SqlReportQueryRunner SqlReportQueryRunner =
            new SqlReportQueryRunner(x => CreateAndOpenConnection()
                                   , Span<KeyValuePair<Type, ColumnDataType>>.Empty);

        private static readonly CsvTypedReportWriter CsvTypedReportWriter = new CsvTypedReportWriter();

        private const string DbFilename = "integrationtest.db";

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
                                                        new SqlListProvider(ps => CreateAndOpenConnection())
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

        private static SqliteConnection CreateAndOpenConnection()
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
                                                        , SqlReportQueryRunner)
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
                Console.WriteLine(csvReader.Context.RawRecord);
                numRows++;
            }

            numRows.Should().Be(1);
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
    }
}