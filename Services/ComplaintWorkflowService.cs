using System.Security.Claims;
using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Services;

public class ComplaintWorkflowService(
    ApplicationDbContext db,
    AppNotificationService notify,
    UserManager<ApplicationUser> users)
{
    public record AccessInfo(
        bool CanView,
        bool CanAddMessage,
        bool CanUploadAttachment,
        bool CanClaimAdvocate,
        bool CanAssignAdvocateAsAdmin,
        bool CanUnassignAdvocate,
        bool CanChangeStatus);

    public static bool IsStatusTransitionAllowed(ComplaintStatus from, ComplaintStatus to, bool isAdmin)
    {
        if (from == to) return false;
        if (isAdmin) return true;
        if (from == ComplaintStatus.Closed) return false;
        if (to == ComplaintStatus.Submitted) return false;
        return true;
    }

    public async Task<AccessInfo?> GetAccessAsync(Complaint c, string userId, ClaimsPrincipal principal)
    {
        var isAdmin = principal.IsInRole(RoleNames.Admin);
        var isAdvocate = principal.IsInRole(RoleNames.Advocate);
        var isConsumer = principal.IsInRole(RoleNames.Consumer);
        var isBusiness = principal.IsInRole(RoleNames.Business);

        var isOwner = c.ConsumerId == userId;
        var isAssignedAdvocate = c.AssignedAdvocateId == userId;

        var businessMatch = false;
        if (isBusiness && c.BusinessId != null)
        {
            businessMatch = await db.Businesses.AsNoTracking()
                .AnyAsync(b => b.Id == c.BusinessId && b.OwnerUserId == userId);
        }

        var canView = isAdmin || isAdvocate
            || (isConsumer && isOwner)
            || (isBusiness && businessMatch);

        if (!canView)
            return null;

        var canAddMessage = isAdmin || isAssignedAdvocate
            || (isConsumer && isOwner)
            || (isBusiness && businessMatch);

        var canUpload = canAddMessage;

        var canClaimAdvocate = isAdvocate && c.AssignedAdvocateId == null;

        var canAssignAdmin = isAdmin;
        var canUnassign = isAdmin && c.AssignedAdvocateId != null;
        var canChangeStatus = isAdmin || (isAdvocate && isAssignedAdvocate);

        return new AccessInfo(canView, canAddMessage, canUpload, canClaimAdvocate, canAssignAdmin, canUnassign, canChangeStatus);
    }

    public async Task<List<(string Id, string Display)>> GetAdvocatesForAdminAsync()
    {
        var advocateUsers = await users.GetUsersInRoleAsync(RoleNames.Advocate);
        return advocateUsers
            .OrderBy(u => u.FullName)
            .Select(u => (u.Id, $"{u.FullName} ({u.Email})"))
            .ToList();
    }

    public async Task<WorkflowResult> TryClaimAdvocateAsync(int complaintId, string userId, ClaimsPrincipal principal)
    {
        if (!principal.IsInRole(RoleNames.Advocate))
            return new WorkflowResult(false, "Only advocates can claim a case.");

        var c = await db.Complaints.FirstOrDefaultAsync(x => x.Id == complaintId);
        if (c == null) return new WorkflowResult(false, "Complaint not found.");

        if (c.AssignedAdvocateId != null)
            return new WorkflowResult(false, "This case already has an assigned advocate.");

        c.AssignedAdvocateId = userId;
        c.Status = ComplaintStatus.UnderReview;
        c.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await notify.NotifyAsync(c.ConsumerId, $"An advocate was assigned to: {c.Title}", complaintId);
        return new WorkflowResult(true);
    }

    public async Task<WorkflowResult> TryAssignAdvocateByAdminAsync(int complaintId, string? advocateUserId, ClaimsPrincipal principal)
    {
        if (!principal.IsInRole(RoleNames.Admin))
            return new WorkflowResult(false, "Not authorized.");

        if (string.IsNullOrWhiteSpace(advocateUserId))
            return new WorkflowResult(false, "Select an advocate.");

        var target = await users.FindByIdAsync(advocateUserId);
        if (target == null || !await users.IsInRoleAsync(target, RoleNames.Advocate))
            return new WorkflowResult(false, "Invalid advocate.");

        var c = await db.Complaints.FirstOrDefaultAsync(x => x.Id == complaintId);
        if (c == null) return new WorkflowResult(false, "Complaint not found.");

        var previousId = c.AssignedAdvocateId;
        c.AssignedAdvocateId = advocateUserId;
        if (c.Status == ComplaintStatus.Submitted)
            c.Status = ComplaintStatus.UnderReview;
        c.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await notify.NotifyAsync(c.ConsumerId, $"An advocate was assigned to: {c.Title}", complaintId);
        if (previousId != null && previousId != advocateUserId)
            await notify.NotifyAsync(previousId, $"You were unassigned from: {c.Title}", complaintId);
        await notify.NotifyAsync(advocateUserId, $"You were assigned to: {c.Title}", complaintId);

        return new WorkflowResult(true);
    }

    public async Task<WorkflowResult> TryUnassignAdvocateAsync(int complaintId, ClaimsPrincipal principal)
    {
        if (!principal.IsInRole(RoleNames.Admin))
            return new WorkflowResult(false, "Not authorized.");

        var c = await db.Complaints.FirstOrDefaultAsync(x => x.Id == complaintId);
        if (c == null) return new WorkflowResult(false, "Complaint not found.");

        var previousId = c.AssignedAdvocateId;
        if (previousId == null)
            return new WorkflowResult(false, "No advocate assigned.");

        c.AssignedAdvocateId = null;
        c.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await notify.NotifyAsync(c.ConsumerId, $"The advocate was unassigned from: {c.Title}", complaintId);
        await notify.NotifyAsync(previousId, $"You were unassigned from: {c.Title}", complaintId);

        return new WorkflowResult(true);
    }

    public async Task<WorkflowResult> TryUpdateStatusAsync(
        int complaintId,
        ComplaintStatus newStatus,
        string userId,
        ClaimsPrincipal principal)
    {
        var isAdmin = principal.IsInRole(RoleNames.Admin);
        var isAdvocate = principal.IsInRole(RoleNames.Advocate);

        var c = await db.Complaints.FirstOrDefaultAsync(x => x.Id == complaintId);
        if (c == null) return new WorkflowResult(false, "Complaint not found.");

        if (!isAdmin && !(isAdvocate && c.AssignedAdvocateId == userId))
            return new WorkflowResult(false, "Only the assigned advocate or an administrator can change status.");

        if (!IsStatusTransitionAllowed(c.Status, newStatus, isAdmin))
            return new WorkflowResult(false, "That status change is not allowed.");

        c.Status = newStatus;
        c.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await notify.NotifyAsync(c.ConsumerId, $"Status updated to {newStatus} for: {c.Title}", complaintId);
        return new WorkflowResult(true);
    }
}

public record WorkflowResult(bool Ok, string? Error = null);
