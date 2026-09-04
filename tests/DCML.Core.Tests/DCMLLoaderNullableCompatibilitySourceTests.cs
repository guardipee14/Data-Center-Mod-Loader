using System;
using System.IO;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLLoaderNullableCompatibilitySourceTests
{
    [Fact]
    public void LoaderDefinesNullableCompilerCompatibilityAttributes()
    {
        string root =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    ".."
                )
            );

        string path =
            Path.Combine(
                root,
                "src",
                "DCML.Loader.MelonLoader",
                "CompilerNullableAttributes.cs"
            );

        string source =
            File.ReadAllText(
                path
            )
            .Replace(
                "\r\n",
                "\n"
            );

        Assert.Contains(
            "internal sealed class NullableAttribute",
            source,
            StringComparison.Ordinal
        );

        Assert.Contains(
            "NullableAttribute(\n            byte value",
            source,
            StringComparison.Ordinal
        );

        Assert.Contains(
            "NullableAttribute(\n            byte[] value",
            source,
            StringComparison.Ordinal
        );

        Assert.Contains(
            "internal sealed class NullableContextAttribute",
            source,
            StringComparison.Ordinal
        );

        Assert.Contains(
            "NullableContextAttribute(\n            byte value",
            source,
            StringComparison.Ordinal
        );
    }
}
