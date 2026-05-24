using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;

namespace JamaConnect.Application.Evidence;

public sealed class EvidenceUseCases
{
    private readonly IProjectReader _projects;
    private readonly ITestManagementReader _testManagement;

    public EvidenceUseCases(IProjectReader projects, ITestManagementReader testManagement)
    {
        _projects = projects;
        _testManagement = testManagement;
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
        JamaPage<TestRun>? runs = null;
        if (testCycleId is not null)
        {
            runs = await _testManagement.GetTestRunsAsync(
                new TestRunQuery(null, testCycleId, null),
                new PageRequest(),
                cancellationToken).ConfigureAwait(false);
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
            runs?.Data ?? [],
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
