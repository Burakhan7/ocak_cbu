using UOM.Domain.Common;
using UOM.Domain.Enums;

namespace UOM.Domain.Entities;

public class Quote : BaseEntity
{
    public string Text { get; set; } = string.Empty;

    public QuoteAuthor Author { get; set; }

    public string? SourceNote { get; set; }

    public bool IsActive { get; set; }

    public int Order { get; set; }
}