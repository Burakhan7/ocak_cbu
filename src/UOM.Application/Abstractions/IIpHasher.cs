namespace UOM.Application.Abstractions;

public interface IIpHasher
{
    string Hash(string? ipAddress);
}