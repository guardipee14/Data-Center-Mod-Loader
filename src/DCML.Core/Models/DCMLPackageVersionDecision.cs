using System;

namespace DCML.Core.Models;

/// <summary>
/// Describes a pure version-policy decision. A decision never stages,
/// installs, updates, or otherwise mutates package state.
/// </summary>
public sealed class DCMLPackageVersionDecision
{
    internal DCMLPackageVersionDecision(
        string currentVersion,
        string availableVersion,
        DCMLPackageVersionTransition transition,
        DCMLPackageVersionChannelTransition channelTransition,
        DCMLPackageVersionRecommendation recommendation,
        string reasonCode,
        string reason)
    {
        CurrentVersion =
            currentVersion;

        AvailableVersion =
            availableVersion;

        Transition =
            transition;

        ChannelTransition =
            channelTransition;

        Recommendation =
            recommendation;

        ReasonCode =
            reasonCode ?? throw new ArgumentNullException(
                nameof(reasonCode));

        Reason =
            reason ?? throw new ArgumentNullException(
                nameof(reason));
    }

    public string CurrentVersion { get; }

    public string AvailableVersion { get; }

    public DCMLPackageVersionTransition Transition { get; }

    public DCMLPackageVersionChannelTransition ChannelTransition { get; }

    public DCMLPackageVersionRecommendation Recommendation { get; }

    public string ReasonCode { get; }

    public string Reason { get; }
}
