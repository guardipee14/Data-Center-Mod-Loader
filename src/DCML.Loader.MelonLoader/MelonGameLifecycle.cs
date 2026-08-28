using System;
using DCML.Core.Abstractions;
using DCML.Core.Models;

namespace DCML.Loader.MelonLoader;

internal sealed class MelonGameLifecycle :
    IDCMLGameLifecycle
{
    private readonly object _syncRoot =
        new object();

    private readonly IDCMLEventBus _eventBus;

    private long _sceneEventCount;

    private bool _hasCurrentScene;

    private int _currentSceneBuildIndex =
        -1;

    private string _currentSceneName =
        string.Empty;

    private DCMLSceneLifecycleStage _currentSceneStage =
        DCMLSceneLifecycleStage.None;

    public MelonGameLifecycle(
        IDCMLEventBus eventBus)
    {
        _eventBus =
            eventBus ??
            throw new ArgumentNullException(
                nameof(eventBus));
    }

    public long SceneEventCount
    {
        get
        {
            lock (_syncRoot)
            {
                return _sceneEventCount;
            }
        }
    }

    public bool HasCurrentScene
    {
        get
        {
            lock (_syncRoot)
            {
                return _hasCurrentScene;
            }
        }
    }

    public int CurrentSceneBuildIndex
    {
        get
        {
            lock (_syncRoot)
            {
                return _currentSceneBuildIndex;
            }
        }
    }

    public string CurrentSceneName
    {
        get
        {
            lock (_syncRoot)
            {
                return _currentSceneName;
            }
        }
    }

    public DCMLSceneLifecycleStage CurrentSceneStage
    {
        get
        {
            lock (_syncRoot)
            {
                return _currentSceneStage;
            }
        }
    }

    public void Report(
        DCMLSceneLifecycleStage stage,
        int buildIndex,
        string sceneName)
    {
        string normalizedSceneName =
            sceneName ??
            string.Empty;

        DCMLSceneLifecycleEvent eventData;

        lock (_syncRoot)
        {
            _sceneEventCount++;

            eventData =
                new DCMLSceneLifecycleEvent(
                    stage,
                    buildIndex,
                    normalizedSceneName,
                    _sceneEventCount);

            if (
                stage ==
                DCMLSceneLifecycleStage.Unloaded
            )
            {
                if (
                    _hasCurrentScene &&
                    _currentSceneBuildIndex ==
                    buildIndex &&
                    string.Equals(
                        _currentSceneName,
                        normalizedSceneName,
                        StringComparison.Ordinal)
                )
                {
                    ClearCurrentScene();
                }
            }
            else
            {
                _hasCurrentScene =
                    true;

                _currentSceneBuildIndex =
                    buildIndex;

                _currentSceneName =
                    normalizedSceneName;

                _currentSceneStage =
                    stage;
            }
        }

        _eventBus.Publish(
            eventData);
    }

    private void ClearCurrentScene()
    {
        _hasCurrentScene =
            false;

        _currentSceneBuildIndex =
            -1;

        _currentSceneName =
            string.Empty;

        _currentSceneStage =
            DCMLSceneLifecycleStage.None;
    }
}
