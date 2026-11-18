using System.ComponentModel.DataAnnotations;

namespace UOM.Web.Areas.Admin.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Beni hatırla")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class TwoFactorLoginViewModel
{
    [Required(ErrorMessage = "Doğrulama kodu zorunludur.")]
    [Display(Name = "Doğrulama kodu")]
    public string Code { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class ChangePasswordViewModel
{
    public bool MustChange { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Mevcut şifre")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni şifre")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni şifre (tekrar)")]
    [Compare(nameof(NewPassword), ErrorMessage = "Şifreler eşleşmiyor.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class TwoFactorSetupViewModel
{
    public string SharedKey { get; set; } = string.Empty;

    public string AuthenticatorUri { get; set; } = string.Empty;

    [Required(ErrorMessage = "Doğrulama kodu zorunludur.")]
    [Display(Name = "Uygulama kodu")]
    public string Code { get; set; } = string.Empty;
}