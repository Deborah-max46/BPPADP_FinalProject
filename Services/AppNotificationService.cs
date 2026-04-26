using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Services;

public class AppNotificationService(ApplicationDbContext db)
{
    public async Task NotifyAsync(string userId, string message, int? complaintId = null, CancellationToken ct = default)
    {
        db.Notifications.Add(new Notification
        {
            UserId = userId,
            Message = message,
            CreatedAt = DateTime.UtcNow,
            ComplaintId = complaintId
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task NotifyManyAsync(IEnumerable<string> userIds, string message, int? complaintId = null, CancellationToken ct = default)
    {
        foreach (var uid in userIds.Distinct())
        {
            db.Notifications.Add(new Notification
            {
                UserId = uid,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                ComplaintId = complaintId
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
