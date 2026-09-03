using System;
using System.IO;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLPersistenceHelperRepositoryTests
{
    [Fact]
    public void Repository_ContainsNet8PersistenceHelper()
    {
        string root =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    ".."));

        string projectPath =
            Path.Combine(
                root,
                "src",
                "DCML.Persistence.Helper",
                "DCML.Persistence.Helper.csproj");

        Assert.True(
            File.Exists(
                projectPath));

        string project =
            File.ReadAllText(
                projectPath);

        Assert.Contains(
            "<TargetFramework>net8.0</TargetFramework>",
            project);

        Assert.Contains(
            "System.Formats.Nrbf",
            project);
    }

    [Fact]
    public void GameSideProjects_DoNotReferencePersistenceHelper()
    {
        string root =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    ".."));

        foreach (
            string relativePath in
            new[]
            {
                Path.Combine(
                    "src",
                    "DCML.DataCenter",
                    "DCML.DataCenter.csproj"),
                Path.Combine(
                    "src",
                    "DCML.TestModule",
                    "DCML.TestModule.csproj"),
                Path.Combine(
                    "src",
                    "DCML.Loader.MelonLoader",
                    "DCML.Loader.MelonLoader.csproj")
            })
        {
            string project =
                File.ReadAllText(
                    Path.Combine(
                        root,
                        relativePath));

            Assert.DoesNotContain(
                "DCML.Persistence.Helper",
                project);

            Assert.DoesNotContain(
                "System.Formats.Nrbf",
                project);

            Assert.DoesNotContain(
                "System.Reflection.Metadata",
                project);
        }
    }
}
