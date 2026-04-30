using JamaConnect.Application.Authentication;
using JamaConnect.Application.Items;
using JamaConnect.Application.Projects;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Infrastructure.Authentication;
using JamaConnect.Infrastructure.JamaConnect;
using JamaConnect.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JamaConnect.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJamaConnectInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JamaConnectOptions>(configuration.GetSection(JamaConnectOptions.SectionName));

        services.AddHttpClient("auth");
        services.AddHttpClient("jama");

        services.AddSingleton<IAuthenticationService, OidcAuthenticationService>();
        services.AddSingleton<JamaConnectClient>();
        services.AddSingleton<IProjectService>(sp => sp.GetRequiredService<JamaConnectClient>());
        services.AddSingleton<IItemService>(sp => sp.GetRequiredService<JamaConnectClient>());

        services.AddTransient<GetProjectsQueryHandler>();
        services.AddTransient<GetItemsQueryHandler>();
        services.AddTransient<LoginCommandHandler>();

        return services;
    }
}
