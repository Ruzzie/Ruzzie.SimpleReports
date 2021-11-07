using System;
using System.IO;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using Ruzzie.Common.Types;
using Ruzzie.SimpleReports.Pipelines;
using Ruzzie.SimpleReports.Reading;
using Ruzzie.SimpleReports.Run;
using Ruzzie.SimpleReports.Types;
using Ruzzie.SimpleReports.Writers;

namespace Ruzzie.SimpleReports.UnitTests
{
    [TestFixture]
    public class DateFilterTests
    {
        private const string                          ReportId = "SALES-PER-CHANNEL-TOTAL";
        private       ReportService                   _service;
        private       ReportsDefinitionTomlRepository _repository;
        readonly      IReportDataWriter               _writer = new CsvTypedReportWriter();

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            //Arrange
            _repository = new ReportsDefinitionTomlRepository("date_filter_report.toml"
                                                            , new TypeResolver<IListProvider
                                                              >(new IListProvider[] { })
                                                            , new TypeResolver<IPostProcessPipeline
                                                              >(new IPostProcessPipeline[] { }));
            _service = new ReportService(_repository);
        }

        [Test]
        public void RepositoryReadsDateFilter_Success()
        {
            _repository.GetReportDefinition(ReportId).TryGetValue(out var definition).Should().BeTrue();
            definition.Parameters.ContainsKey("periode").Should().BeTrue();
        }

        [Test]
        public void CreateDateFilterParameter_Success()
        {
            //Arrange
            var fromValue = DateTime.UtcNow.Subtract(TimeSpan.FromDays(7));
            var to        = DateTime.UtcNow;
            var dateRangeInputValues =
                new DateRangeInputValues(fromValue, to);
            //Act

            var dateFilterParam = _service
                                  .CreateDataRangeParameterValue(ReportId, "periode", dateRangeInputValues)
                                  .Unwrap();

            //Assert
            var asFilterType = (DateRangeParameterValue)dateFilterParam;
            asFilterType.ParameterId.Should().Be("periode");
            asFilterType.FromValue.Should().Be(fromValue);
            asFilterType.ToValue.Should().Be(to);
        }

        [Test]
        public void RunReportWithDataFilter_Success()
        {
            //Arrange
            var fromValue = DateTime.UtcNow.Subtract(TimeSpan.FromDays(7));
            var to        = DateTime.UtcNow;
            var dateRangeInputValues =
                new DateRangeInputValues(fromValue, to);

            var dateFilterParam = _service
                                  .CreateDataRangeParameterValue(ReportId, "periode", dateRangeInputValues)
                                  .Unwrap();


            IReportQueryRunner runner     = new QueryRunnerStub();
            using var          dataStream = new MemoryStream();
            //Act


            var runTask =
                _service.RunReport(new RunReportContext(ReportId
                                                      , new ReadOnlySpan<(string Name, string Value)>()
                                                      , new[] { dateFilterParam }
                                                      , _writer
                                                      , runner)
                                 , dataStream)
                        .Unwrap();

            runTask.Wait();

            //Assert
            dataStream.Seek(0, SeekOrigin.Begin);
            using var reader     = new StreamReader(dataStream, Encoding.UTF8);
            var       headerLine = reader.ReadLine();

            headerLine.Should().Contain("Test");
            Console.WriteLine(headerLine);

            var    rowCount = 0;
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                rowCount++;
                Console.WriteLine(line);
            }

            rowCount.Should().Be(10);
        }
    }
}