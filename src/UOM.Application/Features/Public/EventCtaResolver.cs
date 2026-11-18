using UOM.Application.Features.Public.Models;
using UOM.Domain.Enums;

namespace UOM.Application.Features.Public;

public static class EventCtaResolver
{
    public static EventCtaDisplay Resolve(
        EventCtaType type,
        string? callToActionUrl,
        string? whatsAppChannelUrl,
        bool internalInterestEnabled,
        bool isPast)
    {
        if (isPast)
        {
            return new EventCtaDisplay(
                EventCtaType.None,
                PrimaryLabel: "Bu etkinlik sona erdi",
                PrimaryUrl: null,
                OpensInNewTab: false,
                SecondaryHint: null);
        }

        return type switch
        {
            EventCtaType.JustCome => new EventCtaDisplay(
                type,
                "Kayıt gerekmez, bekliyoruz",
                callToActionUrl,
                false,
                "Yol tarifi için mekan bilgisine bakın"),

            EventCtaType.ExternalForm => new EventCtaDisplay(
                type,
                "Kayıt ol",
                callToActionUrl,
                OpensInNewTab: true,
                SecondaryHint: null),

            EventCtaType.InternalInterest when internalInterestEnabled => new EventCtaDisplay(
                type,
                "Katılım bildir",
                callToActionUrl,
                false,
                "Kısa form ile ilginizi iletin"),

            EventCtaType.InternalInterest => new EventCtaDisplay(
                EventCtaType.None,
                "Kayıt için iletişime geçin",
                "/iletisim",
                false,
                "Etkinlik ilgi formu henüz etkin değil"),

            EventCtaType.WhatsApp => new EventCtaDisplay(
                type,
                "WhatsApp'tan katıl",
                callToActionUrl ?? whatsAppChannelUrl,
                OpensInNewTab: true,
                null),

            EventCtaType.None => new EventCtaDisplay(
                type,
                "Etkinlik bilgisi",
                null,
                false,
                "Kayıt için güncellemeleri takip edin"),

            _ => new EventCtaDisplay(
                EventCtaType.None,
                "Detaylar için iletişime geçin",
                "/iletisim",
                false,
                null),
        };
    }
}