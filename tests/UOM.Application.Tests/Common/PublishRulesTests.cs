using FluentAssertions;
using UOM.Application.Common;
using UOM.Domain.Enums;

namespace UOM.Application.Tests.Common;

public class PublishRulesTests
{
    private sealed class TestPublishable : Domain.Common.IPublishable
    {
        public PublishStatus Status { get; set; }
        public DateTime? PublishedAtUtc { get; set; }
        public string Slug { get; set; } = "test";
    }

    [Fact]
    public void IsPubliclyVisible_returns_true_when_published_and_not_scheduled_in_future()
    {
        var now = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var entity = new TestPublishable
        {
            Status = PublishStatus.Published,
            PublishedAtUtc = now.AddHours(-1),
        };

        entity.IsPubliclyVisible(now).Should().BeTrue();
    }

    [Fact]
    public void IsPubliclyVisible_returns_false_for_draft_or_future_publish()
    {
        var now = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        new TestPublishable { Status = PublishStatus.Draft, PublishedAtUtc = now }
            .IsPubliclyVisible(now).Should().BeFalse();

        new TestPublishable { Status = PublishStatus.Published, PublishedAtUtc = now.AddDays(1) }
            .IsPubliclyVisible(now).Should().BeFalse();
    }

    [Fact]
    public void IsAnnouncementListable_compares_ExpiresAtUtc()
    {
        var now = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        PublishRules.IsAnnouncementListable(now.AddDays(1), now).Should().BeTrue();
        PublishRules.IsAnnouncementListable(now.AddHours(-1), now).Should().BeFalse();
    }
}