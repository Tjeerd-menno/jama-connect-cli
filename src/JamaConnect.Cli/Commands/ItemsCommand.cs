using System.CommandLine;
using System.Text.Json;
using JamaConnect.Application.Items;
using JamaConnect.Application.Relationships;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;
using Microsoft.Extensions.DependencyInjection;

namespace JamaConnect.Cli.Commands;

internal static class ItemsCommandExtensions
{
    public static Command BuildItemsCommand(IServiceProvider services, CliRuntime runtime)
    {
        var command = new Command("items", "Manage Jama Connect items.");
        command.Add(BuildSearchCommand(services, runtime, "search", null));
        command.Add(BuildSearchCommand(services, runtime, "list", null));
        command.Add(BuildGetCommand(services, runtime));
        command.Add(BuildCreateCommand(services, runtime, null));
        command.Add(BuildPatchCommand(services, runtime));
        command.Add(BuildUpdateCommand(services, runtime));
        command.Add(BuildDeleteCommand(services, runtime));
        command.Add(BuildVersionsCommand(services, runtime));
        command.Add(BuildWorkflowCommand(services, runtime));
        return command;
    }

    public static Command BuildTypedCommand(IServiceProvider services, CliRuntime runtime, string name, string alias)
    {
        var command = new Command(name, $"Manage {name}.");
        command.Add(BuildSearchCommand(services, runtime, "list", alias));
        command.Add(BuildGetCommand(services, runtime));
        command.Add(BuildCreateCommand(services, runtime, alias));
        command.Add(BuildPatchCommand(services, runtime));
        command.Add(BuildTypedRelateCommand(services, runtime));
        command.Add(BuildTypedTraceCommand(services, runtime));
        if (name.Equals("test-cases", StringComparison.OrdinalIgnoreCase))
        {
            var steps = new Command("steps", "Manage test case steps.");
            steps.Add(BuildTestCaseStepsGetCommand(services, runtime));
            steps.Add(BuildTestCaseStepsUpdateCommand(services, runtime));
            command.Add(steps);
            command.Add(BuildAddToGroupCommand(services, runtime));
        }

        if (name.Equals("defects", StringComparison.OrdinalIgnoreCase))
        {
            command.Add(BuildDefectCloseCommand(services, runtime));
        }

        return command;
    }

