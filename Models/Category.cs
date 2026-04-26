namespace ConsumersVoiceSystemPrototype.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}
