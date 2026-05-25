using System.CommandLine;
using JamaConnect.Application.Relationships;
using JamaConnect.Domain.Models;
using Microsoft.Extensions.DependencyInjection;

namespace JamaConnect.Cli.Commands;

internal static class RelationsCommandExtensions
{
    public static Command BuildRelationsCommand(IServiceProvider services, CliRuntime runtime)
    {
        var command = new Command("relations", "Manage relationships.");
        command.Add(BuildListCommand(services, runtime));
        command.Add(BuildGetCommand(services, runtime));
        command.Add(BuildCreateCommand(services, runtime));
        command.Add(BuildUpdateCommand(services, runtime));
        command.Add(BuildDeleteCommand(services, runtime));
        command.Add(BuildClearSuspectCommand(services, runtime));
        command.Add(BuildValidateCommand(services, runtime));
        return command;
    }

    private static Command BuildGetCommand(IServiceProvider services, CliRuntime runtime)
    {
        var id = new Argument<int>("id") { Description = "Relationship ID." };
        var command = new Command("get", "Get relationship.");
        command.Add(id);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var result = await services.GetRequiredService<RelationshipUseCases>().GetAsync(parseResult.GetValue(id), ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "relations.get.result", result);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildListCommand(IServiceProvider services, CliRuntime runtime)
    {
        var item = new Option<int?>("--item") { Description = "Item ID." };
        var direction = new Option<string>("--direction") { Description = "upstream, downstream, or both.", DefaultValueFactory = _ => "downstream" };
        var include = new Option<string?>("--include") { Description = "Comma-separated include links." };
        var startAt = CommandHelpers.StartAtOption();
        var pageSize = CommandHelpers.PageSizeOption();
        var limit = CommandHelpers.LimitOption();
        var command = new Command("list", "List relationships.");
        command.Add(item);
        command.Add(direction);
        command.Add(include);
        command.Add(startAt);
        command.Add(pageSize);
        command.Add(limit);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var useCases = services.GetRequiredService<RelationshipUseCases>();
                var page = await useCases.ListAsync(
                    new RelationshipQuery(parseResult.GetValue(item), parseResult.GetValue(direction) ?? "downstream", CommandHelpers.SplitCsv(parseResult.GetValue(include))),
                    CommandHelpers.Page(parseResult, startAt, pageSize, limit),
                    ct).ConfigureAwait(false);
                CliOutput.WritePage(context, "relations.list.result", page);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildCreateCommand(IServiceProvider services, CliRuntime runtime)
    {
        var from = new Option<int>("--from") { Description = "Source item ID.", Required = true };
        var to = new Option<int>("--to") { Description = "Target item ID.", Required = true };
        var type = new Option<string>("--type") { Description = "Relationship type alias or ID.", Required = true };
        var command = new Command("create", "Create relationship.");
        command.Add(from);
        command.Add(to);
        command.Add(type);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                if (context.DryRun)
                {
                    CliOutput.WriteResult(context, "dry-run.plan", CommandHelpers.DryRunPlan(
                        CommandHelpers.DryRunOperation(
                            "POST",
                            "/rest/v1/relationships",
                            "Create relationship",
                            CommandHelpers.ObjectPreview(
                                ("fromItem", parseResult.GetValue(from)),
                                ("toItem", parseResult.GetValue(to)),
                                ("relationshipType", parseResult.GetValue(type))))));
                    return;
                }

                var useCases = services.GetRequiredService<RelationshipUseCases>();
                var result = await useCases.CreateAsync(parseResult.GetValue(from), parseResult.GetValue(to), parseResult.GetValue(type)!, ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "relations.create.result", result);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildDeleteCommand(IServiceProvider services, CliRuntime runtime)
    {
        var id = new Argument<int>("id") { Description = "Relationship ID." };
        var command = new Command("delete", "Delete relationship.");
        command.Add(id);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                if (context.DryRun)
                {
                    CliOutput.WriteResult(context, "dry-run.plan", CommandHelpers.DryRunPlan(
                        CommandHelpers.DryRunOperation("DELETE", $"/rest/v1/relationships/{parseResult.GetValue(id)}", "Delete relationship")));
                    return;
                }

                await services.GetRequiredService<RelationshipUseCases>().DeleteAsync(parseResult.GetValue(id), ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "relations.delete.result", new { id = parseResult.GetValue(id), deleted = true });
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildUpdateCommand(IServiceProvider services, CliRuntime runtime)
    {
        var id = new Argument<int>("id") { Description = "Relationship ID." };
        var json = new Option<string>("--json") { Required = true, Description = "Relationship update JSON file or '-'." };
        var command = new Command("update", "Update relationship.");
        command.Add(id);
        command.Add(json);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                if (context.DryRun)
                {
                    CliOutput.WriteResult(context, "dry-run.plan", CommandHelpers.DryRunPlan(
                        CommandHelpers.DryRunOperation("PUT", $"/rest/v1/relationships/{parseResult.GetValue(id)}", "Update relationship", CommandHelpers.ReadJsonObject(parseResult.GetValue(json)))));
                    return;
                }

                var result = await services.GetRequiredService<RelationshipUseCases>()
                    .UpdateAsync(parseResult.GetValue(id), CommandHelpers.ReadJsonObject(parseResult.GetValue(json)), ct)
                    .ConfigureAwait(false);
                CliOutput.WriteResult(context, "relations.update.result", result);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildClearSuspectCommand(IServiceProvider services, CliRuntime runtime)
    {
        var id = new Argument<int>("id") { Description = "Relationship ID." };
        var command = new Command("clear-suspect", "Clear suspect flag.");
        command.Add(id);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                if (context.DryRun)
                {
                    CliOutput.WriteResult(context, "dry-run.plan", CommandHelpers.DryRunPlan(
                        CommandHelpers.DryRunOperation("DELETE", $"/rest/v1/relationships/{parseResult.GetValue(id)}/suspect", "Clear suspect relationship flag")));
                    return;
                }

                await services.GetRequiredService<RelationshipUseCases>().ClearSuspectAsync(parseResult.GetValue(id), ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "relations.clear-suspect.result", new { id = parseResult.GetValue(id), suspect = false });
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildValidateCommand(IServiceProvider services, CliRuntime runtime)
    {
        var from = new Option<int>("--from") { Description = "Source item ID.", Required = true };
        var to = new Option<int>("--to") { Description = "Target item ID.", Required = true };
        var type = new Option<string>("--type") { Description = "Relationship type alias or ID.", Required = true };
        var command = new Command("validate", "Validate relationship locally.");
        command.Add(from);
        command.Add(to);
        command.Add(type);
        command.SetAction((parseResult) =>
        {
            var result = services.GetRequiredService<RelationshipUseCases>().Validate(parseResult.GetValue(from), parseResult.GetValue(to), parseResult.GetValue(type)!);
            CliOutput.WriteResult(runtime.Create(parseResult), "relations.validate.result", result, result.Warnings);
        });
        return command;
    }
}
