using System;
using System.IO;
using System.Text.Json;
using DCML.Core.Models;
using DCML.Core.Runtime;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLDiagnosticReportExporterTests
{
    [Fact]
    public void ExportJson_PreservesSafeStructuredContext()
    {
        var snapshot =
            new DCMLDiagnosticsSnapshot(
                new[]
                {
                    new DCMLModuleStatus(
                        "dcml.test.module",
                        "Test Module",
                        "1.2.3",
                        DCMLModuleStatusState.Running
                    )
                },
                new[]
                {
                    new DCMLDiagnosticIssue(
                        DCMLDiagnosticStage.DependencyResolution,
                        DCMLDiagnosticSeverity.Error,
                        "DCML_TEST_DEPENDENCY",
                        "Required dependency is unavailable.",
                        moduleId:
                            "dcml.test.module",
                        dependencyId:
                            "dcml.test.common",
                        requirementId:
                            "runtime.test",
                        exceptionType:
                            "System.InvalidOperationException"
                    )
                }
            );

        string json =
            DCMLDiagnosticReportExporter.ExportJson(
                snapshot,
                new DateTimeOffset(
                    2026,
                    9,
                    4,
                    18,
                    0,
                    0,
                    TimeSpan.Zero
                )
            );

        using JsonDocument document =
            JsonDocument.Parse(
                json
            );

        JsonElement root =
            document.RootElement;

        Assert.Equal(
            "1.0",
            root
                .GetProperty(
                    "schemaVersion"
                )
                .GetString()
        );

        Assert.Equal(
            "dcml.test.module",
            root
                .GetProperty(
                    "modules"
                )[0]
                .GetProperty(
                    "moduleId"
                )
                .GetString()
        );

        Assert.Equal(
            "dcml.test.common",
            root
                .GetProperty(
                    "diagnostics"
                )[0]
                .GetProperty(
                    "dependencyId"
                )
                .GetString()
        );
    }

    [Fact]
    public void ExportJson_OmitsFilesystemSourceFields()
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
                            "Safe message.",
                            packageDirectory:
                                @"C:\Users\Example\Package",
                            manifestPath:
                                @"C:\Users\Example\Package\manifest.json"
                        )
                    }
            );

        string json =
            DCMLDiagnosticReportExporter.ExportJson(
                snapshot
            );

        Assert.DoesNotContain(
            "packageDirectory",
            json,
            StringComparison.OrdinalIgnoreCase
        );

        Assert.DoesNotContain(
            "manifestPath",
            json,
            StringComparison.OrdinalIgnoreCase
        );

        Assert.DoesNotContain(
            @"C:\Users\Example\Package",
            json,
            StringComparison.OrdinalIgnoreCase
        );
    }

    [Fact]
    public void ExportJson_RedactsPathsUsernameAndSaveSelectionInMessages()
    {
        string userName =
            Environment.UserName;

        string message =
            "Failure for " +
            userName +
            " at C:\\Users\\Example\\DCML\\module.dll; " +
            "unix /home/example/dcml/module.dll; " +
            "selected save: MyPrivateSave_2026.dat";

        var snapshot =
            new DCMLDiagnosticsSnapshot(
                diagnostics:
                    new[]
                    {
                        new DCMLDiagnosticIssue(
                            DCMLDiagnosticStage.Runtime,
                            DCMLDiagnosticSeverity.Error,
                            "DCML_TEST_REDACTION",
                            message
                        )
                    }
            );

        string json =
            DCMLDiagnosticReportExporter.ExportJson(
                snapshot
            );

        if (
            !string.IsNullOrWhiteSpace(
                userName
            ) &&
            userName.Length >= 2
        )
        {
            Assert.DoesNotContain(
                userName,
                json,
                StringComparison.OrdinalIgnoreCase
            );
        }

        Assert.DoesNotContain(
            @"C:\Users\Example",
            json,
            StringComparison.OrdinalIgnoreCase
        );

        Assert.DoesNotContain(
            "/home/example",
            json,
            StringComparison.OrdinalIgnoreCase
        );

        Assert.DoesNotContain(
            "MyPrivateSave_2026.dat",
            json,
            StringComparison.OrdinalIgnoreCase
        );

        Assert.Contains(
            "[REDACTED_PATH]",
            json
        );

        Assert.Contains(
            "[REDACTED_SAVE]",
            json
        );
    }

    [Fact]
    public void ExportToFile_WritesPrivacySafeJson()
    {
        string root =
            Path.Combine(
                Path.GetTempPath(),
                "DCML-DiagnosticReport-" +
                Guid.NewGuid().ToString(
                    "N"
                )
            );

        string reportPath =
            Path.Combine(
                root,
                "nested",
                "report.json"
            );

        try
        {
            var snapshot =
                new DCMLDiagnosticsSnapshot(
                    diagnostics:
                        new[]
                        {
                            new DCMLDiagnosticIssue(
                                DCMLDiagnosticStage.Runtime,
                                DCMLDiagnosticSeverity.Warning,
                                "DCML_TEST_FILE",
                                @"Could not read C:\Users\Example\private.txt."
                            )
                        }
                );

            DCMLDiagnosticReportExporter.ExportToFile(
                snapshot,
                reportPath,
                new DateTimeOffset(
                    2026,
                    9,
                    4,
                    18,
                    0,
                    0,
                    TimeSpan.Zero
                )
            );

            Assert.True(
                File.Exists(
                    reportPath
                )
            );

            string json =
                File.ReadAllText(
                    reportPath
                );

            using JsonDocument document =
                JsonDocument.Parse(
                    json
                );

            Assert.Equal(
                "DCML_TEST_FILE",
                document
                    .RootElement
                    .GetProperty(
                        "diagnostics"
                    )[0]
                    .GetProperty(
                        "code"
                    )
                    .GetString()
            );

            Assert.DoesNotContain(
                @"C:\Users\Example",
                json,
                StringComparison.OrdinalIgnoreCase
            );
        }
        finally
        {
            if (
                Directory.Exists(
                    root
                )
            )
            {
                Directory.Delete(
                    root,
                    true
                );
            }
        }
    }

    [Fact]
    public void CreateReport_CopiesSnapshotAndSummarizes()
    {
        var modules =
            new[]
            {
                new DCMLModuleStatus(
                    "dcml.test.module",
                    "Test Module",
                    "1.0.0",
                    DCMLModuleStatusState.Running
                )
            };

        var diagnostics =
            new[]
            {
                new DCMLDiagnosticIssue(
                    DCMLDiagnosticStage.Runtime,
                    DCMLDiagnosticSeverity.Warning,
                    "DCML_TEST_WARNING",
                    "Warning."
                )
            };

        var snapshot =
            new DCMLDiagnosticsSnapshot(
                modules,
                diagnostics
            );

        DCMLDiagnosticReport report =
            DCMLDiagnosticReportExporter.CreateReport(
                snapshot,
                new DateTimeOffset(
                    2026,
                    9,
                    4,
                    18,
                    0,
                    0,
                    TimeSpan.Zero
                )
            );

        Assert.Single(
            report.Modules
        );

        Assert.Single(
            report.Diagnostics
        );

        Assert.Equal(
            1,
            report.RunningModuleCount
        );

        Assert.Equal(
            1,
            report.WarningCount
        );

        Assert.False(
            report.HasErrors
        );
    }
}
