namespace UOM.Domain.Common;

public interface ISeoEntity
{
    string? MetaTitle { get; set; }

    string? MetaDescription { get; set; }

    int? OgImageId { get; set; }
}