using System.Data.Common;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Ruzzie.SimpleReports.Db;
using Ruzzie.SimpleReports.Pipelines;
using Ruzzie.SimpleReports.Reading;
using Ruzzie.SimpleReports.Types;

namespace Ruzzie.SimpleReports.UnitTests
{
    [TestFixture]
    public class ParameterDefinitionReadTests
    {
        private readonly TypeResolver<IListProvider>        _listProviderTypeResolver;
        private readonly TypeResolver<IPostProcessPipeline> _postProcessPipelineTypeResolver;

        public ParameterDefinitionReadTests()
        {
            var sqlListProvider = new SqlListProvider(_ =>
                                                      {
                                                          var mockConn = new Mock<DbConnection>();
                                                          return mockConn.Object;
                                                      });

            _listProviderTypeResolver = new TypeResolver<IListProvider>(new IListProvider[]
                                                                        {
                                                                            sqlListProvider
                                                                        });
            _postProcessPipelineTypeResolver = new TypeResolver<IPostProcessPipeline>(new IPostProcessPipeline[] { });
        }

        [Test]
        public void WithParamsArrayTest()
        {
            //Arrange

            var repository = new ReportsDefinitionTomlRepository("paramdefinition_params_report.toml"
                                                               , _listProviderTypeResolver
                                                               , _postProcessPipelineTypeResolver);
            //Act
            var reportDefinition = repository.GetReportDefinition("SALES-PER-CHANNEL-TOTAL").UnwrapOr(default);

            //Assert
            reportDefinition.Parameters["periode"]
                            .ParamsArray.Should()
                            .BeEquivalentTo(new[] { "testing one", "testing two" });
        }

        [TestCase("uint64test", ParameterFieldType.U64)]
        [TestCase("int64test",  ParameterFieldType.I64)]
        [TestCase("tag",        ParameterFieldType.U8)]
        public void WithParamFieldTypeTest(string parameterId, ParameterFieldType expectedFieldType)
        {
            //Arrange
            var repository = new ReportsDefinitionTomlRepository("paramdefinition_params_report.toml"
                                                               , _listProviderTypeResolver
                                                               , _postProcessPipelineTypeResolver);

            //Act
            var reportDefinition = repository.GetReportDefinition("SALES-PER-CHANNEL-TOTAL").UnwrapOr(default);

            //Assert
            reportDefinition.Parameters[parameterId]
                            .ParameterFieldType.Should()
                            .Be(expectedFieldType);
        }

        [TestCase("timezone", ParameterType.TIMEZONE)]
        [TestCase("interval", ParameterType.TIME_INTERVAL)]
        [TestCase("tag",      ParameterType.FILTER_LOOKUP)]
        [TestCase("periode",  ParameterType.DATE_RANGE)]
        public void WithParamTypeTest(string parameterId, ParameterType expectedType)
        {
            //Arrange
            var repository = new ReportsDefinitionTomlRepository("paramdefinition_params_report.toml"
                                                               , _listProviderTypeResolver
                                                               , _postProcessPipelineTypeResolver);
            //Act
            var reportDefinition = repository.GetReportDefinition("SALES-PER-CHANNEL-TOTAL").UnwrapOr(default);

            //Assert
            reportDefinition.Parameters[parameterId].Type.Should().Be(expectedType);
        }
    }
}