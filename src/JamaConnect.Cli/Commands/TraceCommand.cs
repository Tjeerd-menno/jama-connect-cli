using System.CommandLine;
using JamaConnect.Application.Traceability;
using JamaConnect.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace JamaConnect.Cli.Commands;

internal static class TraceCommandExtensions
{
    public static Command BuildTraceCommand(IServiceProvider services, CliRuntime runtime)
    {
        var command = new Command("trace", "Inspect traceability.");
        command.Add(BuildShowCommand(services, runtime));
        command.Add(BuildGapsCommand(services, runtime));
        command.Add(BuildMatrixCommand(services, runtime));
        command.Add(BuildCoverageCommand(services, runtime));
        command.Add(BuildVerificationSummaryCommand(services, runtime));
        return command;
    }

    private static Command BuildShowCommand(IServiceProvider services, CliRuntime runtime)
    {
        var item = new Option<string>("--item") { Description = "Root item ID or document key.", Required = true };
        var direction = new Option<string>("--direction") { Description = "upstream, downstream, or both.", DefaultValueFactory = _ => "both" };
        var depth = new Option<int>("--depth") { Description = "Traversal depth.", DefaultValueFactory = _ => 1 };
        var command = new Command("show", "Show trace graph.");
        command.Add(item);
        command.Add(direction);
        command.Add(depth);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var graph = await services.GetRequiredService<TraceUseCases>()
                    .ShowAsync(parseResult.GetValue(item)!, parseResult.GetValue(direction) ?? "both", parseResult.GetValue(depth), ct)
                    .ConfigureAwait(false);
                CliOutput.WriteResult(context, "trace.graph", graph, graph.Warnings);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildGapsCommand(IServiceProvider services, CliRuntime runtime)
    {
        var project = CommandHelpers.ProjectOption();
        project.Required = true;
        var command = new Command("gaps", "Find traceability gaps.");
        command.Add(project);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var result = await services.GetRequiredService<TraceUseCases>()
                    .FindGapsAsync(parseResult.GetValue(project)!.Value, ct)
                    .ConfigureAwait(false);
                CliOutput.WriteResult(context, "trace.gaps", result, result.Warnings);
                if (result.Summary.Gaps > 0)
                {
                    Environment.ExitCode = 4;
                }
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildMatrixCommand(IServiceProvider services, CliRuntime runtime)
    {
        var project = CommandHelpers.ProjectOption();
        project.Required = true;
        var from = new Option<string>("--from") { Required = true, Description = "Source item type alias or id." };
        var to = new Option<string>("--to") { Required = true, Description = "Target item type alias or id." };
        var relation = new Option<string>("--relation") { Required = true, Description = "Relationship alias or id." };
        var command = new Command("matrix", "Generate a simple trace matrix.");
        command.Add(project);
        command.Add(from);
        command.Add(to);
        command.Add(relation);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var result = await services.GetRequiredService<TraceUseCases>()
                    .MatrixAsync(parseResult.GetValue(project)!.Value, parseResult.GetValue(from)!, parseResult.GetValue(to)!, parseResult.GetValue(relation)!, ct)
                    .ConfigureAwait(false);
                CliOutput.WriteResult(context, "trace.matrix", result);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildCoverageCommand(IServiceProvider services, CliRuntime runtime)
    {
        var project = CommandHelpers.ProjectOption();
        project.Required = true;
        var command = new Command("coverage", "Summarize configured traceability rule coverage.");
        command.Add(project);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var result = await services.GetRequiredService<TraceUseCases>()
                    .CoverageAsync(parseResult.GetValue(project)!.Value, ct)
                    .ConfigureAwait(false);
                CliOutput.WriteResult(context, "trace.coverage", result, result.Warnings);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildVerificationSummaryCommand(IServiceProvider services, CliRuntime runtime)
    {
        var project = CommandHelpers.ProjectOption();
        project.Required = true;
        var testCycle = new Option<int?>("--test-cycle") { Description = "Test cycle ID." };
        var command = new Command("verification-summary", "Summarize trace and test-run verification state.");
        command.Add(project);
        command.Add(testCycle);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var result = await services.GetRequiredService<TraceUseCases>()
                    .VerificationSummaryAsync(
                        parseResult.GetValue(project)!.Value,
                        parseResult.GetValue(testCycle),
                        services.GetRequiredService<ITestManagementReader>(),
                        ct)
                    .ConfigureAwait(false);
                CliOutput.WriteResult(context, "trace.verification-summary", result, result.Warnings);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }
}
