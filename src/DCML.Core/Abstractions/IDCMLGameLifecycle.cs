using DCML.Core.Models;

namespace DCML.Core.Abstractions;

public interface IDCMLGameLifecycle
{
    long SceneEventCount { get; }

    bool HasCurrentScene { get; }

    int CurrentSceneBuildIndex { get; }

    string CurrentSceneName { get; }

    DCMLSceneLifecycleStage CurrentSceneStage { get; }
}
