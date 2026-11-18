using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Admin;

public sealed record DashboardViewModel(
    int PendingApprovalCount,
    int UnreadContactMessages,
    int UpcomingEvents,
    int ExpiringAnnouncements);

public sealed class DashboardAdminService(AppDbContext db, IClock clock)
{
    public async Task<DashboardViewModel> LoadAsync(CancellationToken cancellationToken = default)
    {
        var pendingNews = await db.NewsPosts.CountAsync(n => n.Status == PublishStatus.PendingApproval, cancellationToken);
        var pendingEvents = await db.Events.CountAsync(e => e.Status == PublishStatus.PendingApproval, cancellationToken);
        var unread = await db.ContactMessages.CountAsync(c => !c.IsRead, cancellationToken);
        var upcoming = await db.Events.CountAsync(
            e => e.Status == PublishStatus.Published && e.StartsAtUtc >= clock.UtcNow,
            cancellationToken);
        var week = clock.UtcNow.AddDays(7);
        var expiring = await db.Announcements.CountAsync(
            a => a.ExpiresAtUtc <= week && a.ExpiresAtUtc > clock.UtcNow,
            cancellationToken);

        return new DashboardViewModel(
            pendingNews + pendingEvents,
            unread,
            upcoming,
            expiring);
    }
}