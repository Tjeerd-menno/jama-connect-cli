using System.CommandLine;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;
using Microsoft.Extensions.DependencyInjection;

namespace JamaConnect.Cli.Commands;

internal static class SchemaCommandExtensions
{
    public static Command BuildSchemaCommand(IServiceProvider services, CliRuntime runtime)
    {
        var command = new Command("schema", "Discover Jama schema metadata.");
        var itemTypes = new Command("item-types", "Item type commands.");
        itemTypes.Add(BuildItemTypesListCommand(services, runtime));
        itemTypes.Add(BuildItemTypesGetCommand(services, runtime));
        command.Add(itemTypes);
        var relationshipTypes = new Command("relationship-types", "Relationship type commands.");
        relationshipTypes.Add(BuildRelationshipTypesListCommand(services, runtime));
        relationshipTypes.Add(BuildRelationshipTypesGetCommand(services, runtime));
        command.Add(relationshipTypes);
        var relationshipRules = new Command("relationship-rules", "Relationship rule commands.");
        relationshipRules.Add(BuildRelationshipRulesListCommand(runtime));
        command.Add(relationshipRules);
        var picklists = new Command("picklists", "Picklist commands.");
        picklists.Add(BuildPicklistsListCommand(services, runtime));
        picklists.Add(BuildPicklistsGetCommand(services, runtime));
        command.Add(picklists);
        var fields = new Command("fields", "Field discovery commands.");
        fields.Add(BuildFieldsListCommand(services, runtime));
        command.Add(fields);
        return command;
    }

    private static Command BuildItemTypesListCommand(IServiceProvider services, CliRuntime runtime)
    {
        var startAt = CommandHelpers.StartAtOption();
        var pageSize = CommandHelpers.PageSizeOption();
        var limit = CommandHelpers.LimitOption();
        var command = new Command("list", "List item types.");
        command.Add(startAt);
        command.Add(pageSize);
        command.Add(limit);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var reader = services.GetRequiredService<ISchemaReader>();
                var page = await reader.GetItemTypesAsync(CommandHelpers.Page(parseResult, startAt, pageSize, limit), ct).ConfigureAwait(false);
                CliOutput.WritePage(context, "schema.item-types.list.result", page);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildItemTypesGetCommand(IServiceProvider services, CliRuntime runtime)
    {
        var id = new Argument<int>("id") { Description = "Item type ID." };
        var command = new Command("get", "Get item type.");
        command.Add(id);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var reader = services.GetRequiredService<ISchemaReader>();
                var itemType = await reader.GetItemTypeAsync(parseResult.GetValue(id), ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "schema.item-types.get.result", itemType);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildRelationshipTypesListCommand(IServiceProvider services, CliRuntime runtime)
    {
        var startAt = CommandHelpers.StartAtOption();
        var pageSize = CommandHelpers.PageSizeOption();
        var limit = CommandHelpers.LimitOption();
        var command = new Command("list", "List relationship types.");
        command.Add(startAt);
        command.Add(pageSize);
        command.Add(limit);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var reader = services.GetRequiredService<ISchemaReader>();
                var page = await reader.GetRelationshipTypesAsync(CommandHelpers.Page(parseResult, startAt, pageSize, limit), ct).ConfigureAwait(false);
                CliOutput.WritePage(context, "schema.relationship-types.list.result", page);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildRelationshipTypesGetCommand(IServiceProvider services, CliRuntime runtime)
    {
        var id = new Argument<int>("id") { Description = "Relationship type ID." };
        var command = new Command("get", "Get relationship type.");
        command.Add(id);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var reader = services.GetRequiredService<ISchemaReader>();
                var result = await reader.GetRelationshipTypeAsync(parseResult.GetValue(id), ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "schema.relationship-types.get.result", result);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildPicklistsListCommand(IServiceProvider services, CliRuntime runtime)
    {
        var startAt = CommandHelpers.StartAtOption();
        var pageSize = CommandHelpers.PageSizeOption();
        var limit = CommandHelpers.LimitOption();
        var command = new Command("list", "List picklists.");
        command.Add(startAt);
        command.Add(pageSize);
        command.Add(limit);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var page = await services.GetRequiredService<ISchemaReader>()
                    .GetPickListsAsync(CommandHelpers.Page(parseResult, startAt, pageSize, limit), ct)
                    .ConfigureAwait(false);
                CliOutput.WritePage(context, "schema.picklists.list.result", page);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildPicklistsGetCommand(IServiceProvider services, CliRuntime runtime)
    {
        var id = new Argument<int>("id") { Description = "Picklist ID." };
        var command = new Command("get", "Get picklist.");
        command.Add(id);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var result = await services.GetRequiredService<ISchemaReader>().GetPickListAsync(parseResult.GetValue(id), ct).ConfigureAwait(false);
                CliOutput.WriteResult(context, "schema.picklists.get.result", result);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildFieldsListCommand(IServiceProvider services, CliRuntime runtime)
    {
        var itemType = new Option<int?>("--item-type") { Description = "Optional item type ID." };
        var command = new Command("list", "List item type fields.");
        command.Add(itemType);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await Execution.RunAsync(runtime, parseResult, async (context, ct) =>
            {
                var reader = services.GetRequiredService<ISchemaReader>();
                var itemTypeId = parseResult.GetValue(itemType);
                if (itemTypeId is not null)
                {
                    var definition = await reader.GetItemTypeAsync(itemTypeId.Value, ct).ConfigureAwait(false);
                    CliOutput.WriteResult(context, "schema.fields.list.result", definition?.Fields ?? []);
                    return;
                }

                var page = await reader.GetItemTypesAsync(new PageRequest(), ct).ConfigureAwait(false);
                var fields = page.Data.SelectMany(x => x.Fields.Select(field => new { itemTypeId = x.Id, itemTypeName = x.Name, field })).ToArray();
                CliOutput.WriteResult(context, "schema.fields.list.result", fields);
            }, cancellationToken).ConfigureAwait(false);
        });
        return command;
    }

    private static Command BuildRelationshipRulesListCommand(CliRuntime runtime)
    {
        var command = new Command("list", "List relationship rules when exposed by this CLI configuration.");
        command.SetAction((parseResult) =>
        {
            CliOutput.WriteResult(
                runtime.Create(parseResult),
                "schema.relationship-rules.list.result",
                Array.Empty<object>(),
                ["Relationship rule-set discovery is not exposed by this implementation; relationship rules are still enforced by Jama on create/update."]);
        });
        return command;
    }
}
