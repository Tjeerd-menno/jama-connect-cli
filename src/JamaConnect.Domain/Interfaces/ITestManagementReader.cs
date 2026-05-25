using JamaConnect.Domain.Models;

namespace JamaConnect.Domain.Interfaces;

public interface ITestManagementReader
{
    Task<TestPlan?> GetTestPlanAsync(int id, CancellationToken cancellationToken = default);

    Task<TestRun?> GetTestRunAsync(int id, CancellationToken cancellationToken = default);

    Task<JamaPage<TestPlan>> GetTestPlansAsync(int projectId, PageRequest page, CancellationToken cancellationToken = default);

    Task<JamaPage<TestGroup>> GetTestGroupsAsync(int planId, PageRequest page, CancellationToken cancellationToken = default);

    Task<JamaPage<TestCycle>> GetTestCyclesAsync(int planId, PageRequest page, CancellationToken cancellationToken = default);

    Task<JamaPage<TestRun>> GetTestRunsAsync(TestRunQuery query, PageRequest page, CancellationToken cancellationToken = default);
}
