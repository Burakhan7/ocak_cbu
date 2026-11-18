namespace UOM.Application.Abstractions;

public interface IPreviewTokenService
{
    string CreateToken(string contentType, int id, TimeSpan lifetime);

    bool TryValidate(string contentType, int id, string token);
}