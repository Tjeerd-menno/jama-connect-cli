using System.CommandLine;
using System.Text.Json;
using JamaConnect.Application.TestManagement;
using JamaConnect.Application.Traceability;
using JamaConnect.Domain.Models;
using Microsoft.Extensions.DependencyInjection;

namespace JamaConnect.Cli.Commands;

internal static class TestManagementCommandExtensions
{
    public static Command BuildTestPlansCommand(IServiceProvider services, CliRuntime runtime)
    {
        var command = new Command("test-plans", "Manage test plans.");
        command.Add(BuildPlanListCommand(services, runtime));
        command.Add(BuildPlanGetCommand(services, runtime));
        command.Add(BuildPlanCreateCommand(services, runtime));
        var groups = new Command("groups", "Manage test groups.");
        groups.Add(BuildGroupsListCommand(services, runtime));
        groups.Add(BuildGroupsCreateCommand(services, runtime));
        groups.Add(BuildGroupsAddCaseCommand(services, runtime));
        command.Add(groups);
        var cycles = new Command("cycles", "Manage test cycles for a plan.");
        cycles.Add(BuildCyclesListCommand(services, runtime));
        cycles.Add(BuildCyclesCreateCommand(services, runtime));
        command.Add(cycles);
        return command;
    }

    public static Command BuildTestCyclesCommand(IServiceProvider services, CliRuntime runtime)
    {
        var command = new Command("test-cycles", "Manage test cycles.");
        command.Add(BuildCyclesListCommand(services, runtime));
        command.Add(BuildCyclesCreateCommand(services, runtime));
        var runs = new Command("runs", "Cycle run commands.");
        runs.Add(BuildRunsListCommand(services, runtime));
        runs.Add(BuildCycleRunsSyncCommand(services, runtime));
        command.Add(runs);
        return command;
    }

    public static Command BuildTestRunsCommand(IServiceProvider services, CliRuntime runtime)
    {
        var command = new Command("test-runs", "Manage test runs.");
        command.Add(BuildRunsListCommand(services, runtime));
        command.Add(BuildRunsFindCommand(services, runtime));
        command.Add(BuildRunGetCommand(services, runtime));
        command.Add(BuildRunUpdateCommand(services, runtime));
        var steps = new Command("steps", "Manage test run steps.");
        steps.Add(BuildRunStepsGetCommand(services, runtime));
        steps.Add(BuildRunStepsUpdateCommand(services, runtime));
        command.Add(steps);
        command.Add(new Command("relate", "Use relations create."));
        command.Add(BuildRunTraceCommand(services, runtime));
        return command;
    }