    private static Command BuildTypedRelateCommand(IServiceProvider services, CliRuntime runtime)
    {
        var from = new Option<int>("--from") { Required = true, Description = "Source item ID." };
        var to = new Option<int>("--to") { Required = true, Description = "Target item ID." };
        var relation = new Option<string>("--relation") { Required = true, Description = "Relationship alias or ID." };
        var command = new Command("relate", "Create a relationship.");
        command.Add(from);
        command.Add(to);
        command.Add(relation);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var result = await services.GetRequiredService<RelationshipUseCases>()
                    .CreateAsync(parseResult.GetValue(from), parseResult.GetValue(to), parseResult.GetValue(relation)!, ct)
                    .ConfigureAwait(false);
                CliOutput.WriteResult(context, "relations.create.result", result);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildTypedTraceCommand(IServiceProvider services, CliRuntime runtime)
    {
        var item = new Argument<string>("item") { Description = "Item ID or document key." };
        var command = new Command("trace", "Show trace graph for this artifact.");
        command.Add(item);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var graph = await services.GetRequiredService<JamaConnect.Application.Traceability.TraceUseCases>()
                    .ShowAsync(parseResult.GetValue(item)!, "both", 1, ct)
                    .ConfigureAwait(false);
                CliOutput.WriteResult(context, "trace.graph", graph, graph.Warnings);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildTestCaseStepsGetCommand(IServiceProvider services, CliRuntime runtime)
    {
        var item = new Argument<string>("item") { Description = "Test case item ID or document key." };
        var command = new Command("get", "Get test case steps.");
        command.Add(item);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var result = await services.GetRequiredService<ItemUseCases>()
                    .GetAsync(parseResult.GetValue(item)!, new ItemQueryOptions([], true), ct)
                    .ConfigureAwait(false);
                JsonElement? steps = result is not null && result.Fields.TryGetValue("testCaseSteps", out var value)
                    ? value
                    : null;
                CliOutput.WriteResult<JsonElement?>(context, "test-cases.steps.get.result", steps);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildTestCaseStepsUpdateCommand(IServiceProvider services, CliRuntime runtime)
    {
        var id = new Argument<int>("id") { Description = "Numeric test case item ID." };
        var steps = new Option<string>("--steps") { Required = true, Description = "Steps JSON file or '-'." };
        var command = new Command("update", "Replace test case steps.");
        command.Add(id);
        command.Add(steps);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var operations = new[]
                {
                    new JsonPatchOperation("replace", "/fields/testCaseSteps", CommandHelpers.ReadJsonObject(parseResult.GetValue(steps)))
                };
                if (context.DryRun)
                {
                    CliOutput.WriteResult(context, "dry-run.plan", CommandHelpers.DryRunPlan(
                        CommandHelpers.DryRunOperation("PATCH", $"/rest/v1/items/{parseResult.GetValue(id)}", "Replace test case steps", CommandHelpers.PatchPreview(operations))));
                    return;
                }

                var result = await services.GetRequiredService<ItemUseCases>()
                    .PatchAsync(parseResult.GetValue(id), operations, ct)
                    .ConfigureAwait(false);
                CliOutput.WriteResult(context, "test-cases.steps.update.result", result);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildAddToGroupCommand(IServiceProvider services, CliRuntime runtime)
    {
        var plan = new Option<int>("--plan") { Required = true };
        var group = new Option<int>("--group") { Required = true };
        var testCase = new Option<int>("--test-case") { Required = true };
        var command = new Command("add-to-group", "Add test case to a test plan group.");
        command.Add(plan);
        command.Add(group);
        command.Add(testCase);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                await services.GetRequiredService<JamaConnect.Application.TestManagement.TestManagementUseCases>()
                    .AddCaseAsync(parseResult.GetValue(plan), parseResult.GetValue(group), parseResult.GetValue(testCase), ct)
                    .ConfigureAwait(false);
                CliOutput.WriteResult(context, "test-cases.add-to-group.result", new { added = true, testCase = parseResult.GetValue(testCase), group = parseResult.GetValue(group) });
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildDefectCloseCommand(IServiceProvider services, CliRuntime runtime)
    {
        var id = new Argument<int>("id") { Description = "Numeric defect item ID." };
        var statusField = new Option<string>("--status-field") { DefaultValueFactory = _ => "status", Description = "Configured workflow/status field name." };
        var status = new Option<string>("--status") { DefaultValueFactory = _ => "Closed", Description = "Closed status value." };
        var command = new Command("close", "Close a defect through configured field patching.");
        command.Add(id);
        command.Add(statusField);
        command.Add(status);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var operations = new[]
                {
                    new JsonPatchOperation("replace", $"/fields/{parseResult.GetValue(statusField)}", CommandHelpers.StringElement(parseResult.GetValue(status)))
                };
                if (context.DryRun)
                {
                    CliOutput.WriteResult(context, "dry-run.plan", CommandHelpers.DryRunPlan(
                        CommandHelpers.DryRunOperation("PATCH", $"/rest/v1/items/{parseResult.GetValue(id)}", "Close defect through configured field patching", CommandHelpers.PatchPreview(operations))));
                    return;
                }

                var result = await services.GetRequiredService<ItemUseCases>()
                    .PatchAsync(parseResult.GetValue(id), operations, ct)
                    .ConfigureAwait(false);
                CliOutput.WriteResult(context, "defects.close.result", result);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildSearchCommand(IServiceProvider services, CliRuntime runtime, string name, string? forcedType)
    {
        var project = CommandHelpers.ProjectOption();
        var type = new Option<string?>("--type") { Description = "Item type alias, id, or name." };
        var documentKey = new Option<string?>("--document-key") { Description = "Jama document key." };
        var contains = new Option<string?>("--contains") { Description = "Full-text contains query." };
        var createdSince = new Option<DateTimeOffset?>("--created-since") { Description = "Created date lower bound." };
        var modifiedSince = new Option<DateTimeOffset?>("--modified-since") { Description = "Modified date lower bound." };
        var rootOnly = new Option<bool>("--root-only") { Description = "Restrict to root items when supported by Jama." };
        var include = new Option<string?>("--include") { Description = "Comma-separated include links." };
        var startAt = CommandHelpers.StartAtOption();
        var pageSize = CommandHelpers.PageSizeOption();
        var limit = CommandHelpers.LimitOption();
        var all = CommandHelpers.AllOption();
        var command = new Command(name, "Search items.");
        command.Add(project);
        command.Add(type);
        command.Add(documentKey);
        command.Add(contains);
        command.Add(createdSince);
        command.Add(modifiedSince);
        command.Add(rootOnly);
        command.Add(include);
        command.Add(all);
        command.Add(startAt);
        command.Add(pageSize);
        command.Add(limit);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var useCases = services.GetRequiredService<ItemUseCases>();
                var criteria = new ItemSearchCriteria(
                    parseResult.GetValue(project),
                    forcedType ?? parseResult.GetValue(type),
                    parseResult.GetValue(documentKey),
                    parseResult.GetValue(contains),
                    parseResult.GetValue(createdSince),
                    parseResult.GetValue(modifiedSince),
                    parseResult.GetValue(rootOnly),
                    CommandHelpers.SplitCsv(parseResult.GetValue(include)));
                var pageRequest = CommandHelpers.Page(parseResult, startAt, pageSize, limit);
                var page = parseResult.GetValue(all)
                    ? await ReadAllAsync(
                        (start, size, token) => useCases.SearchAsync(criteria, new PageRequest(start, size, null), token),
                        pageRequest,
                        services.GetRequiredService<IJamaPaginator>(),
                        ct).ConfigureAwait(false)
                    : await useCases.SearchAsync(criteria, pageRequest, ct).ConfigureAwait(false);
                CliOutput.WritePage(context, "items.search.result", page);
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
        var identifier = new Argument<string>("identifier") { Description = "Numeric ID or document key." };
        var include = new Option<string?>("--include") { Description = "Comma-separated include links." };
        var command = new Command("get", "Get one item.");
        command.Add(identifier);
        command.Add(include);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var useCases = services.GetRequiredService<ItemUseCases>();
                var item = await useCases.GetAsync(parseResult.GetValue(identifier)!, new ItemQueryOptions(CommandHelpers.SplitCsv(parseResult.GetValue(include)), true), ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "items.get.result", item);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildCreateCommand(IServiceProvider services, CliRuntime runtime, string? forcedType)
    {
        var project = CommandHelpers.ProjectOption();
        project.Required = true;
        var type = new Option<string?>("--type") { Description = "Item type alias or id.", Required = forcedType is null };
        var parent = new Option<int?>("--parent") { Description = "Parent item ID." };
        var title = new Option<string?>("--title") { Description = "Title/name field." };
        var description = new Option<string?>("--description") { Description = "Description text." };
        var field = new Option<string[]>("--field") { Description = "Additional name=value field.", AllowMultipleArgumentsPerToken = false };
        var json = new Option<string?>("--json") { Description = "Raw RequestItem JSON file or '-'." };
        var command = new Command("create", "Create an item.");
        command.Add(project);
        command.Add(type);
        command.Add(parent);
        command.Add(title);
        command.Add(description);
        command.Add(field);
        command.Add(json);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                if (context.DryRun)
                {
                    CliOutput.WriteResult(context, "dry-run.plan", CommandHelpers.DryRunPlan(
                        CommandHelpers.DryRunOperation(
                            "POST",
                            "/rest/v1/items",
                            "Create item",
                            CommandHelpers.ObjectPreview(
                                ("project", parseResult.GetValue(project)),
                                ("itemType", forcedType ?? parseResult.GetValue(type)),
                                ("parent", parseResult.GetValue(parent)),
                                ("title", CommandHelpers.ReadOptionalTextValue(parseResult.GetValue(title)))))));
                    return;
                }

                var rawJson = parseResult.GetValue(json);
                if (!string.IsNullOrWhiteSpace(rawJson))
                {
                    var writer = services.GetRequiredService<IItemWriter>();
                    var result = await writer.CreateItemAsync(ReadRawCreateItem(CommandHelpers.ReadJsonObject(rawJson)), ct).ConfigureAwait(false);
                    CliOutput.WriteResult(context, "items.create.result", result);
                    return;
                }

                var fields = CommandHelpers.ReadFields(parseResult.GetValue(field) ?? []);
                if (!string.IsNullOrWhiteSpace(parseResult.GetValue(title)))
                {
                    fields["name"] = CommandHelpers.StringElement(CommandHelpers.ReadOptionalTextValue(parseResult.GetValue(title)));
                }

                if (!string.IsNullOrWhiteSpace(parseResult.GetValue(description)))
                {
                    fields["description"] = CommandHelpers.StringElement(CommandHelpers.ReadOptionalTextValue(parseResult.GetValue(description)));
                }

                var useCases = services.GetRequiredService<ItemUseCases>();
                var resultElement = await useCases.CreateAsync(
                    parseResult.GetValue(project)!.Value,
                    forcedType ?? parseResult.GetValue(type)!,
                    parseResult.GetValue(parent),
                    fields,
                    ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "items.create.result", resultElement);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static CreateItemRequest ReadRawCreateItem(JsonElement element)
    {
        return new CreateItemRequest(
            element.GetProperty("project").GetInt32(),
            element.GetProperty("itemType").GetInt32(),
            element.TryGetProperty("location", out var location) && location.TryGetProperty("parent", out var parent) ? parent.GetInt32() : null,
            element.TryGetProperty("fields", out var fields) && fields.ValueKind == JsonValueKind.Object
                ? fields.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.Clone(), StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase),
            element.TryGetProperty("globalId", out var globalId) ? globalId.GetString() : null);
    }

    private static Command BuildPatchCommand(IServiceProvider services, CliRuntime runtime)
    {
        var identifier = new Argument<int>("id") { Description = "Numeric item ID." };
        var set = new Option<string[]>("--set") { Description = "Path=value patch entry.", AllowMultipleArgumentsPerToken = false };
        var command = new Command("patch", "Patch an item.");
        command.Add(identifier);
        command.Add(set);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var operations = (parseResult.GetValue(set) ?? []).Select(value =>
                {
                    var parts = value.Split('=', 2);
                    if (parts.Length != 2)
                    {
                        throw new InvalidOperationException("--set values must use path=value syntax.");
                    }

                    return new JsonPatchOperation("replace", "/" + parts[0].Replace('.', '/'), CommandHelpers.StringElement(CommandHelpers.ReadTextValue(parts[1])));
                }).ToArray();
                if (context.DryRun)
                {
                    CliOutput.WriteResult(context, "dry-run.plan", CommandHelpers.DryRunPlan(
                        CommandHelpers.DryRunOperation("PATCH", $"/rest/v1/items/{parseResult.GetValue(identifier)}", "Patch item", CommandHelpers.PatchPreview(operations))));
                    return;
                }

                var useCases = services.GetRequiredService<ItemUseCases>();
                var result = await useCases.PatchAsync(parseResult.GetValue(identifier), operations, ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "items.patch.result", result);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildUpdateCommand(IServiceProvider services, CliRuntime runtime)
    {
        var identifier = new Argument<int>("id") { Description = "Numeric item ID." };
        var json = new Option<string>("--json") { Description = "Request JSON file or '-'.", Required = true };
        var command = new Command("update", "Fully update an item.");
        command.Add(identifier);
        command.Add(json);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                if (context.DryRun)
                {
                    CliOutput.WriteResult(context, "dry-run.plan", CommandHelpers.DryRunPlan(
                        CommandHelpers.DryRunOperation("PUT", $"/rest/v1/items/{parseResult.GetValue(identifier)}", "Fully update item", CommandHelpers.ReadJsonObject(parseResult.GetValue(json)))));
                    return;
                }

                var writer = services.GetRequiredService<IItemWriter>();
                var result = await writer.UpdateItemAsync(parseResult.GetValue(identifier), CommandHelpers.ReadJsonObject(parseResult.GetValue(json)), ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "items.update.result", result);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildDeleteCommand(IServiceProvider services, CliRuntime runtime)
    {
        var id = new Argument<int>("id") { Description = "Numeric item ID." };
        var yes = new Option<bool>("--yes") { Description = "Confirm deletion without prompting." };
        var command = new Command("delete", "Delete an item.");
        command.Add(id);
        command.Add(yes);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                if (!parseResult.GetValue(yes) && !context.Agent)
                {
                    throw new InvalidOperationException("Use --yes to confirm item deletion.");
                }

                if (context.DryRun)
                {
                    CliOutput.WriteResult(context, "dry-run.plan", CommandHelpers.DryRunPlan(
                        CommandHelpers.DryRunOperation("DELETE", $"/rest/v1/items/{parseResult.GetValue(id)}", "Delete item")));
                    return;
                }

                await services.GetRequiredService<ItemUseCases>().DeleteAsync(parseResult.GetValue(id), ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "items.delete.result", new { id = parseResult.GetValue(id), deleted = true });
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildVersionsCommand(IServiceProvider services, CliRuntime runtime)
    {
        var id = new Argument<int>("id") { Description = "Numeric item ID." };
        var startAt = CommandHelpers.StartAtOption();
        var pageSize = CommandHelpers.PageSizeOption();
        var limit = CommandHelpers.LimitOption();
        var command = new Command("versions", "List item versions.");
        command.Add(id);
        command.Add(startAt);
        command.Add(pageSize);
        command.Add(limit);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var reader = services.GetRequiredService<IItemReader>();
                var page = await reader.GetVersionsAsync(new ItemIdentifier(parseResult.GetValue(id), null), CommandHelpers.Page(parseResult, startAt, pageSize, limit), ct).ConfigureAwait(false);
                CliOutput.WritePage(context, "items.versions.result", page);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildWorkflowCommand(IServiceProvider services, CliRuntime runtime)
    {
        var command = new Command("workflow", "Workflow operations.");
        var options = new Command("options", "Return workflow option discovery guidance.");
        options.SetAction((parseResult) =>
        {
            CliOutput.WriteResult(
                runtime.Create(parseResult),
                "items.workflow.options.result",
                Array.Empty<object>(),
                ["Workflow option discovery depends on Jama workflow configuration; use schema fields and configured field patching when no transition endpoint is available."]);
        });
        command.Add(options);
        command.Add(BuildWorkflowTransitionCommand(services, runtime));
        return command;
    }

    private static Command BuildWorkflowTransitionCommand(IServiceProvider services, CliRuntime runtime)
    {
        var id = new Argument<int>("id") { Description = "Numeric item ID." };
        var statusField = new Option<string>("--status-field") { DefaultValueFactory = _ => "status", Description = "Configured workflow/status field name." };
        var status = new Option<string>("--status") { Required = true, Description = "Target status value." };
        var command = new Command("transition", "Apply a workflow/status transition through configured field patching.");
        command.Add(id);
        command.Add(statusField);
        command.Add(status);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var operations = new[]
                {
                    new JsonPatchOperation("replace", $"/fields/{parseResult.GetValue(statusField)}", CommandHelpers.StringElement(parseResult.GetValue(status)))
                };
                if (context.DryRun)
                {
                    CliOutput.WriteResult(context, "dry-run.plan", CommandHelpers.DryRunPlan(
                        CommandHelpers.DryRunOperation("PATCH", $"/rest/v1/items/{parseResult.GetValue(id)}", "Apply workflow/status field patch", CommandHelpers.PatchPreview(operations))));
                    return;
                }

                var result = await services.GetRequiredService<ItemUseCases>()
                    .PatchAsync(parseResult.GetValue(id), operations, ct)
                    .ConfigureAwait(false);
                CliOutput.WriteResult(context, "items.workflow.transition.result", result, ["Applied as configured field patching; Jama server workflow rules still decide whether the change is allowed."]);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }
}
