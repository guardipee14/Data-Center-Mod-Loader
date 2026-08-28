using System;
using System.IO;
using DCML.Core.Services;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLJsonConfigurationTests :
    IDisposable
{
    private readonly string _root;

    private readonly string _configurationPath;

    public DCMLJsonConfigurationTests()
    {
        _root =
            Path.Combine(
                Path.GetTempPath(),
                "DCML.Tests",
                Guid.NewGuid().ToString("N"));

        _configurationPath =
            Path.Combine(
                _root,
                "config.json");
    }

    [Fact]
    public void Load_ReturnsDefaultWhenFileDoesNotExist()
    {
        var configuration =
            CreateConfiguration();

        var defaultValue =
            new TestSettings
            {
                Name = "default",
                Count = 7
            };

        var result =
            configuration.Load(
                defaultValue);

        Assert.Same(
            defaultValue,
            result);
    }

    [Fact]
    public void Save_CreatesFileAndRoundTripsTypedValue()
    {
        var configuration =
            CreateConfiguration();

        configuration.Save(
            new TestSettings
            {
                Name = "saved",
                Count = 42
            });

        var loaded =
            configuration.Load(
                new TestSettings());

        Assert.True(
            configuration.Exists);

        Assert.Equal(
            "saved",
            loaded.Name);

        Assert.Equal(
            42,
            loaded.Count);
    }

    [Fact]
    public void Save_WritesCamelCaseIndentedJson()
    {
        var configuration =
            CreateConfiguration();

        configuration.Save(
            new TestSettings
            {
                Name = "saved",
                Count = 2
            });

        string json =
            File.ReadAllText(
                _configurationPath);

        Assert.Contains(
            "\"name\"",
            json);

        Assert.Contains(
            Environment.NewLine,
            json);
    }

    [Fact]
    public void Load_ThrowsInvalidDataForMalformedJson()
    {
        Directory.CreateDirectory(
            _root);

        File.WriteAllText(
            _configurationPath,
            "{not-json");

        var configuration =
            CreateConfiguration();

        Assert.Throws<InvalidDataException>(
            () =>
                configuration.Load(
                    new TestSettings()));
    }

    [Fact]
    public void Delete_RemovesExistingConfiguration()
    {
        var configuration =
            CreateConfiguration();

        configuration.Save(
            new TestSettings
            {
                Name = "saved"
            });

        configuration.Delete();

        Assert.False(
            configuration.Exists);
    }

    [Fact]
    public void Save_RejectsNullValue()
    {
        var configuration =
            CreateConfiguration();

        Assert.Throws<ArgumentNullException>(
            () =>
                configuration.Save<TestSettings>(
                    null!));
    }

    [Fact]
    public void Constructor_NormalizesConfigurationPath()
    {
        var configuration =
            CreateConfiguration();

        Assert.Equal(
            Path.GetFullPath(
                _configurationPath),
            configuration.ConfigurationPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(
                _root,
                true);
        }
    }

    private DCMLJsonConfiguration CreateConfiguration()
    {
        return
            new DCMLJsonConfiguration(
                _configurationPath);
    }

    private sealed class TestSettings
    {
        public string Name { get; set; } =
            string.Empty;

        public int Count { get; set; }
    }
}
