using System;
using DCML.Core.Abstractions;

namespace DCML.SDK;

/// <summary>
/// Optional convenience helpers for accessing services exposed through a
/// DCML module context.
/// </summary>
/// <remarks>
/// These helpers do not change loader acceptance requirements. A module may
/// continue to use <see cref="IDCMLModuleContext.Services"/> directly and may
/// omit a reference to DCML.SDK entirely.
/// </remarks>
public static class DCMLModuleContextExtensions
{
    /// <summary>
    /// Attempts to resolve an optional DCML service from the module context.
    /// </summary>
    public static bool TryGetService<TService>(
        this IDCMLModuleContext context,
        out TService? service)
        where TService : class
    {
        if (context is null)
        {
            throw new ArgumentNullException(
                nameof(context));
        }

        service =
            context.Services.GetService(
                typeof(TService))
            as TService;

        return
            service is not null;
    }

    /// <summary>
    /// Resolves an optional DCML service, returning null when the active host
    /// does not provide it.
    /// </summary>
    public static TService? GetOptionalService<TService>(
        this IDCMLModuleContext context)
        where TService : class
    {
        if (context is null)
        {
            throw new ArgumentNullException(
                nameof(context));
        }

        return
            context.Services.GetService(
                typeof(TService))
            as TService;
    }

    /// <summary>
    /// Resolves a DCML service that the module has explicitly chosen to
    /// require.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The active host does not provide the requested service.
    /// </exception>
    public static TService GetRequiredService<TService>(
        this IDCMLModuleContext context)
        where TService : class
    {
        TService? service =
            GetOptionalService<TService>(
                context);

        if (service is not null)
        {
            return service;
        }

        throw new InvalidOperationException(
            "DCML service '" +
            typeof(TService).FullName +
            "' is not available from the active module host.");
    }
}
