using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using JamaConnect.Application.Evidence;
using Microsoft.Extensions.DependencyInjection;

namespace JamaConnect.Cli.Commands;

internal static class AgentCommandExtensions
{
    public static Command BuildCommandsCommand(CliRuntime runtime)
    {
        var command = new Command("commands", "Machine-readable command metadata.");
        command.Add(BuildListCommand(runtime));
        command.Add(BuildDescribeCommand(runtime));
        return command;
    }

    public static Command BuildSchemasCommand(CliRuntime runtime)
    {
        var command = new Command("schemas", "Export JSON Schemas.");
        var export = new Command("export", "Export JSON Schema bundle.");
        var output = new Option<string?>("--output") { Description = "Output file." };
        export.Add(output);
        export.SetAction((parseResult) =>
        {
            var context = runtime.Create(parseResult);
            var schema = new
            {
                apiVersion = CliRuntime.ApiVersion,
                schemas = new Dictionary<string, object>
                {
                    ["configuration"] = JsonSchemas.Configuration,
                    ["itemAlias"] = JsonSchemas.ItemAlias,
                    ["relationshipAlias"] = JsonSchemas.RelationshipAlias,
                    ["commandOutput"] = JsonSchemas.CommandOutput,
                    ["errorOutput"] = JsonSchemas.ErrorOutput,
                    ["evidenceExport"] = JsonSchemas.EvidenceExport,
                    ["testCaseSteps"] = JsonSchemas.TestCaseSteps,
                    ["testRunStepsUpdate"] = JsonSchemas.TestRunStepsUpdate
                }
            };
            var path = parseResult.GetValue(output);
            if (!string.IsNullOrWhiteSpace(path))
            {
                File.WriteAllText(path, JsonSerializer.Serialize(schema, FileJsonOptions));
            }

            CliOutput.WriteResult(context, "schemas.export.result", schema);
        });
        command.Add(export);
        return command;
    }

