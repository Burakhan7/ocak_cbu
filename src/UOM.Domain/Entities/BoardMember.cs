using UOM.Domain.Common;

namespace UOM.Domain.Entities;

public class BoardMember : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int? PhotoImageId { get; set; }

    public MediaFile? PhotoImage { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; }

    public bool ConsentOnFile { get; set; }
}