using System;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.DataCenter.Models;

namespace DCML.DataCenter;

/// <summary>
/// Describes why a Data Center snapshot query is or is not safe to start at
/// the current lifecycle state.
/// </summary>
public enum DataCenterSceneCaptureSafetyReason
{
    Ready = 0,
    ResourceOnly = 1,
    LifecycleUnavailable = 2,
    NoCurrentScene = 3,
    SceneNotInitialized = 4,
    SceneMismatch = 5
}

/// <summary>
/// Immutable result of evaluating scene-capture readiness.
/// </summary>
public sealed class DataCenterSceneCaptureSafetyDecision
{
    public DataCenterSceneCaptureSafetyDecision(
        bool isAllowed,
        DataCenterSceneCaptureSafetyReason reason,
        string requestedSceneName,
        string currentSceneName,
        DCMLSceneLifecycleStage currentSceneStage)
    {
        IsAllowed = isAllowed;
        Reason = reason;

        RequestedSceneName =
            string.IsNullOrWhiteSpace(
                requestedSceneName)
                ? string.Empty
                : requestedSceneName.Trim();

        CurrentSceneName =
            string.IsNullOrWhiteSpace(
                currentSceneName)
                ? string.Empty
                : currentSceneName.Trim();

        CurrentSceneStage =
            currentSceneStage;
    }

    public bool IsAllowed { get; }

    public DataCenterSceneCaptureSafetyReason Reason { get; }

    public string RequestedSceneName { get; }

    public string CurrentSceneName { get; }

    public DCMLSceneLifecycleStage CurrentSceneStage { get; }
}

/// <summary>
/// Provides a reusable, read-only safety gate for Data Center hardware and
/// topology queries that include scene objects.
/// </summary>
/// <remarks>
/// This helper does not mutate game state and does not intercept direct
/// CaptureAsync calls. Consumers should evaluate the query immediately before
/// starting scene-object capture and abandon stale work when the scene changes.
/// </remarks>
public static class DataCenterSceneCaptureSafety
{
    public static DataCenterSceneCaptureSafetyDecision Evaluate(
        IDCMLModuleContext context,
        DataCenterHardwareSnapshotQuery query)
    {
        if (context is null)
        {
            throw new ArgumentNullException(
                nameof(context));
        }

        IDCMLGameLifecycle? lifecycle =
            context.Services.GetService(
                typeof(IDCMLGameLifecycle))
            as IDCMLGameLifecycle;

        return Evaluate(
            lifecycle,
            query);
    }

    public static DataCenterSceneCaptureSafetyDecision Evaluate(
        IDCMLGameLifecycle? lifecycle,
        DataCenterHardwareSnapshotQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(
                nameof(query));
        }

        if (!query.IncludeSceneObjects)
        {
            return
                new DataCenterSceneCaptureSafetyDecision(
                    isAllowed:
                        true,
                    DataCenterSceneCaptureSafetyReason.ResourceOnly,
                    query.SceneName,
                    lifecycle?.CurrentSceneName ??
                        string.Empty,
                    lifecycle?.CurrentSceneStage ??
                        DCMLSceneLifecycleStage.None);
        }

        if (lifecycle is null)
        {
            return
                new DataCenterSceneCaptureSafetyDecision(
                    isAllowed:
                        false,
                    DataCenterSceneCaptureSafetyReason.LifecycleUnavailable,
                    query.SceneName,
                    string.Empty,
                    DCMLSceneLifecycleStage.None);
        }

        if (!lifecycle.HasCurrentScene)
        {
            return
                new DataCenterSceneCaptureSafetyDecision(
                    isAllowed:
                        false,
                    DataCenterSceneCaptureSafetyReason.NoCurrentScene,
                    query.SceneName,
                    lifecycle.CurrentSceneName,
                    lifecycle.CurrentSceneStage);
        }

        if (
            lifecycle.CurrentSceneStage !=
                DCMLSceneLifecycleStage.Initialized
        )
        {
            return
                new DataCenterSceneCaptureSafetyDecision(
                    isAllowed:
                        false,
                    DataCenterSceneCaptureSafetyReason.SceneNotInitialized,
                    query.SceneName,
                    lifecycle.CurrentSceneName,
                    lifecycle.CurrentSceneStage);
        }

        if (
            !string.IsNullOrWhiteSpace(
                query.SceneName) &&
            !string.Equals(
                query.SceneName,
                lifecycle.CurrentSceneName,
                StringComparison.Ordinal)
        )
        {
            return
                new DataCenterSceneCaptureSafetyDecision(
                    isAllowed:
                        false,
                    DataCenterSceneCaptureSafetyReason.SceneMismatch,
                    query.SceneName,
                    lifecycle.CurrentSceneName,
                    lifecycle.CurrentSceneStage);
        }

        return
            new DataCenterSceneCaptureSafetyDecision(
                isAllowed:
                    true,
                DataCenterSceneCaptureSafetyReason.Ready,
                query.SceneName,
                lifecycle.CurrentSceneName,
                lifecycle.CurrentSceneStage);
    }

    public static bool CanCapture(
        IDCMLModuleContext context,
        DataCenterHardwareSnapshotQuery query)
    {
        return
            Evaluate(
                context,
                query)
            .IsAllowed;
    }

    public static bool CanCapture(
        IDCMLGameLifecycle? lifecycle,
        DataCenterHardwareSnapshotQuery query)
    {
        return
            Evaluate(
                lifecycle,
                query)
            .IsAllowed;
    }
}
