using System.Collections.Generic;

namespace DCML.Core.Abstractions;

public interface IDCMLRuntimeInfo
{
    string ModuleId { get; }

    string DCMLVersion { get; }

    string HostName { get; }

    string HostVersion { get; }

    string GameName { get; }

    string GameRoot { get; }

    IReadOnlyCollection<string> Capabilities { get; }

    bool HasCapability(string capability);
}
