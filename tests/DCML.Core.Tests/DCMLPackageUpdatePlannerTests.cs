using System.Collections.Generic;
using DCML.Core.Models;
using DCML.Core.Runtime;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLPackageUpdatePlannerTests
{
    [Fact]
    public void Plan_SimpleStableUpdate_AddsOneStep()
    {
        DCMLPackageUpdatePlanResult result =
            Plan(
                Installed(("module.a", "1.0.0")),
                Metadata(Meta("module.a", "2.0.0")),
                "module.a");

        Assert.True(result.Success);
        Assert.False(result.RequiresReview);
        Assert.Single(result.Steps);
        Assert.Equal("module.a", result.Steps[0].ModuleId);
    }

    [Fact]
    public void Plan_SameVersion_ProducesNoSteps()
    {
        DCMLPackageUpdatePlanResult result =
            Plan(
                Installed(("module.a", "1.0.0")),
                Metadata(Meta("module.a", "1.0.0")),
                "module.a");

        Assert.True(result.Success);
        Assert.Empty(result.Steps);
    }

    [Fact]
    public void Plan_SatisfiedRequiredDependency_DoesNotAddDependencyStep()
    {
        DCMLPackageUpdatePlanResult result =
            Plan(
                Installed(
                    ("module.a", "1.0.0"),
                    ("module.b", "2.0.0")),
                Metadata(
                    Meta(
                        "module.a",
                        "2.0.0",
                        new DCMLPackageUpdateDependency(
                            "module.b",
                            "2.0.0"))),
                "module.a");

        Assert.True(result.Success);
        Assert.Single(result.Steps);
        Assert.Equal("module.a", result.Steps[0].ModuleId);
    }

    [Fact]
    public void Plan_OutdatedRequiredDependency_AddsDependencyFirst()
    {
        DCMLPackageUpdatePlanResult result =
            Plan(
                Installed(
                    ("module.a", "1.0.0"),
                    ("module.b", "1.0.0")),
                Metadata(
                    Meta(
                        "module.a",
                        "2.0.0",
                        new DCMLPackageUpdateDependency(
                            "module.b",
                            "2.0.0")),
                    Meta(
                        "module.b",
                        "2.1.0")),
                "module.a");

        Assert.True(result.Success);
        Assert.Equal(2, result.Steps.Count);
        Assert.Equal("module.b", result.Steps[0].ModuleId);
        Assert.Equal("module.a", result.Steps[1].ModuleId);
    }

    [Fact]
    public void Plan_MissingRequiredDependency_FailsClosed()
    {
        DCMLPackageUpdatePlanResult result =
            Plan(
                Installed(("module.a", "1.0.0")),
                Metadata(
                    Meta(
                        "module.a",
                        "2.0.0",
                        new DCMLPackageUpdateDependency(
                            "module.b",
                            "1.0.0"))),
                "module.a");

        Assert.False(result.Success);
        Assert.Equal(
            "DCML_UPDATE_PLAN_DEPENDENCY_MISSING",
            Assert.Single(result.Issues).Code);
    }

    [Fact]
    public void Plan_OptionalMissingDependency_DoesNotBlock()
    {
        DCMLPackageUpdatePlanResult result =
            Plan(
                Installed(("module.a", "1.0.0")),
                Metadata(
                    Meta(
                        "module.a",
                        "2.0.0",
                        new DCMLPackageUpdateDependency(
                            "module.optional",
                            "1.0.0",
                            true))),
                "module.a");

        Assert.True(result.Success);
        Assert.Single(result.Steps);
    }

    [Fact]
    public void Plan_OutdatedDependencyWithoutMetadata_FailsClosed()
    {
        DCMLPackageUpdatePlanResult result =
            Plan(
                Installed(
                    ("module.a", "1.0.0"),
                    ("module.b", "1.0.0")),
                Metadata(
                    Meta(
                        "module.a",
                        "2.0.0",
                        new DCMLPackageUpdateDependency(
                            "module.b",
                            "2.0.0"))),
                "module.a");

        Assert.False(result.Success);
        Assert.Equal(
            "DCML_UPDATE_PLAN_DEPENDENCY_METADATA_MISSING",
            Assert.Single(result.Issues).Code);
    }

    [Fact]
    public void Plan_DependencyTargetBelowMinimum_FailsClosed()
    {
        DCMLPackageUpdatePlanResult result =
            Plan(
                Installed(
                    ("module.a", "1.0.0"),
                    ("module.b", "1.0.0")),
                Metadata(
                    Meta(
                        "module.a",
                        "2.0.0",
                        new DCMLPackageUpdateDependency(
                            "module.b",
                            "3.0.0")),
                    Meta(
                        "module.b",
                        "2.5.0")),
                "module.a");

        Assert.False(result.Success);
        Assert.Equal(
            "DCML_UPDATE_PLAN_DEPENDENCY_TARGET_UNSATISFIED",
            Assert.Single(result.Issues).Code);
    }

    [Fact]
    public void Plan_UpdateCycle_FailsClosed()
    {
        DCMLPackageUpdatePlanResult result =
            Plan(
                Installed(
                    ("module.a", "1.0.0"),
                    ("module.b", "1.0.0")),
                Metadata(
                    Meta(
                        "module.a",
                        "2.0.0",
                        new DCMLPackageUpdateDependency(
                            "module.b",
                            "2.0.0")),
                    Meta(
                        "module.b",
                        "2.0.0",
                        new DCMLPackageUpdateDependency(
                            "module.a",
                            "2.0.0"))),
                "module.a");

        Assert.False(result.Success);
        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "DCML_UPDATE_PLAN_DEPENDENCY_CYCLE");
    }

    [Fact]
    public void Plan_DuplicateMetadata_FailsBeforePlanning()
    {
        DCMLPackageUpdatePlanResult result =
            Plan(
                Installed(("module.a", "1.0.0")),
                Metadata(
                    Meta("module.a", "2.0.0"),
                    Meta("MODULE.A", "3.0.0")),
                "module.a");

        Assert.False(result.Success);
        Assert.Equal(
            "DCML_UPDATE_PLAN_DUPLICATE_METADATA_MODULE",
            Assert.Single(result.Issues).Code);
        Assert.Empty(result.Steps);
    }

    [Fact]
    public void Plan_DuplicateInstalledEvidence_FailsBeforePlanning()
    {
        DCMLPackageUpdatePlanResult result =
            Plan(
                Installed(
                    ("module.a", "1.0.0"),
                    ("MODULE.A", "1.1.0")),
                Metadata(Meta("module.a", "2.0.0")),
                "module.a");

        Assert.False(result.Success);
        Assert.Equal(
            "DCML_UPDATE_PLAN_DUPLICATE_INSTALLED_MODULE",
            Assert.Single(result.Issues).Code);
        Assert.Empty(result.Steps);
    }

    [Fact]
    public void Plan_AllowedPrerelease_PropagatesReviewRequirement()
    {
        DCMLPackageUpdatePlanResult result =
            DCMLPackageUpdatePlanner.Plan(
                Installed(("module.a", "1.0.0")),
                Metadata(Meta("module.a", "2.0.0-beta.1")),
                new[] { "module.a" },
                new DCMLPackageVersionPolicyOptions(
                    allowPrerelease: true));

        Assert.True(result.Success);
        Assert.True(result.RequiresReview);
        Assert.Single(result.Steps);
        Assert.Equal(
            DCMLPackageVersionRecommendation.ReviewRequired,
            result.Steps[0].VersionDecision.Recommendation);
    }

    private static DCMLPackageUpdatePlanResult Plan(
        IReadOnlyList<DCMLInstalledPackageVersion> installed,
        IReadOnlyList<DCMLPackageUpdateMetadata> metadata,
        params string[] requested)
    {
        return
            DCMLPackageUpdatePlanner.Plan(
                installed,
                metadata,
                requested);
    }

    private static IReadOnlyList<DCMLInstalledPackageVersion> Installed(
        params (string Id, string Version)[] values)
    {
        var result =
            new List<DCMLInstalledPackageVersion>();

        foreach ((string id, string version) in values)
        {
            result.Add(
                new DCMLInstalledPackageVersion(
                    id,
                    version));
        }

        return result;
    }

    private static IReadOnlyList<DCMLPackageUpdateMetadata> Metadata(
        params DCMLPackageUpdateMetadata[] values)
    {
        return values;
    }

    private static DCMLPackageUpdateMetadata Meta(
        string moduleId,
        string version,
        params DCMLPackageUpdateDependency[] dependencies)
    {
        return
            new DCMLPackageUpdateMetadata(
                "source.test",
                "package-" + moduleId,
                moduleId,
                version,
                dependencies:
                    dependencies);
    }
}
