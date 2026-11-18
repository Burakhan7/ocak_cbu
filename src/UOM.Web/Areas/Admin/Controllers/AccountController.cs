using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UOM.Infrastructure.Identity;
using UOM.Web.Areas.Admin.Models;

namespace UOM.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Route("yonetim/hesap")]
public sealed class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ILogger<AccountController> logger) : Controller
{
    [AllowAnonymous]
    [HttpGet("giris")]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl ?? "/yonetim" });

    [AllowAnonymous]
    [HttpPost("giris")]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.RequiresTwoFactor)
        {
            return RedirectToAction(nameof(TwoFactorLogin), new { returnUrl = model.ReturnUrl });
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi.");
            return View(model);
        }

        logger.LogInformation("Admin kullanıcı girişi: {UserId}", user.Id);
        return LocalRedirect(model.ReturnUrl ?? "/yonetim");
    }

    [AllowAnonymous]
    [HttpGet("iki-faktor")]
    public IActionResult TwoFactorLogin(string? returnUrl = null) =>
        View(new TwoFactorLoginViewModel { ReturnUrl = returnUrl ?? "/yonetim" });

    [AllowAnonymous]
    [HttpPost("iki-faktor")]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TwoFactorLogin(TwoFactorLoginViewModel model)
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var code = model.Code.Replace(" ", string.Empty, StringComparison.Ordinal);
        var result = await signInManager.TwoFactorAuthenticatorSignInAsync(code, model.RememberMe, rememberClient: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Geçersiz doğrulama kodu.");
            return View(model);
        }

        return LocalRedirect(model.ReturnUrl ?? "/yonetim");
    }

    [Authorize]
    [HttpGet("sifre-degistir")]
  public async Task<IActionResult> ChangePassword()
    {
        var user = await userManager.GetUserAsync(User);
        return View(new ChangePasswordViewModel { MustChange = user?.MustChangePassword ?? false });
    }

    [Authorize]
    [HttpPost("sifre-degistir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        user.MustChangePassword = false;
        await userManager.UpdateAsync(user);
        await signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "Şifreniz güncellendi.";
        return RedirectToAction("TwoFactorSetup");
    }

    [Authorize]
    [HttpGet("iki-faktor-kur")]
    public async Task<IActionResult> TwoFactorSetup()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user);
        }

        var email = await userManager.GetEmailAsync(user) ?? user.Email ?? "admin";
        var uri = GenerateQrUri(email, key!);
        return View(new TwoFactorSetupViewModel { SharedKey = FormatKey(key!), AuthenticatorUri = uri });
    }

    [Authorize]
    [HttpPost("iki-faktor-kur")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TwoFactorSetup(TwoFactorSetupViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var code = model.Code.Replace(" ", string.Empty, StringComparison.Ordinal);
        var valid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            code);

        if (!valid)
        {
            ModelState.AddModelError(string.Empty, "Doğrulama kodu geçersiz.");
            return await TwoFactorSetup();
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);
        TempData["Success"] = "İki faktörlü doğrulama etkinleştirildi.";
        return RedirectToAction("Index", "Dashboard", new { area = AdminArea.Name });
    }

    [Authorize]
    [HttpPost("cikis")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet("erisim-reddedildi")]
    public IActionResult AccessDenied() => View();

    private static string FormatKey(string key) =>
        string.Join(" ", Enumerable.Range(0, key.Length / 4 + 1)
            .Select(i => key.Substring(i * 4, Math.Min(4, key.Length - i * 4))));

    private static string GenerateQrUri(string email, string key)
    {
        const string issuer = "UlkuOcaklariManisa";
        return $"otpauth://totp/{UrlEncoder.Default.Encode(issuer)}:{UrlEncoder.Default.Encode(email)}?secret={key}&issuer={UrlEncoder.Default.Encode(issuer)}&digits=6";
    }
}