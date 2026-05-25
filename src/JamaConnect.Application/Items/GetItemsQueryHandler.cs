using JamaConnect.Application.Abstractions;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;

namespace JamaConnect.Application.Items;

public sealed class GetItemsQueryHandler : IQueryHandler<GetItemsQuery, IReadOnlyList<Item>>
{
    private readonly IItemReader _itemReader;

    public GetItemsQueryHandler(IItemReader itemReader)
    {
        _itemReader = itemReader;
    }

    public async Task<IReadOnlyList<Item>> HandleAsync(GetItemsQuery query, CancellationToken cancellationToken = default)
    {
        var page = await _itemReader.SearchItemsAsync(
            new ItemSearchCriteria(query.ProjectId, null, null, null, null, null, false, []),
            new PageRequest(),
            cancellationToken).ConfigureAwait(false);
        return page.Data
            .Select(x => new Item
            {
                Id = x.Id,
                DocumentKey = x.DocumentKey ?? string.Empty,
                Subject = x.Title,
                TypeId = x.ItemTypeId,
                ProjectId = x.ProjectId,
                ParentId = x.ParentId
            })
            .ToArray();
    }
}
