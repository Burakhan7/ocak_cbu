using UOM.Application.Features.Public.Models;

namespace UOM.Web.Models;

public sealed class NotFoundPageModel
{
    public IReadOnlyList<EventListItemDto> Upcoming { get; init; } = [];
}
