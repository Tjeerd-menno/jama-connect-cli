using System.Text.Json;

namespace JamaConnect.Domain.Interfaces;

public interface ITestManagementWriter
{
    Task<JsonElement> CreateTestPlanAsync(JsonElement request, CancellationToken cancellationToken = default);

    Task<JsonElement> CreateTestGroupAsync(int planId, JsonElement request, CancellationToken cancellationToken = default);

    Task AddTestCaseToGroupAsync(int planId, int groupId, int testCaseId, CancellationToken cancellationToken = default);

    Task<JsonElement> CreateTestCycleAsync(int planId, JsonElement request, CancellationToken cancellationToken = default);

    Task<JsonElement> UpdateTestCycleAsync(int cycleId, JsonElement request, CancellationToken cancellationToken = default);

    Task<JsonElement> UpdateTestRunAsync(int runId, JsonElement request, CancellationToken cancellationToken = default);
}
