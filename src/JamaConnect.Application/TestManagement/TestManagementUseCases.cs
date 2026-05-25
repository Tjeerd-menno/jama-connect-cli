using System.Text.Json;
using System.Text.Json.Nodes;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;

namespace JamaConnect.Application.TestManagement;

public sealed class TestManagementUseCases
{
    private readonly ITestManagementReader _reader;
    private readonly ITestManagementWriter _writer;

    public TestManagementUseCases(ITestManagementReader reader, ITestManagementWriter writer)
    {
        _reader = reader;
        _writer = writer;
    }

    public Task<JamaPage<TestPlan>> ListPlansAsync(int projectId, PageRequest page, CancellationToken cancellationToken = default)
        => _reader.GetTestPlansAsync(projectId, page, cancellationToken);

    public Task<TestPlan?> GetPlanAsync(int id, CancellationToken cancellationToken = default)
        => _reader.GetTestPlanAsync(id, cancellationToken);

    public Task<JsonElement> CreatePlanAsync(JsonElement request, CancellationToken cancellationToken = default)
        => _writer.CreateTestPlanAsync(request, cancellationToken);

    public Task<JamaPage<TestGroup>> ListGroupsAsync(int planId, PageRequest page, CancellationToken cancellationToken = default)
        => _reader.GetTestGroupsAsync(planId, page, cancellationToken);

    public Task<JsonElement> CreateGroupAsync(int planId, JsonElement request, CancellationToken cancellationToken = default)
        => _writer.CreateTestGroupAsync(planId, request, cancellationToken);

    public Task AddCaseAsync(int planId, int groupId, int testCaseId, CancellationToken cancellationToken = default)
        => _writer.AddTestCaseToGroupAsync(planId, groupId, testCaseId, cancellationToken);

    public Task<JamaPage<TestCycle>> ListCyclesAsync(int planId, PageRequest page, CancellationToken cancellationToken = default)
        => _reader.GetTestCyclesAsync(planId, page, cancellationToken);

    public Task<JsonElement> CreateCycleAsync(int planId, JsonElement request, CancellationToken cancellationToken = default)
        => _writer.CreateTestCycleAsync(planId, request, cancellationToken);

    public Task<JsonElement> SyncCycleAsync(int cycleId, JsonElement request, CancellationToken cancellationToken = default)
        => _writer.UpdateTestCycleAsync(cycleId, request, cancellationToken);

    public Task<JamaPage<TestRun>> ListRunsAsync(TestRunQuery query, PageRequest page, CancellationToken cancellationToken = default)
        => _reader.GetTestRunsAsync(query, page, cancellationToken);

    public Task<JsonElement> UpdateRunAsync(int runId, JsonElement request, CancellationToken cancellationToken = default)
        => _writer.UpdateTestRunAsync(runId, request, cancellationToken);

    public Task<TestRun?> GetRunAsync(int runId, CancellationToken cancellationToken = default)
        => _reader.GetTestRunAsync(runId, cancellationToken);

    public async Task<JsonElement> UpdateRunStepsAsync(int runId, IReadOnlyList<TestRunStepUpdate> updates, CancellationToken cancellationToken = default)
    {
        var existing = await _reader.GetTestRunAsync(runId, cancellationToken).ConfigureAwait(false);
        if (existing?.Raw is null)
        {
            throw new InvalidOperationException($"Test run '{runId}' was not found or did not include raw step data.");
        }

        var root = JsonNode.Parse(existing.Raw.Value.GetRawText())?.AsObject()
            ?? throw new InvalidOperationException("The test run payload could not be parsed.");
        var steps = root["testRunSteps"]?.AsArray()
            ?? throw new InvalidOperationException("The test run does not contain a testRunSteps array.");

        foreach (var update in updates)
        {
            if (update.Index < 0 || update.Index >= steps.Count)
            {
                throw new InvalidOperationException($"Step index {update.Index} is outside the test run step range.");
            }

            var step = steps[update.Index]?.AsObject()
                ?? throw new InvalidOperationException($"Step index {update.Index} is not an object.");
            if (!string.IsNullOrWhiteSpace(update.Status))
            {
                step["status"] = update.Status;
            }

            if (update.ActualResult is not null)
            {
                step["actualResult"] = update.ActualResult;
            }
        }

        var request = JsonElement.Parse(root.ToJsonString());
        return await _writer.UpdateTestRunAsync(runId, request, cancellationToken).ConfigureAwait(false);
    }
}
