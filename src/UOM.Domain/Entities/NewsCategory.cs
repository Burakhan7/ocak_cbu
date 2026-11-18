using UOM.Domain.Common;

namespace UOM.Domain.Entities;

public class NewsCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public int Order { get; set; }

    public ICollection<NewsPost> NewsPosts { get; set; } = [];
}