    private static readonly JsonSerializerOptions FileJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public static Command BuildEvidenceCommand(IServiceProvider services, CliRuntime runtime)
    {
        var command = new Command("evidence", "Evidence export commands.");
        var export = new Command("export", "Export verification evidence.");
        var project = CommandHelpers.ProjectOption();
        project.Required = true;
        var testCycle = new Option<int?>("--test-cycle") { Description = "Test cycle ID." };
        var include = new Option<string?>("--include") { Description = "Comma-separated evidence sections." };
        var output = new Option<string?>("--output") { Description = "Output JSON file." };
        export.Add(project);
        export.Add(testCycle);
        export.Add(include);
        export.Add(output);
        export.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var result = await services.GetRequiredService<EvidenceUseCases>()
                    .ExportAsync(
                        parseResult.GetValue(project)!.Value,
                        parseResult.GetValue(testCycle),
                        context.Profile,
                        CommandHelpers.SplitCsv(parseResult.GetValue(include)),
                        Environment.GetCommandLineArgs(),
                        ct)
                    .ConfigureAwait(false);
                var outputPath = parseResult.GetValue(output);
                if (!string.IsNullOrWhiteSpace(outputPath))
                {
                    File.WriteAllText(outputPath, JsonSerializer.Serialize(result, FileJsonOptions));
                }

                CliOutput.WriteResult(context, "evidence.export.result", result, result.Warnings);
            }, cancellationToken).ConfigureAwait(false);
        });
        command.Add(export);
        return command;
    }

    private static Command BuildListCommand(CliRuntime runtime)
    {
        var command = new Command("list", "List command groups.");
        command.SetAction((parseResult) =>
        {
            var groups = CommandCatalog.All.Keys.ToArray();
            CliOutput.WriteResult(runtime.Create(parseResult), "commands.list.result", groups);
        });
        return command;
    }

    private static Command BuildDescribeCommand(CliRuntime runtime)
    {
        var commandName = new Argument<string[]>("command") { Description = "Command path." };
        var command = new Command("describe", "Describe a command path.");
        command.Add(commandName);
        command.SetAction((parseResult) =>
        {
            var path = (parseResult.GetValue(commandName) ?? [])
                .TakeWhile(x => !x.StartsWith('-'))
                .ToArray();
            var key = string.Join(' ', path);
            var found = CommandCatalog.All.TryGetValue(key, out var metadata);
            CliOutput.WriteResult(runtime.Create(parseResult), "commands.describe.result", new
            {
                command = path,
                found,
                metadata
            });
        });
        return command;
    }

    private static class CommandCatalog
    {
        public static readonly IReadOnlyDictionary<string, object> All = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["login"] = new { description = "Authenticate with Jama Connect.", writes = false },
            ["config validate"] = new { description = "Validate configured aliases against Jama schema.", writes = false, outputKind = "config.validate.result" },
            ["schema item-types list"] = new { description = "List Jama item types.", writes = false, outputKind = "schema.item-types.list.result" },
            ["schema relationship-types list"] = new { description = "List Jama relationship types.", writes = false, outputKind = "schema.relationship-types.list.result" },
            ["schema picklists list"] = new { description = "List Jama picklists.", writes = false, outputKind = "schema.picklists.list.result" },
            ["projects list"] = new { description = "List projects.", writes = false, outputKind = "projects.list.result" },
            ["items search"] = new { description = "Search items through abstract item search.", writes = false, outputKind = "items.search.result" },
            ["items get"] = new { description = "Get item by id or document key.", writes = false, outputKind = "items.get.result" },
            ["items create"] = new { description = "Create an item.", writes = true, dryRun = true, outputKind = "items.create.result" },
            ["items patch"] = new { description = "Patch an item with JSON Patch replace operations.", writes = true, outputKind = "items.patch.result" },
            ["items delete"] = new { description = "Delete an item.", writes = true, confirm = "--yes", outputKind = "items.delete.result" },
            ["relations create"] = new { description = "Create a relationship.", writes = true, outputKind = "relations.create.result" },
            ["relations list"] = new { description = "List item relationships.", writes = false, outputKind = "relations.list.result" },
            ["trace show"] = new { description = "Show a trace graph from one item.", writes = false, outputKind = "trace.graph" },
            ["trace gaps"] = new { description = "Evaluate configured traceability rules.", writes = false, outputKind = "trace.gaps" },
            ["trace matrix"] = new { description = "Generate a simple trace matrix.", writes = false, outputKind = "trace.matrix" },
            ["trace coverage"] = new { description = "Summarize configured trace coverage.", writes = false, outputKind = "trace.coverage" },
            ["test-runs steps update"] = new { description = "Merge step updates into an existing test run.", writes = true, outputKind = "test-runs.steps.update.result" },
            ["evidence export"] = new { description = "Export engineering evidence JSON.", writes = false, outputKind = "evidence.export.result" },
            ["schemas export"] = new { description = "Export command/input/output JSON Schema bundle.", writes = false, outputKind = "schemas.export.result" }
        };
    }

    private static class JsonSchemas
    {
        public static readonly object Configuration = new
        {
            type = "object",
            properties = new
            {
                JamaCli = new
                {
                    type = "object",
                    properties = new
                    {
                        DefaultProfile = new { type = "string" },
                        Profiles = new { type = "object" },
                        Aliases = new { type = "object" },
                        TraceabilityRules = new { type = "array" }
                    }
                }
            }
        };

        public static readonly object ItemAlias = new
        {
            type = "object",
            required = new[] { "itemTypeId" },
            properties = new
            {
                itemTypeId = new { type = "integer" },
                displayName = new { type = "string" },
                requiredFields = new { type = "object", additionalProperties = new { type = "string" } }
            }
        };

        public static readonly object RelationshipAlias = new
        {
            type = "object",
            required = new[] { "relationshipTypeId" },
            properties = new
            {
                relationshipTypeId = new { type = "integer" },
                from = new { type = "string" },
                to = new { type = "string" }
            }
        };

        public static readonly object CommandOutput = new
        {
            type = "object",
            required = new[] { "kind", "apiVersion", "profile" },
            properties = new
            {
                kind = new { type = "string" },
                apiVersion = new { @const = CliRuntime.ApiVersion },
                profile = new { type = "string" },
                result = new { },
                data = new { type = "array" },
                warnings = new { type = "array", items = new { type = "string" } },
                links = new { type = "array" }
            }
        };

        public static readonly object ErrorOutput = new
        {
            type = "object",
            required = new[] { "kind", "apiVersion", "code", "message", "retryable" },
            properties = new
            {
                kind = new { @const = "error" },
                apiVersion = new { @const = CliRuntime.ApiVersion },
                code = new { type = "string" },
                message = new { type = "string" },
                details = new { },
                retryable = new { type = "boolean" }
            }
        };

        public static readonly object EvidenceExport = new
        {
            type = "object",
            properties = new
            {
                exportedAt = new { type = "string", format = "date-time" },
                cliVersion = new { type = "string" },
                profile = new { type = "string" },
                project = new { type = new[] { "object", "null" } },
                testRuns = new { type = "array" },
                warnings = new { type = "array" }
            }
        };

        public static readonly object TestCaseSteps = new
        {
            type = "array",
            items = new
            {
                type = "object",
                required = new[] { "action", "expectedResult" },
                properties = new
                {
                    action = new { type = "string" },
                    expectedResult = new { type = "string" },
                    notes = new { type = "string" }
                }
            }
        };

        public static readonly object TestRunStepsUpdate = new
        {
            type = "array",
            items = new
            {
                type = "object",
                required = new[] { "index" },
                properties = new
                {
                    index = new { type = "integer", minimum = 0 },
                    status = new { @enum = new[] { "PASSED", "NOT_RUN", "FAILED", "INPROGRESS", "BLOCKED" } },
                    actualResult = new { type = "string" }
                }
            }
        };
    }
}
