namespace DCML.Core.Models;

public sealed class DCMLCapabilityRequirement
{
    public string Id { get; set; } =
        string.Empty;

    public string? MinimumVersion { get; set; }
}
