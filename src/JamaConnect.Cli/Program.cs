using System.Reflection;
using System.CommandLine;
using JamaConnect.Cli.Commands;
using JamaConnect.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configurationBuilder = new ConfigurationBuilder();
using var defaultConfiguration = Assembly
    .GetExecutingAssembly()
    .GetManifestResourceStream("JamaConnect.Cli.appsettings.json");
if (defaultConfiguration is not null)
{
    configurationBuilder.AddJsonStream(defaultConfiguration);
}

var configuration = configurationBuilder
    .AddEnvironmentVariables("JAMA_")
    .Build();

var services = new ServiceCollection();
services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Warning);
});
services.AddJamaConnectInfrastructure(configuration);

var serviceProvider = services.BuildServiceProvider();

var rootCommand = new RootCommand("Jama Connect CLI - manage your Jama Connect requirements from the command line.");
rootCommand.Add(LoginCommandExtensions.BuildLoginCommand(serviceProvider));
rootCommand.Add(ProjectsCommandExtensions.BuildProjectsCommand(serviceProvider));
rootCommand.Add(ItemsCommandExtensions.BuildItemsCommand(serviceProvider));

return await rootCommand.Parse(args).InvokeAsync();
