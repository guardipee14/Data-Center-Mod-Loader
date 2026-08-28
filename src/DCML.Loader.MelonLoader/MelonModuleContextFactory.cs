using System;
using System.IO;
using System.Text;
using DCML.Core.Abstractions;
using DCML.Core.Models;

namespace DCML.Loader.MelonLoader;

public sealed class MelonModuleContextFactory :
    IDCMLModuleContextFactory
{
    private readonly string _dataRoot;

    private readonly string _gameRoot;

    private readonly IDCMLEventBus _eventBus;

    private readonly IDCMLGameLifecycle _gameLifecycle;

    public MelonModuleContextFactory(
        string dataRoot,
        string gameRoot,
        IDCMLEventBus eventBus,
        IDCMLGameLifecycle gameLifecycle)
    {
        if (string.IsNullOrWhiteSpace(dataRoot))
        {
            throw new ArgumentException(
                "A DCML data root is required.",
                nameof(dataRoot));
        }

        if (string.IsNullOrWhiteSpace(gameRoot))
        {
            throw new ArgumentException(
                "A game root is required.",
                nameof(gameRoot));
        }

        _eventBus =
            eventBus ??
            throw new ArgumentNullException(
                nameof(eventBus));

        _gameLifecycle =
            gameLifecycle ??
            throw new ArgumentNullException(
                nameof(gameLifecycle));

        _dataRoot =
            Path.GetFullPath(
                dataRoot);

        _gameRoot =
            Path.GetFullPath(
                gameRoot);
    }

    public IDCMLModuleContext CreateContext(
        DCMLModulePackage package)
    {
        if (package is null)
        {
            throw new ArgumentNullException(
                nameof(package));
        }

        var moduleId =
            package.Manifest.Id;

        var moduleDataDirectory =
            Path.Combine(
                _dataRoot,
                CreateSafeDirectoryName(
                    moduleId));

        Directory.CreateDirectory(
            moduleDataDirectory);

        return
            new MelonModuleContext(
                moduleId,
                Path.GetFullPath(
                    package.PackageDirectory),
                moduleDataDirectory,
                _gameRoot,
                _eventBus,
                _gameLifecycle);
    }

    private static string CreateSafeDirectoryName(
        string value)
    {
        var invalidCharacters =
            Path.GetInvalidFileNameChars();

        var builder =
            new StringBuilder(
                value.Length);

        foreach (var character in value)
        {
            var invalid =
                Array.IndexOf(
                    invalidCharacters,
                    character) >= 0;

            builder.Append(
                invalid
                    ? '_'
                    : character);
        }

        var result =
            builder.ToString();

        return
            string.IsNullOrWhiteSpace(result)
                ? "module"
                : result;
    }
}
