using System;
using System.IO;
using DCML.Core.Abstractions;

namespace DCML.CompatibilityProbe.UnsupportedCapability;

public sealed class UnsupportedCapabilityModule :
    IDCMLModule
{
    public string Id =>
        "dcml.probe.compatibility-unsupported";

    public string Name =>
        "DCML Unsupported Capability Compatibility Probe";

    public string Version =>
        "1.0.0";

    public void Initialize(
        IDCMLModuleContext context)
    {
        Directory.CreateDirectory(
            context.DataDirectory);

        File.WriteAllText(
            Path.Combine(
                context.DataDirectory,
                "ACTIVATED-UNEXPECTEDLY.txt"),
            "This package should have been rejected by compatibility evaluation.");

        throw new InvalidOperationException(
            "Compatibility probe was activated unexpectedly.");
    }

    public void Start()
    {
    }

    public void Stop()
    {
    }
}
