using System;
using DCML.Core.Abstractions;
using MelonLoader;

namespace DCML.Loader.MelonLoader;

public sealed class MelonDCMLLogger : IDCMLLogger
{
    private readonly string _moduleId;

    public MelonDCMLLogger(string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
        {
            throw new ArgumentException(
                "Module ID cannot be empty.",
                nameof(moduleId));
        }

        _moduleId = moduleId;
    }

    public void Log(
        DCMLLogLevel level,
        string message,
        Exception? exception = null)
    {
        var formatted =
            $"[Module:{_moduleId}] {message}";

        switch (level)
        {
            case DCMLLogLevel.Debug:
                MelonLogger.Msg(
                    $"[DEBUG] {formatted}");
                break;

            case DCMLLogLevel.Information:
                MelonLogger.Msg(
                    formatted);
                break;

            case DCMLLogLevel.Warning:
                MelonLogger.Warning(
                    formatted);
                break;

            case DCMLLogLevel.Error:
                if (exception is null)
                {
                    MelonLogger.Error(
                        formatted);
                }
                else
                {
                    MelonLogger.Error(
                        $"{formatted}{Environment.NewLine}{exception}");
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    level,
                    "Unknown DCML log level.");
        }
    }

    public void Debug(string message)
    {
        Log(
            DCMLLogLevel.Debug,
            message);
    }

    public void Info(string message)
    {
        Log(
            DCMLLogLevel.Information,
            message);
    }

    public void Warning(string message)
    {
        Log(
            DCMLLogLevel.Warning,
            message);
    }

    public void Error(
        string message,
        Exception? exception = null)
    {
        Log(
            DCMLLogLevel.Error,
            message,
            exception);
    }
}
