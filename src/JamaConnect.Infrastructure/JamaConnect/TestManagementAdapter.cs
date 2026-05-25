using System.Text.Json;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;
using JamaConnect.Infrastructure.JamaConnect.Dto;
using JamaConnect.Infrastructure.Json;

namespace JamaConnect.Infrastructure.JamaConnect;

internal sealed class TestManagementAdapter : ITestManagementReader, ITestManagementWriter
{
    private readonly JamaRestClient _client;

    public TestManagementAdapter(JamaRestClient client)
    {
        _client = client;
    }

    public Task<TestPlan?> GetTestPlanAsync(int id, CancellationToken cancellationToken = default)
        => _client.GetSingleAsync($"/rest/v1/testplans/{id}", MapPlan, cancellationToken);

    public Task<TestRun?> GetTestRunAsync(int id, CancellationToken cancellationToken = default)
        => _client.GetSingleAsync($"/rest/v1/testruns/{id}", MapRun, cancellationToken);

    public Task<JamaPage<TestPlan>> GetTestPlansAsync(int projectId, PageRequest page, CancellationToken cancellationToken = default)
        => _client.GetPageAsync($"/rest/v1/testplans?project={projectId}", page, MapPlan, cancellationToken);

    public Task<JamaPage<TestGroup>> GetTestGroupsAsync(int planId, PageRequest page, CancellationToken cancellationToken = default)
        => _client.GetPageAsync($"/rest/v1/testplans/{planId}/testgroups", page, x => MapGroup(planId, x), cancellationToken);

    public Task<JamaPage<TestCycle>> GetTestCyclesAsync(int planId, PageRequest page, CancellationToken cancellationToken = default)
        => _client.GetPageAsync($"/rest/v1/testplans/{planId}/testcycles", page, x => MapCycle(planId, x), cancellationToken);

    public Task<JamaPage<TestRun>> GetTestRunsAsync(TestRunQuery query, PageRequest page, CancellationToken cancellationToken = default)
    {
        var path = query.CycleId is not null
            ? $"/rest/v1/testcycles/{query.CycleId.Value}/testruns"
            : new EndpointBuilder("/rest/v1/testruns")
                .Add("testPlan", query.PlanId)
                .Add("testCycle", query.CycleId)
                .Add("testCase", query.TestCaseId)
                .ToString();
        return _client.GetPageAsync(path, page, MapRun, cancellationToken);
    }

    public Task<JsonElement> CreateTestPlanAsync(JsonElement request, CancellationToken cancellationToken = default)
        => _client.SendJsonAsync(HttpMethod.Post, "/rest/v1/testplans", request, cancellationToken);

    public Task<JsonElement> CreateTestGroupAsync(int planId, JsonElement request, CancellationToken cancellationToken = default)
        => _client.SendJsonAsync(HttpMethod.Post, $"/rest/v1/testplans/{planId}/testgroups", request, cancellationToken);

    public async Task AddTestCaseToGroupAsync(int planId, int groupId, int testCaseId, CancellationToken cancellationToken = default)
    {
        var dto = new RequestTestCaseDto(testCaseId);
        var body = JsonSerializer.SerializeToElement(dto, JamaConnectJsonSerializerContext.Default.RequestTestCaseDto);
        _ = await _client.SendJsonAsync(HttpMethod.Post, $"/rest/v1/testplans/{planId}/testgroups/{groupId}/testcases", body, cancellationToken).ConfigureAwait(false);
    }

    public Task<JsonElement> CreateTestCycleAsync(int planId, JsonElement request, CancellationToken cancellationToken = default)
        => _client.SendJsonAsync(HttpMethod.Post, $"/rest/v1/testplans/{planId}/testcycles", request, cancellationToken);

    public Task<JsonElement> UpdateTestCycleAsync(int cycleId, JsonElement request, CancellationToken cancellationToken = default)
        => _client.SendJsonAsync(HttpMethod.Put, $"/rest/v1/testcycles/{cycleId}", request, cancellationToken);

    public Task<JsonElement> UpdateTestRunAsync(int runId, JsonElement request, CancellationToken cancellationToken = default)
        => _client.SendJsonAsync(HttpMethod.Put, $"/rest/v1/testruns/{runId}", request, cancellationToken);

    private static TestPlan MapPlan(JsonElement element)
    {
        var fields = element.GetObject("fields");
        return new TestPlan(element.GetInt("id"), element.GetInt("project"), fields?.GetStringOrNull("name") ?? element.GetStringOrNull("name") ?? string.Empty, element);
    }

    private static TestGroup MapGroup(int planId, JsonElement element)
    {
        var fields = element.GetObject("fields");
        return new TestGroup(element.GetInt("id"), planId, fields?.GetStringOrNull("name") ?? element.GetStringOrNull("name") ?? string.Empty, element);
    }

    private static TestCycle MapCycle(int planId, JsonElement element)
    {
        var fields = element.GetObject("fields");
        return new TestCycle(element.GetInt("id"), planId, fields?.GetStringOrNull("name") ?? element.GetStringOrNull("name") ?? string.Empty, element);
    }

    private static TestRun MapRun(JsonElement element)
    {
        var steps = new List<TestRunStep>();
        if (element.TryGetProperty("testRunSteps", out var stepArray) && stepArray.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var step in stepArray.EnumerateArray())
            {
                steps.Add(new TestRunStep(
                    index++,
                    step.GetStringOrNull("action"),
                    step.GetStringOrNull("expectedResult"),
                    step.GetStringOrNull("status"),
                    step.GetStringOrNull("actualResult"),
                    step));
            }
        }

        var fields = element.GetObject("fields");
        return new TestRun(
            element.GetInt("id"),
            element.GetNullableInt("testCase"),
            element.GetNullableInt("testCycle"),
            element.GetNullableInt("testPlan"),
            fields?.GetStringOrNull("testRunStatus") ?? element.GetStringOrNull("status"),
            steps,
            element);
    }
}
