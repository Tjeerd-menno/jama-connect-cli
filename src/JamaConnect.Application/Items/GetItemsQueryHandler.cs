using JamaConnect.Application.Abstractions;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;

namespace JamaConnect.Application.Items;

public sealed class GetItemsQueryHandler : IQueryHandler<GetItemsQuery, IReadOnlyList<Item>>
{
    private readonly IItemService _itemService;

    public GetItemsQueryHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public Task<IReadOnlyList<Item>> HandleAsync(GetItemsQuery query, CancellationToken cancellationToken = default)
    {
        return _itemService.GetItemsAsync(query.ProjectId, cancellationToken);
    }
}
