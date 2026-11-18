namespace UOM.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}