using System;
using System.Collections.Generic;

namespace DCML.Core.Services;

public sealed class DCMLServiceProvider : IServiceProvider
{
    private readonly IReadOnlyDictionary<Type, object> _services;

    public DCMLServiceProvider(
        params (Type ServiceType, object Instance)[] services)
    {
        var registrations =
            new Dictionary<Type, object>();

        foreach (var service in services)
        {
            if (service.ServiceType is null)
            {
                throw new ArgumentException(
                    "A service registration cannot have a null service type.",
                    nameof(services));
            }

            if (service.Instance is null)
            {
                throw new ArgumentException(
                    $"Service '{service.ServiceType.FullName}' cannot have a null instance.",
                    nameof(services));
            }

            if (!service.ServiceType.IsInstanceOfType(service.Instance))
            {
                throw new ArgumentException(
                    $"Service instance '{service.Instance.GetType().FullName}' does not implement or derive from '{service.ServiceType.FullName}'.",
                    nameof(services));
            }

            if (!registrations.TryAdd(
                    service.ServiceType,
                    service.Instance))
            {
                throw new ArgumentException(
                    $"Service '{service.ServiceType.FullName}' is already registered.",
                    nameof(services));
            }
        }

        _services = registrations;
    }

    public object? GetService(Type serviceType)
    {
        if (serviceType is null)
        {
            throw new ArgumentNullException(
                nameof(serviceType));
        }

        return
            _services.TryGetValue(
                serviceType,
                out var service)
                ? service
                : null;
    }
}
