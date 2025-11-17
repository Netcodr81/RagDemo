using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Mediator.Abstractions;

namespace SharedKernel.Mediator;

public static class MediatorServiceCollectionExtension
{
    public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        var handlerTypes = assemblies.SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                             i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
                .Select(i => new { Service = i, Implementation = t }));

        foreach (var handler in handlerTypes)
        {
            services.AddTransient(handler.Service, handler.Implementation);
        }

        services.AddScoped<IMediator, Mediator>();
        
        return services;
    }
}