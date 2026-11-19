using UOM.Domain.Enums;

namespace UOM.Infrastructure.Features.Public;

internal static class QuoteAuthorLabels
{
    public static string ToTurkish(QuoteAuthor author) =>
        author switch
        {
            QuoteAuthor.Ataturk => "Mustafa Kemal Atatürk",
            QuoteAuthor.Basbug => "Başbuğ",
            QuoteAuthor.Lider => "Lider",
            QuoteAuthor.GenelBaskan => "Genel Başkan",
            _ => author.ToString(),
        };
}