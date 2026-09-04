using System.Collections.Generic;
using System.Linq;
using DCML.Core.Models;
using DCML.Core.Runtime;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLStatusPanelTextTests
{
    [Fact]
    public void Build_ReportsSummaryAndModuleState()
    {
        var snapshot =
            new DCMLDiagnosticsSnapshot(
                new[]
                {
                    new DCMLModuleStatus(
                        "dcml.test.running",
                        "Running",
                        "1.2.3",
                        DCMLModuleStatusState.Running
                    )
                },
                new[]
                {
                    new DCMLDiagnosticIssue(
                        DCMLDiagnosticStage.Runtime,
                        DCMLDiagnosticSeverity.Warning,
                        "DCML_TEST_WARNING",
                        "Test warning."
                    )
                }
            );

        IReadOnlyList<string> lines =
            DCMLStatusPanelText.Build(
                snapshot
            );

        Assert.Contains(
            "Modules: 1 | Running: 1 | Errors: 0 | Warnings: 1",
            lines
        );

        Assert.Contains(
            "[Running] dcml.test.running v1.2.3",
            lines
        );
    }

    [Fact]
    public void Build_OrdersModulesDeterministically()
    {
        var snapshot =
            new DCMLDiagnosticsSnapshot(
                new[]
                {
                    new DCMLModuleStatus(
                        "dcml.test.zeta",
                        "Zeta",
                        "1.0.0",
                        DCMLModuleStatusState.Running
                    ),
                    new DCMLModuleStatus(
                        "dcml.test.alpha",
                        "Alpha",
                        "1.0.0",
                        DCMLModuleStatusState.Blocked
                    )
                }
            );

        IReadOnlyList<string> lines =
            DCMLStatusPanelText.Build(
                snapshot
            );

        string[] moduleLines =
            lines
                .Where(
                    line =>
                        line.StartsWith(
                            "[",
                            System.StringComparison.Ordinal
                        )
                )
                .ToArray();

        Assert.Equal(
            "[Blocked] dcml.test.alpha v1.0.0",
            moduleLines[0]
        );

        Assert.Equal(
            "[Running] dcml.test.zeta v1.0.0",
            moduleLines[1]
        );
    }

    [Fact]
    public void Build_DoesNotExposePackageOrManifestPaths()
    {
        var snapshot =
            new DCMLDiagnosticsSnapshot(
                diagnostics:
                    new[]
                    {
                        new DCMLDiagnosticIssue(
                            DCMLDiagnosticStage.Discovery,
                            DCMLDiagnosticSeverity.Error,
                            "DCML_TEST_PATH",
                            "Path-safe diagnostic.",
                            moduleId:
                                "dcml.test.module",
                            packageDirectory:
                                @"C:\Users\Example\SecretPackage",
                            manifestPath:
                                @"C:\Users\Example\SecretPackage\manifest.json"
                        )
                    }
            );

        string text =
            string.Join(
                "\n",
                DCMLStatusPanelText.Build(
                    snapshot
                )
            );

        Assert.DoesNotContain(
            @"C:\Users\Example",
            text
        );

        Assert.DoesNotContain(
            "SecretPackage",
            text
        );

        Assert.Contains(
            "DCML_TEST_PATH",
            text
        );
    }

    [Fact]
    public void Build_TruncatesLargeInventoriesAndDiagnostics()
    {
        var modules =
            Enumerable.Range(
                1,
                4
            )
            .Select(
                index =>
                    new DCMLModuleStatus(
                        "dcml.test." +
                        index,
                        "Module " +
                        index,
                        "1.0.0",
                        DCMLModuleStatusState.Running
                    )
            )
            .ToArray();

        var diagnostics =
            Enumerable.Range(
                1,
                3
            )
            .Select(
                index =>
                    new DCMLDiagnosticIssue(
                        DCMLDiagnosticStage.Runtime,
                        DCMLDiagnosticSeverity.Error,
                        "DCML_TEST_" +
                        index,
                        "Diagnostic " +
                        index
                    )
            )
            .ToArray();

        IReadOnlyList<string> lines =
            DCMLStatusPanelText.Build(
                new DCMLDiagnosticsSnapshot(
                    modules,
                    diagnostics
                ),
                maxModules:
                    2,
                maxDiagnostics:
                    1
            );

        Assert.Contains(
            "... 2 more module(s)",
            lines
        );

        Assert.Contains(
            "... 2 more diagnostic(s)",
            lines
        );
    }
}
