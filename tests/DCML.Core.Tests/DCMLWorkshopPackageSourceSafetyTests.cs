using System;
using System.IO;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLWorkshopPackageSourceSafetyTests
{
    [Fact]
    public void WorkshopAdapter_RemainsFilesystemOnlyAndSanctionedContentOnly()
    {
        string root =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    ".."));

        string sourcePath =
            Path.Combine(
                root,
                "src",
                "DCML.DataCenter",
                "PackageSources",
                "DCMLWorkshopPackageSource.cs");

        string source =
            File.ReadAllText(
                sourcePath);

        Assert.Contains(
            "\"4170200\"",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "Directory.GetDirectories(",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "File.Copy(",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "System.Diagnostics.Process",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "HttpClient",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "WebClient",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "Steamworks",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "Subscribe",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "DownloadItem",
            source,
            StringComparison.Ordinal);
    }
}
