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
            var sqlListProvider = new SqlListProvider(runParams =>
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

        [Test]
        public void WithParamTypeU8Test()
        {
            //Arrange
            var repository = new ReportsDefinitionTomlRepository("paramdefinition_params_report.toml"
                                                               , _listProviderTypeResolver
                                                               , _postProcessPipelineTypeResolver);
            //Act
            var reportDefinition = repository.GetReportDefinition("SALES-PER-CHANNEL-TOTAL").UnwrapOr(default);

            //Assert
            reportDefinition.Parameters["tag"]
                            .ParameterFieldType.Should()
                            .BeEquivalentTo(ParameterFieldType.U8);
        }
    }
}