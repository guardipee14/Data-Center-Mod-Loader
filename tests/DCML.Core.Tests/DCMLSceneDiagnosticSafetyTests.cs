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
            "RunHardwareSnapshotsAsync("
        })
        {
            Assert.DoesNotContain(
                forbiddenCall,
                callback);
        }
    }
}
