using JamaConnect.Application.Authentication;
using JamaConnect.Application.Configuration;
using JamaConnect.Application.Evidence;
using JamaConnect.Application.Items;
using JamaConnect.Application.Projects;
using JamaConnect.Application.Relationships;
using JamaConnect.Application.TestManagement;
using JamaConnect.Application.Traceability;
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
        IOptions<JamaConnectOptions> options = new OptionsWrapper<JamaConnectOptions>(LoadOptions(configuration));
        services.AddSingleton(options);
        services.AddSingleton(LoadCliConfiguration(configuration));
        services.AddSingleton<AliasResolver>();

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
        services.AddSingleton<JamaRestClient>();
        services.AddSingleton<IJamaPaginator, JamaPaginator>();
        services.AddSingleton<ProjectAdapter>();
        services.AddSingleton<ItemAdapter>();
        services.AddSingleton<SchemaAdapter>();
        services.AddSingleton<RelationshipAdapter>();
        services.AddSingleton<TestManagementAdapter>();
        services.AddSingleton<IProjectReader>(sp => sp.GetRequiredService<ProjectAdapter>());
        services.AddSingleton<IItemReader>(sp => sp.GetRequiredService<ItemAdapter>());
        services.AddSingleton<IItemWriter>(sp => sp.GetRequiredService<ItemAdapter>());
        services.AddSingleton<ISchemaReader>(sp => sp.GetRequiredService<SchemaAdapter>());
        services.AddSingleton<IRelationshipReader>(sp => sp.GetRequiredService<RelationshipAdapter>());
        services.AddSingleton<IRelationshipWriter>(sp => sp.GetRequiredService<RelationshipAdapter>());
        services.AddSingleton<ITestManagementReader>(sp => sp.GetRequiredService<TestManagementAdapter>());
        services.AddSingleton<ITestManagementWriter>(sp => sp.GetRequiredService<TestManagementAdapter>());

        services.AddTransient<GetProjectsQueryHandler>();
        services.AddTransient<GetItemsQueryHandler>();
        services.AddTransient<LoginCommandHandler>();
        services.AddTransient<ValidateConfigurationHandler>();
        services.AddTransient<ItemUseCases>();
        services.AddTransient<RelationshipUseCases>();
        services.AddTransient<TraceUseCases>();
        services.AddTransient<TestManagementUseCases>();
        services.AddTransient<EvidenceUseCases>();

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
            RetryMaxAttempts = ReadPositiveInt(section[nameof(JamaConnectOptions.RetryMaxAttempts)], 5, nameof(JamaConnectOptions.RetryMaxAttempts)),
            RetryInitialDelayMilliseconds = ReadPositiveInt(section[nameof(JamaConnectOptions.RetryInitialDelayMilliseconds)], 250, nameof(JamaConnectOptions.RetryInitialDelayMilliseconds)),
            RetryMaxDelaySeconds = ReadPositiveInt(section[nameof(JamaConnectOptions.RetryMaxDelaySeconds)], 10, nameof(JamaConnectOptions.RetryMaxDelaySeconds)),
        };
    }

    private static int ReadTimeoutSeconds(string? value)
        => ReadPositiveInt(value, 30, nameof(JamaConnectOptions.TimeoutSeconds));

    private static int ReadPositiveInt(string? value, int fallback, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (!int.TryParse(value, out var timeoutSeconds) || timeoutSeconds <= 0)
        {
            throw new InvalidOperationException($"JamaConnect:{name} must be a positive integer.");
        }

        return timeoutSeconds;
    }

    private static JamaCliConfiguration LoadCliConfiguration(IConfiguration configuration)
    {
        var cli = new JamaCliConfiguration();
        var section = configuration.GetSection(JamaCliConfiguration.SectionName);
        if (!section.Exists())
        {
            return cli;
        }

        var defaultProfile = section[nameof(JamaCliConfiguration.DefaultProfile)] ?? cli.DefaultProfile;
        var profiles = new Dictionary<string, JamaProfile>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in section.GetSection(nameof(JamaCliConfiguration.Profiles)).GetChildren())
        {
            profiles[profile.Key] = new JamaProfile
            {
                BaseUrl = profile[nameof(JamaProfile.BaseUrl)],
                Project = int.TryParse(profile[nameof(JamaProfile.Project)], out var project) ? project : null,
                Output = profile[nameof(JamaProfile.Output)] ?? "table",
                Production = bool.TryParse(profile[nameof(JamaProfile.Production)], out var production) && production,
            };
        }

        if (profiles.Count == 0)
        {
            profiles["default"] = new JamaProfile();
        }

        var itemTypes = new Dictionary<string, ItemTypeAlias>(StringComparer.OrdinalIgnoreCase);
        foreach (var alias in section.GetSection("Aliases:ItemTypes").GetChildren())
        {
            itemTypes[alias.Key] = new ItemTypeAlias
            {
                ItemTypeId = int.TryParse(alias[nameof(ItemTypeAlias.ItemTypeId)], out var itemTypeId) ? itemTypeId : 0,
                DisplayName = alias[nameof(ItemTypeAlias.DisplayName)],
                RequiredFields = alias.GetSection(nameof(ItemTypeAlias.RequiredFields))
                    .GetChildren()
                    .ToDictionary(x => x.Key, x => x.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            };
        }

        var relationships = new Dictionary<string, RelationshipAlias>(StringComparer.OrdinalIgnoreCase);
        foreach (var alias in section.GetSection("Aliases:Relationships").GetChildren())
        {
            relationships[alias.Key] = new RelationshipAlias
            {
                RelationshipTypeId = int.TryParse(alias[nameof(RelationshipAlias.RelationshipTypeId)], out var relationshipTypeId) ? relationshipTypeId : 0,
                From = alias[nameof(RelationshipAlias.From)],
                To = alias[nameof(RelationshipAlias.To)]
            };
        }

        return new JamaCliConfiguration
        {
            DefaultProfile = defaultProfile,
            Profiles = profiles,
            Aliases = new AliasConfiguration
            {
                ItemTypes = itemTypes,
                Relationships = relationships
            }
        };
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
