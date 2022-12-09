namespace Ruzzie.SimpleReports.Reading;

public interface ITypeResolver<out T>
{
    //TODO: Refactor to TryGet style: so a proper error can be thrown
    T GetInstanceForTypeName(string typeName);
}