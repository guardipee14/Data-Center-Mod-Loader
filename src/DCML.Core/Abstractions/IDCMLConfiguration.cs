namespace DCML.Core.Abstractions;

public interface IDCMLConfiguration
{
    string ConfigurationPath { get; }

    bool Exists { get; }

    T Load<T>(T defaultValue);

    void Save<T>(T value);

    void Delete();
}
