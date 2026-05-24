using System.Reflection;
using System.CommandLine;
using JamaConnect.Cli.Commands;
using JamaConnect.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configurationBuilder = new ConfigurationBuilder();
var assembly = Assembly.GetExecutingAssembly();
using var defaultConfiguration = assembly.GetManifestResourceStream("JamaConnect.Cli.appsettings.json")
    ?? throw new InvalidOperationException(
        "Required embedded configuration resource 'JamaConnect.Cli.appsettings.json' was not found. Ensure appsettings.json is included as an embedded resource.");
configurationBuilder.AddJsonStream(defaultConfiguration);

var configuration = configurationBuilder
    .AddJsonFile("jama-connect.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables("JAMA_")
    .Build();

var services = new ServiceCollection();
services.AddJamaConnectInfrastructure(configuration);

var serviceProvider = services.BuildServiceProvider();

var formatOption = new Option<string>("--format")
{
    Description = "Output format: table, json, ndjson, or csv.",
    DefaultValueFactory = _ => "table"
};
var profileOption = new Option<string>("--profile")
{
    Description = "Configuration profile name.",
    DefaultValueFactory = _ => "default"
};
var quietOption = new Option<bool>("--quiet") { Description = "Reduce non-result output." };
var verboseOption = new Option<bool>("--verbose") { Description = "Increase diagnostic output." };
var noColorOption = new Option<bool>("--no-color") { Description = "Disable color output." };
var dryRunOption = new Option<bool>("--dry-run") { Description = "Plan write operations without sending them." };
var agentOption = new Option<bool>("--agent") { Description = "Use deterministic JSON agent mode." };
var errorFormatOption = new Option<string>("--error-format")
{
    Description = "Error format: text or json.",
    DefaultValueFactory = _ => "text"
};
var runtime = new CliRuntime(formatOption, profileOption, quietOption, verboseOption, noColorOption, dryRunOption, agentOption, errorFormatOption);

var rootCommand = new RootCommand("Jama Connect CLI - manage Jama Connect requirements, verification, traceability, and test execution.");
rootCommand.Add(formatOption);
rootCommand.Add(profileOption);
rootCommand.Add(quietOption);
rootCommand.Add(verboseOption);
rootCommand.Add(noColorOption);
rootCommand.Add(dryRunOption);
rootCommand.Add(agentOption);
rootCommand.Add(errorFormatOption);
rootCommand.Add(LoginCommandExtensions.BuildLoginCommand(serviceProvider));
rootCommand.Add(ConfigCommandExtensions.BuildConfigCommand(serviceProvider, runtime));
rootCommand.Add(SchemaCommandExtensions.BuildSchemaCommand(serviceProvider, runtime));
rootCommand.Add(ProjectsCommandExtensions.BuildProjectsCommand(serviceProvider, runtime));
rootCommand.Add(ItemsCommandExtensions.BuildItemsCommand(serviceProvider, runtime));
rootCommand.Add(ItemsCommandExtensions.BuildTypedCommand(serviceProvider, runtime, "needs", "user-need"));
rootCommand.Add(ItemsCommandExtensions.BuildTypedCommand(serviceProvider, runtime, "requirements", "requirement"));
rootCommand.Add(ItemsCommandExtensions.BuildTypedCommand(serviceProvider, runtime, "subsystem-requirements", "subsystem-requirement"));
rootCommand.Add(ItemsCommandExtensions.BuildTypedCommand(serviceProvider, runtime, "test-cases", "test-case"));
rootCommand.Add(ItemsCommandExtensions.BuildTypedCommand(serviceProvider, runtime, "defects", "defect"));
rootCommand.Add(RelationsCommandExtensions.BuildRelationsCommand(serviceProvider, runtime));
rootCommand.Add(TraceCommandExtensions.BuildTraceCommand(serviceProvider, runtime));
rootCommand.Add(TestManagementCommandExtensions.BuildTestPlansCommand(serviceProvider, runtime));
rootCommand.Add(TestManagementCommandExtensions.BuildTestCyclesCommand(serviceProvider, runtime));
rootCommand.Add(TestManagementCommandExtensions.BuildTestRunsCommand(serviceProvider, runtime));
rootCommand.Add(AgentCommandExtensions.BuildEvidenceCommand(serviceProvider, runtime));
rootCommand.Add(AgentCommandExtensions.BuildCommandsCommand(runtime));
rootCommand.Add(AgentCommandExtensions.BuildSchemasCommand(runtime));

return await rootCommand.Parse(args).InvokeAsync();
