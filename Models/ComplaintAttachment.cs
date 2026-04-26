namespace ConsumersVoiceSystemPrototype.Models;

public class ComplaintAttachment
{
    public int Id { get; set; }
    public int ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;

    public string UploadedByUserId { get; set; } = string.Empty;
    public ApplicationUser UploadedBy { get; set; } = null!;

    /// <summary>Original file name from the client.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Safe unique name under wwwroot/uploads.</summary>
    public string StoredFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/octet-stream";
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
