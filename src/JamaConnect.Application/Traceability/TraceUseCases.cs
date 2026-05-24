using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;
using JamaConnect.Application.Configuration;

namespace JamaConnect.Application.Traceability;

public sealed class TraceUseCases
{
    private readonly IItemReader _items;
    private readonly IRelationshipReader _relationships;
    private readonly JamaCliConfiguration _configuration;
    private readonly AliasResolver _aliases;
    private readonly IJamaPaginator _paginator;

    public TraceUseCases(
        IItemReader items,
        IRelationshipReader relationships,
        JamaCliConfiguration configuration,
        AliasResolver aliases,
        IJamaPaginator paginator)
    {
        _items = items;
        _relationships = relationships;
        _configuration = configuration;
        _aliases = aliases;
        _paginator = paginator;
    }

    public async Task<TraceGraph> ShowAsync(string item, string direction, int depth, CancellationToken cancellationToken = default)
    {
        var root = await _items.GetItemAsync(ItemIdentifier.Parse(item), new ItemQueryOptions([], true), cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            throw new InvalidOperationException($"Item '{item}' was not found.");
        }

        var nodes = new Dictionary<int, JamaItem> { [root.Id] = root };
        var edges = new Dictionary<int, JamaRelationship>();
        var frontier = new Queue<(int Id, int Level)>();
        frontier.Enqueue((root.Id, 0));

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current.Level >= Math.Max(1, depth))
            {
                continue;
            }

            var relationships = await _relationships
                .GetRelationshipsAsync(new RelationshipQuery(current.Id, direction, []), new PageRequest(), cancellationToken)
                .ConfigureAwait(false);

