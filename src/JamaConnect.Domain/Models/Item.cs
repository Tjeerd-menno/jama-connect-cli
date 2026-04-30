namespace JamaConnect.Domain.Models;

public sealed class Item
{
    public int Id { get; init; }
    public required string DocumentKey { get; init; }
    public required string Subject { get; init; }
    public int TypeId { get; init; }
    public int ProjectId { get; init; }
    public int? ParentId { get; init; }
    public ItemStatus Status { get; init; }
}
