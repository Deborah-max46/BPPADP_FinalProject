using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ConsumersVoiceSystemPrototype.Services;

public class ComplaintAttachmentStorage(IWebHostEnvironment env)
{
    private const long MaxBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".webp", ".doc", ".docx", ".xlsx", ".txt"
    };

    public long MaxFileSizeBytes => MaxBytes;

    public bool IsAllowedExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
    }

    /// <summary>
    /// Saves under wwwroot/uploads/complaints/{complaintId}/ and returns metadata (caller saves entity).
    /// </summary>
    public async Task<(string StoredFileName, string RelativeWebPath, string ContentType, long Size)?> SaveAsync(
        int complaintId,
        IFormFile file,
        CancellationToken ct = default)
    {
        if (file.Length <= 0 || file.Length > MaxBytes)
            return null;

        if (!IsAllowedExtension(file.FileName))
            return null;

        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var dir = Path.Combine(webRoot, "uploads", "complaints", complaintId.ToString());
        Directory.CreateDirectory(dir);

        var ext = Path.GetExtension(file.FileName);
        var stored = $"{Guid.NewGuid():N}{ext}";
        var physical = Path.Combine(dir, stored);

        await using (var fs = File.Create(physical))
        {
            await file.CopyToAsync(fs, ct);
        }

        var relative = $"/uploads/complaints/{complaintId}/{stored}";
        return (stored, relative, string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType, file.Length);
    }

    public string GetPhysicalPath(int complaintId, string storedFileName)
    {
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        return Path.Combine(webRoot, "uploads", "complaints", complaintId.ToString(), storedFileName);
    }

    public bool FileExists(int complaintId, string storedFileName)
    {
        var path = GetPhysicalPath(complaintId, storedFileName);
        return File.Exists(path);
    }
}
