using System;
using System.IO;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLStatusOverlaySourceTests
{
    [Fact]
    public void MelonLoaderProject_ReferencesImGuiAndIl2CppDependencies()
    {
        string root =
            GetRepositoryRoot();

        string projectPath =
            Path.Combine(
                root,
                "src",
                "DCML.Loader.MelonLoader",
                "DCML.Loader.MelonLoader.csproj"
            );

        string source =
            File.ReadAllText(
                projectPath
            );

        Assert.Contains(
            "<Reference Include=\"UnityEngine.CoreModule\">",
            source,
            StringComparison.Ordinal
        );

        Assert.Contains(
            "<Reference Include=\"UnityEngine.IMGUIModule\">",
            source,
            StringComparison.Ordinal
        );

        Assert.Contains(
            "<Reference Include=\"Il2Cppmscorlib\">",
            source,
            StringComparison.Ordinal
        );

        Assert.Contains(
            "<Reference Include=\"Il2CppInterop.Runtime\">",
            source,
            StringComparison.Ordinal
        );

        Assert.Contains(
            "$(DataCenterRoot)\\MelonLoader\\Il2CppAssemblies\\UnityEngine.IMGUIModule.dll",
            source,
            StringComparison.Ordinal
        );

        Assert.Contains(
            "$(DataCenterRoot)\\MelonLoader\\Il2CppAssemblies\\Il2Cppmscorlib.dll",
            source,
            StringComparison.Ordinal
        );

        Assert.Contains(
            "Il2CppInterop.Runtime.dll</HintPath>",
            source,
            StringComparison.Ordinal
        );

        Assert.DoesNotContain(
            "UnityEngine.InputLegacyModule",
            source,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void StatusOverlay_UsesImGuiKeyboardEventWithProofLogging()
    {
        string root =
            GetRepositoryRoot();

        string overlayPath =
            Path.Combine(
                root,
                "src",
                "DCML.Loader.MelonLoader",
                "MelonStatusOverlay.cs"
            );

        string hostPath =
            Path.Combine(
                root,
                "src",
                "DCML.Loader.MelonLoader",
                "DCMLMelonMod.cs"
            );

        string source =
            File.ReadAllText(
                overlayPath
            );

        string hostSource =
            File.ReadAllText(
                hostPath
            );

        Assert.Contains(
            "using UnityEngine;",
            source,
            StringComparison.Ordinal
        );

        Assert.Contains(
            "Event.current",
            source,
            StringComparison.Ordinal
        );

        Assert.Contains(
            "EventType.KeyDown",
            source,
            StringComparison.Ordinal
        );

        Assert.Contains(
            "KeyCode.F8",
            source,
            StringComparison.Ordinal
        );

        Assert.Contains(
            "GUI.Box(",
            source,
            StringComparison.Ordinal
        );

        Assert.Contains(
            "GUI.Label(",
            source,
            StringComparison.Ordinal
        );

        Assert.Contains(
            "Status UI render ready.",
            source,
            StringComparison.Ordinal
        );

        Assert.Contains(
            "Status UI rendering failed and has been disabled.",
            source,
            StringComparison.Ordinal
        );

        Assert.DoesNotContain(
            "Input.GetKeyDown(",
            source,
            StringComparison.Ordinal
        );

        Assert.DoesNotContain(
            "FindLoadedType(",
            source,
            StringComparison.Ordinal
        );

        Assert.DoesNotContain(
            "_statusOverlay.UpdateToggle();",
            hostSource,
            StringComparison.Ordinal
        );
    }

    private static string GetRepositoryRoot()
    {
        return Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                ".."
            )
        );
    }
}
