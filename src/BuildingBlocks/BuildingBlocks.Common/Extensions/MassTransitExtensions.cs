using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Common.Extensions;

public static class MassTransitExtensions
{
    public static IServiceCollection AddAppMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        services.AddMassTransit(x =>
        {
            configureConsumers?.Invoke(x);

            var azureServiceBusConnection = configuration["AzureServiceBus:ConnectionString"];

            if (!string.IsNullOrWhiteSpace(azureServiceBusConnection))
            {
                // Production Cloud Mode: Uses Azure Service Bus
                x.UsingAzureServiceBus((context, cfg) =>
                {
                    cfg.Host(azureServiceBusConnection);
                    cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                // Development Local Mode: In-Memory Message Bus (zero external dependencies required)
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            }
        });

        return services;
    }
}
