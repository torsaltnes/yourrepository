using DeviationManagement.Application.Abstractions.Persistence;
using DeviationManagement.Application.Abstractions.Services;
using DeviationManagement.Application.Services;
using DeviationManagement.Application.Validation;
using DeviationManagement.Infrastructure.Persistence.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace DeviationManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDeviationRepository, InMemoryDeviationRepository>();
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<DeviationValidator>();
        services.AddScoped<IDeviationService, DeviationService>();
        return services;
    }
}
