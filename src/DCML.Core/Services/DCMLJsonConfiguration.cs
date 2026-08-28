using System;
using System.IO;
using System.Text.Json;
using DCML.Core.Abstractions;

namespace DCML.Core.Services;

public sealed class DCMLJsonConfiguration :
    IDCMLConfiguration
{
    private static readonly JsonSerializerOptions JsonOptions =
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

    public DCMLJsonConfiguration(
        string configurationPath)
    {
        if (string.IsNullOrWhiteSpace(configurationPath))
        {
            throw new ArgumentException(
                "Configuration path cannot be empty.",
                nameof(configurationPath));
        }

        ConfigurationPath =
            Path.GetFullPath(
                configurationPath);
    }

    public string ConfigurationPath { get; }

    public bool Exists =>
        File.Exists(
            ConfigurationPath);

    public T Load<T>(
        T defaultValue)
    {
        if (!Exists)
        {
            return defaultValue;
        }

        string json =
            File.ReadAllText(
                ConfigurationPath);

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException(
                $"DCML configuration file '{ConfigurationPath}' is empty.");
        }

        try
        {
            T? value =
                JsonSerializer.Deserialize<T>(
                    json,
                    JsonOptions);

            if (value is null)
            {
                throw new InvalidDataException(
                    $"DCML configuration file '{ConfigurationPath}' produced a null value.");
            }

            return value;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"DCML configuration file '{ConfigurationPath}' contains invalid JSON.",
                exception);
        }
    }

    public void Save<T>(
        T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(
                nameof(value));
        }

        string? directory =
            Path.GetDirectoryName(
                ConfigurationPath);

        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException(
                $"DCML could not resolve the configuration directory for '{ConfigurationPath}'.");
        }

        Directory.CreateDirectory(
            directory);

        string json =
            JsonSerializer.Serialize(
                value,
                JsonOptions);

        string temporaryPath =
            ConfigurationPath + ".tmp";

        try
        {
            File.WriteAllText(
                temporaryPath,
                json);

            File.Copy(
                temporaryPath,
                ConfigurationPath,
                true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(
                    temporaryPath);
            }
        }
    }

    public void Delete()
    {
        if (Exists)
        {
            File.Delete(
                ConfigurationPath);
        }
    }
}
