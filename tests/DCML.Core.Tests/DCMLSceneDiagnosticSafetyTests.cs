using System;
using System.IO;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLSceneDiagnosticSafetyTests
{
    [Fact]
    public void TestModule_InitializedSceneCallbackDoesNotRunDiagnosticsInline()
    {
        string solutionRoot =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    ".."));

        string sourcePath =
            Path.Combine(
                solutionRoot,
                "src",
                "DCML.TestModule",
                "TestModule.cs");

        Assert.True(
            File.Exists(
                sourcePath),
            "DCML.TestModule TestModule.cs was not found.");

        string source =
            File.ReadAllText(
                sourcePath);

        int callbackStart =
            source.IndexOf(
                "private void OnSceneLifecycleEvent(",
                StringComparison.Ordinal);

        int nextMethodStart =
            source.IndexOf(
                "private void ScheduleAutomaticSceneDiagnostics(",
                callbackStart,
                StringComparison.Ordinal);

        Assert.True(
            callbackStart >= 0 &&
            nextMethodStart > callbackStart,
            "The scene lifecycle callback or deferred scheduler was not found.");

        string callback =
            source.Substring(
                callbackStart,
                nextMethodStart - callbackStart);

        Assert.Contains(
            "EnableAutomaticSceneDiagnostics",
            callback);

        Assert.Contains(
            "ScheduleAutomaticSceneDiagnostics(",
            callback);

        foreach (string forbiddenCall in new[]
        {
            "RunObjectDiscovery(",
            "RunRecommendedDataCenterApi(",
            "RunTargetedSemanticDiscovery(",
            "RunComponentInventory(",
            "RunGameTypeCatalog(",
            "RunGameResourceDiscovery(",
            "RunGameTypeInspection(",
            "RunHardwareSnapshotsAsync(",
            "RunCablePersistenceMetadataProbe("
        })
        {
            Assert.DoesNotContain(
                forbiddenCall,
                callback);
        }
    }

    [Fact]
    public void TestModule_CablePersistenceMetadataProbeUsesMetadataOnlyServices()
    {
        string solutionRoot =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    ".."));

        string sourcePath =
            Path.Combine(
                solutionRoot,
                "src",
                "DCML.TestModule",
                "TestModule.cs");

        Assert.True(
            File.Exists(
                sourcePath),
            "DCML.TestModule TestModule.cs was not found.");

        string source =
            File.ReadAllText(
                sourcePath);

        Assert.Contains(
            "public bool EnableCablePersistenceMetadataProbe { get; set; }",
            source);

        int probeStart =
            source.IndexOf(
                "private void RunCablePersistenceMetadataProbe(",
                StringComparison.Ordinal);

        int writerStart =
            source.IndexOf(
                "private string WriteCablePersistenceMetadata(",
                probeStart,
                StringComparison.Ordinal);

        Assert.True(
            probeStart >= 0 &&
            writerStart > probeStart,
            "The cable persistence metadata probe was not found.");

        string probe =
            source.Substring(
                probeStart,
                writerStart - probeStart);

        Assert.Contains(
            "_gameTypeCatalog.Find(",
            probe);

        Assert.Contains(
            "_gameTypeInspector.Inspect(",
            probe);

        foreach (string forbiddenText in new[]
        {
            "_gameComponentStateReader",
            "_gameObjectDiscovery",
            "_dataCenterApi",
            "ReadAsync(",
            "CaptureAsync(",
            "CollectPatchPanelChainCables",
            "LoadData(",
            "SetUpBase(",
            "SetUpApp("
        })
        {
            Assert.DoesNotContain(
                forbiddenText,
                probe);
        }
    }
}
