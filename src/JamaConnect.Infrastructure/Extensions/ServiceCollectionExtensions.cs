using JamaConnect.Application.Authentication;
using JamaConnect.Application.Items;
using JamaConnect.Application.Projects;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Infrastructure.Authentication;
using JamaConnect.Infrastructure.JamaConnect;
using JamaConnect.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JamaConnect.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJamaConnectInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(Options.Create(LoadOptions(configuration)));

        services.AddHttpClient("auth")
            .ConfigureHttpClient((sp, client) =>
            {
                var opts = sp.GetRequiredService<IOptions<JamaConnectOptions>>().Value;
                client.BaseAddress = CreateBaseUri(opts.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
            });

        services.AddHttpClient("jama")
            .ConfigureHttpClient((sp, client) =>
            {
                var opts = sp.GetRequiredService<IOptions<JamaConnectOptions>>().Value;
                client.BaseAddress = CreateBaseUri(opts.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
            });

        services.AddSingleton<IAuthenticationService, OidcAuthenticationService>();
        services.AddSingleton<JamaConnectClient>();
        services.AddSingleton<IProjectService>(sp => sp.GetRequiredService<JamaConnectClient>());
        services.AddSingleton<IItemService>(sp => sp.GetRequiredService<JamaConnectClient>());

        services.AddTransient<GetProjectsQueryHandler>();
        services.AddTransient<GetItemsQueryHandler>();
        services.AddTransient<LoginCommandHandler>();

        return services;
    }

    private static JamaConnectOptions LoadOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(JamaConnectOptions.SectionName);

        return new JamaConnectOptions
        {
            BaseUrl = section[nameof(JamaConnectOptions.BaseUrl)] ?? string.Empty,
            ClientId = section[nameof(JamaConnectOptions.ClientId)] ?? string.Empty,
            ClientSecret = section[nameof(JamaConnectOptions.ClientSecret)] ?? string.Empty,
            TokenEndpoint = section[nameof(JamaConnectOptions.TokenEndpoint)] ?? "/rest/oauth/token",
            TimeoutSeconds = ReadTimeoutSeconds(section[nameof(JamaConnectOptions.TimeoutSeconds)]),
        };
    }

    private static int ReadTimeoutSeconds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 30;
        }

        if (!int.TryParse(value, out var timeoutSeconds) || timeoutSeconds <= 0)
        {
            throw new InvalidOperationException("JamaConnect:TimeoutSeconds must be a positive integer.");
        }

        return timeoutSeconds;
    }

    private static Uri CreateBaseUri(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("JamaConnect:BaseUrl must be configured as an absolute URI.");
        }

        return uri;
    }
}
