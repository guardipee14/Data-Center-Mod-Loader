using System;
using System.IO;
using DCML.Core;
using DCML.Core.Runtime;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLReleaseVersionTests
{
    [Fact]
    public void V003ReleaseVersion_IsConsistent()
    {
        const string expected =
            "0.0.3";

        Assert.Equal(
            expected,
            DCMLVersion.Current);

        string root =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    ".."));

        string manifestPath =
            Path.Combine(
                root,
                "src",
                "DCML.TestModule",
                "manifest.json");

        string testModulePath =
            Path.Combine(
                root,
                "src",
                "DCML.TestModule",
                "TestModule.cs");

        var result =
            DCMLManifestJson.Deserialize(
                File.ReadAllText(
                    manifestPath));

        Assert.True(
            result.Success);

        Assert.NotNull(
            result.Manifest);

        Assert.Equal(
            expected,
            result.Manifest!.Version);

        Assert.Equal(
            "0.0.1",
            result.Manifest.MinimumDCMLVersion);

        string testModuleSource =
            File.ReadAllText(
                testModulePath);

        Assert.Contains(
            "DCML.Core.DCMLVersion.Current",
            testModuleSource,
            StringComparison.Ordinal);
    }
}