using System.ComponentModel.DataAnnotations;
using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using ConsumersVoiceSystemPrototype.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Complaints;

[Authorize(Roles = RoleNames.Consumer)]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly AppNotificationService _notify;
    private readonly ComplaintAttachmentStorage _storage;
    private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _users;

    public CreateModel(
        ApplicationDbContext db,
        AppNotificationService notify,
        ComplaintAttachmentStorage storage,
        Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> users)
    {
        _db = db;
        _notify = notify;
        _storage = storage;
        _users = users;
    }

    [BindProperty]
    public Input InputModel { get; set; } = new();

    [BindProperty]
    public List<IFormFile>? UploadFiles { get; set; }

    public SelectList? CategoryOptions { get; set; }
    public SelectList? BusinessOptions { get; set; }

    public class Input
    {
        [Required, StringLength(200)] public string Title { get; set; } = string.Empty;

        [Required, StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Category")] public int CategoryId { get; set; }

        [Display(Name = "Business involved (optional)")]
        public int? BusinessId { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadLookupsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLookupsAsync();
        if (!ModelState.IsValid)
            return Page();

        var uid = _users.GetUserId(User);
        if (uid == null)
            return Forbid();

        var complaint = new Complaint
        {
            Title = InputModel.Title.Trim(),
            Description = InputModel.Description.Trim(),
            CategoryId = InputModel.CategoryId,
            BusinessId = InputModel.BusinessId,
            ConsumerId = uid,
            Status = ComplaintStatus.Submitted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Complaints.Add(complaint);
        await _db.SaveChangesAsync();

        if (UploadFiles != null)
        {
            foreach (var file in UploadFiles.Where(f => f.Length > 0))
            {
                var meta = await _storage.SaveAsync(complaint.Id, file);
                if (meta == null) continue;

                _db.ComplaintAttachments.Add(new ComplaintAttachment
                {
                    ComplaintId = complaint.Id,
                    UploadedByUserId = uid,
                    FileName = Path.GetFileName(file.FileName),
                    StoredFileName = meta.Value.StoredFileName,
                    ContentType = meta.Value.ContentType,
                    FileSizeBytes = meta.Value.Size,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
        }

        var advocateList = await _users.GetUsersInRoleAsync(RoleNames.Advocate);
        foreach (var a in advocateList)
            await _notify.NotifyAsync(a.Id, $"New complaint submitted: {complaint.Title}", complaint.Id);

        if (complaint.BusinessId is { } bid)
        {
            var biz = await _db.Businesses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bid);
            if (biz != null)
                await _notify.NotifyAsync(biz.OwnerUserId, $"A complaint involves your business: {complaint.Title}", complaint.Id);
        }

        TempData["StatusMessage"] = "Complaint submitted successfully.";
        return RedirectToPage("/Complaints/Details", new { id = complaint.Id });
    }

    private async Task LoadLookupsAsync()
    {
        var cats = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        CategoryOptions = new SelectList(cats, "Id", "Name");

        var biz = await _db.Businesses.AsNoTracking().OrderBy(b => b.Name).ToListAsync();
        BusinessOptions = new SelectList(biz, "Id", "Name");
    }
}
