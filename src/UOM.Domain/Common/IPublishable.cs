using UOM.Domain.Enums;

namespace UOM.Domain.Common;

public interface IPublishable
{
    PublishStatus Status { get; set; }

    DateTime? PublishedAtUtc { get; set; }

    string Slug { get; set; }
}