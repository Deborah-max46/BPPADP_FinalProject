using System.ComponentModel.DataAnnotations;
using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using ConsumersVoiceSystemPrototype.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Complaints;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly AppNotificationService _notify;
    private readonly ComplaintWorkflowService _workflow;
    private readonly ComplaintAttachmentStorage _attachmentStorage;

    public DetailsModel(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        AppNotificationService notify,
        ComplaintWorkflowService workflow,
        ComplaintAttachmentStorage attachmentStorage)
    {
        _db = db;
        _users = users;
        _notify = notify;
        _workflow = workflow;
        _attachmentStorage = attachmentStorage;
    }

    public DetailVm? Detail { get; set; }

    public ComplaintWorkflowService.AccessInfo? Access { get; set; }

    [BindProperty]
    public ComplaintMessageForm MessageInput { get; set; } = new();

    [BindProperty]
    public ComplaintStatusForm StatusInput { get; set; } = new();

    [BindProperty]
    public string? AssignAdvocateUserId { get; set; }

    [BindProperty]
    public List<IFormFile>? UploadFiles { get; set; }

    public SelectList? StatusOptions { get; set; }
    public SelectList? AdvocateOptions { get; set; }

    public class ComplaintMessageForm
    {
        [Required, StringLength(4000)] public string Body { get; set; } = string.Empty;
    }

    public class ComplaintStatusForm
    {
        public ComplaintStatus Status { get; set; }
    }

    public record DetailVm(
        int CaseNumber,
        string Title,
        string Description,
        string CategoryName,
        string? BusinessName,
        string ConsumerName,
        ComplaintStatus Status,
        string? AdvocateName,
        IReadOnlyList<MessageVm> Messages,
        IReadOnlyList<AttachmentVm> Attachments);

    public record MessageVm(string AuthorName, string Body, DateTime CreatedAt);

    public record AttachmentVm(int Id, string FileName, string UploadedByName, long SizeBytes, DateTime CreatedAt);

    private async Task<bool> LoadAsync(int id)
    {
        var uid = _users.GetUserId(User);
        if (uid == null)
            return false;

        var c = await _db.Complaints
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Business)
            .Include(x => x.Consumer)
            .Include(x => x.AssignedAdvocate)
            .Include(x => x.Messages).ThenInclude(m => m.Author)
            .Include(x => x.Attachments).ThenInclude(a => a.UploadedBy)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (c == null)
            return false;

        var access = await _workflow.GetAccessAsync(c, uid, User);
        if (access == null || !access.CanView)
            return false;

        Access = access;

        var msgs = c.Messages.OrderBy(m => m.CreatedAt).Select(m => new MessageVm(m.Author.FullName, m.Body, m.CreatedAt)).ToList();

        var atts = c.Attachments
            .OrderBy(a => a.CreatedAt)
            .Select(a => new AttachmentVm(a.Id, a.FileName, a.UploadedBy.FullName, a.FileSizeBytes, a.CreatedAt))
            .ToList();

        Detail = new DetailVm(
            c.Id,
            c.Title,
            c.Description,
            c.Category.Name,
            c.Business?.Name,
            c.Consumer.FullName,
            c.Status,
            c.AssignedAdvocate?.FullName,
            msgs,
            atts);

        var statusItems = Enum.GetValues<ComplaintStatus>()
            .Cast<ComplaintStatus>()
            .Select(s => new SelectListItem { Text = s.ToString(), Value = ((int)s).ToString(), Selected = s == c.Status })
            .ToList();
        StatusOptions = new SelectList(statusItems, "Value", "Text", ((int)c.Status).ToString());
        StatusInput.Status = c.Status;

        if (access.CanAssignAdvocateAsAdmin)
        {
            var advocates = await _workflow.GetAdvocatesForAdminAsync();
            var items = advocates.Select(a => new SelectListItem(a.Display, a.Id)).ToList();
            items.Insert(0, new SelectListItem("— Select advocate —", ""));
            AdvocateOptions = new SelectList(items, "Value", "Text", c.AssignedAdvocateId);
        }

        return true;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadAsync(id))
            return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAddMessageAsync(int id)
    {
        ModelState.Clear();
        if (!TryValidateModel(MessageInput, nameof(MessageInput)))
        {
            if (!await LoadAsync(id))
                return NotFound();
            return Page();
        }

        var uid = _users.GetUserId(User)!;
        var complaint = await _db.Complaints.FirstOrDefaultAsync(c => c.Id == id);
        if (complaint == null)
            return NotFound();

        var access = await _workflow.GetAccessAsync(complaint, uid, User);
        if (access == null || !access.CanAddMessage)
            return Forbid();

        _db.ComplaintMessages.Add(new ComplaintMessage
        {
            ComplaintId = id,
            AuthorUserId = uid,
            Body = MessageInput.Body.Trim(),
            CreatedAt = DateTime.UtcNow
        });
        complaint.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var others = new List<string>();
        if (complaint.ConsumerId != uid) others.Add(complaint.ConsumerId);
        if (complaint.AssignedAdvocateId != null && complaint.AssignedAdvocateId != uid) others.Add(complaint.AssignedAdvocateId);
        if (complaint.BusinessId != null)
        {
            var owner = await _db.Businesses.AsNoTracking().Where(b => b.Id == complaint.BusinessId).Select(b => b.OwnerUserId).FirstOrDefaultAsync();
            if (!string.IsNullOrEmpty(owner) && owner != uid) others.Add(owner);
        }

        foreach (var o in others.Distinct())
            await _notify.NotifyAsync(o, $"New message on complaint: {complaint.Title}", id);

        TempData["StatusMessage"] = "Message posted.";
        MessageInput = new ComplaintMessageForm();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostClaimAdvocateAsync(int id)
    {
        var uid = _users.GetUserId(User)!;
        var result = await _workflow.TryClaimAdvocateAsync(id, uid, User);
        if (!result.Ok)
        {
            TempData["ErrorMessage"] = result.Error;
            return RedirectToPage(new { id });
        }

        TempData["StatusMessage"] = "You are now assigned to this case.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAssignAdvocateAsync(int id)
    {
        var result = await _workflow.TryAssignAdvocateByAdminAsync(id, AssignAdvocateUserId, User);
        if (!result.Ok)
        {
            TempData["ErrorMessage"] = result.Error;
            return RedirectToPage(new { id });
        }

        TempData["StatusMessage"] = "Advocate assignment updated.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUnassignAdvocateAsync(int id)
    {
        var result = await _workflow.TryUnassignAdvocateAsync(id, User);
        if (!result.Ok)
        {
            TempData["ErrorMessage"] = result.Error;
            return RedirectToPage(new { id });
        }

        TempData["StatusMessage"] = "Advocate unassigned.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostSetStatusAsync(int id)
    {
        var uid = _users.GetUserId(User)!;
        var result = await _workflow.TryUpdateStatusAsync(id, StatusInput.Status, uid, User);
        if (!result.Ok)
        {
            TempData["ErrorMessage"] = result.Error;
            return RedirectToPage(new { id });
        }

        TempData["StatusMessage"] = "Status updated.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUploadAttachmentsAsync(int id)
    {
        var uid = _users.GetUserId(User)!;
        var complaint = await _db.Complaints.FirstOrDefaultAsync(c => c.Id == id);
        if (complaint == null)
            return NotFound();

        var access = await _workflow.GetAccessAsync(complaint, uid, User);
        if (access == null || !access.CanUploadAttachment)
            return Forbid();

        if (UploadFiles == null || UploadFiles.Count == 0)
        {
            TempData["ErrorMessage"] = "Choose at least one file.";
            return RedirectToPage(new { id });
        }

        var saved = 0;
        foreach (var file in UploadFiles)
        {
            if (file.Length == 0) continue;
            var meta = await _attachmentStorage.SaveAsync(id, file);
            if (meta == null) continue;

            _db.ComplaintAttachments.Add(new ComplaintAttachment
            {
                ComplaintId = id,
                UploadedByUserId = uid,
                FileName = Path.GetFileName(file.FileName),
                StoredFileName = meta.Value.StoredFileName,
                ContentType = meta.Value.ContentType,
                FileSizeBytes = meta.Value.Size,
                CreatedAt = DateTime.UtcNow
            });
            saved++;
        }

        if (saved == 0)
        {
            TempData["ErrorMessage"] = "No valid files uploaded (check type and size, max 10 MB each).";
            return RedirectToPage(new { id });
        }

        complaint.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = $"Uploaded {saved} file(s).";
        return RedirectToPage(new { id });
    }
}
