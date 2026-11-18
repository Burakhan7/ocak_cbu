using FluentAssertions;
using UOM.Application.Features.Public;
using UOM.Domain.Enums;

namespace UOM.Application.Tests.Features.Public;

public class EventCtaResolverTests
{
    [Theory]
    [InlineData(EventCtaType.None)]
    [InlineData(EventCtaType.JustCome)]
    [InlineData(EventCtaType.ExternalForm)]
    [InlineData(EventCtaType.InternalInterest)]
    [InlineData(EventCtaType.WhatsApp)]
    public void Resolve_assigns_label_for_all_cta_types(EventCtaType type)
    {
        var cta = EventCtaResolver.Resolve(type, "https://example.com/form", "https://wa.me/1", internalInterestEnabled: true, isPast: false);
        cta.PrimaryLabel.Should().NotBeNullOrWhiteSpace();
        if (type == EventCtaType.InternalInterest)
        {
            cta.Type.Should().Be(EventCtaType.InternalInterest);
        }
    }

    [Fact]
    public void Resolve_past_event_overrides_cta()
    {
        var cta = EventCtaResolver.Resolve(EventCtaType.ExternalForm, "https://x", null, true, true);
        cta.PrimaryLabel.Should().Contain("sona erdi");
    }
}