using System;
using DCML.Core.Abstractions;
using DCML.Core.Services;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLServiceProviderTests
{
    [Fact]
    public void GetService_ReturnsRegisteredLogger()
    {
        var logger =
            new TestLogger();

        var provider =
            new DCMLServiceProvider(
                (
                    typeof(IDCMLLogger),
                    logger
                ));

        Assert.Same(
            logger,
            provider.GetService(
                typeof(IDCMLLogger)));
    }

    [Fact]
    public void GetService_ReturnsNullForUnknownService()
    {
        var provider =
            new DCMLServiceProvider();

        Assert.Null(
            provider.GetService(
                typeof(IDCMLLogger)));
    }

    [Fact]
    public void Constructor_RejectsMismatchedServiceInstance()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new DCMLServiceProvider(
                    (
                        typeof(IDCMLLogger),
                        new object()
                    )));
    }

    [Fact]
    public void Constructor_RejectsDuplicateServiceType()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new DCMLServiceProvider(
                    (
                        typeof(IDCMLLogger),
                        new TestLogger()
                    ),
                    (
                        typeof(IDCMLLogger),
                        new TestLogger()
                    )));
    }

    private sealed class TestLogger : IDCMLLogger
    {
        public void Log(
            DCMLLogLevel level,
            string message,
            Exception? exception = null)
        {
        }

        public void Debug(string message)
        {
        }

        public void Info(string message)
        {
        }

        public void Warning(string message)
        {
        }

        public void Error(
            string message,
            Exception? exception = null)
        {
        }
    }
}
