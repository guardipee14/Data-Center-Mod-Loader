using System;

namespace DCML.Core.Models;

public sealed class DCMLSceneLifecycleEvent
{
    public DCMLSceneLifecycleEvent(
        DCMLSceneLifecycleStage stage,
        int buildIndex,
        string sceneName,
        long sequence)
    {
        if (
            stage == DCMLSceneLifecycleStage.None ||
            !Enum.IsDefined(
                typeof(DCMLSceneLifecycleStage),
                stage)
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(stage),
                stage,
                "A valid scene lifecycle stage is required.");
        }

        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                sequence,
                "Scene event sequence must be greater than zero.");
        }

        Stage =
            stage;

        BuildIndex =
            buildIndex;

        SceneName =
            sceneName ??
            string.Empty;

        Sequence =
            sequence;
    }

    public DCMLSceneLifecycleStage Stage { get; }

    public int BuildIndex { get; }

    public string SceneName { get; }

    public long Sequence { get; }
}
