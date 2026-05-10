namespace NotionClone.DAL.Entities;

public class Page
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "Untitled";
    public string Content { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? CoverColor { get; set; }
    public string Status { get; set; } = "Todo";
    public DateTime? ScheduledFor { get; set; }
    public Guid? ParentId { get; set; }
    public Page? Parent { get; set; }
    public ICollection<Page> Children { get; set; } = new List<Page>();
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
}
