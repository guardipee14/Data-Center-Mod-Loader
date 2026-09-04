using System;
using System.IO;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLTopologyCaptureExamplesTests
{
    [Fact]
    public void Repository_ContainsBuildCheckedTopologyExampleProject()
    {
        string root =
            GetSolutionRoot();

        string project =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "examples",
                    "DCML.DataCenter.TopologyCapture",
                    "DCML.DataCenter.TopologyCapture.csproj"));

        Assert.Contains(
            "<TargetFramework>net6.0</TargetFramework>",
            project,
            StringComparison.Ordinal);

        Assert.Contains(
            @"..\..\src\DCML.Core\DCML.Core.csproj",
            project,
            StringComparison.Ordinal);

        Assert.Contains(
            @"..\..\src\DCML.DataCenter\DCML.DataCenter.csproj",
            project,
            StringComparison.Ordinal);

        Assert.Contains(
            @"..\..\src\DCML.DataCenter.Persistence\DCML.DataCenter.Persistence.csproj",
            project,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Example_CoversLiveAndOptionalPersistenceCapture()
    {
        string source =
            ReadExampleSource();

        Assert.Contains(
            "CaptureLiveAsync(",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "DataCenterApi.Create(",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "CaptureWithOptionalPersistenceAsync(",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "DataCenterProcessCablePersistenceSourceFactory.Create(",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "api.Topology",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            ".CaptureAsync(",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Example_RequiresExplicitSceneSelectionAndUsesEvidenceFields()
    {
        string source =
            ReadExampleSource();

        Assert.Contains(
            "CaptureExplicitSceneAsync(",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "sceneQuery.IncludeSceneObjects",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "sceneQuery.SceneName",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "graph.PhysicalCableEdges",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "edge.IsFullyResolved",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "edge.EvidenceSource",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Example_DoesNotDiscoverSavesOrInferPhysicalTopology()
    {
        string source =
            ReadExampleSource();

        string documentation =
            File.ReadAllText(
                Path.Combine(
                    GetSolutionRoot(),
                    "docs",
                    "TOPOLOGY-CAPTURE-EXAMPLES.md"));

        string combined =
            source +
            Environment.NewLine +
            documentation;

        string[] forbidden =
        {
            "Directory.GetFiles",
            "Directory.EnumerateFiles",
            "OrderByDescending",
            "MaxBy(",
            "newest save",
            "guess the active scene",
            "infer a physical path from object names"
        };

        Assert.DoesNotContain(
            "Directory.GetFiles",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "Directory.EnumerateFiles",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "OrderByDescending",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "MaxBy(",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "does not guess the active scene",
            documentation,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "Do not infer a physical path from object names",
            documentation,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadExampleSource()
    {
        return
            File.ReadAllText(
                Path.Combine(
                    GetSolutionRoot(),
                    "examples",
                    "DCML.DataCenter.TopologyCapture",
                    "TopologyCaptureExamples.cs"));
    }

    private static string GetSolutionRoot()
    {
        DirectoryInfo? directory =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (
                File.Exists(
                    Path.Combine(
                        directory.FullName,
                        "DCML.sln")))
            {
                return
                    directory.FullName;
            }

            directory =
                directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the DCML solution root.");
    }
}
