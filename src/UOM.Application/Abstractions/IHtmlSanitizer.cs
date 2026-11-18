namespace UOM.Application.Abstractions;

public interface IHtmlSanitizer
{
    string Sanitize(string? html);
}