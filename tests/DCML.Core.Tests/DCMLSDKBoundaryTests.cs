using System;
using System.Linq;
using DCML.Core.Abstractions;
using DCML.SDK;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLSDKBoundaryTests
{
    [Fact]
    public void SDKAssembly_ReferencesCoreWithoutHostOrDataCenterDependencies()
    {
        string[] references =
            typeof(DCMLModuleContextExtensions)
                .Assembly
                .GetReferencedAssemblies()
                .Select(
                    value =>
                        value.Name ?? string.Empty)
                .ToArray();

        Assert.Contains(
            "DCML.Core",
            references);

        Assert.DoesNotContain(
            "DCML.DataCenter",
            references);

        Assert.DoesNotContain(
            "DCML.Loader.MelonLoader",
            references);

        Assert.DoesNotContain(
            "MelonLoader",
            references);
    }

    [Fact]
    public void CoreAssembly_DoesNotDependOnSDK()
    {
        string[] references =
            typeof(IDCMLModule)
                .Assembly
                .GetReferencedAssemblies()
                .Select(
                    value =>
                        value.Name ?? string.Empty)
                .ToArray();

        Assert.DoesNotContain(
            "DCML.SDK",
            references);
    }

    [Fact]
    public void OptionalLookup_ReturnsNullWhenServiceIsUnavailable()
    {
        var context =
            new TestContext(
                EmptyServiceProvider.Instance);

        IDCMLLogger? logger =
            context.GetOptionalService<IDCMLLogger>();

        Assert.Null(
            logger);
    }

    [Fact]
    public void TryLookup_AndDirectProviderAccess_ReturnSameRegisteredService()
    {
        var logger =
            new TestLogger();

        var provider =
            new SingleServiceProvider(
                typeof(IDCMLLogger),
                logger);

        var context =
            new TestContext(
                provider);

        bool found =
            context.TryGetService<IDCMLLogger>(
                out IDCMLLogger? sdkLogger);

        object? directLogger =
            context.Services.GetService(
                typeof(IDCMLLogger));

        Assert.True(
            found);

        Assert.Same(
            logger,
            sdkLogger);

        Assert.Same(
            logger,
            directLogger);
    }

    [Fact]
    public void RequiredLookup_ThrowsWhenServiceIsUnavailable()
    {
        var context =
            new TestContext(
                EmptyServiceProvider.Instance);

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    context.GetRequiredService<IDCMLLogger>());

        Assert.Contains(
            typeof(IDCMLLogger).FullName!,
            exception.Message,
            StringComparison.Ordinal);
    }

    private sealed class TestContext :
        IDCMLModuleContext
    {
        public TestContext(
            IServiceProvider services)
        {
            Services =
                services;
        }

        public string ModuleDirectory =>
            "module";

        public string DataDirectory =>
            "data";

        public IServiceProvider Services { get; }
    }

    private sealed class EmptyServiceProvider :
        IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } =
            new EmptyServiceProvider();

        public object? GetService(
            Type serviceType)
        {
            return null;
        }
    }

    private sealed class SingleServiceProvider :
        IServiceProvider
    {
        private readonly Type _serviceType;

        private readonly object _service;

        public SingleServiceProvider(
            Type serviceType,
            object service)
        {
            _serviceType =
                serviceType;

            _service =
                service;
        }

        public object? GetService(
            Type serviceType)
        {
            return
                serviceType == _serviceType
                    ? _service
                    : null;
        }
    }

    private sealed class TestLogger :
        IDCMLLogger
    {
        public void Log(
            DCMLLogLevel level,
            string message,
            Exception? exception = null)
        {
        }

        public void Debug(
            string message)
        {
        }

        public void Info(
            string message)
        {
        }

        public void Warning(
            string message)
        {
        }

        public void Error(
            string message,
            Exception? exception = null)
        {
        }
    }
}
