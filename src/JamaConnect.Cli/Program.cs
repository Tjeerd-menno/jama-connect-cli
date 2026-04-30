using System.CommandLine;
using JamaConnect.Cli.Commands;
using JamaConnect.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
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
rootCommand.AddCommand(LoginCommandExtensions.BuildLoginCommand(serviceProvider));
rootCommand.AddCommand(ProjectsCommandExtensions.BuildProjectsCommand(serviceProvider));
rootCommand.AddCommand(ItemsCommandExtensions.BuildItemsCommand(serviceProvider));

return await rootCommand.InvokeAsync(args);
