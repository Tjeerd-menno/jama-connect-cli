using System.CommandLine;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;
using Microsoft.Extensions.DependencyInjection;

namespace JamaConnect.Cli.Commands;

internal static class ProjectsCommandExtensions
{
    public static Command BuildProjectsCommand(IServiceProvider services, CliRuntime runtime)
    {
        var command = new Command("projects", "Manage Jama Connect projects.");
        command.Add(BuildListCommand(services, runtime));
        command.Add(BuildGetCommand(services, runtime));
        return command;
    }

    private static Command BuildListCommand(IServiceProvider services, CliRuntime runtime)
    {
        var command = new Command("list", "List all accessible projects.");
        var startAt = CommandHelpers.StartAtOption();
        var pageSize = CommandHelpers.PageSizeOption();
        var limit = CommandHelpers.LimitOption();
        var all = CommandHelpers.AllOption();
        command.Add(startAt);
        command.Add(pageSize);
        command.Add(limit);
        command.Add(all);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var reader = services.GetRequiredService<IProjectReader>();
                var pageRequest = CommandHelpers.Page(parseResult, startAt, pageSize, limit);
                var page = parseResult.GetValue(all)
                    ? await ReadAllAsync(
                        (start, size, token) => reader.GetProjectsAsync(new PageRequest(start, size, null), token),
                        pageRequest,
                        services.GetRequiredService<IJamaPaginator>(),
                        ct).ConfigureAwait(false)
                    : await reader.GetProjectsAsync(pageRequest, ct).ConfigureAwait(false);
                CliOutput.WritePage(context, "projects.list.result", page);
            }, cancellationToken).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<JamaPage<T>> ReadAllAsync<T>(
        Func<int, int, CancellationToken, Task<JamaPage<T>>> fetchPage,
        PageRequest pageRequest,
        IJamaPaginator paginator,
        CancellationToken cancellationToken)
    {
        var data = new List<T>();
        await foreach (var item in paginator.GetAllAsync(fetchPage, pageRequest.MaxResults, pageRequest.Limit, cancellationToken).ConfigureAwait(false))
        {
            data.Add(item);
        }

        return new JamaPage<T>(0, pageRequest.MaxResults, data.Count, data.Count, data);
    }

    private static Command BuildGetCommand(IServiceProvider services, CliRuntime runtime)
    {
        var id = new Argument<int>("id") { Description = "Project ID." };
        var command = new Command("get", "Get one project.");
        command.Add(id);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var reader = services.GetRequiredService<IProjectReader>();
                var project = await reader.GetProjectAsync(parseResult.GetValue(id), ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "projects.get.result", project);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }
}
