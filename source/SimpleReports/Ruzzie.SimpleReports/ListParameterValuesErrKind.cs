namespace Ruzzie.SimpleReports
{
    public enum ListParameterValuesErrKind
    {
        CannotBeNullOrEmpty,
        ReportIdDoesNotExist,
        ParameterIdDoesNotExist,
        ParameterHasNoListProvider,
        Unexpected
    }
}