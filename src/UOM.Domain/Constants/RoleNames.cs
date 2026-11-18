namespace UOM.Domain.Constants;

public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string ProvinceAdmin = "ProvinceAdmin";
    public const string Editor = "Editor";
    public const string CampusEditor = "CampusEditor";

    public static readonly IReadOnlyList<string> All =
    [
        SuperAdmin,
        ProvinceAdmin,
        Editor,
        CampusEditor,
    ];
}