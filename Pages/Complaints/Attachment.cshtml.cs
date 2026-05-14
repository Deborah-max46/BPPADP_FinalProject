using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using ConsumersVoiceSystemPrototype.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Complaints;

[Authorize]
public class AttachmentModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ComplaintWorkflowService _workflow;
    private readonly ComplaintAttachmentStorage _storage;

    public AttachmentModel(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        ComplaintWorkflowService workflow,
        ComplaintAttachmentStorage storage)
    {
        _db = db;
        _users = users;
        _workflow = workflow;
        _storage = storage;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var att = await _db.ComplaintAttachments
            .AsNoTracking()
            .Include(a => a.Complaint)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (att == null)
            return NotFound();

        var uid = _users.GetUserId(User);
        if (uid == null)
            return Forbid();

        var access = await _workflow.GetAccessAsync(att.Complaint, uid, User);
        if (access == null || !access.CanView)
            return Forbid();

        // If stored in R2 / cloud, redirect to the public URL
        if (_storage.IsR2Url(att.StoredFileName))
            return Redirect(att.StoredFileName);

        var path = _storage.GetPhysicalPath(att.ComplaintId, att.StoredFileName);
        if (!System.IO.File.Exists(path))
            return NotFound();

        return PhysicalFile(path, att.ContentType, att.FileName);
    }
}
