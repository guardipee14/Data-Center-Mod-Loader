using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DCML.Core.Abstractions;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLCapabilityApiReferenceTests
{
    private static readonly IReadOnlyDictionary<string, string>
        ExpectedCapabilityServices =
            new Dictionary<string, string>(
                StringComparer.Ordinal)
            {
                [DCMLRuntimeCapabilities.Logging] = "IDCMLLogger",
                [DCMLRuntimeCapabilities.RuntimeInformation] = "IDCMLRuntimeInfo",
                [DCMLRuntimeCapabilities.RuntimeCapabilities] = "IDCMLCapabilityCatalog",
                [DCMLRuntimeCapabilities.Configuration] = "IDCMLConfiguration",
                [DCMLRuntimeCapabilities.Events] = "IDCMLEventBus",
                [DCMLRuntimeCapabilities.GameSceneLifecycle] = "IDCMLGameLifecycle",
                [DCMLRuntimeCapabilities.GameObjectDiscovery] = "IDCMLGameObjectDiscovery",
                [DCMLRuntimeCapabilities.GameTypeCatalog] = "IDCMLGameTypeCatalog",
                [DCMLRuntimeCapabilities.GameResourceDiscovery] = "IDCMLGameResourceDiscovery",
                [DCMLRuntimeCapabilities.GameTypeInspection] = "IDCMLGameTypeInspector",
                [DCMLRuntimeCapabilities.GameMainThread] = "IDCMLGameThread",
                [DCMLRuntimeCapabilities.GameComponentState] = "IDCMLGameComponentStateReader"
            };

    [Fact]
    public void RuntimeCapabilityConstants_MatchCurrentV004Surface()
    {
        string[] actual =
            typeof(DCMLRuntimeCapabilities)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field =>
                    field.IsLiteral &&
                    !field.IsInitOnly &&
                    field.FieldType == typeof(string))
                .Select(field => field.GetRawConstantValue() as string)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        string[] expected =
            ExpectedCapabilityServices.Keys
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ApiReference_DocumentsEveryCapabilityServiceAndVersion()
    {
        string document =
            File.ReadAllText(
                Path.Combine(
                    GetSolutionRoot(),
                    "docs",
                    "API-REFERENCE.md"));

        foreach (KeyValuePair<string, string> pair in ExpectedCapabilityServices)
        {
            Assert.Contains(pair.Key, document, StringComparison.Ordinal);
            Assert.Contains(pair.Value, document, StringComparison.Ordinal);
        }

        Assert.Contains(
            DCMLCapabilityVersions.V1,
            document,
            StringComparison.Ordinal);

        Assert.Contains(
            "Stable capability APIs:",
            document,
            StringComparison.Ordinal);

        Assert.Contains(
            "none declared yet",
            document,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "Provisional capability APIs:",
            document,
            StringComparison.Ordinal);
    }

    [Fact]
    public void MelonHostCapabilities_AdvertisesEveryCurrentCapabilityAtV1()
    {
        string source =
            File.ReadAllText(
                Path.Combine(
                    GetSolutionRoot(),
                    "src",
                    "DCML.Loader.MelonLoader",
                    "MelonHostCapabilities.cs"));

        foreach (
            FieldInfo field in
            typeof(DCMLRuntimeCapabilities)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field =>
                    field.IsLiteral &&
                    !field.IsInitOnly &&
                    field.FieldType == typeof(string)))
        {
            Assert.Contains(
                "DCMLRuntimeCapabilities." + field.Name,
                source,
                StringComparison.Ordinal);
        }

        Assert.Contains(
            "DCMLCapabilityVersions.V1",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void MelonModuleContext_RegistersServiceForEveryCapability()
    {
        string source =
            File.ReadAllText(
                Path.Combine(
                    GetSolutionRoot(),
                    "src",
                    "DCML.Loader.MelonLoader",
                    "MelonModuleContext.cs"));

        foreach (string serviceName in ExpectedCapabilityServices.Values)
        {
            Assert.Contains(
                "typeof(" + serviceName + ")",
                source,
                StringComparison.Ordinal);
        }
    }

    private static string GetSolutionRoot()
    {
        DirectoryInfo? directory =
            new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DCML.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the DCML solution root.");
    }
}
