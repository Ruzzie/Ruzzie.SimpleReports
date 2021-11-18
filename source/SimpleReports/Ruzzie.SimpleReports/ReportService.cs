using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ruzzie.Common.Types;
using Ruzzie.SimpleReports.Reading;
using Ruzzie.SimpleReports.Run;
using Ruzzie.SimpleReports.Types;

namespace Ruzzie.SimpleReports
{
    public class ReportService : IReportService
    {
        private readonly        IReportsDefinitionRepository _repository;
        private static readonly ReportDefinition             EmptyReportDefinition = new ReportDefinition();
        private static readonly IListProvider                EmptyListProvider     = new EmptyListProvider();

        public ReportService(IReportsDefinitionRepository repository
        )
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public Result<Err<CreateParameterErrKind>, IReportParameterValue> CreateParameterValue<TValue>(
            string reportId,
            string parameterId,
            TValue value)
        {
            if (ReferenceEquals(value, null))
            {
                return new Err<CreateParameterErrKind>(nameof(value), CreateParameterErrKind.CannotBeNullOrEmpty);
            }

            if (string.IsNullOrWhiteSpace(reportId))
            {
                return new Err<CreateParameterErrKind>(nameof(reportId), CreateParameterErrKind.CannotBeNullOrEmpty);
            }

            if (string.IsNullOrWhiteSpace(parameterId))
            {
                return new Err<CreateParameterErrKind>(nameof(parameterId),
                                                       CreateParameterErrKind.CannotBeNullOrEmpty);
            }

            if (!_repository.GetReportDefinition(reportId).TryGetValue(out var reportDefinition, EmptyReportDefinition))
            {
                return Error($"Report: [{reportId}] not found.", CreateParameterErrKind.ReportIdDoesNotExist);
            }

            if (!reportDefinition.Parameters.TryGetValue(parameterId, out var parameterDefinition))
            {
                return Error($"Parameter: [{parameterId}] not found.",
                             CreateParameterErrKind.ParameterIdDoesNotExist);
            }

            return Ok(parameterDefinition.CreateValue(value));
        }

        public Result<Err<RunReportErrKind, Exception>, Task> RunReport(RunReportContext runContext,
                                                                        Stream          streamToWriteTo)
        {
            try
            {
                //get the report definition
                if (!_repository.GetReportDefinition(runContext.ReportId).TryGetValue(out var reportDefinition, EmptyReportDefinition))
                {
                    return new Err<RunReportErrKind, Exception>($"Report: [{runContext.ReportId}] not found.",
                                                                RunReportErrKind.ReportIdDoesNotExist);
                }

                var ctx            = new ReportQueryCtx(reportDefinition);
                var allQueryParams =  new List<IQueryRunParameter>();

                for (var i = 0; i < runContext.ReportParamValues.Length; i++)
                {
                    var reportParameterValue = runContext.ReportParamValues[i];

                    var createQueryParamResult = reportParameterValue.CreateQueryParameters(ctx);
                    var (isError, isOk) =
                        createQueryParamResult.GetValue(out IQueryRunParameter[]? queryRunParameters, out var innerErr);

                    if (isError)
                    {
                        return new Err<RunReportErrKind, Exception>(innerErr.ErrorKind.ToString() + " : " + innerErr.Message
                                                                  , RunReportErrKind.ParameterError
                                                                  , innerErr);
                    }

                    if (isOk && queryRunParameters != null)
                        allQueryParams.AddRange(queryRunParameters);
                }

                IAsyncQueryResult qr = runContext.QueryRunner.Run(runContext.Args, reportDefinition.QueryText,
                                                                  allQueryParams);

                foreach (var pipelineDefinition in reportDefinition.PostProcessPipelines)
                {
                    qr = pipelineDefinition.Pipeline.Process(pipelineDefinition.ParamsArray, runContext.Args, qr);
                }

                var rrTask = runContext.Writer.Write(qr, streamToWriteTo);

                //Wait
                return rrTask;
            }
            catch (Exception e)
            {
                return new Err<RunReportErrKind, Exception>($"Unexpected exception: {e.Message}",
                                                            RunReportErrKind.Unexpected,
                                                            e);
            }
        }

        public IReadOnlyCollection<IReportDefinitionInfo> GetAllReportDefinitionInfos()
        {
            return _repository.GetAllReportDefinitions();
        }


        public Result<Err<ListParameterValuesErrKind, Exception>, IListParameterValues> ListParameterValues(
            string                                    reportId,
            string                                    parameterId,
            ReadOnlySpan<(string Name, string Value)> args)
        {
            if (string.IsNullOrWhiteSpace(reportId))
            {
                return new Err<ListParameterValuesErrKind, Exception>($"{nameof(reportId)} Cannot be null or empty"
                                                                     ,
                                                                      ListParameterValuesErrKind.CannotBeNullOrEmpty);
            }

            if (string.IsNullOrWhiteSpace(parameterId))
            {
                return new Err<ListParameterValuesErrKind, Exception>($"{nameof(parameterId)} Cannot be null or empty"
                                                                     ,
                                                                      ListParameterValuesErrKind.CannotBeNullOrEmpty);
            }

            if (!_repository.GetReportDefinition(reportId).TryGetValue(out var reportDefinition, EmptyReportDefinition))
            {
                return ErrorE($"Report: [{reportId}] not found.", ListParameterValuesErrKind.ReportIdDoesNotExist);
            }

            if (!reportDefinition.Parameters.TryGetValue(parameterId, out var parameterDefinition))
            {
                return ErrorE($"Parameter: [{parameterId}] not found.",
                              ListParameterValuesErrKind.ParameterIdDoesNotExist);
            }

            if (!parameterDefinition.ListProviderType.TryGetValue(out var listProvider, EmptyListProvider))
            {
                return ErrorE($"Parameter: [{parameterId}] has no list_provider defined.",
                              ListParameterValuesErrKind.ParameterHasNoListProvider);
            }

            try
            {
                var result = new ListParameterValues(reportId, parameterId,
                                                     listProvider.ListParameterValues(parameterDefinition, args));
                return result;
            }
            catch (Exception e)
            {
                return new Err<ListParameterValuesErrKind, Exception>($"Unexpected exception: {e.Message}",
                                                                      ListParameterValuesErrKind.Unexpected,
                                                                      e);
            }
        }

        private static Err<TErr, Exception> ErrorE<TErr>(string msg, TErr errKind) where TErr : Enum
        {
            return new Err<TErr, Exception>(msg, errKind);
        }

        private static Err<TErr> Error<TErr>(string msg, TErr errKind) where TErr : Enum
        {
            return new Err<TErr>(msg, errKind);
        }

        private static Result<Err<CreateParameterErrKind>, TOk> Ok<TOk>(TOk okValue)
        {
            return okValue;
        }
    }
}