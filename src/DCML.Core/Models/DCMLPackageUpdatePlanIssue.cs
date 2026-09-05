using System;

namespace DCML.Core.Models;

/// <summary>
/// Describes one reason an update plan could not be built safely.
/// </summary>
public sealed class DCMLPackageUpdatePlanIssue
{
    public DCMLPackageUpdatePlanIssue(
        string moduleId,
        string code,
        string message,
        string? dependencyId = null)
    {
        ModuleId =
            moduleId?.Trim() ??
            string.Empty;

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Update-plan issue code cannot be empty.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Update-plan issue message cannot be empty.",
                nameof(message));
        }

        Code =
            code.Trim();

        Message =
            message.Trim();

        DependencyId =
            string.IsNullOrWhiteSpace(dependencyId)
                ? null
                : dependencyId.Trim();
    }

    public string ModuleId { get; }

    public string Code { get; }

    public string Message { get; }

    public string? DependencyId { get; }
}
