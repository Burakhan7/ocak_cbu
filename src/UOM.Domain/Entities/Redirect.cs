using UOM.Domain.Common;

namespace UOM.Domain.Entities;

public class Redirect : BaseEntity
{
    public string FromPath { get; set; } = string.Empty;

    public string ToPath { get; set; } = string.Empty;

    public int StatusCode { get; set; } = 301;

    public int HitCount { get; set; }
}