    private static Command BuildPlanListCommand(IServiceProvider services, CliRuntime runtime)
    {
        var project = CommandHelpers.ProjectOption();
        project.Required = true;
        var startAt = CommandHelpers.StartAtOption();
        var pageSize = CommandHelpers.PageSizeOption();
        var limit = CommandHelpers.LimitOption();
        var command = new Command("list", "List test plans.");
        command.Add(project);
        command.Add(startAt);
        command.Add(pageSize);
        command.Add(limit);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var page = await services.GetRequiredService<TestManagementUseCases>()
                    .ListPlansAsync(parseResult.GetValue(project)!.Value, CommandHelpers.Page(parseResult, startAt, pageSize, limit), ct)
                    .ConfigureAwait(false);
                CliOutput.WritePage(context, "test-plans.list.result", page);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildPlanGetCommand(IServiceProvider services, CliRuntime runtime)
    {
        var id = new Argument<int>("id");
        var command = new Command("get", "Get test plan.");
        command.Add(id);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var plan = await services.GetRequiredService<TestManagementUseCases>().GetPlanAsync(parseResult.GetValue(id), ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "test-plans.get.result", plan);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildPlanCreateCommand(IServiceProvider services, CliRuntime runtime)
    {
        var json = new Option<string>("--json") { Description = "Request JSON file or '-'.", Required = true };
        var command = new Command("create", "Create test plan.");
        command.Add(json);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var result = await services.GetRequiredService<TestManagementUseCases>().CreatePlanAsync(CommandHelpers.ReadJsonObject(parseResult.GetValue(json)), ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "test-plans.create.result", result);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildGroupsListCommand(IServiceProvider services, CliRuntime runtime)
    {
        var plan = new Option<int>("--plan") { Required = true };
        var startAt = CommandHelpers.StartAtOption();
        var pageSize = CommandHelpers.PageSizeOption();
        var limit = CommandHelpers.LimitOption();
        var command = new Command("list", "List test groups.");
        command.Add(plan);
        command.Add(startAt);
        command.Add(pageSize);
        command.Add(limit);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var page = await services.GetRequiredService<TestManagementUseCases>().ListGroupsAsync(parseResult.GetValue(plan), CommandHelpers.Page(parseResult, startAt, pageSize, limit), ct).ConfigureAwait(false);
                CliOutput.WritePage(context, "test-plans.groups.list.result", page);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildGroupsCreateCommand(IServiceProvider services, CliRuntime runtime)
    {
        var plan = new Option<int>("--plan") { Required = true };
        var json = new Option<string>("--json") { Required = true };
        var command = new Command("create", "Create test group.");
        command.Add(plan);
        command.Add(json);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var result = await services.GetRequiredService<TestManagementUseCases>().CreateGroupAsync(parseResult.GetValue(plan), CommandHelpers.ReadJsonObject(parseResult.GetValue(json)), ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "test-plans.groups.create.result", result);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildGroupsAddCaseCommand(IServiceProvider services, CliRuntime runtime)
    {
        var plan = new Option<int>("--plan") { Required = true };
        var group = new Option<int>("--group") { Required = true };
        var testCase = new Option<int>("--test-case") { Required = true };
        var command = new Command("add-case", "Add test case to group.");
        command.Add(plan);
        command.Add(group);
        command.Add(testCase);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                await services.GetRequiredService<TestManagementUseCases>().AddCaseAsync(parseResult.GetValue(plan), parseResult.GetValue(group), parseResult.GetValue(testCase), ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "test-plans.groups.add-case.result", new { added = true, testCase = parseResult.GetValue(testCase) });
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildCyclesListCommand(IServiceProvider services, CliRuntime runtime)
    {
        var plan = new Option<int>("--plan") { Required = true };
        var startAt = CommandHelpers.StartAtOption();
        var pageSize = CommandHelpers.PageSizeOption();
        var limit = CommandHelpers.LimitOption();
        var command = new Command("list", "List test cycles.");
        command.Add(plan);
        command.Add(startAt);
        command.Add(pageSize);
        command.Add(limit);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var page = await services.GetRequiredService<TestManagementUseCases>().ListCyclesAsync(parseResult.GetValue(plan), CommandHelpers.Page(parseResult, startAt, pageSize, limit), ct).ConfigureAwait(false);
                CliOutput.WritePage(context, "test-cycles.list.result", page);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildCyclesCreateCommand(IServiceProvider services, CliRuntime runtime)
    {
        var plan = new Option<int>("--plan") { Required = true };
        var json = new Option<string>("--json") { Required = true };
        var command = new Command("create", "Create test cycle.");
        command.Add(plan);
        command.Add(json);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var result = await services.GetRequiredService<TestManagementUseCases>().CreateCycleAsync(parseResult.GetValue(plan), CommandHelpers.ReadJsonObject(parseResult.GetValue(json)), ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "test-cycles.create.result", result);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildRunsListCommand(IServiceProvider services, CliRuntime runtime)
        => BuildRunsListCommand(services, runtime, "list", "List test runs.");

    private static Command BuildRunsListCommand(IServiceProvider services, CliRuntime runtime, string name, string description)
    {
        var plan = new Option<int?>("--plan");
        var cycle = new Option<int?>("--cycle");
        var testCase = new Option<int?>("--test-case");
        var startAt = CommandHelpers.StartAtOption();
        var pageSize = CommandHelpers.PageSizeOption();
        var limit = CommandHelpers.LimitOption();
        var command = new Command(name, description);
        command.Add(plan);
        command.Add(cycle);
        command.Add(testCase);
        command.Add(startAt);
        command.Add(pageSize);
        command.Add(limit);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var page = await services.GetRequiredService<TestManagementUseCases>().ListRunsAsync(
                    new TestRunQuery(parseResult.GetValue(plan), parseResult.GetValue(cycle), parseResult.GetValue(testCase)),
                    CommandHelpers.Page(parseResult, startAt, pageSize, limit),
                    ct).ConfigureAwait(false);
                CliOutput.WritePage(context, "test-runs.list.result", page);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildRunsFindCommand(IServiceProvider services, CliRuntime runtime)
    {
        return BuildRunsListCommand(services, runtime, "find", "Find test runs.");
    }

    private static Command BuildRunUpdateCommand(IServiceProvider services, CliRuntime runtime)
    {
        var id = new Argument<int>("id");
        var json = new Option<string>("--json") { Required = true };
        var command = new Command("update", "Update test run.");
        command.Add(id);
        command.Add(json);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var result = await services.GetRequiredService<TestManagementUseCases>().UpdateRunAsync(parseResult.GetValue(id), CommandHelpers.ReadJsonObject(parseResult.GetValue(json)), ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "test-runs.update.result", result);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildCycleRunsSyncCommand(IServiceProvider services, CliRuntime runtime)
    {
        var cycle = new Option<int>("--cycle") { Required = true, Description = "Test cycle ID." };
        var json = new Option<string>("--json") { Required = true, Description = "Cycle update JSON file or '-'." };
        var command = new Command("sync", "Update a cycle and let Jama sync generated runs.");
        command.Add(cycle);
        command.Add(json);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var result = await services.GetRequiredService<TestManagementUseCases>()
                    .SyncCycleAsync(parseResult.GetValue(cycle), CommandHelpers.ReadJsonObject(parseResult.GetValue(json)), ct)
                    .ConfigureAwait(false);
                CliOutput.WriteResult(context, "test-cycles.runs.sync.result", result, ["Jama may add or remove generated test runs based on the submitted cycle configuration."]);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildRunTraceCommand(IServiceProvider services, CliRuntime runtime)
    {
        var run = new Argument<string>("run") { Description = "Test run ID." };
        var command = new Command("trace", "Show trace graph for a test run.");
        command.Add(run);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var graph = await services.GetRequiredService<TraceUseCases>()
                    .ShowAsync(parseResult.GetValue(run)!, "both", 1, ct)
                    .ConfigureAwait(false);
                CliOutput.WriteResult(context, "trace.graph", graph, graph.Warnings);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildRunGetCommand(IServiceProvider services, CliRuntime runtime)
    {
        var id = new Argument<int>("id");
        var command = new Command("get", "Get test run.");
        command.Add(id);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var result = await services.GetRequiredService<TestManagementUseCases>()
                    .GetRunAsync(parseResult.GetValue(id), ct)
                    .ConfigureAwait(false);
                CliOutput.WriteResult(context, "test-runs.get.result", result);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildRunStepsGetCommand(IServiceProvider services, CliRuntime runtime)
    {
        var id = new Argument<int>("id");
        var command = new Command("get", "Get test run steps.");
        command.Add(id);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var result = await services.GetRequiredService<TestManagementUseCases>()
                    .GetRunAsync(parseResult.GetValue(id), ct)
                    .ConfigureAwait(false);
                CliOutput.WriteResult(context, "test-runs.steps.get.result", result?.Steps ?? []);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildRunStepsUpdateCommand(IServiceProvider services, CliRuntime runtime)
    {
        var id = new Argument<int>("id");
        var steps = new Option<string>("--steps") { Required = true, Description = "Step update JSON file or '-'." };
        var command = new Command("update", "Merge and update test run steps.");
        command.Add(id);
        command.Add(steps);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var updates = ReadStepUpdates(CommandHelpers.ReadJsonObject(parseResult.GetValue(steps)));
                var result = await services.GetRequiredService<TestManagementUseCases>()
                    .UpdateRunStepsAsync(parseResult.GetValue(id), updates, ct)
                    .ConfigureAwait(false);
                CliOutput.WriteResult(context, "test-runs.steps.update.result", result);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static List<TestRunStepUpdate> ReadStepUpdates(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Step updates must be a JSON array.");
        }

        var updates = new List<TestRunStepUpdate>();
        foreach (var update in element.EnumerateArray())
        {
            updates.Add(new TestRunStepUpdate(
                update.GetProperty("index").GetInt32(),
                update.TryGetProperty("status", out var status) ? status.GetString() : null,
                update.TryGetProperty("actualResult", out var actualResult) ? actualResult.GetString() : null));
        }

        return updates;
    }
}
