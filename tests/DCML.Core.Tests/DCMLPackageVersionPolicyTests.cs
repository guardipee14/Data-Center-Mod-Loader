using DCML.Core.Models;
using DCML.Core.Runtime;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLPackageVersionPolicyTests
{
    [Fact]
    public void Evaluate_StableUpgrade_IsRecommended()
    {
        DCMLPackageVersionDecision decision =
            DCMLPackageVersionPolicy.Evaluate(
                "1.2.3",
                "1.3.0");

        Assert.Equal(
            DCMLPackageVersionTransition.Upgrade,
            decision.Transition);

        Assert.Equal(
            DCMLPackageVersionChannelTransition.StableToStable,
            decision.ChannelTransition);

        Assert.Equal(
            DCMLPackageVersionRecommendation.Recommended,
            decision.Recommendation);
    }

    [Fact]
    public void Evaluate_SamePrecedenceWithDifferentBuildMetadata_IsNoAction()
    {
        DCMLPackageVersionDecision decision =
            DCMLPackageVersionPolicy.Evaluate(
                "1.2.3+build.1",
                "1.2.3+build.2");

        Assert.Equal(
            DCMLPackageVersionTransition.Same,
            decision.Transition);

        Assert.Equal(
            DCMLPackageVersionRecommendation.NoAction,
            decision.Recommendation);
    }

    [Fact]
    public void Evaluate_Downgrade_IsBlockedByDefault()
    {
        DCMLPackageVersionDecision decision =
            DCMLPackageVersionPolicy.Evaluate(
                "2.0.0",
                "1.9.0");

        Assert.Equal(
            DCMLPackageVersionTransition.Downgrade,
            decision.Transition);

        Assert.Equal(
            DCMLPackageVersionRecommendation.Blocked,
            decision.Recommendation);
    }

    [Fact]
    public void Evaluate_AllowedDowngrade_RequiresReview()
    {
        DCMLPackageVersionDecision decision =
            DCMLPackageVersionPolicy.Evaluate(
                "2.0.0",
                "1.9.0",
                new DCMLPackageVersionPolicyOptions(
                    allowDowngrade: true));

        Assert.Equal(
            DCMLPackageVersionRecommendation.ReviewRequired,
            decision.Recommendation);

        Assert.Equal(
            "DCML_VERSION_DOWNGRADE_REVIEW_REQUIRED",
            decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_StableToPrerelease_IsBlockedByDefault()
    {
        DCMLPackageVersionDecision decision =
            DCMLPackageVersionPolicy.Evaluate(
                "1.0.0",
                "2.0.0-beta.1");

        Assert.Equal(
            DCMLPackageVersionChannelTransition.StableToPrerelease,
            decision.ChannelTransition);

        Assert.Equal(
            DCMLPackageVersionRecommendation.Blocked,
            decision.Recommendation);
    }

    [Fact]
    public void Evaluate_AllowedStableToPrerelease_RequiresReview()
    {
        DCMLPackageVersionDecision decision =
            DCMLPackageVersionPolicy.Evaluate(
                "1.0.0",
                "2.0.0-beta.1",
                new DCMLPackageVersionPolicyOptions(
                    allowPrerelease: true));

        Assert.Equal(
            DCMLPackageVersionChannelTransition.StableToPrerelease,
            decision.ChannelTransition);

        Assert.Equal(
            DCMLPackageVersionRecommendation.ReviewRequired,
            decision.Recommendation);
    }

    [Fact]
    public void Evaluate_PrereleaseToPrerelease_RequiresOptInAndReview()
    {
        DCMLPackageVersionDecision blocked =
            DCMLPackageVersionPolicy.Evaluate(
                "2.0.0-beta.1",
                "2.0.0-beta.2");

        DCMLPackageVersionDecision allowed =
            DCMLPackageVersionPolicy.Evaluate(
                "2.0.0-beta.1",
                "2.0.0-beta.2",
                new DCMLPackageVersionPolicyOptions(
                    allowPrerelease: true));

        Assert.Equal(
            DCMLPackageVersionChannelTransition.PrereleaseToPrerelease,
            blocked.ChannelTransition);

        Assert.Equal(
            DCMLPackageVersionRecommendation.Blocked,
            blocked.Recommendation);

        Assert.Equal(
            DCMLPackageVersionRecommendation.ReviewRequired,
            allowed.Recommendation);
    }

    [Fact]
    public void Evaluate_PrereleaseToStableUpgrade_IsRecommended()
    {
        DCMLPackageVersionDecision decision =
            DCMLPackageVersionPolicy.Evaluate(
                "2.0.0-rc.1",
                "2.0.0");

        Assert.Equal(
            DCMLPackageVersionChannelTransition.PrereleaseToStable,
            decision.ChannelTransition);

        Assert.Equal(
            DCMLPackageVersionRecommendation.Recommended,
            decision.Recommendation);
    }

    [Fact]
    public void Evaluate_InvalidCurrentVersion_IsBlocked()
    {
        DCMLPackageVersionDecision decision =
            DCMLPackageVersionPolicy.Evaluate(
                "not-a-version",
                "1.0.0");

        Assert.Equal(
            DCMLPackageVersionTransition.Invalid,
            decision.Transition);

        Assert.Equal(
            "DCML_VERSION_CURRENT_INVALID",
            decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_InvalidAvailableVersion_IsBlocked()
    {
        DCMLPackageVersionDecision decision =
            DCMLPackageVersionPolicy.Evaluate(
                "1.0.0",
                "latest");

        Assert.Equal(
            DCMLPackageVersionTransition.Invalid,
            decision.Transition);

        Assert.Equal(
            "DCML_VERSION_AVAILABLE_INVALID",
            decision.ReasonCode);
    }
}
