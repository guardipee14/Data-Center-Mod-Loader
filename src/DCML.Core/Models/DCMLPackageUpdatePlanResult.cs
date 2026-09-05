using System.Collections.Generic;

namespace DCML.Core.Models;

/// <summary>
/// Contains a dependency-aware update plan and any blocking planning issues.
/// </summary>
public sealed class DCMLPackageUpdatePlanResult
{
    private readonly List<DCMLPackageUpdatePlanStep> _steps =
        new List<DCMLPackageUpdatePlanStep>();

    private readonly List<DCMLPackageUpdatePlanIssue> _issues =
        new List<DCMLPackageUpdatePlanIssue>();

    public IReadOnlyList<DCMLPackageUpdatePlanStep> Steps =>
        _steps;

    public IReadOnlyList<DCMLPackageUpdatePlanIssue> Issues =>
        _issues;

    public bool Success =>
        _issues.Count == 0;

    public bool RequiresReview { get; internal set; }

    internal void AddStep(
        DCMLPackageUpdatePlanStep step)
    {
        _steps.Add(step);

        if (
            step.VersionDecision.Recommendation ==
            DCMLPackageVersionRecommendation.ReviewRequired)
        {
            RequiresReview =
                true;
        }
    }

    internal void AddIssue(
        DCMLPackageUpdatePlanIssue issue)
    {
        _issues.Add(issue);
    }
}