            foreach (var relationship in relationships.Data)
            {
                edges.TryAdd(relationship.Id, relationship);
                var relatedId = relationship.FromItemId == current.Id ? relationship.ToItemId : relationship.FromItemId;
                if (nodes.ContainsKey(relatedId))
                {
                    continue;
                }

                var related = await _items.GetItemAsync(new ItemIdentifier(relatedId, null), new ItemQueryOptions([], true), cancellationToken).ConfigureAwait(false);
                if (related is not null)
                {
                    nodes[related.Id] = related;
                    frontier.Enqueue((related.Id, current.Level + 1));
                }
            }
        }

        return new TraceGraph(
            root,
            nodes.Values.ToArray(),
            edges.Values.ToArray(),
            depth,
            []);
    }

    public async Task<TraceGapsResult> FindGapsAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var gaps = new List<TraceGap>();
        var warnings = new List<string>();
        var rulesEvaluated = 0;
        var itemsEvaluated = 0;

        foreach (var rule in _configuration.TraceabilityRules)
        {
            rulesEvaluated++;

            int relationshipTypeId;
            try
            {
                relationshipTypeId = _aliases.ResolveRelationshipTypeId(rule.Relation);
            }
            catch (InvalidOperationException ex)
            {
                warnings.Add(ex.Message);
                continue;
            }

            await foreach (var item in _paginator.GetAllAsync(
                (startAt, maxResults, ct) => _items.SearchItemsAsync(
                    new ItemSearchCriteria(projectId, rule.Source, null, null, null, null, false, []),
                    new PageRequest(startAt, maxResults),
                    ct),
                50,
                null,
                cancellationToken))
            {
                itemsEvaluated++;
                var actualTargets = 0;
                await foreach (var relationship in _paginator.GetAllAsync(
                    (startAt, maxResults, ct) => _relationships.GetRelationshipsAsync(
                        new RelationshipQuery(item.Id, rule.Direction, []),
                        new PageRequest(startAt, maxResults),
                        ct),
                    50,
                    null,
                    cancellationToken))
                {
                    if (relationship.RelationshipTypeId == relationshipTypeId)
                    {
                        actualTargets++;
                    }
                }

                if (actualTargets < rule.MinTargets)
                {
                    gaps.Add(new TraceGap(
                        rule.Name,
                        item,
                        new TraceGapExpectation(rule.Relation, rule.Target, rule.MinTargets),
                        actualTargets));
                }
            }
        }

        if (rulesEvaluated == 0)
        {
            warnings.Add("No traceability rules are configured.");
        }

        return new TraceGapsResult(projectId, new TraceGapsSummary(rulesEvaluated, itemsEvaluated, gaps.Count), gaps, warnings);
    }

    public async Task<TraceMatrixResult> MatrixAsync(int projectId, string from, string to, string relation, CancellationToken cancellationToken = default)
    {
        var relationshipTypeId = _aliases.ResolveRelationshipTypeId(relation);
        var rows = new List<TraceMatrixRow>();
        await foreach (var source in _paginator.GetAllAsync(
            (startAt, maxResults, ct) => _items.SearchItemsAsync(
                new ItemSearchCriteria(projectId, from, null, null, null, null, false, []),
                new PageRequest(startAt, maxResults),
                ct),
            50,
            null,
            cancellationToken))
        {
            var hasMatchingRelationship = false;
            await foreach (var relationship in _paginator.GetAllAsync(
                (startAt, maxResults, ct) => _relationships.GetRelationshipsAsync(
                    new RelationshipQuery(source.Id, "both", []),
                    new PageRequest(startAt, maxResults),
                    ct),
                50,
                null,
                cancellationToken))
            {
                if (relationship.RelationshipTypeId != relationshipTypeId)
                {
                    continue;
                }

                hasMatchingRelationship = true;
                var targetId = relationship.FromItemId == source.Id ? relationship.ToItemId : relationship.FromItemId;
                var targetItem = await _items.GetItemAsync(new ItemIdentifier(targetId, null), new ItemQueryOptions([], true), cancellationToken).ConfigureAwait(false);
                if (targetItem is null || (!string.IsNullOrWhiteSpace(to) && !TargetMatches(to, targetItem)))
                {
                    continue;
                }

                rows.Add(new TraceMatrixRow(source, targetItem, relation, true, relationship.Suspect));
            }

            if (!hasMatchingRelationship)
            {
                rows.Add(new TraceMatrixRow(source, null, relation, false, false));
            }
        }

        return new TraceMatrixResult(projectId, from, to, relation, rows);
    }

    public async Task<TraceCoverageResult> CoverageAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var gaps = await FindGapsAsync(projectId, cancellationToken).ConfigureAwait(false);
        var covered = Math.Max(0, gaps.Summary.ItemsEvaluated - gaps.Summary.Gaps);
        var percent = gaps.Summary.ItemsEvaluated == 0 ? 0 : Math.Round((double)covered / gaps.Summary.ItemsEvaluated * 100, 2);
        return new TraceCoverageResult(projectId, gaps.Summary.ItemsEvaluated, covered, gaps.Summary.Gaps, percent, gaps.Warnings);
    }

    public async Task<VerificationSummaryResult> VerificationSummaryAsync(int projectId, int? testCycleId, ITestManagementReader testManagement, CancellationToken cancellationToken = default)
    {
        var gaps = await FindGapsAsync(projectId, cancellationToken).ConfigureAwait(false);
        var runs = testCycleId is null
            ? new JamaPage<TestRun>(0, 50, 0, 0, [])
            : await testManagement.GetTestRunsAsync(new TestRunQuery(null, testCycleId, null), new PageRequest(), cancellationToken).ConfigureAwait(false);
        var statuses = runs.Data
            .GroupBy(x => x.Status ?? "UNKNOWN", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
        return new VerificationSummaryResult(
            projectId,
            testCycleId,
            gaps.Summary.Gaps,
            runs.Data.Count(x => string.Equals(x.Status, "FAILED", StringComparison.OrdinalIgnoreCase)),
            runs.Data.Count(x => string.Equals(x.Status, "BLOCKED", StringComparison.OrdinalIgnoreCase)),
            statuses,
            gaps.Warnings);
    }

    private bool TargetMatches(string aliasOrId, JamaItem target)
    {
        if (int.TryParse(aliasOrId, out var id))
        {
            return target.ItemTypeId == id;
        }

        try
        {
            return target.ItemTypeId == _aliases.ResolveItemTypeId(aliasOrId);
        }
        catch (InvalidOperationException)
        {
            return string.Equals(target.ItemTypeAlias, aliasOrId, StringComparison.OrdinalIgnoreCase);
        }
    }
}

public sealed record TraceGraph(JamaItem Root, IReadOnlyList<JamaItem> Nodes, IReadOnlyList<JamaRelationship> Edges, int Depth, IReadOnlyList<string> Warnings);

public sealed record TraceGapsResult(int ProjectId, TraceGapsSummary Summary, IReadOnlyList<TraceGap> Gaps, IReadOnlyList<string> Warnings);

public sealed record TraceGapsSummary(int RulesEvaluated, int ItemsEvaluated, int Gaps);

public sealed record TraceGap(string Rule, JamaItem Source, TraceGapExpectation Expected, int ActualTargets);

public sealed record TraceGapExpectation(string Relation, string TargetType, int MinTargets);

public sealed record TraceMatrixResult(int ProjectId, string From, string To, string Relation, IReadOnlyList<TraceMatrixRow> Rows);

public sealed record TraceMatrixRow(JamaItem Source, JamaItem? Target, string Relation, bool Covered, bool Suspect);

public sealed record TraceCoverageResult(int ProjectId, int ItemsEvaluated, int Covered, int Gaps, double CoveragePercent, IReadOnlyList<string> Warnings);

public sealed record VerificationSummaryResult(
    int ProjectId,
    int? TestCycleId,
    int TraceGaps,
    int FailedRuns,
    int BlockedRuns,
    IReadOnlyDictionary<string, int> TestRunStatuses,
    IReadOnlyList<string> Warnings);
