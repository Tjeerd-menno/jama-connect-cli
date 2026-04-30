using System.CommandLine;
using JamaConnect.Application.Items;
using Microsoft.Extensions.DependencyInjection;

namespace JamaConnect.Cli.Commands;

internal static class ItemsCommandExtensions
{
    public static Command BuildItemsCommand(IServiceProvider services)
    {
        var command = new Command("items", "Manage Jama Connect items.");
        command.AddCommand(BuildListCommand(services));
        return command;
    }

    private static Command BuildListCommand(IServiceProvider services)
    {
        var projectIdOption = new Option<int>(
            "--project",
            description: "The ID of the project to list items from.")
        { IsRequired = true };
        projectIdOption.AddAlias("-p");

        var command = new Command("list", "List items in a project.");
        command.AddOption(projectIdOption);

        command.SetHandler(async (context) =>
        {
            var projectId = context.ParseResult.GetValueForOption(projectIdOption);
            var handler = services.GetRequiredService<GetItemsQueryHandler>();
            var items = await handler.HandleAsync(new GetItemsQuery(projectId), context.GetCancellationToken()).ConfigureAwait(false);

            if (!items.Any())
            {
                Console.WriteLine("No items found.");
                return;
            }

            Console.WriteLine($"{"ID",-8} {"Document Key",-15} {"Subject"}");
            Console.WriteLine(new string('-', 60));
            foreach (var item in items)
            {
                Console.WriteLine($"{item.Id,-8} {item.DocumentKey,-15} {item.Subject}");
            }
        });

        return command;
    }
}
