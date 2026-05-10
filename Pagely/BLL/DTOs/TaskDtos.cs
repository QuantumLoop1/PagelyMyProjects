namespace NotionClone.BLL.DTOs;

public sealed class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Todo";
    public int Order { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CreateTaskRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Status { get; set; }
    public DateTime? ScheduledFor { get; set; }
}

public sealed class UpdateTaskRequestDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public DateTime? ScheduledFor { get; set; }
}

public sealed class MoveTaskRequestDto
{
    public string Status { get; set; } = "Todo";
    public int? Order { get; set; }
}

public sealed class TaskSummaryDto
{
    public int Total { get; set; }
    public int Todo { get; set; }
    public int Doing { get; set; }
    public int Done { get; set; }
    public double CompletionPercent { get; set; }
}

public sealed class TaskCalendarDayDto
{
    public DateTime Date { get; set; }
    public List<TaskDto> Tasks { get; set; } = new();
}
