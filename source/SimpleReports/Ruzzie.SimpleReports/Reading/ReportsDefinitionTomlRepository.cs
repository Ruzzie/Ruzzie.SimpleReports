using System;
using System.Collections.Generic;
using System.IO;
using Ruzzie.Common.Types;
using Ruzzie.SimpleReports.Pipelines;
using Ruzzie.SimpleReports.Types;
using TinyToml;
using TinyToml.Types;

namespace Ruzzie.SimpleReports.Reading
{
    public class ReportsDefinitionTomlRepository : IReportsDefinitionRepository
    {
        private readonly Dictionary<string, IReportDefinition> _allReportDefinitions =
            new Dictionary<string, IReportDefinition>(StringComparer.OrdinalIgnoreCase);

        public ReportsDefinitionTomlRepository(string                              filename,
                                               ITypeResolver<IListProvider>        listProviderTypeResolver,
                                               ITypeResolver<IPostProcessPipeline> postProcessPipelineTypeResolver)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(filename));
            }

            var tomlDoc = Toml.Parse(File.ReadAllText(filename));

            foreach (var reportId in tomlDoc.Keys)
            {
                var tomlValue = tomlDoc[reportId];

                if (tomlValue.TryReadTomlTable(out var reportTable))
                {
                    var reportDefinition =
                        ParseReportDefinitionTable(reportId, reportTable, listProviderTypeResolver,
                                                   postProcessPipelineTypeResolver);
                    _allReportDefinitions[reportDefinition.Id] = reportDefinition;
                }
                else
                {
                    throw new
                        InvalidDataException($"Error while parsing {filename}. Each Key in the root document must be a TomlTable that represents a ReportDefinition. But found key:[{tomlValue.Key}] of type: [{tomlValue.TomlType}] with value \"{tomlValue}\".");
                }
            }
        }

        private static ReportDefinition ParseReportDefinitionTable(string                       reportId,
                                                                   TomlTable                    reportTable,
                                                                   ITypeResolver<IListProvider> typeResolver,
                                                                   ITypeResolver<IPostProcessPipeline>
                                                                       postProcessPipelineTypeResolver)
        {
            var id = StringIdExtensions.ToStringId(reportId);

            var report = new ReportDefinition
            {
                Id          = id,
                Title       = reportTable.ReadStringValueOr("title",       string.Empty),
                Description = reportTable.ReadStringValueOr("description", string.Empty),
                Category    = reportTable.ReadStringValueOr("category",    string.Empty),
                Feature     = reportTable.ReadStringValueOr("feature",     string.Empty),
                WithTotals  = reportTable.ReadBoolValueOrDefault("with_totals"),
                QueryText =
                    reportTable.ReadStringValueOrThrow("query",
                                                       $"query not set for Report:[{id}]. 'query' is mandatory for a report. please define the query like 'query = \"select x from y\"'")
            };

            if (reportTable.TryGetValue<TomlTable>("columns", out var columnsTable))
            {
                report.Columns = ParseColumnsTable(columnsTable);
            }

            if (reportTable.TryGetValue<TomlTable>("parameters", out var parametersTable))
            {
                var paramDefinitions = ParseParameterDefinitions(parametersTable, typeResolver);
                report.Parameters = paramDefinitions;
            }

            if (reportTable.TryGetValue<TomlTable>("post-process", out var postProcess) &&
                postProcess.TryGetValue<TomlArray>("pipelines", out var pipelines))
            {
                report.PostProcessPipelines = ParsePostProcessPipelines(pipelines, postProcessPipelineTypeResolver);
            }

            return report;
        }

        private static List<IPostProcessPipelineDefinition> ParsePostProcessPipelines(
            TomlArray                           pipelinesArray,
            ITypeResolver<IPostProcessPipeline> typeResolver)
        {
            var result = new List<IPostProcessPipelineDefinition>(pipelinesArray.Count);

            for (int i = 0; i < pipelinesArray.Count; i++)
            {
                if (pipelinesArray[i] is TomlTable pipelineArrayElement)
                {
                    var postProcessDefOption = ParsePostProcessPipeline(pipelineArrayElement, typeResolver);
                    postProcessDefOption.For(() => { },
                                             value => result.Add(value)
                                            );
                }
            }

            return result;
        }

        private static Option<IPostProcessPipelineDefinition> ParsePostProcessPipeline(
            TomlTable                           pipelineArrayElement,
            ITypeResolver<IPostProcessPipeline> typeResolver
        )
        {
            var typeName = pipelineArrayElement.ReadStringValueOr("pipeline", string.Empty);

            if (pipelineArrayElement.TryGetValue<TomlArray>("params", out var pipelineParams))
            {
                var paramsArray = new string[pipelineParams.Count];

                for (int i = 0; i < pipelineParams.Count; i++)
                {
                    if (pipelineParams[i] is TomlString paramStringValue)
                    {
                        paramsArray[i] = paramStringValue.Value;
                    }
                }

                return new PostProcessPipelineDefinition(typeResolver.GetInstanceForTypeName(typeName), paramsArray);
            }

            return Option.None<IPostProcessPipelineDefinition>();
        }

        private static IReadOnlyDictionary<string, IReportParameterDefinition> ParseParameterDefinitions(
            TomlTable                    parametersTable,
            ITypeResolver<IListProvider> typeResolver)
        {
            var paramDefinitions = new Dictionary<string, IReportParameterDefinition>(StringComparer.OrdinalIgnoreCase);

            foreach (var parameterId in parametersTable.Keys)
            {
                if (parametersTable.TryGetValue<TomlTable>(parameterId, out var paramTable))
                {
                    var def = ParseParameter(parameterId, paramTable, typeResolver);
                    paramDefinitions[parameterId] = def;
                }
            }

            return paramDefinitions;
        }

        private static IReportParameterDefinition ParseParameter(string                       parameterId,
                                                                 TomlTable                    paramTable,
                                                                 ITypeResolver<IListProvider> typeResolver)
        {
            var type          = paramTable.ReadStringValueOr("type", string.Empty);
            var parameterType = Enum.Parse<ParameterType>(type, true);


            var fieldType =
                Enum.Parse<ParameterFieldType>(paramTable.ReadStringValueOr("field_type", string.Empty), true);

            switch (parameterType)
            {
                case ParameterType.DATE_RANGE:
                    return ParseDateRangeParameter(parameterId, paramTable, fieldType, parameterType,
                                                   typeResolver);
                case ParameterType.FILTER_LOOKUP:
                case ParameterType.TIMEZONE:
                case ParameterType.TIME_INTERVAL:

                    return ParseSimpleParameterTypeParameter(parameterId
                                                           , paramTable
                                                           , fieldType
                                                           , parameterType
                                                           , typeResolver);

                case ParameterType.NONE:
                    throw new ArgumentOutOfRangeException(nameof(type), parameterType, "Invalid");
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), parameterType, "Not supported");
            }
        }

        private static DateRangeParameterDefinition ParseDateRangeParameter(string             parameterId,
                                                                            TomlTable          paramTable,
                                                                            ParameterFieldType parameterFieldType,
                                                                            ParameterType      parameterType,
                                                                            ITypeResolver<IListProvider>
                                                                                typeResolver)
        {
            var from                 = paramTable.ReadStringValueOr("from",          string.Empty);
            var to                   = paramTable.ReadStringValueOr("to",            string.Empty);
            var displayName          = paramTable.ReadStringValueOr("display_name",  string.Empty);
            var listProviderTypeName = paramTable.ReadStringValueOr("list_provider", string.Empty);
            var listProviderOption   = Option<IListProvider>.None;

            var @params = ReadTomlArrayAsStringArrayOrEmpty("params", paramTable);

            if (!string.IsNullOrWhiteSpace(listProviderTypeName))
            {
                listProviderOption = typeResolver.GetInstanceForTypeName(listProviderTypeName).AsSome();
            }
            //TODO: Determine if display_name is appropriate for this parameter type

            return new DateRangeParameterDefinition(parameterId
                                                  , displayName
                                                  , from
                                                  , to
                                                  , listProviderOption
                                                  , @params
                                                  , parameterType
                                                  , parameterFieldType);
        }

        private static FilterParameterDefinition ParseSimpleParameterTypeParameter(string             parameterId,
                                                                            TomlTable          paramTable,
                                                                            ParameterFieldType parameterFieldType,
                                                                            ParameterType      parameterType,
                                                                            ITypeResolver<IListProvider>
                                                                                typeResolver)
        {
            var name                 = paramTable.ReadStringValueOr("name",          string.Empty);
            var displayName          = paramTable.ReadStringValueOr("display_name",  string.Empty);
            var listProviderTypeName = paramTable.ReadStringValueOr("list_provider", string.Empty);
            var listProviderOption   = Option<IListProvider>.None;

            var @params = ReadTomlArrayAsStringArrayOrEmpty("params", paramTable);

            if (!string.IsNullOrWhiteSpace(listProviderTypeName))
            {
                listProviderOption = typeResolver.GetInstanceForTypeName(listProviderTypeName).AsSome();
            }

            var def = new FilterParameterDefinition(parameterId
                                                  , displayName
                                                  , name
                                                  , parameterType
                                                  , parameterFieldType
                                                  , listProviderOption
                                                  , @params);
            return def;
        }

        private static string[] ReadTomlArrayAsStringArrayOrEmpty(string key, TomlTable paramTable)
        {
            var @params = Array.Empty<string>();

            if (paramTable.TryGetValue<TomlArray>(key, out var tomlArray))
            {
                var paramsArray = new string[tomlArray.Count];

                for (int i = 0; i < tomlArray.Count; i++)
                {
                    if (tomlArray[i] is TomlString paramStringValue)
                    {
                        paramsArray[i] = paramStringValue.Value;
                    }
                }

                @params = paramsArray;
            }

            return @params;
        }

        private static IReadOnlyDictionary<string, IColumnInfo> ParseColumnsTable(TomlTable columnsTable)
        {
            var columnInfos = new Dictionary<string, IColumnInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var columnName in columnsTable.Keys)
            {
                if (columnsTable.TryGetValue(columnName, out var columnInfoValue))
                {
                    if (columnInfoValue.TryReadTomlTable(out var columnInfoTable))
                    {
                        var type = columnInfoTable.ReadStringValueOr("type", string.Empty);
                        columnInfos[columnName] = new ColumnInfo(columnName, type);
                    }
                }
            }

            return columnInfos;
        }

        public Option<IReportDefinition> GetReportDefinition(string reportId)
        {
            if (string.IsNullOrWhiteSpace(reportId))
            {
                return Option<IReportDefinition>.None;
            }

            if (_allReportDefinitions.TryGetValue(reportId, out var definition))
            {
                return Option.Some(definition);
            }

            return Option<IReportDefinition>.None;
        }

        public IReadOnlyCollection<IReportDefinition> GetAllReportDefinitions()
        {
            return _allReportDefinitions.Values;
        }
    }
}