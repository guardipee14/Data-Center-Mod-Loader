namespace DCML.Core.Runtime;

public sealed class DCMLPackageCompatibilityIssue
{
    public DCMLPackageCompatibilityIssue(
        string moduleId,
        string code,
        string message,
        string? requirementId = null)
    {
        ModuleId =
            moduleId;

        Code =
            code;

        Message =
            message;

        RequirementId =
            requirementId;
    }

    public string ModuleId { get; }

    public string Code { get; }

    public string Message { get; }

    public string? RequirementId { get; }
}
