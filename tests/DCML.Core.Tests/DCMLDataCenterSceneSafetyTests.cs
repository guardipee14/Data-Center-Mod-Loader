using System;
using System.IO;
using System.Linq;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.DataCenter;
using DCML.DataCenter.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLDataCenterSceneSafetyTests
{
    [Fact]
    public void Evaluate_ResourceOnlyQueryDoesNotRequireLifecycle()
    {
        DataCenterHardwareSnapshotQuery query =
            new(
                includeSceneObjects:
                    false,
                includeResources:
                    true);

        DataCenterSceneCaptureSafetyDecision decision =
            DataCenterSceneCaptureSafety.Evaluate(
                lifecycle:
                    null,
                query);

        Assert.True(
            decision.IsAllowed);

        Assert.Equal(
            DataCenterSceneCaptureSafetyReason.ResourceOnly,
            decision.Reason);
    }

    [Fact]
    public void Evaluate_SceneObjectsRequireLifecycle()
    {
        DataCenterHardwareSnapshotQuery query =
            new(
                sceneName:
                    "BaseScene");

        DataCenterSceneCaptureSafetyDecision decision =
            DataCenterSceneCaptureSafety.Evaluate(
                lifecycle:
                    null,
                query);

        Assert.False(
            decision.IsAllowed);

        Assert.Equal(
            DataCenterSceneCaptureSafetyReason.LifecycleUnavailable,
            decision.Reason);
    }

    [Fact]
    public void Evaluate_SceneObjectsRequireCurrentScene()
    {
        FakeLifecycle lifecycle =
            new(
                hasCurrentScene:
                    false,
                sceneName:
                    string.Empty,
                DCMLSceneLifecycleStage.None);

        DataCenterSceneCaptureSafetyDecision decision =
            DataCenterSceneCaptureSafety.Evaluate(
                lifecycle,
                new DataCenterHardwareSnapshotQuery(
                    sceneName:
                        "BaseScene"));

        Assert.False(
            decision.IsAllowed);

        Assert.Equal(
            DataCenterSceneCaptureSafetyReason.NoCurrentScene,
            decision.Reason);
    }

    [Fact]
    public void Evaluate_SceneObjectsRequireInitializedStage()
    {
        FakeLifecycle lifecycle =
            new(
                hasCurrentScene:
                    true,
                sceneName:
                    "BaseScene",
                DCMLSceneLifecycleStage.Loaded);

        DataCenterSceneCaptureSafetyDecision decision =
            DataCenterSceneCaptureSafety.Evaluate(
                lifecycle,
                new DataCenterHardwareSnapshotQuery(
                    sceneName:
                        "BaseScene"));

        Assert.False(
            decision.IsAllowed);

        Assert.Equal(
            DataCenterSceneCaptureSafetyReason.SceneNotInitialized,
            decision.Reason);
    }

    [Fact]
    public void Evaluate_NamedSceneMustMatchCurrentScene()
    {
        FakeLifecycle lifecycle =
            Initialized(
                "BaseScene");

        DataCenterSceneCaptureSafetyDecision decision =
            DataCenterSceneCaptureSafety.Evaluate(
                lifecycle,
                new DataCenterHardwareSnapshotQuery(
                    sceneName:
                        "OtherScene"));

        Assert.False(
            decision.IsAllowed);

        Assert.Equal(
            DataCenterSceneCaptureSafetyReason.SceneMismatch,
            decision.Reason);
    }

    [Fact]
    public void Evaluate_InitializedCurrentSceneAllowsUnnamedSceneCapture()
    {
        FakeLifecycle lifecycle =
            Initialized(
                "BaseScene");

        DataCenterSceneCaptureSafetyDecision decision =
            DataCenterSceneCaptureSafety.Evaluate(
                lifecycle,
                new DataCenterHardwareSnapshotQuery());

        Assert.True(
            decision.IsAllowed);

        Assert.Equal(
            DataCenterSceneCaptureSafetyReason.Ready,
            decision.Reason);

        Assert.Equal(
            "BaseScene",
            decision.CurrentSceneName);
    }

    [Fact]
    public void Evaluate_InitializedMatchingNamedSceneIsAllowed()
    {
        FakeLifecycle lifecycle =
            Initialized(
                "BaseScene");

        DataCenterSceneCaptureSafetyDecision decision =
            DataCenterSceneCaptureSafety.Evaluate(
                lifecycle,
                new DataCenterHardwareSnapshotQuery(
                    sceneName:
                        "BaseScene"));

        Assert.True(
            decision.IsAllowed);

        Assert.Equal(
            DataCenterSceneCaptureSafetyReason.Ready,
            decision.Reason);
    }

    [Fact]
    public void ContextOverload_UsesLifecycleService()
    {
        FakeLifecycle lifecycle =
            Initialized(
                "BaseScene");

        FakeContext context =
            new(
                lifecycle);

        Assert.True(
            DataCenterSceneCaptureSafety.CanCapture(
                context,
                new DataCenterHardwareSnapshotQuery(
                    sceneName:
                        "BaseScene")));
    }

    [Fact]
    public void ContextOverload_MissingLifecycleFailsClosedForSceneObjects()
    {
        FakeContext context =
            new(
                lifecycle:
                    null);

        Assert.False(
            DataCenterSceneCaptureSafety.CanCapture(
                context,
                new DataCenterHardwareSnapshotQuery(
                    sceneName:
                        "BaseScene")));
    }

    [Fact]
    public void DataCenterSource_DoesNotExposeKnownMutationEntryPoints()
    {
        string root =
            GetSolutionRoot();

        string[] sourceRoots =
        {
            Path.Combine(
                root,
                "src",
                "DCML.DataCenter"),
            Path.Combine(
                root,
                "src",
                "DCML.DataCenter.Persistence")
        };

        string[] forbidden =
        {
            "SaveAsync(",
            "WriteAsync(",
            "DeleteAsync(",
            "SetValue(",
            "SetField(",
            "SetMember(",
            "InvokeMethod(",
            "PowerButton(",
            "SetIP(",
            "UpdateAppID(",
            "AddRoute(",
            "RemoveRoute(",
            "AddSubnet(",
            "SetVlanAllowed(",
            "AddRule(",
            "InsertSFP(",
            "RemoveSFP("
        };

        string[] files =
            sourceRoots
                .SelectMany(
                    sourceRoot =>
                        Directory.EnumerateFiles(
                            sourceRoot,
                            "*.cs",
                            SearchOption.AllDirectories))
                .Where(
                    path =>
                        !path.Contains(
                            $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                            StringComparison.OrdinalIgnoreCase) &&
                        !path.Contains(
                            $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                            StringComparison.OrdinalIgnoreCase))
                .ToArray();

        Assert.NotEmpty(
            files);

        foreach (string path in files)
        {
            string source =
                File.ReadAllText(
                    path);

            foreach (string marker in forbidden)
            {
                Assert.DoesNotContain(
                    marker,
                    source,
                    StringComparison.Ordinal);
            }
        }
    }

    private static FakeLifecycle Initialized(
        string sceneName)
    {
        return
            new FakeLifecycle(
                hasCurrentScene:
                    true,
                sceneName,
                DCMLSceneLifecycleStage.Initialized);
    }

    private static string GetSolutionRoot()
    {
        DirectoryInfo? directory =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (
                File.Exists(
                    Path.Combine(
                        directory.FullName,
                        "DCML.sln"))
            )
            {
                return
                    directory.FullName;
            }

            directory =
                directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the DCML solution root.");
    }

    private sealed class FakeLifecycle :
        IDCMLGameLifecycle
    {
        public FakeLifecycle(
            bool hasCurrentScene,
            string sceneName,
            DCMLSceneLifecycleStage stage)
        {
            HasCurrentScene =
                hasCurrentScene;

            CurrentSceneName =
                sceneName;

            CurrentSceneStage =
                stage;
        }

        public long SceneEventCount =>
            1;

        public bool HasCurrentScene { get; }

        public int CurrentSceneBuildIndex =>
            HasCurrentScene
                ? 1
                : -1;

        public string CurrentSceneName { get; }

        public DCMLSceneLifecycleStage CurrentSceneStage { get; }
    }

    private sealed class FakeContext :
        IDCMLModuleContext
    {
        public FakeContext(
            IDCMLGameLifecycle? lifecycle)
        {
            Services =
                new FakeServiceProvider(
                    lifecycle);
        }

        public string ModuleDirectory =>
            string.Empty;

        public string DataDirectory =>
            string.Empty;

        public IServiceProvider Services { get; }
    }

    private sealed class FakeServiceProvider :
        IServiceProvider
    {
        private readonly IDCMLGameLifecycle?
            _lifecycle;

        public FakeServiceProvider(
            IDCMLGameLifecycle? lifecycle)
        {
            _lifecycle =
                lifecycle;
        }

        public object? GetService(
            Type serviceType)
        {
            if (
                serviceType ==
                    typeof(IDCMLGameLifecycle)
            )
            {
                return
                    _lifecycle;
            }

            return null;
        }
    }
}
