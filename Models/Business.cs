namespace ConsumersVoiceSystemPrototype.Models;

public class Business
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? Phone { get; set; }

    /// <summary>Identity user that owns this business profile (one-to-one style).</summary>
    public string OwnerUserId { get; set; } = string.Empty;
    public ApplicationUser Owner { get; set; } = null!;

    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}
