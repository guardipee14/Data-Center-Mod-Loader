using System;
using System.IO;
using DCML.Core.Abstractions;

namespace DCML.LoaderAcceptanceProbe.Minimal;

public sealed class MinimalModule :
    IDCMLModule
{
    private string _tracePath =
        string.Empty;

    public string Id =>
        "dcml.probe.loader-acceptance-minimal";

    public string Name =>
        "DCML Minimal Loader Acceptance Probe";

    public string Version =>
        "1.0.0";

    public void Initialize(
        IDCMLModuleContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(
                nameof(context));
        }

        Directory.CreateDirectory(
            context.DataDirectory);

        _tracePath =
            Path.Combine(
                context.DataDirectory,
                "loader-acceptance-probe.log");

        File.WriteAllText(
            _tracePath,
            "Initialize" +
            Environment.NewLine);
    }

    public void Start()
    {
        File.AppendAllText(
            _tracePath,
            "Start" +
            Environment.NewLine);
    }

    public void Stop()
    {
        File.AppendAllText(
            _tracePath,
            "Stop" +
            Environment.NewLine);
    }
}
