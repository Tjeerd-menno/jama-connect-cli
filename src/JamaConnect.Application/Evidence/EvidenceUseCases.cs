using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;

namespace JamaConnect.Application.Evidence;

public sealed class EvidenceUseCases
{
    private readonly IProjectReader _projects;
    private readonly ITestManagementReader _testManagement;
    private readonly IJamaPaginator _paginator;

    public EvidenceUseCases(IProjectReader projects, ITestManagementReader testManagement, IJamaPaginator paginator)
    {
        _projects = projects;
        _testManagement = testManagement;
        _paginator = paginator;
    }

    public async Task<EvidenceExport> ExportAsync(
        int projectId,
        int? testCycleId,
        string profile,
        IReadOnlyList<string> include,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var project = await _projects.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<TestRun> runs = [];
        if (testCycleId is not null)
        {
            var allRuns = new List<TestRun>();
            await foreach (var run in _paginator.GetAllAsync(
                (startAt, maxResults, ct) => _testManagement.GetTestRunsAsync(
                    new TestRunQuery(null, testCycleId, null),
                    new PageRequest(startAt, maxResults),
                    ct),
                50,
                null,
                cancellationToken))
            {
                allRuns.Add(run);
            }

            runs = allRuns;
        }

        var warnings = new List<string>();
        if (project is null)
        {
            warnings.Add($"Project '{projectId}' could not be retrieved.");
        }

        if (testCycleId is null)
        {
            warnings.Add("No test cycle was supplied, so test run evidence is not included.");
        }

        return new EvidenceExport(
            DateTimeOffset.UtcNow,
            "jama-connect-cli/v1",
            profile,
            arguments,
            project,
            testCycleId,
            include,
            runs,
            warnings);
    }
}

public sealed record EvidenceExport(
    DateTimeOffset ExportedAt,
    string CliVersion,
    string Profile,
    IReadOnlyList<string> Arguments,
    Project? Project,
    int? TestCycleId,
    IReadOnlyList<string> Include,
    IReadOnlyList<TestRun> TestRuns,
    IReadOnlyList<string> Warnings);
