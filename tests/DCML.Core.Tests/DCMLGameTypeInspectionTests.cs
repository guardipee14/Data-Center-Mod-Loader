using System;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLGameTypeInspectionTests
{
    [Fact]
    public void Query_RequiresTypeName()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new DCMLGameTypeInspectionQuery(
                    " "));
    }

    [Fact]
    public void Query_UsesSafeDefaults()
    {
        var query =
            new DCMLGameTypeInspectionQuery(
                "Il2Cpp.Server");

        Assert.Equal(
            "Il2Cpp.Server",
            query.TypeFullName);

        Assert.Equal(
            string.Empty,
            query.AssemblyName);

        Assert.True(
            query.IncludeInheritedMembers);

        Assert.Equal(
            DCMLGameTypeInspectionQuery.DefaultMaxMembers,
            query.MaxMembers);
    }

    [Fact]
    public void Query_NormalizesAssemblyName()
    {
        var query =
            new DCMLGameTypeInspectionQuery(
                " Il2Cpp.Server ",
                " Assembly-CSharp ");

        Assert.Equal(
            "Il2Cpp.Server",
            query.TypeFullName);

        Assert.Equal(
            "Assembly-CSharp",
            query.AssemblyName);
    }

    [Fact]
    public void Query_RejectsZeroMemberLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DCMLGameTypeInspectionQuery(
                    "Il2Cpp.Server",
                    maxMembers:
                        0));
    }

    [Fact]
    public void Query_RejectsTooLargeMemberLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DCMLGameTypeInspectionQuery(
                    "Il2Cpp.Server",
                    maxMembers:
                        DCMLGameTypeInspectionQuery.MaximumMaxMembers +
                        1));
    }

    [Fact]
    public void Parameter_NormalizesMissingName()
    {
        var parameter =
            new DCMLGameParameterInfo(
                2,
                null,
                "System.String",
                false,
                false,
                false);

        Assert.Equal(
            "arg2",
            parameter.Name);

        Assert.Equal(
            "System.String",
            parameter.TypeFullName);
    }

    [Fact]
    public void Member_PreservesSignatureAndParameters()
    {
        var parameter =
            new DCMLGameParameterInfo(
                0,
                "port",
                "System.Int32",
                false,
                false,
                false);

        var member =
            new DCMLGameTypeMemberInfo(
                "method",
                "Connect",
                "Il2Cpp.Server",
                "System.Boolean",
                "public",
                false,
                false,
                false,
                false,
                false,
                0,
                new[]
                {
                    parameter
                },
                "public System.Boolean Connect(System.Int32 port)");

        Assert.Equal(
            "method",
            member.Kind);

        Assert.Single(
            member.Parameters);

        Assert.Equal(
            "System.Boolean",
            member.ValueTypeFullName);

        Assert.Contains(
            "Connect",
            member.Signature);
    }

    [Fact]
    public void Inspection_PreservesBaseChainOrder()
    {
        var inspection =
            new DCMLGameTypeInspection(
                "Il2Cpp.Router",
                "Assembly-CSharp",
                new[]
                {
                    "Il2Cpp.NetworkSwitch",
                    "Il2Cpp.UsableObject"
                },
                null,
                null,
                0,
                false);

        Assert.Equal(
            new[]
            {
                "Il2Cpp.NetworkSwitch",
                "Il2Cpp.UsableObject"
            },
            inspection.BaseTypeFullNames);
    }

    [Fact]
    public void Inspection_NormalizesInterfaces()
    {
        var inspection =
            new DCMLGameTypeInspection(
                "Il2Cpp.Server",
                "Assembly-CSharp",
                null,
                new[]
                {
                    " Il2Cpp.ITimedDevice ",
                    "Il2Cpp.INetworkEndpoint",
                    "Il2Cpp.ITimedDevice"
                },
                null,
                0,
                false);

        Assert.Equal(
            new[]
            {
                "Il2Cpp.INetworkEndpoint",
                "Il2Cpp.ITimedDevice"
            },
            inspection.InterfaceFullNames);
    }

    [Fact]
    public void Inspection_ExposesMemberGroups()
    {
        var constructor =
            new DCMLGameTypeMemberInfo(
                "constructor",
                ".ctor",
                "Il2Cpp.Server",
                string.Empty,
                "public",
                false,
                false,
                false,
                false,
                false,
                0,
                null,
                "public Server()");

        var field =
            new DCMLGameTypeMemberInfo(
                "field",
                "Power",
                "Il2Cpp.Server",
                "System.Single",
                "public",
                false,
                false,
                false,
                true,
                true,
                0,
                null,
                "public System.Single Power");

        var property =
            new DCMLGameTypeMemberInfo(
                "property",
                "Enabled",
                "Il2Cpp.Server",
                "System.Boolean",
                "public",
                false,
                false,
                false,
                true,
                true,
                0,
                null,
                "public System.Boolean Enabled { get; set; }");

        var method =
            new DCMLGameTypeMemberInfo(
                "method",
                "Tick",
                "Il2Cpp.Server",
                "System.Void",
                "public",
                false,
                false,
                false,
                false,
                false,
                0,
                null,
                "public System.Void Tick()");

        var inspection =
            new DCMLGameTypeInspection(
                "Il2Cpp.Server",
                "Assembly-CSharp",
                null,
                null,
                new[]
                {
                    constructor,
                    field,
                    property,
                    method
                },
                4,
                false);

        Assert.Single(
            inspection.Constructors);

        Assert.Single(
            inspection.Fields);

        Assert.Single(
            inspection.Properties);

        Assert.Single(
            inspection.Methods);
    }

    [Fact]
    public void Inspection_PreservesLimitMetadata()
    {
        var inspection =
            new DCMLGameTypeInspection(
                "Il2Cpp.Server",
                "Assembly-CSharp",
                null,
                null,
                null,
                5000,
                true);

        Assert.Equal(
            5000,
            inspection.TotalMemberCount);

        Assert.True(
            inspection.AtMemberLimit);
    }

    [Fact]
    public void Capability_HasStableIdentifier()
    {
        Assert.Equal(
            "dcml.game.type-inspection",
            DCMLRuntimeCapabilities.GameTypeInspection);
    }
}
