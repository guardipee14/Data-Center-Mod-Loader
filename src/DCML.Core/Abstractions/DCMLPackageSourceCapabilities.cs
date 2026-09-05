using System;

namespace DCML.Core.Abstractions;

[Flags]
public enum DCMLPackageSourceCapabilities
{
    None = 0,

    Discovery = 1 << 0,

    Staging = 1 << 1,

    UpdateMetadata = 1 << 2
}
