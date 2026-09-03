using System.Collections.Generic;
using DCML.Core.Models;

namespace DCML.Core.Runtime;

public sealed class DCMLPackageCompatibilityResult
{
    private readonly List<DCMLModulePackage> _compatiblePackages =
        new List<DCMLModulePackage>();

    private readonly List<DCMLModulePackage> _incompatiblePackages =
        new List<DCMLModulePackage>();

    private readonly List<DCMLPackageCompatibilityIssue> _issues =
        new List<DCMLPackageCompatibilityIssue>();

    public IReadOnlyList<DCMLModulePackage> CompatiblePackages =>
        _compatiblePackages;

    public IReadOnlyList<DCMLModulePackage> IncompatiblePackages =>
        _incompatiblePackages;

    public IReadOnlyList<DCMLPackageCompatibilityIssue> Issues =>
        _issues;

    public bool Success =>
        _incompatiblePackages.Count == 0;

    public int IncompatiblePackageCount =>
        _incompatiblePackages.Count;

    internal void AddCompatible(
        DCMLModulePackage package)
    {
        _compatiblePackages.Add(
            package);
    }

    internal void AddIncompatible(
        DCMLModulePackage package)
    {
        _incompatiblePackages.Add(
            package);
    }

    internal void AddIssue(
        DCMLPackageCompatibilityIssue issue)
    {
        _issues.Add(
            issue);
    }
}
