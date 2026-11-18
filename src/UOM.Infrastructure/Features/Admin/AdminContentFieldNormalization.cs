namespace UOM.Infrastructure.Features.Admin;

internal static class AdminContentFieldNormalization
{
    public static string NormalizeRequiredHtml(string? value) => value ?? string.Empty;
}