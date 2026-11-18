using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UOM.Application.Abstractions;
using UOM.Domain.Constants;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
using UOM.Infrastructure.Identity;

namespace UOM.Infrastructure.Persistence.Seed;

public sealed class DatabaseSeeder
{
    private const string PlaceholderBody = "TODO: İl başkanlığından alınacak";

    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IClock _clock;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IClock clock,
        ILogger<DatabaseSeeder> logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _clock = clock;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(cancellationToken);
        await SeedSuperAdminAsync(cancellationToken);
        await SeedOptionalUserAsync(
            "SEED_EDITOR_EMAIL",
            "SEED_EDITOR_PASSWORD",
            RoleNames.Editor,
            mustChangePassword: true,
            cancellationToken);
        await SeedOptionalUserAsync(
            "SEED_PROVINCE_ADMIN_EMAIL",
            "SEED_PROVINCE_ADMIN_PASSWORD",
            RoleNames.ProvinceAdmin,
            mustChangePassword: true,
            cancellationToken);
        await SeedDistrictsAsync(cancellationToken);
        await SeedStaticPagesAsync(cancellationToken);
        await SeedGuideArticleDraftsAsync(cancellationToken);
        await SeedSiteSettingsAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        foreach (var role in RoleNames.All)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Role {Role} could not be created: {Errors}", role, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private async Task SeedSuperAdminAsync(CancellationToken cancellationToken)
    {
        var email = Environment.GetEnvironmentVariable("SEED_ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("SEED_ADMIN_EMAIL / SEED_ADMIN_PASSWORD not set; skipping SuperAdmin user seed.");
            return;
        }

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            MustChangePassword = true,
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            _logger.LogError("SuperAdmin seed failed: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await _userManager.AddToRoleAsync(user, RoleNames.SuperAdmin);
        _logger.LogInformation("Seeded SuperAdmin user {Email} (password change required on first login).", email);
    }

    private async Task SeedOptionalUserAsync(
        string emailVar,
        string passwordVar,
        string role,
        bool mustChangePassword,
        CancellationToken cancellationToken)
    {
        var email = Environment.GetEnvironmentVariable(emailVar);
        var password = Environment.GetEnvironmentVariable(passwordVar);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            MustChangePassword = mustChangePassword,
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            _logger.LogWarning("Seed user {EmailVar} failed: {Errors}", emailVar, string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await _userManager.AddToRoleAsync(user, role);
        _logger.LogInformation("Seeded {Role} user {Email}.", role, email);
    }

    private async Task SeedDistrictsAsync(CancellationToken cancellationToken)
    {
        if (await _db.Districts.AnyAsync(cancellationToken))
        {
            return;
        }

        var districts = new (string Name, string Slug)[]
        {
            ("Ahmetli", "ahmetli"),
            ("Akhisar", "akhisar"),
            ("Alaşehir", "alasehir"),
            ("Demirci", "demirci"),
            ("Gölmarmara", "golmarmara"),
            ("Gördes", "gordes"),
            ("Kırkağaç", "kirkagac"),
            ("Köprübaşı", "koprubasi"),
            ("Kula", "kula"),
            ("Salihli", "salihli"),
            ("Sarıgöl", "sarigol"),
            ("Saruhanlı", "saruhanli"),
            ("Selendi", "selendi"),
            ("Soma", "soma"),
            ("Şehzadeler", "sehzadeler"),
            ("Turgutlu", "turgutlu"),
            ("Yunusemre", "yunusemre"),
        };

        var order = 0;
        foreach (var (name, slug) in districts)
        {
            order++;
            _db.Districts.Add(new District
            {
                Name = name,
                Slug = slug,
                IsActive = false,
                Order = order,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedStaticPagesAsync(CancellationToken cancellationToken)
    {
        if (await _db.StaticPages.AnyAsync(cancellationToken))
        {
            return;
        }

        var pages = new (string Title, string Slug, int Order, bool Footer)[]
        {
            ("Hakkımızda", "hakkimizda", 1, true),
            ("KVKK Aydınlatma Metni", "kvkk-aydinlatma-metni", 2, true),
            ("Çerez Politikası", "cerez-politikasi", 3, true),
            ("Kullanım Koşulları", "kullanim-kosullari", 4, true),
        };

        foreach (var (title, slug, order, footer) in pages)
        {
            _db.StaticPages.Add(new StaticPage
            {
                Title = title,
                Slug = slug,
                BodyHtml = PlaceholderBody,
                ShowInFooter = footer,
                Order = order,
                Status = PublishStatus.Draft,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedGuideArticleDraftsAsync(CancellationToken cancellationToken)
    {
        if (await _db.GuideArticles.AnyAsync(cancellationToken))
        {
            return;
        }

        var categories = Enum.GetValues<GuideCategory>();
        var order = 0;
        foreach (var category in categories)
        {
            order++;
            var slug = category.ToString().ToLowerInvariant();
            _db.GuideArticles.Add(new GuideArticle
            {
                Title = $"{category} (taslak)",
                Slug = slug,
                BodyHtml = string.Empty,
                Summary = string.Empty,
                Category = category,
                Order = order,
                Status = PublishStatus.Draft,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSiteSettingsAsync(CancellationToken cancellationToken)
    {
        if (await _db.SiteSettings.AnyAsync(cancellationToken))
        {
            return;
        }

        _db.SiteSettings.Add(new SiteSettings());
        await _db.SaveChangesAsync(cancellationToken);
    }
}