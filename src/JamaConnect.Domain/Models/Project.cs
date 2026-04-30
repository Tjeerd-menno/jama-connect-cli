namespace JamaConnect.Domain.Models;

public sealed class Project
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsFolder { get; init; }
    public string? ProjectKey { get; init; }
}
