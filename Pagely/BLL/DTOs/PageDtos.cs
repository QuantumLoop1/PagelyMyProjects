namespace NotionClone.BLL.DTOs;

public sealed class PageDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? CoverColor { get; set; }
    public string Status { get; set; } = "Todo";
    public DateTime? ScheduledFor { get; set; }
    public Guid? ParentId { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<PageDto> Children { get; set; } = new();
}

public sealed class CreatePageRequestDto
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Icon { get; set; }
    public string? CoverColor { get; set; }
    public string? Status { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public Guid? ParentId { get; set; }
    public int? Order { get; set; }
}

public sealed class UpdatePageRequestDto
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Icon { get; set; }
    public string? CoverColor { get; set; }
    public string? Status { get; set; }
    public DateTime? ScheduledFor { get; set; }
}

public sealed class MovePageRequestDto
{
    public Guid? ParentId { get; set; }
    public int? Order { get; set; }
}
