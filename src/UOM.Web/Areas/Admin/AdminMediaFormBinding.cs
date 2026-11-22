using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using UOM.Infrastructure.Features.Admin;

namespace UOM.Web.Areas.Admin;

public static class AdminMediaFormBinding
{
    public static async Task<bool> TryBindCoverImageAsync(
        MediaAdminService mediaAdmin,
        ModelStateDictionary modelState,
        int? currentCoverImageId,
        bool removeCoverImage,
        IFormFile? coverImageUpload,
        string? coverImageAlt,
        string defaultAltText,
        Action<int?> setCoverImageId,
        CancellationToken cancellationToken)
    {
        await using var stream = coverImageUpload?.OpenReadStream();
        var result = await mediaAdmin.ApplyCoverImageAsync(
            currentCoverImageId,
            removeCoverImage,
            stream,
            coverImageUpload?.Length ?? 0,
            coverImageUpload?.FileName,
            coverImageAlt,
            defaultAltText,
            cancellationToken);

        if (!result.Succeeded)
        {
            modelState.AddModelError(string.Empty, result.Error ?? "Görsel yüklenemedi.");
            return false;
        }

        setCoverImageId(result.Value);
        return true;
    }

    public static async Task<bool> TryBindResourceFileAsync(
        MediaAdminService mediaAdmin,
        ModelStateDictionary modelState,
        int? currentFileId,
        bool removeResourceFile,
        IFormFile? resourceFileUpload,
        string defaultAltText,
        Action<int?> setFileId,
        CancellationToken cancellationToken)
    {
        await using var stream = resourceFileUpload?.OpenReadStream();
        var result = await mediaAdmin.ApplyResourceFileAsync(
            currentFileId,
            removeResourceFile,
            stream,
            resourceFileUpload?.Length ?? 0,
            resourceFileUpload?.FileName,
            defaultAltText,
            cancellationToken);

        if (!result.Succeeded)
        {
            modelState.AddModelError(string.Empty, result.Error ?? "Dosya yüklenemedi.");
            return false;
        }

        setFileId(result.Value);
        return true;
    }

    public static async Task SetCoverImagePreviewAsync(
        MediaAdminService mediaAdmin,
        ViewDataDictionary viewData,
        int? coverImageId,
        CancellationToken cancellationToken)
    {
        if (coverImageId is null)
        {
            return;
        }

        var file = await mediaAdmin.GetByIdAsync(coverImageId.Value, cancellationToken);
        if (file is null)
        {
            return;
        }

        viewData["CoverImagePreviewUrl"] = AdminMediaUrl.Resolve(file.StoragePath, file.Variants);
        viewData["CoverImageAltText"] = file.AltText;
    }

    public static async Task<bool> TryBindLogoImageAsync(
        MediaAdminService mediaAdmin,
        ModelStateDictionary modelState,
        int? currentLogoImageId,
        bool removeLogoImage,
        IFormFile? logoImageUpload,
        string? logoImageAlt,
        string defaultAltText,
        Action<int?> setLogoImageId,
        CancellationToken cancellationToken) =>
        await TryBindCoverImageAsync(
            mediaAdmin,
            modelState,
            currentLogoImageId,
            removeLogoImage,
            logoImageUpload,
            logoImageAlt,
            defaultAltText,
            setLogoImageId,
            cancellationToken);

    public static async Task SetLogoImagePreviewAsync(
        MediaAdminService mediaAdmin,
        ViewDataDictionary viewData,
        int? logoImageId,
        CancellationToken cancellationToken)
    {
        if (logoImageId is null)
        {
            return;
        }

        var file = await mediaAdmin.GetByIdAsync(logoImageId.Value, cancellationToken);
        if (file is null)
        {
            return;
        }

        viewData["LogoImagePreviewUrl"] = AdminMediaUrl.Resolve(file.StoragePath, file.Variants, "thumb");
        viewData["LogoImageAltText"] = file.AltText;
    }

    public static Task<bool> TryBindCbuHubCoverAsync(
        MediaAdminService mediaAdmin,
        ModelStateDictionary modelState,
        int? currentCoverImageId,
        bool removeCoverImage,
        IFormFile? coverImageUpload,
        string? coverImageAlt,
        string defaultAltText,
        Action<int?> setCoverImageId,
        CancellationToken cancellationToken) =>
        TryBindCoverImageAsync(
            mediaAdmin,
            modelState,
            currentCoverImageId,
            removeCoverImage,
            coverImageUpload,
            coverImageAlt,
            defaultAltText,
            setCoverImageId,
            cancellationToken);

    public static async Task SetCbuHubCoverPreviewAsync(
        MediaAdminService mediaAdmin,
        ViewDataDictionary viewData,
        int? coverImageId,
        CancellationToken cancellationToken)
    {
        if (coverImageId is null)
        {
            return;
        }

        var file = await mediaAdmin.GetByIdAsync(coverImageId.Value, cancellationToken);
        if (file is null)
        {
            return;
        }

        viewData["CbuHubCoverPreviewUrl"] = AdminMediaUrl.Resolve(file.StoragePath, file.Variants, "medium");
        viewData["CbuHubCoverAltText"] = file.AltText;
    }

    public static async Task SetResourceFilePreviewAsync(
        MediaAdminService mediaAdmin,
        ViewDataDictionary viewData,
        int? fileId,
        CancellationToken cancellationToken)
    {
        if (fileId is null)
        {
            return;
        }

        var file = await mediaAdmin.GetByIdAsync(fileId.Value, cancellationToken);
        if (file is null)
        {
            return;
        }

        viewData["ResourceFileName"] = file.OriginalFileName;
        viewData["ResourceFileUrl"] = AdminMediaUrl.Resolve(file.StoragePath, file.Variants);
    }
}