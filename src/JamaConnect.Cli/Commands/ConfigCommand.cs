using System.CommandLine;
using JamaConnect.Application.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JamaConnect.Cli.Commands;

internal static class ConfigCommandExtensions
{
    public static Command BuildConfigCommand(IServiceProvider services, CliRuntime runtime)
    {
        var command = new Command("config", "Inspect and validate CLI configuration.");
        command.Add(BuildShowCommand(services, runtime));
        command.Add(BuildValidateCommand(services, runtime));
        var profiles = new Command("profiles", "Profile commands.");
        profiles.Add(BuildProfilesListCommand(services, runtime));
        command.Add(profiles);
        var aliases = new Command("aliases", "Alias commands.");
        aliases.Add(BuildAliasesListCommand(services, runtime));
        aliases.Add(BuildValidateCommand(services, runtime, "validate"));
        command.Add(aliases);
        return command;
    }

    private static Command BuildShowCommand(IServiceProvider services, CliRuntime runtime)
    {
        var command = new Command("show", "Show non-secret configuration.");
        command.SetAction((parseResult) =>
        {
            var context = runtime.Create(parseResult);
            var configuration = services.GetRequiredService<JamaCliConfiguration>();
            CliOutput.WriteResult(context, "config.show.result", configuration);
        });
        return command;
    }

    private static Command BuildValidateCommand(IServiceProvider services, CliRuntime runtime, string name = "validate")
    {
        var project = CommandHelpers.ProjectOption();
        var command = new Command(name, "Validate aliases against Jama schema.");
        command.Add(project);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var handler = services.GetRequiredService<ValidateConfigurationHandler>();
                var result = await handler.HandleAsync(context.Profile, parseResult.GetValue(project), ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "config.validate.result", result, result.Warnings);
                if (!result.Valid)
                {
                    Environment.ExitCode = 4;
                }
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildProfilesListCommand(IServiceProvider services, CliRuntime runtime)
    {
        var command = new Command("list", "List profiles.");
        command.SetAction((parseResult) =>
        {
            var context = runtime.Create(parseResult);
            var configuration = services.GetRequiredService<JamaCliConfiguration>();
            CliOutput.WriteResult(context, "config.profiles.list.result", configuration.Profiles.Keys.ToArray());
        });
        return command;
    }

    private static Command BuildAliasesListCommand(IServiceProvider services, CliRuntime runtime)
    {
        var command = new Command("list", "List aliases.");
        command.SetAction((parseResult) =>
        {
            var context = runtime.Create(parseResult);
            var configuration = services.GetRequiredService<JamaCliConfiguration>();
            CliOutput.WriteResult(context, "config.aliases.list.result", configuration.Aliases);
        });
        return command;
    }
}
