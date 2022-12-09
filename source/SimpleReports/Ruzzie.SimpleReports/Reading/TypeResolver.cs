using System;
using System.Collections.Generic;

namespace Ruzzie.SimpleReports.Reading;

/// A default TypeResolver that stored the types / instances as a reusable 'singleton' in a Dictionary internally.
public class TypeResolver<T> : ITypeResolver<T>
{
    private readonly Dictionary<string, T> _typeProviderMap;

    public TypeResolver(IEnumerable<T> instances)
    {
        if (ReferenceEquals(instances, null))
        {
            throw new ArgumentNullException(nameof(instances));
        }

        _typeProviderMap = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in instances)
        {
            var fullName = provider?.GetType().FullName;
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                _typeProviderMap[fullName] = provider;
            }
            else
            {
                throw new ArgumentException($"Invalid provider {provider}. FullName from Type is null or empty.");
            }
        }
    }

    public T GetInstanceForTypeName(string typeName)
    {
        return _typeProviderMap[typeName];
    }
}

public static class TypeResolver
{
    public static TypeResolver<T> Create<T>(IEnumerable<T> instances)
    {
        return new TypeResolver<T>(instances);
    }
}