namespace ConsumersVoiceSystemPrototype.Models;

public class Complaint
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Submitted;

    public string ConsumerId { get; set; } = string.Empty;
    public ApplicationUser Consumer { get; set; } = null!;

    public int? BusinessId { get; set; }
    public Business? Business { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string? AssignedAdvocateId { get; set; }
    public ApplicationUser? AssignedAdvocate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ComplaintMessage> Messages { get; set; } = new List<ComplaintMessage>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<ComplaintAttachment> Attachments { get; set; } = new List<ComplaintAttachment>();
}
