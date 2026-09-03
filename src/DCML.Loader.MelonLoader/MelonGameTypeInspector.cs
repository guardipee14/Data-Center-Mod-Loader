using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DCML.Core.Abstractions;
using DCML.Core.Models;

namespace DCML.Loader.MelonLoader;

internal sealed class MelonGameTypeInspector :
    IDCMLGameTypeInspector
{
    private const BindingFlags DeclaredMemberFlags =
        BindingFlags.Public |
        BindingFlags.NonPublic |
        BindingFlags.Instance |
        BindingFlags.Static |
        BindingFlags.DeclaredOnly;

    public DCMLGameTypeInspection? Inspect(
        DCMLGameTypeInspectionQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(
                nameof(query));
        }

        Type? targetType =
            FindType(
                query);

        if (targetType is null)
        {
            return null;
        }

        string assemblyName =
            targetType.Assembly
                .GetName()
                .Name ??
            "unknown";

        IReadOnlyList<string> baseTypeFullNames =
            GetBaseTypeFullNames(
                targetType);

        IReadOnlyList<string> interfaceFullNames =
            GetInterfaceFullNames(
                targetType);

        var members =
            new List<DCMLGameTypeMemberInfo>();

        IReadOnlyList<Type> memberTypes =
            query.IncludeInheritedMembers
                ? GetTypeAndBaseTypes(
                    targetType)
                : new[]
                {
                    targetType
                };

        foreach (
            Type memberType in
            memberTypes
        )
        {
            AddFields(
                targetType,
                memberType,
                members);

            AddProperties(
                targetType,
                memberType,
                members);

            AddMethods(
                targetType,
                memberType,
                members);
        }

        AddConstructors(
            targetType,
            members);

        DCMLGameTypeMemberInfo[] ordered =
            members
                .GroupBy(
                    value =>
                        value.Kind +
                        "\u001f" +
                        value.DeclaringTypeFullName +
                        "\u001f" +
                        value.Signature,
                    StringComparer.Ordinal)
                .Select(
                    group =>
                        group.First())
                .OrderBy(
                    value =>
                        GetKindOrder(
                            value.Kind))
                .ThenBy(
                    value =>
                        value.DeclaringTypeFullName,
                    StringComparer.Ordinal)
                .ThenBy(
                    value =>
                        value.Name,
                    StringComparer.Ordinal)
                .ThenBy(
                    value =>
                        value.Signature,
                    StringComparer.Ordinal)
                .ToArray();

        int totalMemberCount =
            ordered.Length;

        DCMLGameTypeMemberInfo[] bounded =
            ordered
                .Take(
                    query.MaxMembers)
                .ToArray();

        return
            new DCMLGameTypeInspection(
                GetTypeName(
                    targetType),
                assemblyName,
                baseTypeFullNames,
                interfaceFullNames,
                bounded,
                totalMemberCount,
                totalMemberCount >
                    query.MaxMembers);
    }

    private static Type? FindType(
        DCMLGameTypeInspectionQuery query)
    {
        return
            AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(
                    assembly =>
                        query.AssemblyName.Length == 0 ||
                        string.Equals(
                            assembly.GetName().Name,
                            query.AssemblyName,
                            StringComparison.OrdinalIgnoreCase))
                .OrderBy(
                    assembly =>
                        assembly.GetName().Name ??
                        string.Empty,
                    StringComparer.OrdinalIgnoreCase)
                .SelectMany(
                    GetLoadableTypes)
                .FirstOrDefault(
                    type =>
                        string.Equals(
                            GetTypeName(
                                type),
                            query.TypeFullName,
                            StringComparison.OrdinalIgnoreCase));
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

    private static IReadOnlyList<Type> GetTypeAndBaseTypes(
        Type targetType)
    {
        var values =
            new List<Type>();

        for (
            Type? current = targetType;
            current is not null;
            current = current.BaseType)
        {
            values.Add(
                current);
        }

        return
            values;
    }

    private static IReadOnlyList<string> GetBaseTypeFullNames(
        Type targetType)
    {
        var values =
            new List<string>();

        for (
            Type? current = targetType.BaseType;
            current is not null;
            current = current.BaseType)
        {
            values.Add(
                GetTypeName(
                    current));
        }

        return
            values;
    }

    private static IReadOnlyList<string> GetInterfaceFullNames(
        Type targetType)
    {
        try
        {
            return
                targetType
                    .GetInterfaces()
                    .Select(
                        GetTypeName)
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
            return
                Array.Empty<string>();
        }
    }

    private static void AddFields(
        Type targetType,
        Type memberType,
        ICollection<DCMLGameTypeMemberInfo> destination)
    {
        FieldInfo[] fields;

        try
        {
            fields =
                memberType.GetFields(
                    DeclaredMemberFlags);
        }
        catch
        {
            return;
        }

        foreach (
            FieldInfo field in
            fields
        )
        {
            try
            {
                string accessibility =
                    GetFieldAccessibility(
                        field);

                string valueTypeName =
                    GetTypeName(
                        field.FieldType);

                string signature =
                    accessibility +
                    " " +
                    (field.IsStatic
                        ? "static "
                        : string.Empty) +
                    valueTypeName +
                    " " +
                    field.Name;

                destination.Add(
                    new DCMLGameTypeMemberInfo(
                        "field",
                        field.Name,
                        GetTypeName(
                            field.DeclaringType ??
                            memberType),
                        valueTypeName,
                        accessibility,
                        field.IsStatic,
                        false,
                        memberType !=
                            targetType,
                        true,
                        !field.IsInitOnly &&
                            !field.IsLiteral,
                        0,
                        null,
                        signature));
            }
            catch
            {
                // One unusual generated member must not
                // invalidate the rest of the inspection.
            }
        }
    }

    private static void AddProperties(
        Type targetType,
        Type memberType,
        ICollection<DCMLGameTypeMemberInfo> destination)
    {
        PropertyInfo[] properties;

        try
        {
            properties =
                memberType.GetProperties(
                    DeclaredMemberFlags);
        }
        catch
        {
            return;
        }

        foreach (
            PropertyInfo property in
            properties
        )
        {
            try
            {
                MethodInfo? getter =
                    property.GetGetMethod(
                        true);

                MethodInfo? setter =
                    property.GetSetMethod(
                        true);

                MethodInfo? accessor =
                    GetMostVisibleAccessor(
                        getter,
                        setter);

                string accessibility =
                    accessor is null
                        ? "unknown"
                        : GetMethodAccessibility(
                            accessor);

                bool isStatic =
                    (getter?.IsStatic ?? false) ||
                    (setter?.IsStatic ?? false);

                bool isAbstract =
                    (getter?.IsAbstract ?? false) ||
                    (setter?.IsAbstract ?? false);

                IReadOnlyList<DCMLGameParameterInfo> parameters =
                    CreateParameters(
                        property.GetIndexParameters());

                string valueTypeName =
                    GetTypeName(
                        property.PropertyType);

                string parameterList =
                    FormatParameterList(
                        parameters);

                string propertyName =
                    parameters.Count == 0
                        ? property.Name
                        : property.Name +
                            "[" +
                            parameterList +
                            "]";

                string signature =
                    accessibility +
                    " " +
                    (isStatic
                        ? "static "
                        : string.Empty) +
                    valueTypeName +
                    " " +
                    propertyName +
                    " { " +
                    (getter is null
                        ? string.Empty
                        : "get; ") +
                    (setter is null
                        ? string.Empty
                        : "set; ") +
                    "}";

                destination.Add(
                    new DCMLGameTypeMemberInfo(
                        "property",
                        property.Name,
                        GetTypeName(
                            property.DeclaringType ??
                            memberType),
                        valueTypeName,
                        accessibility,
                        isStatic,
                        isAbstract,
                        memberType !=
                            targetType,
                        getter is not null,
                        setter is not null,
                        0,
                        parameters,
                        signature));
            }
            catch
            {
                // Keep inspection best-effort and read-only.
            }
        }
    }

    private static void AddMethods(
        Type targetType,
        Type memberType,
        ICollection<DCMLGameTypeMemberInfo> destination)
    {
        MethodInfo[] methods;

        try
        {
            methods =
                memberType.GetMethods(
                    DeclaredMemberFlags);
        }
        catch
        {
            return;
        }

        foreach (
            MethodInfo method in
            methods
        )
        {
            if (
                method.IsSpecialName &&
                (
                    method.Name.StartsWith(
                        "get_",
                        StringComparison.Ordinal) ||
                    method.Name.StartsWith(
                        "set_",
                        StringComparison.Ordinal) ||
                    method.Name.StartsWith(
                        "add_",
                        StringComparison.Ordinal) ||
                    method.Name.StartsWith(
                        "remove_",
                        StringComparison.Ordinal)
                )
            )
            {
                continue;
            }

            try
            {
                IReadOnlyList<DCMLGameParameterInfo> parameters =
                    CreateParameters(
                        method.GetParameters());

                int genericArgumentCount =
                    method.IsGenericMethod
                        ? method.GetGenericArguments().Length
                        : 0;

                string methodName =
                    method.Name +
                    (
                        genericArgumentCount == 0
                            ? string.Empty
                            : "<" +
                                genericArgumentCount +
                                ">"
                    );

                string accessibility =
                    GetMethodAccessibility(
                        method);

                string valueTypeName =
                    GetTypeName(
                        method.ReturnType);

                string signature =
                    accessibility +
                    " " +
                    (method.IsStatic
                        ? "static "
                        : string.Empty) +
                    (method.IsAbstract
                        ? "abstract "
                        : string.Empty) +
                    valueTypeName +
                    " " +
                    methodName +
                    "(" +
                    FormatParameterList(
                        parameters) +
                    ")";

                destination.Add(
                    new DCMLGameTypeMemberInfo(
                        "method",
                        method.Name,
                        GetTypeName(
                            method.DeclaringType ??
                            memberType),
                        valueTypeName,
                        accessibility,
                        method.IsStatic,
                        method.IsAbstract,
                        memberType !=
                            targetType,
                        false,
                        false,
                        genericArgumentCount,
                        parameters,
                        signature));
            }
            catch
            {
                // Keep inspection best-effort and read-only.
            }
        }
    }

    private static void AddConstructors(
        Type targetType,
        ICollection<DCMLGameTypeMemberInfo> destination)
    {
        var constructors =
            new List<ConstructorInfo>();

        try
        {
            constructors.AddRange(
                targetType.GetConstructors(
                    DeclaredMemberFlags));
        }
        catch
        {
            // Continue and try the static initializer.
        }

        try
        {
            if (
                targetType.TypeInitializer is
                    ConstructorInfo typeInitializer &&
                constructors.All(
                    value =>
                        value != typeInitializer)
            )
            {
                constructors.Add(
                    typeInitializer);
            }
        }
        catch
        {
            // A missing/unusual type initializer is harmless.
        }

        foreach (
            ConstructorInfo constructor in
            constructors
        )
        {
            try
            {
                IReadOnlyList<DCMLGameParameterInfo> parameters =
                    CreateParameters(
                        constructor.GetParameters());

                string accessibility =
                    GetMethodAccessibility(
                        constructor);

                string signature =
                    accessibility +
                    " " +
                    (constructor.IsStatic
                        ? "static "
                        : string.Empty) +
                    targetType.Name +
                    "(" +
                    FormatParameterList(
                        parameters) +
                    ")";

                destination.Add(
                    new DCMLGameTypeMemberInfo(
                        "constructor",
                        constructor.IsStatic
                            ? ".cctor"
                            : ".ctor",
                        GetTypeName(
                            targetType),
                        string.Empty,
                        accessibility,
                        constructor.IsStatic,
                        false,
                        false,
                        false,
                        false,
                        0,
                        parameters,
                        signature));
            }
            catch
            {
                // Keep inspection best-effort and read-only.
            }
        }
    }

    private static IReadOnlyList<DCMLGameParameterInfo> CreateParameters(
        IEnumerable<ParameterInfo> parameters)
    {
        var values =
            new List<DCMLGameParameterInfo>();

        foreach (
            ParameterInfo parameter in
            parameters)
        {
            try
            {
                Type parameterType =
                    parameter.ParameterType;

                values.Add(
                    new DCMLGameParameterInfo(
                        Math.Max(
                            0,
                            parameter.Position),
                        parameter.Name,
                        GetTypeName(
                            parameterType),
                        parameter.IsOptional,
                        parameter.IsOut,
                        parameterType.IsByRef));
            }
            catch
            {
                // Skip a malformed generated parameter.
            }
        }

        return
            values
                .OrderBy(
                    value =>
                        value.Position)
                .ToArray();
    }

    private static string FormatParameterList(
        IReadOnlyList<DCMLGameParameterInfo> parameters)
    {
        return
            string.Join(
                ", ",
                parameters.Select(
                    parameter =>
                        (
                            parameter.IsOut
                                ? "out "
                                : parameter.IsByRef
                                    ? "ref "
                                    : string.Empty
                        ) +
                        parameter.TypeFullName +
                        " " +
                        parameter.Name +
                        (
                            parameter.IsOptional
                                ? " = optional"
                                : string.Empty
                        )));
    }

    private static MethodInfo? GetMostVisibleAccessor(
        MethodInfo? left,
        MethodInfo? right)
    {
        if (left is null)
        {
            return right;
        }

        if (right is null)
        {
            return left;
        }

        return
            GetAccessibilityRank(
                left) >=
            GetAccessibilityRank(
                right)
                ? left
                : right;
    }

    private static int GetAccessibilityRank(
        MethodBase method)
    {
        if (method.IsPublic)
        {
            return 6;
        }

        if (method.IsFamilyOrAssembly)
        {
            return 5;
        }

        if (method.IsFamily)
        {
            return 4;
        }

        if (method.IsAssembly)
        {
            return 3;
        }

        if (method.IsFamilyAndAssembly)
        {
            return 2;
        }

        if (method.IsPrivate)
        {
            return 1;
        }

        return 0;
    }

    private static string GetMethodAccessibility(
        MethodBase method)
    {
        if (method.IsPublic)
        {
            return "public";
        }

        if (method.IsFamilyOrAssembly)
        {
            return "protected-internal";
        }

        if (method.IsFamily)
        {
            return "protected";
        }

        if (method.IsAssembly)
        {
            return "internal";
        }

        if (method.IsFamilyAndAssembly)
        {
            return "private-protected";
        }

        if (method.IsPrivate)
        {
            return "private";
        }

        return "unknown";
    }

    private static string GetFieldAccessibility(
        FieldInfo field)
    {
        if (field.IsPublic)
        {
            return "public";
        }

        if (field.IsFamilyOrAssembly)
        {
            return "protected-internal";
        }

        if (field.IsFamily)
        {
            return "protected";
        }

        if (field.IsAssembly)
        {
            return "internal";
        }

        if (field.IsFamilyAndAssembly)
        {
            return "private-protected";
        }

        if (field.IsPrivate)
        {
            return "private";
        }

        return "unknown";
    }

    private static string GetTypeName(
        Type type)
    {
        if (type.IsByRef)
        {
            Type elementType =
                type.GetElementType() ??
                type;

            return
                GetTypeName(
                    elementType) +
                "&";
        }

        if (type.IsPointer)
        {
            Type elementType =
                type.GetElementType() ??
                type;

            return
                GetTypeName(
                    elementType) +
                "*";
        }

        if (type.IsArray)
        {
            Type elementType =
                type.GetElementType() ??
                type;

            return
                GetTypeName(
                    elementType) +
                "[]";
        }

        return
            type.FullName ??
            type.Name;
    }

    private static int GetKindOrder(
        string kind)
    {
        switch (kind)
        {
            case "constructor":
                return 0;

            case "field":
                return 1;

            case "property":
                return 2;

            case "method":
                return 3;

            default:
                return 4;
        }
    }
}
