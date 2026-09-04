using System.Collections.Generic;
using DCML.Core.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLDiagnosticsModelTests
{
    [Fact]
    public void DiagnosticIssue_PreservesStructuredContext()
    {
        var issue =
            new DCMLDiagnosticIssue(
                DCMLDiagnosticStage.DependencyResolution,
                DCMLDiagnosticSeverity.Error,
                "DCML_DEPENDENCY_REQUIRED_MISSING",
                "A required dependency is unavailable.",
                moduleId:
                    "dcml.test.consumer",
                packageDirectory:
                    @"C:\Packages\Consumer",
                manifestPath:
                    @"C:\Packages\Consumer\manifest.json",
                dependencyId:
                    "dcml.test.provider",
                exceptionType:
                    "System.InvalidOperationException"
            );

        Assert.Equal(
            DCMLDiagnosticStage.DependencyResolution,
            issue.Stage
        );

        Assert.Equal(
            DCMLDiagnosticSeverity.Error,
            issue.Severity
        );

        Assert.Equal(
            "DCML_DEPENDENCY_REQUIRED_MISSING",
            issue.Code
        );

        Assert.Equal(
            "dcml.test.consumer",
            issue.ModuleId
        );

        Assert.Equal(
            @"C:\Packages\Consumer",
            issue.PackageDirectory
        );

        Assert.Equal(
            @"C:\Packages\Consumer\manifest.json",
            issue.ManifestPath
        );

        Assert.Equal(
            "dcml.test.provider",
            issue.DependencyId
        );

        Assert.Equal(
            "System.InvalidOperationException",
            issue.ExceptionType
        );
    }

    [Fact]
    public void ModuleStatus_ReportsRunningState()
    {
        var running =
            new DCMLModuleStatus(
                "dcml.test.running",
                "Running Module",
                "1.0.0",
                DCMLModuleStatusState.Running
            );

        var blocked =
            new DCMLModuleStatus(
                "dcml.test.blocked",
                "Blocked Module",
                "1.0.0",
                DCMLModuleStatusState.Blocked
            );

        Assert.True(running.IsRunning);
        Assert.False(blocked.IsRunning);
    }

    [Fact]
    public void DiagnosticsSnapshot_CopiesCollectionsAndSummarizes()
    {
        var modules =
            new List<DCMLModuleStatus>
            {
                new DCMLModuleStatus(
                    "dcml.test.running",
                    "Running",
                    "1.0.0",
                    DCMLModuleStatusState.Running
                ),
                new DCMLModuleStatus(
                    "dcml.test.failed",
                    "Failed",
                    "1.0.0",
                    DCMLModuleStatusState.Failed
                )
            };

        var diagnostics =
            new List<DCMLDiagnosticIssue>
            {
                new DCMLDiagnosticIssue(
                    DCMLDiagnosticStage.Compatibility,
                    DCMLDiagnosticSeverity.Warning,
                    "DCML_TEST_WARNING",
                    "Warning diagnostic."
                ),
                new DCMLDiagnosticIssue(
                    DCMLDiagnosticStage.Start,
                    DCMLDiagnosticSeverity.Error,
                    "DCML_TEST_ERROR",
                    "Error diagnostic.",
                    moduleId:
                        "dcml.test.failed"
                )
            };

        var snapshot =
            new DCMLDiagnosticsSnapshot(
                modules,
                diagnostics
            );

        modules.Clear();
        diagnostics.Clear();

        Assert.Equal(2, snapshot.Modules.Count);
        Assert.Equal(2, snapshot.Diagnostics.Count);
        Assert.Equal(1, snapshot.RunningModuleCount);
        Assert.Equal(0, snapshot.InfoCount);
        Assert.Equal(1, snapshot.WarningCount);
        Assert.Equal(1, snapshot.ErrorCount);
        Assert.True(snapshot.HasErrors);
    }
}
