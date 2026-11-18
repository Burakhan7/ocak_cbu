using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UOM.Application.Abstractions;

namespace UOM.Infrastructure.Slugs;

public sealed partial class SlugService : ISlugService
{
    private const int MaxLength = 80;

    public async Task<string> GenerateSlugAsync(
        string title,
        Func<string, Task<bool>> slugExistsAsync,
        CancellationToken cancellationToken = default)
    {
        var slug = Normalize(title);
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "icerik";
        }

        if (slug.Length > MaxLength)
        {
            slug = slug[..MaxLength].TrimEnd('-');
        }

        var candidate = slug;
        var suffix = 2;
        while (await slugExistsAsync(candidate))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tail = $"-{suffix}";
            var baseLen = MaxLength - tail.Length;
            candidate = slug.Length > baseLen ? slug[..baseLen] + tail : slug + tail;
            suffix++;
        }

        return candidate;
    }

    private static string Normalize(string title)
    {
        var lower = title.Trim().ToLower(new CultureInfo("tr-TR"));
        var sb = new StringBuilder(lower.Length);
        foreach (var ch in lower.Normalize(NormalizationForm.FormD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            sb.Append(ch switch
            {
                'ç' => 'c',
                'ğ' => 'g',
                'ı' => 'i',
                'ö' => 'o',
                'ş' => 's',
                'ü' => 'u',
                'İ' => 'i',
                _ => ch,
            });
        }

        var ascii = sb.ToString().Normalize(NormalizationForm.FormC);
        ascii = NonAlphaNumeric().Replace(ascii, "-");
        ascii = MultiDash().Replace(ascii, "-").Trim('-');
        return ascii;
    }

    [GeneratedRegex(@"[^a-z0-9\-]+")]
    private static partial Regex NonAlphaNumeric();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultiDash();
}