using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DCML.Core.Abstractions;
using DCML.Core.Models;

namespace DCML.Loader.MelonLoader;

internal sealed class MelonGameTypeCatalog :
    IDCMLGameTypeCatalog
{
    public IReadOnlyList<DCMLGameTypeInfo> Find(
        DCMLGameTypeQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(
                nameof(query));
        }

        var matches =
            new List<DCMLGameTypeInfo>();

        foreach (
            Assembly assembly in
            AppDomain.CurrentDomain.GetAssemblies())
        {
            string assemblyName =
                assembly.GetName().Name ??
                string.Empty;

            if (
                query.AssemblyName.Length > 0 &&
                !string.Equals(
                    assemblyName,
                    query.AssemblyName,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                continue;
            }

            foreach (
                Type type in
                GetLoadableTypes(
                    assembly))
            {
                DCMLGameTypeInfo info;

                try
                {
                    info =
                        CreateInfo(
                            type,
                            assemblyName);
                }
                catch
                {
                    // One unusual generated type must not
                    // invalidate a read-only runtime catalog.
                    continue;
                }

                if (
                    query.FullNameStartsWith.Length > 0 &&
                    !info.FullName.StartsWith(
                        query.FullNameStartsWith,
                        StringComparison.OrdinalIgnoreCase)
                )
                {
                    continue;
                }

                if (
                    query.NameContains.Length > 0 &&
                    info.FullName.IndexOf(
                        query.NameContains,
                        StringComparison.OrdinalIgnoreCase) < 0
                )
                {
                    continue;
                }

                matches.Add(
                    info);
            }
        }

        return
            matches
                .OrderBy(
                    value =>
                        value.FullName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    value =>
                        value.AssemblyName,
                    StringComparer.OrdinalIgnoreCase)
                .Take(
                    query.MaxResults)
                .ToArray();
    }

    private static IEnumerable<Type> GetLoadableTypes(
        Assembly assembly)
    {
        try
        {
            return
                assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return
                exception.Types
                    .Where(
                        value =>
                            value is not null)
                    .Cast<Type>()
                    .ToArray();
        }
        catch
        {
            return
                Array.Empty<Type>();
        }
    }

    private static DCMLGameTypeInfo CreateInfo(
        Type type,
        string assemblyName)
    {
        string fullName =
            type.FullName ??
            type.Name;

        string baseTypeFullName =
            type.BaseType?.FullName ??
            type.BaseType?.Name ??
            string.Empty;

        IReadOnlyList<string> interfaces;

        try
        {
            interfaces =
                type
                    .GetInterfaces()
                    .Select(
                        value =>
                            value.FullName ??
                            value.Name)
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value))
                    .Distinct(
                        StringComparer.Ordinal)
                    .OrderBy(
                        value =>
                            value,
                        StringComparer.Ordinal)
                    .ToArray();
        }
        catch
        {
            interfaces =
                Array.Empty<string>();
        }

        return
            new DCMLGameTypeInfo(
                fullName,
                type.Namespace,
                type.Name,
                assemblyName.Length == 0
                    ? "unknown"
                    : assemblyName,
                baseTypeFullName,
                type.IsClass,
                type.IsInterface,
                type.IsEnum,
                type.IsValueType,
                type.IsAbstract,
                interfaces);
    }
}
