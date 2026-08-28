using System;

namespace DCML.Core.Abstractions;

public interface IDCMLLogger
{
    void Log(
        DCMLLogLevel level,
        string message,
        Exception? exception = null);

    void Debug(string message);

    void Info(string message);

    void Warning(string message);

    void Error(
        string message,
        Exception? exception = null);
}
