using System.Text.Json;

namespace JamaConnect.Domain.Models;

public sealed record TestPlan(int Id, int ProjectId, string Name, JsonElement? Raw);

public sealed record TestGroup(int Id, int TestPlanId, string Name, JsonElement? Raw);

public sealed record TestCycle(int Id, int TestPlanId, string Name, JsonElement? Raw);

public sealed record TestRun(
    int Id,
    int? TestCaseId,
    int? TestCycleId,
    int? TestPlanId,
    string? Status,
    IReadOnlyList<TestRunStep> Steps,
    JsonElement? Raw);

public sealed record TestRunStep(
    int Index,
    string? Action,
    string? ExpectedResult,
    string? Status,
    string? ActualResult,
    JsonElement? Raw);
