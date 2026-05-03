using System.CommandLine;
using JamaConnect.Application.Projects;
using Microsoft.Extensions.DependencyInjection;

namespace JamaConnect.Cli.Commands;

internal static class ProjectsCommandExtensions
{
    public static Command BuildProjectsCommand(IServiceProvider services)
    {
        var command = new Command("projects", "Manage Jama Connect projects.");
        command.Add(BuildListCommand(services));
        return command;
    }

    private static Command BuildListCommand(IServiceProvider services)
    {
        var command = new Command("list", "List all accessible projects.");

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var handler = services.GetRequiredService<GetProjectsQueryHandler>();
            var projects = await handler.HandleAsync(new GetProjectsQuery(), cancellationToken).ConfigureAwait(false);

            if (!projects.Any())
            {
                Console.WriteLine("No projects found.");
                return;
            }

            Console.WriteLine($"{"ID",-8} {"Key",-12} {"Name"}");
            Console.WriteLine(new string('-', 60));
            foreach (var project in projects)
            {
                Console.WriteLine($"{project.Id,-8} {project.ProjectKey ?? string.Empty,-12} {project.Name}");
            }
        });

        return command;
    }
}
