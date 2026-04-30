using System.CommandLine;
using JamaConnect.Application.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace JamaConnect.Cli.Commands;

internal static class LoginCommandExtensions
{
    public static Command BuildLoginCommand(IServiceProvider services)
    {
        var command = new Command("login", "Authenticate with the Jama Connect server.");

        command.SetHandler(async (context) =>
        {
            var handler = services.GetRequiredService<LoginCommandHandler>();
            await handler.HandleAsync(new LoginCommand(), context.GetCancellationToken()).ConfigureAwait(false);
            Console.WriteLine("Successfully authenticated with Jama Connect.");
        });

        return command;
    }
}
