using System.ComponentModel.DataAnnotations;
using UOM.Application.Features.Public.Models;
using UOM.Domain.Enums;

namespace UOM.Web.Models;

public sealed class ContactFormViewModel
{
    public SiteSettingsDto Site { get; set; } = new(
        "Ülkü Ocakları Manisa", null, null, null, null, null, null, null, null, false, null);

    public IReadOnlyList<FacultyOptionDto> Faculties { get; set; } = [];

    public bool TurnstileEnabled { get; set; }

    public string? TurnstileSiteKey { get; set; }

    public bool Submitted { get; set; }

    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

  [Display(Name = "Telefon")]
    public string? Phone { get; set; }

    [Required]
    [Display(Name = "Konu")]
    public ContactSubject Subject { get; set; } = ContactSubject.General;

    [Required(ErrorMessage = "Mesaj zorunludur.")]
    public string Message { get; set; } = string.Empty;

    [Display(Name = "CBÜ öğrencisiyim")]
    public bool IsStudentAtCbu { get; set; }

    [Display(Name = "Fakülte")]
    public int? FacultyId { get; set; }

    [Display(Name = "KVKK aydınlatma metnini okudum ve onaylıyorum.")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "KVKK onayı zorunludur.")]
    public bool KvkkConsent { get; set; }

    public string? TurnstileToken { get; set; }

    public string? Website { get; set; }
}