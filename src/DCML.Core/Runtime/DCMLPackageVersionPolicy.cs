using DCML.Core.Models;

namespace DCML.Core.Runtime;

/// <summary>
/// Evaluates semantic-version transitions without staging or mutating package
/// state.
/// </summary>
public static class DCMLPackageVersionPolicy
{
    public static DCMLPackageVersionDecision Evaluate(
        string? currentVersion,
        string? availableVersion,
        DCMLPackageVersionPolicyOptions? options = null)
    {
        string current =
            currentVersion?.Trim() ??
            string.Empty;

        string available =
            availableVersion?.Trim() ??
            string.Empty;

        DCMLPackageVersionPolicyOptions effectiveOptions =
            options ??
            DCMLPackageVersionPolicyOptions.SafeDefault;

        if (!DCMLSemanticVersion.IsValid(current))
        {
            return Decision(
                current,
                available,
                DCMLPackageVersionTransition.Invalid,
                GetChannelTransition(current, available),
                DCMLPackageVersionRecommendation.Blocked,
                "DCML_VERSION_CURRENT_INVALID",
                "The current package version is not a valid Semantic Versioning 2.0.0 value.");
        }

        if (!DCMLSemanticVersion.IsValid(available))
        {
            return Decision(
                current,
                available,
                DCMLPackageVersionTransition.Invalid,
                GetChannelTransition(current, available),
                DCMLPackageVersionRecommendation.Blocked,
                "DCML_VERSION_AVAILABLE_INVALID",
                "The available package version is not a valid Semantic Versioning 2.0.0 value.");
        }

        if (!DCMLSemanticVersion.TryCompare(
            available,
            current,
            out int comparison))
        {
            return Decision(
                current,
                available,
                DCMLPackageVersionTransition.Invalid,
                GetChannelTransition(current, available),
                DCMLPackageVersionRecommendation.Blocked,
                "DCML_VERSION_COMPARE_FAILED",
                "The package versions could not be compared safely.");
        }

        DCMLPackageVersionChannelTransition channelTransition =
            GetChannelTransition(
                current,
                available);

        if (comparison == 0)
        {
            return Decision(
                current,
                available,
                DCMLPackageVersionTransition.Same,
                channelTransition,
                DCMLPackageVersionRecommendation.NoAction,
                "DCML_VERSION_SAME_PRECEDENCE",
                "The current and available versions have the same semantic-version precedence.");
        }

        if (comparison < 0)
        {
            if (!effectiveOptions.AllowDowngrade)
            {
                return Decision(
                    current,
                    available,
                    DCMLPackageVersionTransition.Downgrade,
                    channelTransition,
                    DCMLPackageVersionRecommendation.Blocked,
                    "DCML_VERSION_DOWNGRADE_BLOCKED",
                    "The available version is older than the current version and downgrades are disabled.");
            }

            return Decision(
                current,
                available,
                DCMLPackageVersionTransition.Downgrade,
                channelTransition,
                DCMLPackageVersionRecommendation.ReviewRequired,
                "DCML_VERSION_DOWNGRADE_REVIEW_REQUIRED",
                "The available version is older than the current version. The policy permits downgrade consideration, but explicit review is required.");
        }

        if (IsPrerelease(available))
        {
            if (!effectiveOptions.AllowPrerelease)
            {
                return Decision(
                    current,
                    available,
                    DCMLPackageVersionTransition.Upgrade,
                    channelTransition,
                    DCMLPackageVersionRecommendation.Blocked,
                    "DCML_VERSION_PRERELEASE_BLOCKED",
                    "The available version is newer but is a prerelease target and prerelease transitions are disabled.");
            }

            return Decision(
                current,
                available,
                DCMLPackageVersionTransition.Upgrade,
                channelTransition,
                DCMLPackageVersionRecommendation.ReviewRequired,
                "DCML_VERSION_PRERELEASE_REVIEW_REQUIRED",
                "The available version is newer and targets a prerelease channel. Explicit review is required.");
        }

        return Decision(
            current,
            available,
            DCMLPackageVersionTransition.Upgrade,
            channelTransition,
            DCMLPackageVersionRecommendation.Recommended,
            "DCML_VERSION_UPGRADE_RECOMMENDED",
            "The available version has higher semantic-version precedence and is a stable target.");
    }

    private static DCMLPackageVersionDecision Decision(
        string currentVersion,
        string availableVersion,
        DCMLPackageVersionTransition transition,
        DCMLPackageVersionChannelTransition channelTransition,
        DCMLPackageVersionRecommendation recommendation,
        string reasonCode,
        string reason)
    {
        return new DCMLPackageVersionDecision(
            currentVersion,
            availableVersion,
            transition,
            channelTransition,
            recommendation,
            reasonCode,
            reason);
    }

    private static DCMLPackageVersionChannelTransition GetChannelTransition(
        string currentVersion,
        string availableVersion)
    {
        bool currentPrerelease =
            IsPrerelease(currentVersion);

        bool availablePrerelease =
            IsPrerelease(availableVersion);

        if (!currentPrerelease && !availablePrerelease)
        {
            return DCMLPackageVersionChannelTransition.StableToStable;
        }

        if (!currentPrerelease && availablePrerelease)
        {
            return DCMLPackageVersionChannelTransition.StableToPrerelease;
        }

        if (currentPrerelease && availablePrerelease)
        {
            return DCMLPackageVersionChannelTransition.PrereleaseToPrerelease;
        }

        return DCMLPackageVersionChannelTransition.PrereleaseToStable;
    }

    private static bool IsPrerelease(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        int buildIndex =
            value.IndexOf('+');

        string withoutBuild =
            buildIndex >= 0
                ? value.Substring(0, buildIndex)
                : value;

        return withoutBuild.IndexOf('-') >= 0;
    }
}
